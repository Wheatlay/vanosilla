using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.PacketGeneration;
using WingsEmu.Game;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Inventory.Event;
using WingsEmu.Game.Items;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Raids;

namespace WingsEmu.Plugins.BasicImplementations.Event.Items;

public class ThrowItemEventHandler : IAsyncEventProcessor<ThrowItemEvent>
{
    private readonly IGameItemInstanceFactory _gameItem;
    private readonly IRandomGenerator _randomGenerator;

    public ThrowItemEventHandler(IGameItemInstanceFactory gameItem, IRandomGenerator randomGenerator)
    {
        _gameItem = gameItem;
        _randomGenerator = randomGenerator;
    }

    public async Task HandleAsync(ThrowItemEvent e, CancellationToken cancellation)
    {
        GameItemInstance newItem = _gameItem.CreateItem(e.ItemVnum, e.Quantity);

        int rndX = e.BattleEntity.PositionX + _randomGenerator.RandomNumber(e.MinimumDistance, e.MaximumDistance + 1) * (_randomGenerator.RandomNumber(0, 2) * 2 - 1);
        int rndY = e.BattleEntity.PositionY + _randomGenerator.RandomNumber(e.MinimumDistance, e.MaximumDistance + 1) * (_randomGenerator.RandomNumber(0, 2) * 2 - 1);

        var position = new Position((short)rndX, (short)rndY);

        var item = new MonsterMapItem(position.X, position.Y, newItem, e.BattleEntity.MapInstance);

        e.BattleEntity.MapInstance.AddDrop(item);
        e.BattleEntity.BroadcastThrow(item);
    }
}

public class DropItemEventHandler : IAsyncEventProcessor<DropMapItemEvent>
{
    private readonly IGameItemInstanceFactory _gameItem;

    public DropItemEventHandler(IGameItemInstanceFactory gameItem) => _gameItem = gameItem;

    public async Task HandleAsync(DropMapItemEvent e, CancellationToken cancellation)
    {
        IMapInstance map = e.Map;
        GameItemInstance newItem = _gameItem.CreateItem(e.Vnum, e.Amount, (byte)e.Upgrade, (sbyte)e.Rarity, (byte)e.Design);
        var item = new MonsterMapItem(e.Position.X, e.Position.Y, newItem, e.Map, e.OwnerId, e.IsQuest);

        map.AddDrop(item);
        item.BroadcastDrop();
    }
}