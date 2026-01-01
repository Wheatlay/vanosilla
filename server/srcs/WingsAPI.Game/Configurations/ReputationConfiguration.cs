using System.Collections.Generic;
using System.Linq;
using WingsEmu.Game._enum;

namespace WingsEmu.Game.Configurations;

public interface IReputationConfiguration
{
    public ReputationInfo GetReputationInfo(long reputation, int place);
}

public class ReputationConfiguration : IReputationConfiguration
{
    private readonly List<ReputationInfo> _reputationInfos;

    public ReputationConfiguration(IEnumerable<ReputationInfo> reputationInfos)
    {
        _reputationInfos = reputationInfos.OrderByDescending(s => s.Threshold).ToList();
    }

    public ReputationInfo GetReputationInfo(long reputation, int place)
    {
        var reputationInfo = _reputationInfos.Where(s => reputation >= s.Threshold).ToList();
        return reputationInfo.FirstOrDefault(s => place >= s.MaxPlayers && place <= s.MinPlayers) ?? reputationInfo.FirstOrDefault();
    }
}

public class ReputationInfo
{
    public ReputationType Rank { get; set; }
    public long Threshold { get; set; }
    public int? MinPlayers { get; set; }
    public int? MaxPlayers { get; set; }
}