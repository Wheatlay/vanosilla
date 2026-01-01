using System;
using WingsEmu.DTOs.Skills;
using WingsEmu.Game.Managers.StaticData;

namespace WingsEmu.Game.Skills;

public class PartnerSkill : PartnerSkillDTO, IBattleEntitySkill
{
    private SkillDTO _skill;

    public DateTime LastUse { get; set; } = DateTime.MinValue;
    public short Rate { get; } = 100;

    public SkillDTO Skill => _skill ??= StaticSkillsManager.Instance.GetSkill(SkillId);
}