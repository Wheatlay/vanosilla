// WingsEmu
// 
// Developed by NosWings Team

using WingsEmu.Game.Inventory;

namespace WingsEmu.Game.Shops;

public class ShopPlayerItem
{
    public InventoryItem InventoryItem { get; set; }

    public long PricePerUnit { get; set; }

    public short SellAmount { get; set; }

    public short ShopSlot { get; set; }
}