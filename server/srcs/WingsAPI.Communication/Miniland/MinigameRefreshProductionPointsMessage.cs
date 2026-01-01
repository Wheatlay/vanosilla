// WingsEmu
// 
// Developed by NosWings Team

using PhoenixLib.ServiceBus;
using PhoenixLib.ServiceBus.Routing;

namespace WingsAPI.Communication.Miniland
{
    [MessageType("minigame.refresh-production.daily")]
    public class MinigameRefreshProductionPointsMessage : IMessage
    {
        public bool Force { get; set; }
    }
}