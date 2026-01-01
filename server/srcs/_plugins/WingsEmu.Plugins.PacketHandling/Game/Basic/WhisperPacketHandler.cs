using System.Linq;
using System.Threading.Tasks;
using WingsEmu.Game.InterChannel;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.ClientPackets;

namespace WingsEmu.Plugins.PacketHandling.Game.Basic;

public class WhisperPacketHandler : GenericGamePacketHandlerBase<WhisperPacket>
{
    protected override async Task HandlePacketAsync(IClientSession session, WhisperPacket whisperPacket)
    {
        if (string.IsNullOrEmpty(whisperPacket.Message) || whisperPacket.Message.Length < 2)
        {
            return;
        }

        string[] messageSplit = whisperPacket.Message.Split(' ');
        string characterName = messageSplit[0];
        string message = string.Join(" ", messageSplit.Skip(1));

        if (message.Length > 60)
        {
            message = message.Substring(0, 60);
        }

        message = message.Trim();

        await session.EmitEventAsync(new InterChannelSendWhisperEvent(characterName, message));
    }
}