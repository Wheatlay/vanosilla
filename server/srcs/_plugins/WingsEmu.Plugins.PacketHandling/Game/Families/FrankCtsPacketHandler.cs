using System.Threading.Tasks;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.ClientPackets;

namespace WingsEmu.Plugins.PacketHandling.Game.Families;

public class FrankCtsPacketHandler : GenericGamePacketHandlerBase<FrankCtsPacket>
{
    protected override async Task HandlePacketAsync(IClientSession session, FrankCtsPacket packet)
    {
    }
}