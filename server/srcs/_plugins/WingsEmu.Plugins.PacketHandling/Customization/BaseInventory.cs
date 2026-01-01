// WingsEmu
// 
// Developed by NosWings Team

using System.Collections.Generic;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Customization.NewCharCustomisation;

public class BaseInventory
{
    public BaseInventory() => Items = new List<StartupInventoryItem>
    {
        new()
        {
            InventoryType = InventoryType.EquippedItems,
            Slot = (short)EquipmentType.MainWeapon,
            Quantity = 1,
            Vnum = 1
        },
        new()
        {
            InventoryType = InventoryType.EquippedItems,
            Slot = (short)EquipmentType.Armor,
            Quantity = 1,
            Vnum = 12
        },
        new()
        {
            InventoryType = InventoryType.EquippedItems,
            Slot = (short)EquipmentType.SecondaryWeapon,
            Quantity = 1,
            Vnum = 8
        },
        new()
        {
            InventoryType = InventoryType.Etc,
            Slot = 0,
            Quantity = 10,
            Vnum = 2024
        },
        new()
        {
            Vnum = 2081,
            Slot = 1,
            Quantity = 1,
            InventoryType = InventoryType.Etc
        }
    };

    public List<StartupInventoryItem> Items { get; set; }

    public class StartupInventoryItem
    {
        public short Vnum { get; set; }
        public short Quantity { get; set; }
        public short Slot { get; set; }
        public InventoryType InventoryType { get; set; }
    }
}