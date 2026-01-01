using WingsEmu.Game._packetHandling;

namespace WingsEmu.Game.Warehouse.Events;

public class PartnerWarehouseMoveEvent : PlayerEvent
{
    public PartnerWarehouseMoveEvent(short originalSlot, short amount, short newSlot)
    {
        OriginalSlot = originalSlot;
        Amount = amount;
        NewSlot = newSlot;
    }

    public short OriginalSlot { get; }
    public short Amount { get; }
    public short NewSlot { get; }
}