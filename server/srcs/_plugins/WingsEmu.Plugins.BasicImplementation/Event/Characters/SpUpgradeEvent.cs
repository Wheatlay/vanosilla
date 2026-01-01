using WingsEmu.Game._packetHandling;
using WingsEmu.Game.Inventory;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.Event.Characters;

public class SpUpgradeEvent : PlayerEvent
{
    public SpUpgradeEvent(UpgradeProtection upgradeProtection, InventoryItem inventoryItem, bool isFree = false)
    {
        UpgradeProtection = upgradeProtection;
        InventoryItem = inventoryItem;
        IsFree = isFree;
    }

    public UpgradeProtection UpgradeProtection { get; }
    public InventoryItem InventoryItem { get; }
    public bool IsFree { get; }
}