// WingsEmu
// 
// Developed by NosWings Team

using System;
using WingsEmu.DTOs.BCards;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.BCards.Handlers;

public class BCardRecoveryAndDamagePercentHandler : IBCardEffectAsyncHandler
{
    public BCardType HandledType => BCardType.RecoveryAndDamagePercent;

    public void Execute(IBCardEffectContext ctx)
    {
        IBattleEntity sender = ctx.Sender;
        IBattleEntity target = ctx.Target;
        sender ??= target;

        BCardDTO bCard = ctx.BCard;
        int firstDataValue = bCard.FirstDataValue(sender.Level);
        switch (ctx.BCard.SubType)
        {
            case (byte)AdditionalTypes.RecoveryAndDamagePercent.HPRecovered:
                int heal = (int)(target.MaxHp * (firstDataValue * 0.01));
                target.EmitEvent(new BattleEntityHealEvent
                {
                    Entity = target,
                    HpHeal = heal
                });

                break;
            case (byte)AdditionalTypes.RecoveryAndDamagePercent.HPReduced:
                int damage = (int)(target.MaxHp * (firstDataValue * 0.01));
                if (target.Hp - damage <= 1)
                {
                    damage = Math.Abs(target.Hp - 1);
                }

                target.Hp -= damage;
                if (damage == 0)
                {
                    return;
                }

                target.BroadcastDamage(damage);
                break;
            case (byte)AdditionalTypes.RecoveryAndDamagePercent.MPRecovered:
                int mpRegen = (int)(target.MaxMp * (firstDataValue * 0.01));

                if (target.Mp + mpRegen > target.MaxMp)
                {
                    target.Mp = target.MaxMp;
                }
                else
                {
                    target.Mp += mpRegen;
                }

                break;
            case (byte)AdditionalTypes.RecoveryAndDamagePercent.MPReduced:
                int mpDamage = (int)(target.MaxMp * (firstDataValue * 0.01));
                if (target.Mp - mpDamage <= 1)
                {
                    mpDamage = Math.Abs(target.Mp - 1);
                }

                target.Mp -= mpDamage;
                break;
        }

        if (target is not IPlayerEntity playerEntity)
        {
            return;
        }

        playerEntity.Session.RefreshStat();
    }
}