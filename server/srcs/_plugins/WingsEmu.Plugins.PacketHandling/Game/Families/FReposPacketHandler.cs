using System.Threading.Tasks;
using WingsEmu.Game.Families.Event;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.ClientPackets;

namespace WingsEmu.Plugins.PacketHandling.Game.Families;

public class FReposPacketHandler : GenericGamePacketHandlerBase<FReposPacket>
{
    protected override async Task HandlePacketAsync(IClientSession session, FReposPacket fReposPacket)
    {
        if (fReposPacket.Amount < 1 || 999 < fReposPacket.Amount)
        {
            return;
        }

        await session.EmitEventAsync(new FamilyWarehouseMoveItemEvent
        {
            OldSlot = fReposPacket.OldSlot,
            Amount = fReposPacket.Amount,
            NewSlot = fReposPacket.NewSlot
        });
    }
}