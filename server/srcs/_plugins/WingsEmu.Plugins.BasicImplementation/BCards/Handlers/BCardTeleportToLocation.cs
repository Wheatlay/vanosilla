using WingsEmu.Game.Buffs;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.BCards.Handlers;

public class BCardTeleportToLocation : IBCardEffectAsyncHandler
{
    public BCardType HandledType => BCardType.FairyXPIncrease;

    public void Execute(IBCardEffectContext ctx)
    {
        IBattleEntity sender = ctx.Sender;
        IBattleEntity target = ctx.Target;
        Position position = ctx.Position;

        switch (ctx.BCard.SubType)
        {
            case (byte)AdditionalTypes.FairyXPIncrease.TeleportToLocation:
                sender.ChangePosition(position);
                sender.TeleportOnMap(sender.PositionX, sender.PositionY);
                break;
        }
    }
}