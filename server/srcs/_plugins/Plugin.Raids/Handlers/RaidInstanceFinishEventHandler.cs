using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.Quests;
using WingsAPI.Packets.Enums;
using WingsEmu.DTOs.Quests;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Quests;
using WingsEmu.Game.Quests.Event;
using WingsEmu.Game.Raids;
using WingsEmu.Game.Raids.Configuration;
using WingsEmu.Game.Raids.Enum;
using WingsEmu.Game.Raids.Events;
using WingsEmu.Game.Revival;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace Plugin.Raids;

public class RaidInstanceFinishEventHandler : IAsyncEventProcessor<RaidInstanceFinishEvent>
{
    private readonly IAsyncEventPipeline _eventPipeline;
    private readonly IGameLanguageService _languageService;
    private readonly IQuestManager _questManager;
    private readonly RaidConfiguration _raidConfiguration;
    private readonly IRaidManager _raidManager;
    private readonly ISessionManager _sessionManager;

    public RaidInstanceFinishEventHandler(RaidConfiguration raidConfiguration, IRaidManager raidManager, IAsyncEventPipeline eventPipeline, ISessionManager sessionManager,
        IGameLanguageService languageService, IQuestManager questManager)
    {
        _raidConfiguration = raidConfiguration;
        _raidManager = raidManager;
        _eventPipeline = eventPipeline;
        _sessionManager = sessionManager;
        _languageService = languageService;
        _questManager = questManager;
    }

    public async Task HandleAsync(RaidInstanceFinishEvent e, CancellationToken cancellation)
    {
        if (e.RaidParty.Finished)
        {
            return;
        }

        _raidManager.UnregisterRaidFromRaidPublishList(e.RaidParty);

        RaidWindowType windowType = RaidWindowType.MISSION_FAIL;
        DateTime currentTime = DateTime.UtcNow;

        if (e.RaidFinishType != RaidFinishType.Disbanded)
        {
            foreach (RaidSubInstance subInstance in e.RaidParty.Instance.RaidSubInstances.Values)
            {
                foreach (IMonsterEntity monsterEntity in subInstance.MapInstance.GetAliveMonsters())
                {
                    if (monsterEntity.IsBoss)
                    {
                        continue;
                    }

                    subInstance.MapInstance.DespawnMonster(monsterEntity);
                    subInstance.MapInstance.RemoveMonster(monsterEntity);
                }
            }
        }

        switch (e.RaidFinishType)
        {
            case RaidFinishType.Disbanded:
                await _eventPipeline.ProcessEventAsync(new RaidInstanceDestroyEvent(e.RaidParty), cancellation);
                return;
            case RaidFinishType.MissionClear:
                windowType = RaidWindowType.MISSION_CLEAR;
                e.RaidParty.Instance.SetFinishSlowMoDate(currentTime + _raidConfiguration.RaidSlowMoDelay);
                break;
            case RaidFinishType.TimeIsUp:
                windowType = RaidWindowType.TIMES_UP;
                break;
            case RaidFinishType.NoLivesLeft:
                windowType = RaidWindowType.NO_LIVES_LEFT;
                break;
        }

        IClientSession[] sessions = e.RaidParty.Members.ToArray();

        foreach (IClientSession session in sessions)
        {
            if (!session.PlayerEntity.IsAlive())
            {
                await session.EmitEventAsync(new RevivalReviveEvent());
            }

            if (e.RaidFinishType == RaidFinishType.MissionClear)
            {
                session.TrySendRaidBossDeadPackets();
                await session.EmitEventAsync(new RaidWonEvent());
                await CheckRaidQuest(session, e.RaidParty.Type);
            }

            session.SendRaidUiPacket(e.RaidParty.Type, windowType);
            session.SendRemoveClockPacket();
            session.SendRaidPacket(RaidPacketType.LIST_MEMBERS);
        }

        if (e.RaidFinishType == RaidFinishType.MissionClear)
        {
            IMonsterEntity raidBoss = FindBossMap(e.RaidParty);
            BroadcastRaidFinishMessage(e.RaidParty);
            await e.RaidParty.Leader.EmitEventAsync(new RaidTargetKilledEvent { DamagerCharactersIds = raidBoss.PlayersDamage.Keys.ToArray() });
            await _eventPipeline.ProcessEventAsync(new RaidGiveRewardsEvent(e.RaidParty, raidBoss, e.RaidParty.Instance.RaidReward), cancellation);
        }
        else if (e.RaidFinishType != RaidFinishType.Disbanded)
        {
            await e.RaidParty.Leader.EmitEventAsync(new RaidLostEvent());
        }

        foreach (IClientSession session in sessions)
        {
            session.SendRaidmbf();
            session.RefreshRaidMemberList();
        }

        e.RaidParty.FinishRaid(currentTime + _raidConfiguration.RaidMapDestroyDelay);
    }

    private async Task CheckRaidQuest(IClientSession session, RaidType raidType)
    {
        IEnumerable<CharacterQuest> characterQuests = session.PlayerEntity.GetCurrentQuestsByTypes(new[] { QuestType.WIN_RAID_AND_TALK_TO_NPC });
        foreach (CharacterQuest quest in characterQuests)
        {
            foreach (QuestObjectiveDto objective in quest.Quest.Objectives)
            {
                if (raidType != (RaidType)objective.Data0)
                {
                    continue;
                }

                CharacterQuestObjectiveDto questObjectiveDto = quest.ObjectiveAmount[objective.ObjectiveIndex];

                int amountLeft = questObjectiveDto.RequiredAmount - questObjectiveDto.CurrentAmount;
                if (amountLeft == 0)
                {
                    break;
                }

                questObjectiveDto.CurrentAmount++;
                session.RefreshQuestProgress(_questManager, quest.QuestId);
                await session.EmitEventAsync(new QuestObjectiveUpdatedEvent
                {
                    CharacterQuest = quest
                });
            }
        }
    }

    private void BroadcastRaidFinishMessage(RaidParty raidParty)
    {
        _sessionManager.Broadcast(x => x.GenerateMsgPacket(string.Format(
                _languageService.GetLanguage(GameDialogKey.RAID_SHOUTMESSAGE_COMPLETED, x.UserLanguage), raidParty.Leader.PlayerEntity.Name, x.GenerateRaidName(_languageService, raidParty.Type)),
            MsgMessageType.Middle));
    }

    private IMonsterEntity FindBossMap(RaidParty raidParty)
    {
        IMonsterEntity raidBoss = null;

        foreach (RaidSubInstance subInstance in raidParty.Instance.RaidSubInstances.Values)
        {
            foreach (IMonsterEntity monsterEntity in subInstance.DeadBossMonsters)
            {
                raidBoss = monsterEntity;
                break;
            }
        }

        return raidBoss;
    }
}