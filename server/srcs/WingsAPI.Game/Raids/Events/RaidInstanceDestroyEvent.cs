using PhoenixLib.Events;

namespace WingsEmu.Game.Raids.Events;

public class RaidInstanceDestroyEvent : IAsyncEvent
{
    public RaidInstanceDestroyEvent(RaidParty raidParty) => RaidParty = raidParty;

    public RaidParty RaidParty { get; }
}