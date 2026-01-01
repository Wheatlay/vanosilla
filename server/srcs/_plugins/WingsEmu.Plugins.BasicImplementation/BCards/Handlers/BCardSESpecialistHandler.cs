using System.Linq;
using WingsEmu.DTOs.BCards;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Entities;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.BCards.Handlers;

public class BCardSESpecialistHandler : IBCardEffectAsyncHandler
{
    private readonly IBuffFactory _buffFactory;

    public BCardSESpecialistHandler(IBuffFactory buffFactory) => _buffFactory = buffFactory;

    public BCardType HandledType => BCardType.SESpecialist;

    public void Execute(IBCardEffectContext ctx)
    {
        IBattleEntity sender = ctx.Sender;
        BCardDTO bCard = ctx.BCard;

        int firstDataValue = bCard.FirstDataValue(sender.Level);
        int secondDataValue = bCard.SecondDataValue(sender.Level);

        switch (ctx.BCard.SubType)
        {
            case (byte)AdditionalTypes.SESpecialist.EnterNumberOfBuffsAndDamage:
                bool alreadyHaveBuffDamage = sender.EndBuffDamages.Any(x => x.Key == firstDataValue);
                if (alreadyHaveBuffDamage)
                {
                    sender.RemoveEndBuffDamage((short)firstDataValue);
                }

                sender.AddEndBuff((short)firstDataValue, secondDataValue * 1000);
                break;
        }
    }
}