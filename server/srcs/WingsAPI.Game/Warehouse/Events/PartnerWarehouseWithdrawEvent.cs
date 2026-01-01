using WingsEmu.Game._packetHandling;

namespace WingsEmu.Game.Warehouse.Events;

public class PartnerWarehouseWithdrawEvent : PlayerEvent
{
    public PartnerWarehouseWithdrawEvent(short slot, short amount)
    {
        Slot = slot;
        Amount = amount;
    }

    public short Slot { get; }
    public short Amount { get; }
}