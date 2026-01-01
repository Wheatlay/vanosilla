using WingsEmu.Game._packetHandling;

namespace WingsEmu.Game.Families.Event;

public class FamilyWarehouseWithdrawItemEvent : PlayerEvent
{
    public short Slot { get; init; }
    public short Amount { get; init; }
}