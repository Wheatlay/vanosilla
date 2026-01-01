using WingsEmu.Game._packetHandling;

namespace WingsEmu.Game.Bazaar.Events;

public class BazaarItemRemovedEvent : PlayerEvent
{
    public int ItemVnum { get; init; }
    public short SoldAmount { get; init; }
    public short Amount { get; init; }
    public long TotalProfit { get; init; }
}