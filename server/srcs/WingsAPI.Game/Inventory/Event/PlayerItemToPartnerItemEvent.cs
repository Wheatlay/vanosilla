using WingsEmu.Game._packetHandling;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Game.Inventory.Event;

public class PlayerItemToPartnerItemEvent : PlayerEvent
{
    public PlayerItemToPartnerItemEvent(byte slot, InventoryType inventoryType)
    {
        Slot = slot;
        InventoryType = inventoryType;
    }

    public byte Slot { get; }
    public InventoryType InventoryType { get; }
}