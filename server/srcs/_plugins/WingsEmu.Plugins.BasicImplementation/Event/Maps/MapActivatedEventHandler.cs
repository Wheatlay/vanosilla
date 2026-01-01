// WingsEmu
// 
// Developed by NosWings Team

using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.Game._ECS;
using WingsEmu.Game.Maps.Event;

namespace WingsEmu.Plugins.BasicImplementations.Event.Maps;

public class MapActivatedEventHandler : IAsyncEventProcessor<MapActivatedEvent>
{
    private readonly ITickManager _tickManager;

    public MapActivatedEventHandler(ITickManager tickManager) => _tickManager = tickManager;

    public async Task HandleAsync(MapActivatedEvent e, CancellationToken cancellation) => _tickManager.AddProcessable(e.MapInstance);
}

public class MapDeactivatedEventHandler : IAsyncEventProcessor<MapDeactivatedEvent>
{
    private readonly ITickManager _tickManager;

    public MapDeactivatedEventHandler(ITickManager tickManager) => _tickManager = tickManager;

    public async Task HandleAsync(MapDeactivatedEvent e, CancellationToken cancellation) => _tickManager.RemoveProcessable(e.MapInstance);
}