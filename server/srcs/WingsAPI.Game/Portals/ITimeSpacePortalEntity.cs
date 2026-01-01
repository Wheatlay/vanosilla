using System.Collections.Generic;
using WingsEmu.Game.Helpers.Damages;

namespace WingsEmu.Game.Portals;

public interface ITimeSpacePortalEntity
{
    public long TimeSpaceId { get; }
    public Position Position { get; }
    public bool IsHero { get; }
    public bool IsSpecial { get; }
    public bool IsHidden { get; }
    public byte MinLevel { get; }
    public byte MaxLevel { get; }
    public byte SeedsOfPowerRequired { get; }
    public string Name { get; }
    public string Description { get; }

    public List<(short, short)> DrawRewards { get; }
    public List<(short, short)> SpecialRewards { get; }
    public List<(short, short)> BonusRewards { get; }

    public long? GroupId { get; }
}