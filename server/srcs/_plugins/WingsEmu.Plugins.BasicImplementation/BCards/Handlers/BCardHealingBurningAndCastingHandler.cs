// WingsEmu
// 
// Developed by NosWings Team

using PhoenixLib.Events;
using WingsEmu.DTOs.BCards;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.BCards.Handlers;

public class BCardHealingBurningAndCastingHandler : IBCardEffectAsyncHandler
{
    private readonly IAsyncEventPipeline _asyncEventPipeline;

    public BCardHealingBurningAndCastingHandler(IAsyncEventPipeline asyncEventPipeline) => _asyncEventPipeline = asyncEventPipeline;

    public BCardType HandledType => BCardType.HealingBurningAndCasting;

    public void Execute(IBCardEffectContext ctx)
    {
        IBattleEntity target = ctx.Target;
        IBattleEntity sender = ctx.Sender;
        byte subType = ctx.BCard.SubType;

        if (sender?.MapInstance == null)
        {
            return;
        }

        if (target?.MapInstance == null)
        {
            return;
        }

        BCardDTO bCard = ctx.BCard;
        int firstDataValue = bCard.FirstDataValue(sender.Level);

        if (!target.IsAlive())
        {
            return;
        }

        if (target.IsMateTrainer())
        {
            return;
        }

        switch ((AdditionalTypes.HealingBurningAndCasting)subType)
        {
            case AdditionalTypes.HealingBurningAndCasting.RestoreHP:
            case AdditionalTypes.HealingBurningAndCasting.RestoreHPWhenCasting:
                target.EmitEvent(new BattleEntityHealEvent
                {
                    Entity = target,
                    HpHeal = firstDataValue
                });

                break;
            case AdditionalTypes.HealingBurningAndCasting.RestoreMP:
                if (target.Mp + firstDataValue < target.MaxMp)
                {
                    target.Mp += firstDataValue;
                }
                else
                {
                    target.Mp = target.MaxMp;
                }

                break;
            case AdditionalTypes.HealingBurningAndCasting.DecreaseHP:
                int damage = firstDataValue;

                if (target.Hp - damage <= 0)
                {
                    if (target.Hp != 1)
                    {
                        target.BroadcastDamage(target.Hp - 1);
                    }

                    target.Hp = 1;
                }
                else
                {
                    target.BroadcastDamage(damage);
                    target.Hp -= damage;
                }

                break;
            case AdditionalTypes.HealingBurningAndCasting.DecreaseMP:

                int mpDecrease = firstDataValue;
                target.Mp = target.Mp - mpDecrease <= 0 ? 1 : target.Mp - mpDecrease;
                break;
        }

        if (target is not IPlayerEntity targetPlayer)
        {
            return;
        }

        targetPlayer.Session?.RefreshStat();
    }
}