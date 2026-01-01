// WingsEmu
// 
// Developed by NosWings Team

using System.Threading.Tasks;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.ClientPackets;

namespace WingsEmu.Plugins.PacketHandling.CharScreen;

public class CreateBrawlerPacketHandler : GenericCharScreenPacketHandlerBase<BrawlerCreatePacket>
{
    protected override async Task HandlePacketAsync(IClientSession session, BrawlerCreatePacket packet)
    {
        // Sorry, not today :c
    }
}