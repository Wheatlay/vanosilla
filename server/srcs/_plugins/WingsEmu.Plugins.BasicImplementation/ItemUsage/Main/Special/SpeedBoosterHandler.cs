using System.Threading.Tasks;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsEmu.Game._enum;
using WingsEmu.Game._ItemUsage;
using WingsEmu.Game._ItemUsage.Event;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;
using WingsEmu.Plugins.BasicImplementations.Vehicles;

namespace WingsEmu.Plugins.BasicImplementations.ItemUsage.Main.Special;

public class SpeedBoosterHandler : IItemHandler
{
    private readonly IVehicleConfigurationProvider _provider;

    public SpeedBoosterHandler(IVehicleConfigurationProvider provider) => _provider = provider;

    public ItemType ItemType => ItemType.Special;
    public long[] Effects => new long[] { 998 };

    public async Task HandleAsync(IClientSession session, InventoryUseItemEvent e)
    {
        if (!session.PlayerEntity.IsOnVehicle)
        {
            return;
        }

        if (session.PlayerEntity.HasBuff(BuffVnums.SPEED_BOOSTER))
        {
            return;
        }

        VehicleConfiguration vehicle = _provider.GetByMorph(session.PlayerEntity.Morph, session.PlayerEntity.Gender);

        if (vehicle?.VehicleBoostType == null)
        {
            return;
        }

        await session.EmitEventAsync(new SpeedBoosterEvent());
        await session.RemoveItemFromInventory(item: e.Item);
    }
}