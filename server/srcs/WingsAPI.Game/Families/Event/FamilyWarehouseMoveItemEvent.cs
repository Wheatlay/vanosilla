using WingsEmu.Game._packetHandling;

namespace WingsEmu.Game.Families.Event;

public class FamilyWarehouseMoveItemEvent : PlayerEvent
{
    public short OldSlot { get; init; }

    public short Amount { get; init; }

    public short NewSlot { get; init; }
}