using WingsEmu.DTOs.BCards;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Entities;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.BCards.Handlers;

public class BCardModeHandler : IBCardEffectAsyncHandler
{
    public BCardType HandledType => BCardType.Mode;

    public void Execute(IBCardEffectContext ctx)
    {
        IBattleEntity sender = ctx.Sender;

        if (!(sender is IMonsterEntity monsterEntity))
        {
            return;
        }

        BCardDTO bCardDto = ctx.BCard;
        int firstData = bCardDto.FirstDataValue(sender.Level);

        switch ((AdditionalTypes.Mode)bCardDto.SubType)
        {
            case AdditionalTypes.Mode.Range:

                monsterEntity.BasicSkill.Range = (byte)firstData;

                break;
            case AdditionalTypes.Mode.ReturnRange:

                monsterEntity.BasicSkill.Range = monsterEntity.BasicRange;

                break;
            case AdditionalTypes.Mode.AttackTimeIncreased:

                monsterEntity.BasicSkill.Cooldown = (short)(monsterEntity.BasicSkill.Cooldown + firstData);

                break;
            case AdditionalTypes.Mode.AttackTimeDecreased:

                monsterEntity.BasicSkill.Cooldown = (short)(monsterEntity.BasicSkill.Cooldown - firstData);

                break;
        }
    }
}