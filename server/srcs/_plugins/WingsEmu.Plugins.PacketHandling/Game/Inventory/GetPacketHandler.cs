using System;
using System.Threading.Tasks;
using WingsEmu.Game.Inventory.Event;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.ClientPackets;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.PacketHandling.Game.Inventory;

public class GetPacketHandler : GenericGamePacketHandlerBase<GetPacket>
{
    protected override async Task HandlePacketAsync(IClientSession session, GetPacket getPacket)
    {
        if (!Enum.TryParse(getPacket.PickerType.ToString(), out VisualType type))
        {
            return;
        }

        await session.EmitEventAsync(new InventoryPickUpItemEvent(type, getPacket.PickerId, getPacket.TransportId));
    }
}