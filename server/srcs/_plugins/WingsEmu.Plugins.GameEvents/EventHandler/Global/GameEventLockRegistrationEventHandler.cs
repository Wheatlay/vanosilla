using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using PhoenixLib.Logging;
using WingsEmu.Core.Generics;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.GameEvent;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums.Chat;
using WingsEmu.Plugins.GameEvents.Event.Global;

namespace WingsEmu.Plugins.GameEvents.EventHandler.Global
{
    public class GameEventLockRegistrationEventHandler : IAsyncEventProcessor<GameEventLockRegistrationEvent>
    {
        private readonly IAsyncEventPipeline _eventPipeline;
        private readonly IGameEventRegistrationManager _gameEventRegistrationManager;
        private readonly IGameLanguageService _languageService;
        private readonly ISessionManager _sessionManager;

        public GameEventLockRegistrationEventHandler(IGameEventRegistrationManager gameEventRegistrationManager, ISessionManager sessionManager, IGameLanguageService languageService,
            IAsyncEventPipeline eventPipeline)
        {
            _gameEventRegistrationManager = gameEventRegistrationManager;
            _sessionManager = sessionManager;
            _languageService = languageService;
            _eventPipeline = eventPipeline;
        }

        public async Task HandleAsync(GameEventLockRegistrationEvent e, CancellationToken cancellation)
        {
            _gameEventRegistrationManager.RemoveGameEventRegistration(e.Type);

            GameDialogKey? gameEventKey = e.Type switch
            {
                GameEventType.InstantBattle => GameDialogKey.INSTANT_COMBAT_NAME,
                _ => null
            };

            Log.Debug("[GameEvent] Locked Registration for Event: " + e.Type);

            _sessionManager.Broadcast(x =>
            {
                string gameEventName = gameEventKey != null ? _languageService.GetLanguage(gameEventKey.Value, x.UserLanguage) : "?";
                string message = _languageService.GetLanguageFormat(GameDialogKey.GAMEEVENT_MESSAGE_START, x.UserLanguage, gameEventName);
                return x.GenerateMsgPacket(message, MsgMessageType.BottomCard);
            });

            _sessionManager.Broadcast(x => x.GenerateEsfPacket(4));

            ThreadSafeHashSet<long> registeredCharacters = _gameEventRegistrationManager.GetAndRemoveCharactersByGameEventInclination(e.Type);
            if (registeredCharacters == null)
            {
                return;
            }

            var list = new List<IClientSession>();

            foreach (long character in registeredCharacters)
            {
                IClientSession session = _sessionManager.GetSessionByCharacterId(character);
                if (session == null)
                {
                    continue;
                }

                list.Add(session);
            }

            await _eventPipeline.ProcessEventAsync(new GameEventMatchmakeEvent(e.Type, list), cancellation);
        }
    }
}