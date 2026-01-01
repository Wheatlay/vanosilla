using System.Threading.Tasks;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Chat;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Networking.Broadcasting;
using WingsEmu.Packets.ClientPackets;
using WingsEmu.Packets.Enums.Chat;
using ChatType = WingsEmu.Game._playerActionLogs.ChatType;

namespace WingsEmu.Plugins.PacketHandling.Game.Basic;

public class SayPacketHandler : GenericGamePacketHandlerBase<SayPacket>
{
    private readonly ISessionManager _sessionManager;

    public SayPacketHandler(ISessionManager sessionManager) => _sessionManager = sessionManager;

    protected override async Task HandlePacketAsync(IClientSession session, SayPacket packet)
    {
        if (string.IsNullOrEmpty(packet.Message))
        {
            return;
        }

        if (session.CurrentMapInstance == null)
        {
            return;
        }

        if (session.PlayerEntity.CheatComponent.IsInvisible)
        {
            return;
        }

        if (session.IsMuted())
        {
            session.SendMuteMessage();
            return;
        }

        string message = packet.Message;

        if (message.Length > 60)
        {
            message = message.Substring(0, 60);
        }

        if (message.StartsWith("!"))
        {
            ProcessTimeSpaceMessage(session, message);
            return;
        }

        await session.EmitEventAsync(new NormalChatEvent
        {
            Message = message
        });

        await session.EmitEventAsync(new ChatGenericEvent
        {
            Message = message.Trim(),
            ChatType = ChatType.General
        });
    }

    private void ProcessTimeSpaceMessage(IClientSession session, string message)
    {
        if (!session.PlayerEntity.TimeSpaceComponent.IsInTimeSpaceParty)
        {
            return;
        }

        message = message[1..];
        session.SendChatMessage(message, ChatMessageColorType.LightYellow);
        _sessionManager.Broadcast(session.GenerateSpkPacket(message, SpeakType.TimeSpace), new TimeSpaceBroadcast(session), new ExpectBlockedPlayerBroadcast(session.PlayerEntity.Id));
    }
}