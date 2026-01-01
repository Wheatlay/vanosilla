using System.Threading.Tasks;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.ClientPackets;

namespace WingsEmu.Plugins.PacketHandling.Game.Useless;

public class CClosePacketHandler : GenericGamePacketHandlerBase<CClosePacket>
{
    protected override async Task HandlePacketAsync(IClientSession session, CClosePacket packet)
    {
        if (!session.HasSelectedCharacter)
        {
            return;
        }

        session.PlayerEntity.IsBankOpen = false;
    }
}