// WingsEmu
// 
// Developed by NosWings Team

using WingsEmu.Game._packetHandling;
using WingsEmu.Game.Inventory;

namespace WingsEmu.Game.Raids.Events;

public class RaidPartyCreateEvent : PlayerEvent
{
    public RaidPartyCreateEvent(byte raidType, InventoryItem itemToRemove)
    {
        RaidType = raidType;
        ItemToRemove = itemToRemove;
    }

    public byte RaidType { get; }
    public InventoryItem ItemToRemove { get; }
}