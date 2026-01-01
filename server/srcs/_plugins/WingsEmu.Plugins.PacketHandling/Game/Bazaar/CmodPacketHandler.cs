using System.Threading.Tasks;
using WingsEmu.Game.Bazaar.Events;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.ClientPackets;

namespace WingsEmu.Plugins.PacketHandling.Game.Bazaar;

public class CmodPacketHandler : GenericGamePacketHandlerBase<CmodPacket>
{
    protected override async Task HandlePacketAsync(IClientSession session, CmodPacket packet)
    {
        await session.EmitEventAsync(new BazaarItemChangePriceEvent(packet.BazaarId, packet.NewPricePerItem, packet.Confirmed != 0));
    }
}