using System.Threading.Tasks;
using WingsEmu.Game.Bazaar.Events;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.ClientPackets;

namespace WingsEmu.Plugins.PacketHandling.Game.Bazaar;

public class CScaclcPacketHandler : GenericGamePacketHandlerBase<CScalcPacket>
{
    protected override async Task HandlePacketAsync(IClientSession session, CScalcPacket packet)
    {
        await session.EmitEventAsync(new BazaarItemRemoveEvent(packet.BazaarId));
    }
}