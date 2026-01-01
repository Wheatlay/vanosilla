using System;
using System.Collections.Generic;
using PhoenixLib.ServiceBus.Routing;

namespace Plugin.PlayerLogs.Messages.Family
{
    [MessageType("logs.family.created")]
    public class LogFamilyCreatedMessage : IPlayerActionLogMessage
    {
        public long FamilyId { get; set; }
        public string FamilyName { get; set; }
        public List<long> DeputiesIds { get; set; }
        public DateTime CreatedAt { get; init; }
        public int ChannelId { get; init; }
        public long CharacterId { get; init; }
        public string CharacterName { get; init; }
        public string IpAddress { get; init; }
    }
}