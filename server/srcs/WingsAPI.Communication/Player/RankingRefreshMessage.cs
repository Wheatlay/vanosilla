using System.Collections.Generic;
using PhoenixLib.ServiceBus;
using PhoenixLib.ServiceBus.Routing;
using WingsAPI.Data.Character;

namespace WingsAPI.Communication.Player
{
    [MessageType("ranking.refresh")]
    public class RankingRefreshMessage : IMessage
    {
        public IReadOnlyList<CharacterDTO> TopReputation { get; init; }
        public IReadOnlyList<CharacterDTO> TopCompliment { get; init; }
        public IReadOnlyList<CharacterDTO> TopPoints { get; init; }
    }
}