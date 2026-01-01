// WingsEmu
// 
// Developed by NosWings Team

using System.Collections.Generic;
using WingsEmu.Game;
using WingsEmu.Game._enum;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Entities.Extensions;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Networking.Broadcasting;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.BCards.Handlers;

public class BCardSpecialEffectHandler : IBCardEffectAsyncHandler
{
    private readonly IGibberishConfig _gibberishConfig;
    private readonly IRandomGenerator _randomGenerator;

    public BCardSpecialEffectHandler(IRandomGenerator randomGenerator, IGibberishConfig gibberishConfig)
    {
        _randomGenerator = randomGenerator;
        _gibberishConfig = gibberishConfig;
    }

    public BCardType HandledType => BCardType.SpecialEffects;

    public void Execute(IBCardEffectContext ctx)
    {
        IBattleEntity target = ctx.Target;
        IBattleEntity sender = ctx.Sender;
        byte subType = ctx.BCard.SubType;
        int firstData = ctx.BCard.FirstDataValue(sender.Level);
        int secondData = ctx.BCard.SecondDataValue(sender.Level);

        switch ((AdditionalTypes.SpecialEffects)subType)
        {
            case AdditionalTypes.SpecialEffects.DecreaseKillerHP:
                if (target.Killer == null)
                {
                    return;
                }

                IBattleEntity killer = target.Killer;

                if (killer.MapInstance.Id != target.MapInstance.Id)
                {
                    return;
                }

                if (killer.BCardComponent.HasBCard(BCardType.Buff, (byte)AdditionalTypes.Buff.EffectResistance) && sender.IsMandra())
                {
                    return;
                }

                int removeHp = (int)(killer.Hp * (firstData * 0.01));
                killer.Hp = killer.Hp - removeHp <= 0 ? 1 : killer.Hp - removeHp;
                killer.BroadcastEffectInRange(EffectType.DecreaseHp);
                if (killer is IPlayerEntity characterKiller)
                {
                    characterKiller.Session.RefreshStat();
                }

                break;
            case AdditionalTypes.SpecialEffects.ToNonPrefferedAttack:
                int randomNumber = _randomGenerator.RandomNumber();
                if (randomNumber > firstData)
                {
                    return;
                }

                if (sender is IMonsterEntity { Target: { } } monsterEntity)
                {
                    monsterEntity.MapInstance.RemoveTarget(monsterEntity, monsterEntity.Target);
                }

                if (target is IMonsterEntity { Target: { } } monsterTarget)
                {
                    monsterTarget.MapInstance.RemoveTarget(monsterTarget, monsterTarget.Target);
                }

                break;
            case AdditionalTypes.SpecialEffects.Gibberish:

                IReadOnlyList<string> getConfig = _gibberishConfig.GetKeysById(firstData);
                if (getConfig.Count == 0)
                {
                    return;
                }

                string message = getConfig[_randomGenerator.RandomNumber(getConfig.Count)];

                target.MapInstance.Broadcast(x => target.GenerateSayPacket(x.GetLanguage(message), ChatMessageColorType.PlayerSay),
                    new RangeBroadcast(target.PositionX, target.PositionY, 30));
                break;
        }
    }
}