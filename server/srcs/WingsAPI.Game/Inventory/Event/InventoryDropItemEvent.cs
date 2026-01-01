using WingsEmu.Game._packetHandling;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Game.Inventory.Event;

public class InventoryDropItemEvent : PlayerEvent
{
    public InventoryDropItemEvent(InventoryType inventoryType, short slot, short amount)
    {
        InventoryType = inventoryType;
        Slot = slot;
        Amount = amount;
    }

    public InventoryType InventoryType { get; }
    public short Slot { get; }
    public short Amount { get; }
}