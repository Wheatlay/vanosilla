// WingsEmu
// 
// Developed by NosWings Team

using System.Threading.Tasks;
using WingsEmu.Game._ItemUsage;
using WingsEmu.Game._ItemUsage.Event;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Raids.Events;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.ItemUsage.Etc.Special;

public class RaidSealHandler : IItemHandler
{
    public ItemType ItemType => ItemType.Special;
    public long[] Effects => new long[] { 301 };

    public async Task HandleAsync(IClientSession session, InventoryUseItemEvent e)
    {
        await session.EmitEventAsync(new RaidPartyCreateEvent((byte)e.Item.ItemInstance.GameItem.EffectValue, e.Item));
    }
}