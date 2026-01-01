using WingsAPI.Packets.Enums.Shells;
using WingsEmu.Game._enum;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Mates;
using WingsEmu.Packets.Enums;
using WingsEmu.Plugins.BasicImplementations.Event.Algorithm;

namespace WingsEmu.Plugins.BasicImplementations.Algorithms;

public static class ExperienceExtension
{
    public static ExcessExperience GetMoreExperience(this IPlayerEntity character, IServerManager serverManager)
    {
        double lowLevel = 1;
        double lowJob = 1;
        double lowJobSp = 1;
        double mates = 1;

        lowLevel = character.Level switch
        {
            <= 5 => 3,
            <= 18 => 2,
            _ => lowLevel
        };

        lowJob = character.Level switch
        {
            <= 12 => 3,
            <= 20 => 2,
            _ => lowJob
        };

        double additionalLevel = 1;
        double additionalJob = 1;
        double additionalHeroLevel = 1;
        double additionalMateLevel = 1;
        double additionalPartnerLevel = 1;
        double additionalSp = 0;

        int increaseExperienceBuff = character.BCardComponent.GetAllBCardsInformation(BCardType.Item, (byte)AdditionalTypes.Item.EXPIncreased, character.Level).firstData;

        additionalLevel += increaseExperienceBuff * 0.01;
        additionalJob += increaseExperienceBuff * 0.01;

        int increaseHeroExperienceBuff = character.BCardComponent.GetAllBCardsInformation(BCardType.ReputHeroLevel, (byte)AdditionalTypes.ReputHeroLevel.ReceivedHeroExpIncrease, character.Level)
            .firstData;

        additionalHeroLevel += increaseHeroExperienceBuff * 0.01;

        if (character.HasBuff(BuffVnums.GUARDIAN_BLESS))
        {
            additionalMateLevel += 0.5;
            additionalPartnerLevel += 0.5;
        }

        if (character.HasBuff(BuffVnums.SOULSTONE_BLESSING))
        {
            additionalSp += 0.5;
        }

        if (character.HasBuff(BuffVnums.FAMILY_BUFF_XP))
        {
            additionalLevel += 0.1; //TODO: check, if buff.familyId == character.Family.Id
        }

        additionalLevel += character.GetMaxWeaponShellValue(ShellEffectType.GainMoreXP, true) * 0.01;
        additionalJob += character.GetMaxWeaponShellValue(ShellEffectType.GainMoreCXP, true) * 0.01;
        double additionalJobSp = additionalJob + additionalSp;

        IMateEntity mate = character.MateComponent.GetMate(x => x.MateType == MateType.Pet && x.IsTeamMember);
        IMateEntity partner = character.MateComponent.GetMate(x => x.MateType == MateType.Partner && x.IsTeamMember);

        if (mate != null && partner == null)
        {
            mates = mate.IsAlive() ? 1.045 : 0.95;
        }

        if (mate == null && partner != null)
        {
            mates = partner.IsAlive() ? 1.0625 : 0.85;
        }

        if (mate != null && partner != null)
        {
            if (mate.IsAlive() && partner.IsAlive())
            {
                mates = 1.08;
            }
            else if (mate.IsAlive() && !partner.IsAlive())
            {
                mates = 0.88;
            }
            else if (!mate.IsAlive() && partner.IsAlive())
            {
                mates = 1;
            }
            else
            {
                mates = 0.8;
            }
        }

        additionalLevel *= serverManager.MobXpRate;
        additionalJob *= serverManager.JobXpRate;
        additionalJobSp *= serverManager.JobXpRate;
        additionalMateLevel *= serverManager.MateXpRate;
        additionalPartnerLevel *= serverManager.PartnerXpRate;

        if (character.Specialist != null && character.UseSp)
        {
            additionalJob = character.Specialist.SpLevel < 20 ? 0 : additionalJobSp / 2.0;

            lowJobSp = character.Specialist.SpLevel switch
            {
                <= 9 => 10,
                <= 17 => 5,
                _ => lowJobSp
            };
        }
        else
        {
            additionalJobSp = 0;
        }

        return new ExcessExperience(additionalLevel, additionalJob, additionalJobSp, additionalHeroLevel, additionalMateLevel, additionalPartnerLevel, lowLevel, lowJob, lowJobSp, mates);
    }
}