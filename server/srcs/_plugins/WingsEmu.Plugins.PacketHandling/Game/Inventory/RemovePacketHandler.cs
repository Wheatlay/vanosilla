using System.Threading.Tasks;
using WingsEmu.Game.Inventory.Event;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.ClientPackets;

namespace WingsEmu.Plugins.PacketHandling.Game.Inventory;

public class RemovePacketHandler : GenericGamePacketHandlerBase<RemovePacket>
{
    protected override async Task HandlePacketAsync(IClientSession session, RemovePacket removePacket)
    {
        if (session.PlayerEntity.IsSeal)
        {
            return;
        }

        if (removePacket.PartnerSlot == 0)
        {
            await session.EmitEventAsync(new InventoryTakeOffItemEvent(removePacket.InventorySlot));
            return;
        }

        await session.EmitEventAsync(new PartnerInventoryTakeOffItemEvent(removePacket.PartnerSlot, removePacket.InventorySlot));
    }
}