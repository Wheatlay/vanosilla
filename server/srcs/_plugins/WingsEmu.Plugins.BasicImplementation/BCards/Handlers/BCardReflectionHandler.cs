using WingsEmu.Game;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Entities;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.BCards.Handlers;

public class BCardReflectionHandler : IBCardEffectAsyncHandler
{
    private readonly IRandomGenerator _random;

    public BCardReflectionHandler(IRandomGenerator random) => _random = random;

    public BCardType HandledType => BCardType.Reflection;

    public void Execute(IBCardEffectContext ctx)
    {
        IBattleEntity target = ctx.Target;
        byte subType = ctx.BCard.SubType;
        int firstData = ctx.BCard.FirstData;
        int secondData = ctx.BCard.SecondData;

        int randomNumber = _random.RandomNumber();
        if (randomNumber > firstData)
        {
            return;
        }

        switch (subType)
        {
            case (byte)AdditionalTypes.Reflection.ChanceMpLost:
                int loss = (int)(target.Mp * (secondData * 0.01));
                target.Mp = target.Mp - loss <= 0 ? 0 : target.Mp - loss;
                break;
        }
    }
}