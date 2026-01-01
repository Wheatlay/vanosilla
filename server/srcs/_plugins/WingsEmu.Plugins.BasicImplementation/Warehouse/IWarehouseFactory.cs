using WingsEmu.Game.Characters;
using WingsEmu.Game.Warehouse;

namespace WingsEmu.Plugins.BasicImplementations.Warehouse;

public interface IWarehouseFactory
{
    IWarehouse Create(IPlayerEntity entity);
}