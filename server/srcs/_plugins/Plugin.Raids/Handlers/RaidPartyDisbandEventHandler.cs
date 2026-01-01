using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Raids;
using WingsEmu.Game.Raids.Enum;
using WingsEmu.Game.Raids.Events;
using WingsEmu.Game.Revival;
using WingsEmu.Packets.Enums.Chat;

namespace Plugin.Raids;

public class RaidPartyDisbandEventHandler : IAsyncEventProcessor<RaidPartyDisbandEvent>
{
    private readonly IAsyncEventPipeline _eventPipeline;
    private readonly IGameLanguageService _gameLanguage;
    private readonly IRaidManager _raidManager;

    public RaidPartyDisbandEventHandler(IGameLanguageService gameLanguage, IRaidManager raidManager, IAsyncEventPipeline eventPipeline)
    {
        _gameLanguage = gameLanguage;
        _raidManager = raidManager;
        _eventPipeline = eventPipeline;
    }

    public async Task HandleAsync(RaidPartyDisbandEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;

        if (!session.PlayerEntity.IsInRaidParty)
        {
            return;
        }

        RaidParty raid = session.PlayerEntity.Raid;

        if (raid?.Members == null)
        {
            return;
        }

        if (!session.PlayerEntity.IsRaidLeader(session.PlayerEntity.Id))
        {
            return;
        }

        if (session.PlayerEntity.Raid.Finished && e.IsByRdPacket)
        {
            return;
        }

        if (session.PlayerEntity.HasRaidStarted && session.PlayerEntity.IsRaidLeader(session.PlayerEntity.Id) && raid.Members.Count > 1)
        {
            await session.EmitEventAsync(new RaidPartyLeaveEvent(false));
            return;
        }

        foreach (IClientSession member in raid.Members)
        {
            RemoveFromRaid(member, raid);
        }

        await _eventPipeline.ProcessEventAsync(new RaidInstanceFinishEvent(raid, RaidFinishType.Disbanded), cancellation);
        await session.EmitEventAsync(new RaidAbandonedEvent { RaidId = raid.Id });
    }

    private void RemoveFromRaid(IClientSession session, RaidParty raidParty)
    {
        RaidPartyLeaveEventHandler.InternalLeave(session);

        if (raidParty.Started)
        {
            if (!session.PlayerEntity.IsAlive())
            {
                session.EmitEvent(new RevivalReviveEvent());
            }

            session.ChangeToLastBaseMap();
        }

        session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.RAID_MESSAGE_DISOLVED, session.UserLanguage), ChatMessageColorType.Red);
        session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.RAID_MESSAGE_DISOLVED, session.UserLanguage), MsgMessageType.Middle);
    }
}