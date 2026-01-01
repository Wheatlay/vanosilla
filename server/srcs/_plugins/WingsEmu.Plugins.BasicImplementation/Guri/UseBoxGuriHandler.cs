using System.Threading.Tasks;
using PhoenixLib.Logging;
using WingsEmu.Game._Guri;
using WingsEmu.Game._Guri.Event;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.Guri;

public class UseBoxGuriHandler : IGuriHandler
{
    public long GuriEffectId => 300;

    public async Task ExecuteAsync(IClientSession session, GuriEvent guriPacket)
    {
        if (guriPacket.Data != 8023)
        {
            return;
        }

        if (guriPacket.User == null)
        {
            return;
        }

        short slot = (short)guriPacket.User.Value;
        InventoryItem box = session.PlayerEntity.GetItemBySlotAndType(slot, InventoryType.Equipment);

        if (box == null)
        {
            Log.Info("No box");
            return;
        }

        await session.EmitEventAsync(new RollItemBoxEvent
        {
            Item = box
        });
    }
}