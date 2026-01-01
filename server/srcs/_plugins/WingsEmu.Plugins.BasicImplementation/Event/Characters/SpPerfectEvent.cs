using WingsEmu.Game._packetHandling;
using WingsEmu.Game.Inventory;

namespace WingsEmu.Plugins.BasicImplementations.Event.Characters;

public class SpPerfectEvent : PlayerEvent
{
    public SpPerfectEvent(InventoryItem inventoryItem) => InventoryItem = inventoryItem;

    public InventoryItem InventoryItem { get; }
}