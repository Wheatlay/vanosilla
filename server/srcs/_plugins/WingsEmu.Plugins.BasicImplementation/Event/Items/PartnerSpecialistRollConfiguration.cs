using System.Collections.Generic;
using System.Linq;
using WingsEmu.Game;

namespace WingsEmu.Plugins.BasicImplementations.Event.Items;

public interface IPartnerSpecialistSkillRoll
{
    byte GetRandomSkillRank();
}

public class PartnerSpecialistSkillRoll : IPartnerSpecialistSkillRoll
{
    private readonly RandomBag<PartnerSpecialistSkillChances> _randomRanks;

    public PartnerSpecialistSkillRoll(PartnerSpecialistSkillRollConfiguration randomRarities, IRandomGenerator randomGenerator)
    {
        var skillRanks = randomRarities.OrderBy(s => s.Chance).ToList();
        _randomRanks = new RandomBag<PartnerSpecialistSkillChances>(randomGenerator);

        foreach (PartnerSpecialistSkillChances skill in skillRanks)
        {
            _randomRanks.AddEntry(skill, skill.Chance);
        }
    }

    public byte GetRandomSkillRank() => _randomRanks.GetRandom().SkillRank;
}

public class PartnerSpecialistSkillRollConfiguration : List<PartnerSpecialistSkillChances>
{
}

public class PartnerSpecialistSkillChances
{
    public int Chance { get; set; }
    public byte SkillRank { get; set; }
}