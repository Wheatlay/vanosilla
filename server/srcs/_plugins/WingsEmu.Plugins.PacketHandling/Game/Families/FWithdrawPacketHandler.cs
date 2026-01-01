using System.Threading.Tasks;
using WingsEmu.Game.Families.Event;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.ClientPackets;

namespace WingsEmu.Plugins.PacketHandling.Game.Families;

public class FWithdrawPacketHandler : GenericGamePacketHandlerBase<FWithdrawPacket>
{
    protected override async Task HandlePacketAsync(IClientSession session, FWithdrawPacket packet)
    {
        if (packet.Amount < 1 || 999 < packet.Amount)
        {
            return;
        }

        await session.EmitEventAsync(new FamilyWarehouseWithdrawItemEvent
        {
            Slot = packet.Slot,
            Amount = packet.Amount
        });
    }
}