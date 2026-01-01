// WingsEmu
// 
// Developed by NosWings Team

using WingsEmu.DTOs.BCards;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.BCards.Handlers;

public class BCardDrainHandler : IBCardEffectAsyncHandler
{
    public BCardType HandledType => BCardType.Drain;

    public void Execute(IBCardEffectContext ctx)
    {
        IBattleEntity target = ctx.Target;
        IBattleEntity sender = ctx.Sender;
        byte subType = ctx.BCard.SubType;
        BCardDTO bCard = ctx.BCard;

        int firstDataValue = bCard.FirstDataValue(sender.Level);

        switch ((AdditionalTypes.Drain)subType)
        {
            case AdditionalTypes.Drain.TransferEnemyHPNegated:
            case AdditionalTypes.Drain.TransferEnemyHP:
                if (target.Hp - firstDataValue <= 0)
                {
                    if (target.Hp != 1)
                    {
                        target.BroadcastDamage(target.Hp);
                    }

                    target.Hp = 1;
                }
                else
                {
                    target.BroadcastDamage(firstDataValue);
                    target.Hp -= firstDataValue;
                }

                if (sender.MapInstance?.Id != target.MapInstance?.Id)
                {
                    return;
                }

                sender.EmitEvent(new BattleEntityHealEvent
                {
                    Entity = sender,
                    HpHeal = firstDataValue
                });
                break;
        }
    }
}