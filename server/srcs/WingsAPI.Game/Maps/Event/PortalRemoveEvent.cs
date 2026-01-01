using PhoenixLib.Events;

namespace WingsEmu.Game.Maps.Event;

public class PortalRemoveEvent : IAsyncEvent
{
    public IPortalEntity Portal { get; init; }
}