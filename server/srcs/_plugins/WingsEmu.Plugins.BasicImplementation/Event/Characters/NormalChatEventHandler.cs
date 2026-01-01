using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.DTOs.Maps;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Entities.Extensions;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Networking.Broadcasting;
using WingsEmu.Game.TimeSpaces;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.Event.Characters;

public class NormalChatEventHandler : IAsyncEventProcessor<NormalChatEvent>
{
    public async Task HandleAsync(NormalChatEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;

        if (session.CurrentMapInstance.HasMapFlag(MapFlags.ACT_4))
        {
            session.CurrentMapInstance.Broadcast(session.PlayerEntity.GenerateSayPacket(e.Message.Trim(), ChatMessageColorType.White),
                new FactionBroadcast(session.PlayerEntity.Faction), new ExceptSessionBroadcast(session));

            FactionType enemyFaction = session.PlayerEntity.Faction == FactionType.Angel ? FactionType.Demon : FactionType.Angel;
            session.CurrentMapInstance.Broadcast(session.PlayerEntity.GenerateSayPacket("^$#%#&^%$@#", ChatMessageColorType.PlayerSay),
                new FactionBroadcast(enemyFaction, true), new ExceptSessionBroadcast(session));
            return;
        }

        session.CurrentMapInstance.Broadcast(session.PlayerEntity.GenerateSayPacket(e.Message.Trim(), ChatMessageColorType.White), new ExceptSessionBroadcast(session),
            new RainbowTeamBroadcast(session));

        TimeSpaceParty timeSpace = session.PlayerEntity.TimeSpaceComponent.TimeSpace;
        if (timeSpace?.Instance == null)
        {
            return;
        }

        foreach (TimeSpaceSubInstance timeSpaceSubInstance in timeSpace.Instance.TimeSpaceSubInstances.Values)
        {
            if (session.CurrentMapInstance.Id == timeSpaceSubInstance.MapInstance.Id)
            {
                continue;
            }

            foreach (IClientSession member in timeSpaceSubInstance.MapInstance.Sessions)
            {
                session.SendSpeakToTarget(member, e.Message.Trim(), SpeakType.Normal);
            }
        }
    }
}