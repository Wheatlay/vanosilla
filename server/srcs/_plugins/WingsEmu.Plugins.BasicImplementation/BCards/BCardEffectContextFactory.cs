using WingsEmu.DTOs.BCards;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Helpers.Damages;

namespace WingsEmu.Plugins.BasicImplementations.BCards;

public class BCardEffectContextFactory : IBCardEventContextFactory
{
    public IBCardEffectContext NewContext(IBattleEntity sender, IBattleEntity target, BCardDTO bcard, SkillInfo skill = null, Position position = default)
        => new BcardEffectContext(sender, target, bcard, skill, position);
}