using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Raids;
using WingsEmu.Game.Raids.Configuration;
using WingsEmu.Game.Raids.Events;
using WingsEmu.Game.Revival;
using WingsEmu.Packets.Enums.Chat;

namespace Plugin.Raids;

public class RaidPartyLeaveEventHandler : IAsyncEventProcessor<RaidPartyLeaveEvent>
{
    private readonly IGameLanguageService _gameLanguage;
    private readonly RaidConfiguration _raidConfiguration;

    public RaidPartyLeaveEventHandler(IGameLanguageService gameLanguage, RaidConfiguration raidConfiguration)
    {
        _gameLanguage = gameLanguage;
        _raidConfiguration = raidConfiguration;
    }

    public async Task HandleAsync(RaidPartyLeaveEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;
        IPlayerEntity character = session.PlayerEntity;
        bool byKick = e.ByKick;

        if (session?.PlayerEntity == null)
        {
            return;
        }

        if (!character.IsInRaidParty)
        {
            return;
        }

        bool isLeader = character.IsRaidLeader(character.Id);
        if (!character.HasRaidStarted && isLeader)
        {
            await session.EmitEventAsync(new RaidPartyDisbandEvent());
            return;
        }

        RaidParty raidParty = character.Raid;

        if (raidParty == null)
        {
            return;
        }

        if (raidParty.Members == null)
        {
            InternalLeave(session);
            return;
        }

        if (raidParty.Members.Count - 1 < 1)
        {
            await session.EmitEventAsync(new RaidPartyDisbandEvent());
            return;
        }

        if (session.PlayerEntity.RaidDeaths >= _raidConfiguration.LivesPerCharacter && session.PlayerEntity.Raid.Instance.Lives > 0)
        {
            session.PlayerEntity.ShowRaidDeathInfo = true;
        }

        InternalLeave(session);

        if (raidParty.Started)
        {
            if (!session.PlayerEntity.IsAlive())
            {
                await session.EmitEventAsync(new RevivalReviveEvent());
            }

            session.ChangeToLastBaseMap();

            if (e.RemoveLife)
            {
                raidParty.Instance.IncreaseOrDecreaseLives(-1);
            }
        }

        GameDialogKey chatMessageDialog = GameDialogKey.RAID_CHATMESSAGE_LEFT;
        GameDialogKey msgDialog = GameDialogKey.RAID_SHOUTMESSAGE_LEFT;

        if (byKick)
        {
            chatMessageDialog = GameDialogKey.RAID_CHATMESSAGE_KICKED_OTHER;
            msgDialog = GameDialogKey.RAID_SHOUTMESSAGE_KICKED_OTHER;

            session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.RAID_CHATMESSAGE_KICKED, session.UserLanguage), ChatMessageColorType.Yellow);
            session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.RAID_SHOUTMESSAGE_KICKED, session.UserLanguage), MsgMessageType.Middle);
        }

        await session.EmitEventAsync(new RaidLeftEvent { RaidId = raidParty.Id });

        foreach (IClientSession member in raidParty.Members)
        {
            if (raidParty.Started && isLeader)
            {
                member.SendChatMessage(_gameLanguage.GetLanguageFormat(GameDialogKey.RAID_CHATMESSAGE_NEW_LEADER, member.UserLanguage, raidParty.Leader.PlayerEntity.Name),
                    ChatMessageColorType.Yellow);
                member.SendMsg(_gameLanguage.GetLanguageFormat(GameDialogKey.RAID_SHOUTMESSAGE_NEW_LEADER, member.UserLanguage, raidParty.Leader.PlayerEntity.Name), MsgMessageType.Middle);
            }
            else
            {
                member.SendChatMessage(_gameLanguage.GetLanguageFormat(chatMessageDialog, member.UserLanguage, character.Name), ChatMessageColorType.Yellow);
                member.SendMsg(_gameLanguage.GetLanguageFormat(msgDialog, member.UserLanguage, character.Name), MsgMessageType.Middle);
            }

            member.SendRaidPacket(RaidPacketType.LIST_MEMBERS);
            member.RefreshRaidMemberList();
        }
    }

    public static void InternalLeave(IClientSession session)
    {
        if (session?.PlayerEntity == null)
        {
            return;
        }

        session.PlayerEntity.Raid?.RemoveMember(session);
        session.PlayerEntity.SetRaidParty(null);
        session.SendRaidPacket(RaidPacketType.LEAVE, true);
        session.SendRaidPacket(RaidPacketType.LEADER_RELATED, true);
    }
}