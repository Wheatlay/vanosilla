using WingsEmu.DTOs.BCards;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Helpers.Damages;

namespace WingsEmu.Plugins.BasicImplementations.BCards;

public class BcardEffectContext : IBCardEffectContext
{
    public BcardEffectContext(IBattleEntity sender, IBattleEntity target, BCardDTO bCard, SkillInfo skill = null, Position position = default)
    {
        Sender = sender;
        Target = target;
        BCard = bCard;
        Skill = skill;
        Position = position;
    }

    public IBattleEntity Sender { get; }
    public IBattleEntity Target { get; }
    public BCardDTO BCard { get; }
    public SkillInfo Skill { get; }
    public Position Position { get; }
}