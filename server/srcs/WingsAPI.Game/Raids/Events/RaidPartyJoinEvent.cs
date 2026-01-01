using WingsEmu.Game._packetHandling;

namespace WingsEmu.Game.Raids.Events;

public class RaidPartyJoinEvent : PlayerEvent
{
    public RaidPartyJoinEvent(long raidOwnerId, bool isByRaidList)
    {
        RaidOwnerId = raidOwnerId;
        IsByRaidList = isByRaidList;
    }

    public long RaidOwnerId { get; }
    public bool IsByRaidList { get; }
}