using WingsEmu.DTOs.BCards;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Helpers.Damages;

namespace WingsEmu.Game.Buffs;

public interface IBCardEventContextFactory
{
    IBCardEffectContext NewContext(IBattleEntity sender, IBattleEntity target, BCardDTO bCard, SkillInfo skill = null, Position position = default);
}