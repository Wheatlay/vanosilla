using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Mates.Events;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.Event.Characters;

public class BattleEntityHealEventHandler : IAsyncEventProcessor<BattleEntityHealEvent>
{
    public async Task HandleAsync(BattleEntityHealEvent e, CancellationToken cancellation)
    {
        IBattleEntity battleEntity = e.Entity;

        if (!battleEntity.IsAlive())
        {
            return;
        }

        int maxHp = battleEntity.MaxHp;
        int maxMp = battleEntity.MaxMp;

        int hpHeal = battleEntity.Hp + e.HpHeal > maxHp ? maxHp - battleEntity.Hp : e.HpHeal;
        int mpHeal = battleEntity.Mp + e.MpHeal > maxMp ? maxMp - battleEntity.Mp : e.MpHeal;

        (int hpToDecrease, int _) =
            battleEntity.BCardComponent.GetAllBCardsInformation(BCardType.DamageConvertingSkill, (byte)AdditionalTypes.DamageConvertingSkill.HPRecoveryDecreased, battleEntity.Level);

        (int hpToIncrease, int _) =
            battleEntity.BCardComponent.GetAllBCardsInformation(BCardType.DamageConvertingSkill, (byte)AdditionalTypes.DamageConvertingSkill.HPRecoveryIncreased, battleEntity.Level);

        int hpToChange = hpToIncrease - hpToDecrease;
        hpHeal = (int)(hpHeal * (1 + hpToChange / 100.0));

        battleEntity.Hp += hpHeal;
        battleEntity.Mp += mpHeal;

        battleEntity.BroadcastHeal(hpHeal);

        if (battleEntity is not IPlayerEntity playerEntity)
        {
            return;
        }

        IClientSession session = playerEntity.Session;

        session.RefreshStat();
        session.RefreshStatInfo();

        if (!e.HealMates)
        {
            return;
        }

        foreach (IMateEntity mate in session.PlayerEntity.MateComponent.TeamMembers())
        {
            (int mateHpToDecrease, int _) =
                mate.BCardComponent.GetAllBCardsInformation(BCardType.DamageConvertingSkill, (byte)AdditionalTypes.DamageConvertingSkill.HPRecoveryDecreased, mate.Level);

            (int mateHpToIncrease, int _) =
                mate.BCardComponent.GetAllBCardsInformation(BCardType.DamageConvertingSkill, (byte)AdditionalTypes.DamageConvertingSkill.HPRecoveryIncreased, mate.Level);

            int mateHpToChange = mateHpToDecrease - mateHpToIncrease;
            int mateHpHeal = (int)(e.HpHeal * (1 + mateHpToChange / 100.0));

            await session.EmitEventAsync(new MateHealEvent
            {
                MateEntity = mate,
                HpHeal = mateHpHeal,
                MpHeal = e.MpHeal
            });
        }
    }
}