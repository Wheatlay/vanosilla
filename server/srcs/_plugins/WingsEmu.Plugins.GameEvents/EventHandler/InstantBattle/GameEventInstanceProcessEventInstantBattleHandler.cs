using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.GameEvent;
using WingsEmu.Game.GameEvent.Event;
using WingsEmu.Packets.Enums.Chat;
using WingsEmu.Plugins.GameEvents.Configuration.InstantBattle;
using WingsEmu.Plugins.GameEvents.DataHolder;
using WingsEmu.Plugins.GameEvents.Event.InstantBattle;

namespace WingsEmu.Plugins.GameEvents.EventHandler.InstantBattle
{
    public class GameEventInstanceProcessEventInstantBattleHandler : IAsyncEventProcessor<GameEventInstanceProcessEvent>
    {
        private readonly IAsyncEventPipeline _asyncEventPipeline;
        private readonly IGameEventInstanceManager _gameEventInstanceManager;
        private readonly IGameLanguageService _languageService;

        public GameEventInstanceProcessEventInstantBattleHandler(IGameEventInstanceManager gameEventInstanceManager, IAsyncEventPipeline asyncEventPipeline, IGameLanguageService languageService)
        {
            _gameEventInstanceManager = gameEventInstanceManager;
            _asyncEventPipeline = asyncEventPipeline;
            _languageService = languageService;
        }

        public async Task HandleAsync(GameEventInstanceProcessEvent e, CancellationToken cancellation)
        {
            IReadOnlyCollection<IGameEventInstance> gameEventInstances = _gameEventInstanceManager.GetGameEventsByType(GameEventType.InstantBattle);
            if (gameEventInstances == null || gameEventInstances.Count < 1)
            {
                return;
            }

            DateTime currentTime = e.CurrentTime;

            foreach (IGameEventInstance gameEventInstance in gameEventInstances.ToArray())
            {
                var instantBattleInstance = (InstantBattleInstance)gameEventInstance;
                await ProcessGameEventInstance(instantBattleInstance, currentTime);
            }
        }

        private async Task ProcessGameEventInstance(InstantBattleInstance gameEventInstance, DateTime currentTime)
        {
            if (gameEventInstance.DestroyDate < currentTime || gameEventInstance.MapInstance.Sessions.Count < 1)
            {
                await _asyncEventPipeline.ProcessEventAsync(new InstantBattleDestroyEvent(gameEventInstance));
                return;
            }

            await ProcessWarnings(gameEventInstance, currentTime);
            await ProcessWaves(gameEventInstance, currentTime);
        }

        private async Task ProcessWarnings(InstantBattleInstance gameEventInstance, DateTime currentTime)
        {
            if (gameEventInstance.ClosingTimeWarnings.Count < 1)
            {
                return;
            }

            TimeSpan currentWarning = gameEventInstance.ClosingTimeWarnings.First();

            if (currentTime < gameEventInstance.StartDate + currentWarning)
            {
                return;
            }

            TimeSpan timeLeft = gameEventInstance.DestroyDate - currentTime;
            gameEventInstance.ClosingTimeWarnings.Remove(currentWarning);
            bool isSeconds = timeLeft.TotalMinutes < 1;
            GameDialogKey key = isSeconds ? GameDialogKey.INSTANT_COMBAT_SHOUTMESSAGE_SECONDS_REMAINING : GameDialogKey.INSTANT_COMBAT_SHOUTMESSAGE_MINUTES_REMAINING;

            gameEventInstance.MapInstance.Broadcast(x =>
                x.GenerateMsgPacket(_languageService.GetLanguageFormat(key, x.UserLanguage, (isSeconds ? timeLeft.Seconds : timeLeft.Minutes).ToString()), MsgMessageType.Middle));
        }

        private async Task ProcessWaves(InstantBattleInstance gameEventInstance, DateTime currentTime)
        {
            InstantBattleConfiguration configuration = gameEventInstance.InternalConfiguration;
            if (gameEventInstance.AvailableWaves.Count < 1)
            {
                return;
            }

            InstantBattleInstanceWave prioritizedWave = gameEventInstance.AvailableWaves.First();
            DateTime waveStartDate = gameEventInstance.StartDate + prioritizedWave.Configuration.TimeStart;
            DateTime waveEndDate = gameEventInstance.StartDate + prioritizedWave.Configuration.TimeEnd;

            if (!prioritizedWave.PreWaveLongWarningDone && waveStartDate - configuration.PreWaveLongWarningTime < currentTime)
            {
                prioritizedWave.PreWaveLongWarningDone = true;

                gameEventInstance.MapInstance.Broadcast(x =>
                    x.GenerateMsgPacket(
                        _languageService.GetLanguageFormat(GameDialogKey.INSTANT_COMBAT_SHOUTMESSAGE_WAVE_SECONDS_REMAINING, x.UserLanguage, configuration.PreWaveLongWarningTime.TotalSeconds),
                        MsgMessageType.Middle));
            }

            if (!prioritizedWave.PreWaveSoonWarningDone && waveStartDate - configuration.PreWaveSoonWarningTime < currentTime)
            {
                prioritizedWave.PreWaveSoonWarningDone = true;

                gameEventInstance.MapInstance.Broadcast(x =>
                    x.GenerateMsgPacket(_languageService.GetLanguage(GameDialogKey.INSTANT_COMBAT_SHOUTMESSAGE_MONSTERS_INCOMING, x.UserLanguage), MsgMessageType.Middle));
            }

            if (!prioritizedWave.StartedWave && waveStartDate < currentTime)
            {
                prioritizedWave.StartedWave = true;

                await _asyncEventPipeline.ProcessEventAsync(new InstantBattleStartWaveEvent(gameEventInstance, prioritizedWave));
            }

            int currentInstantMonsters = gameEventInstance.MapInstance.GetAliveMonsters(x => x.IsInstantBattle).Count;

            if (prioritizedWave.StartedWave && waveEndDate < currentTime && currentInstantMonsters > 0)
            {
                gameEventInstance.MapInstance.Broadcast(x =>
                    x.GenerateMsgPacket(_languageService.GetLanguage(GameDialogKey.INSTANT_COMBAT_SHOUTMESSAGE_WAVE_FAILED, x.UserLanguage), MsgMessageType.Middle));
            }

            if (prioritizedWave.StartedWave && gameEventInstance.AvailableWaves.Count > 0 && waveEndDate < currentTime)
            {
                gameEventInstance.AvailableWaves.Remove(prioritizedWave);
                await _asyncEventPipeline.ProcessEventAsync(new InstantBattleDropEvent(gameEventInstance, prioritizedWave.Configuration));
                return;
            }

            // Add some delay, because server needs to process monster spawn
            if (prioritizedWave.StartedWave && prioritizedWave.MonsterSpawn.AddSeconds(5) < currentTime && gameEventInstance.AvailableWaves.Count == 1
                && !gameEventInstance.Finished && currentInstantMonsters < 1)
            {
                gameEventInstance.AvailableWaves.Remove(prioritizedWave);
                await _asyncEventPipeline.ProcessEventAsync(new InstantBattleDropEvent(gameEventInstance, prioritizedWave.Configuration));
                await _asyncEventPipeline.ProcessEventAsync(new InstantBattleCompleteEvent(gameEventInstance));
            }
        }
    }
}