using System.Collections.Generic;
using WingsEmu.Game.Raids;

namespace Plugin.Raids;

public class RaidManager : IRaidManager
{
    private readonly HashSet<RaidParty> _raidPublishList = new(RaidParty.IdComparer);
    private readonly HashSet<RaidParty> _raids = new(RaidParty.IdComparer);
    public IReadOnlyCollection<RaidParty> Raids => _raids;
    public IReadOnlyCollection<RaidParty> RaidPublishList => _raidPublishList;

    public void AddRaid(RaidParty raidParty)
    {
        if (_raids.Contains(raidParty))
        {
            return;
        }

        _raids.Add(raidParty);
    }

    public void RemoveRaid(RaidParty raidParty)
    {
        _raids.Remove(raidParty);
    }

    public bool ContainsRaidInRaidPublishList(RaidParty raidParty) => _raidPublishList.Contains(raidParty);

    public void RegisterRaidInRaidPublishList(RaidParty raidParty)
    {
        if (_raidPublishList.Contains(raidParty))
        {
            return;
        }

        _raidPublishList.Add(raidParty);
    }

    public void UnregisterRaidFromRaidPublishList(RaidParty raidParty)
    {
        _raidPublishList.Remove(raidParty);
    }
}