// WingsEmu
// 
// Developed by NosWings Team

using System;
using WingsEmu.Game._packetHandling;

namespace WingsEmu.Game.Maps.Event;

public class JoinMapEvent : PlayerEvent
{
    public JoinMapEvent(int joinedMap, short? x = null, short? y = null)
    {
        JoinedMapId = joinedMap;
        X = x;
        Y = y;
    }

    public JoinMapEvent(Guid joinedMap, short? x = null, short? y = null)
    {
        JoinedMapGuid = joinedMap;
        X = x;
        Y = y;
    }

    public JoinMapEvent(IMapInstance joinedMap, short? x = null, short? y = null)
    {
        JoinedMapInstance = joinedMap;
        X = x;
        Y = y;
    }

    public int JoinedMapId { get; }

    public Guid JoinedMapGuid { get; }

    public IMapInstance JoinedMapInstance { get; }

    public short? X { get; }

    public short? Y { get; }
}