using System.Collections.Generic;
using System.Linq;
using WingsAPI.Data.Families;
using WingsAPI.Packets.Enums.Shells;
using WingsEmu.Core.Extensions;
using WingsEmu.DTOs.BCards;
using WingsEmu.DTOs.Skills;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Extensions;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Character;

namespace WingsEmu.Game.EntityStatistics;

public class PlayerStatisticsComponent : IPlayerStatisticsComponent
{
    private readonly Dictionary<PassiveType, int> _passive = new();
    private readonly IPlayerEntity _playerEntity;
    private readonly Dictionary<Statistics, int> _stats = new();

    public PlayerStatisticsComponent(IPlayerEntity playerEntity) => _playerEntity = playerEntity;

    public IReadOnlyDictionary<PassiveType, int> Passives => _passive;

    public void RefreshPassives()
    {
        _passive.Clear();
        IEnumerable<SkillDTO> passiveSkills = _playerEntity.CharacterSkills.Values.Where(x => x?.Skill != null && x.Skill.IsPassiveSkill()).Select(x => x.Skill);

        int hp = 0;
        int mp = 0;
        int meleeAttack = 0;
        int rangedAttack = 0;
        int magicAttack = 0;
        int regenHp = 0;
        int regenMp = 0;
        int passiveRegen = 1;
        int meleeDefence = 0;
        int rangedDefence = 0;
        int magicDefence = 0;
        int meleeHitrate = 0;
        int rangedHitrate = 0;
        int meleeDodge = 0;
        int rangedDodge = 0;

        foreach (SkillDTO skill in passiveSkills)
        {
            switch (skill.CastId)
            {
                case 0:
                    meleeAttack += skill.UpgradeSkill;
                    meleeDefence += skill.UpgradeSkill / 2;
                    break;
                case 1:
                    rangedAttack += skill.UpgradeSkill;
                    rangedDefence += skill.UpgradeSkill / 2;
                    meleeHitrate += skill.UpgradeSkill * 2;
                    rangedHitrate += skill.UpgradeSkill * 2;
                    meleeDodge += skill.UpgradeSkill;
                    rangedDodge += skill.UpgradeSkill;
                    break;
                case 2:
                    magicAttack += skill.UpgradeSkill;
                    magicDefence += skill.UpgradeSkill / 2;
                    break;
                case 4:
                    hp += skill.UpgradeSkill;
                    break;
                case 5:
                    mp += skill.UpgradeSkill;
                    break;
                case 6:
                    meleeAttack += skill.UpgradeSkill;
                    rangedAttack += skill.UpgradeSkill;
                    magicAttack += skill.UpgradeSkill;
                    break;
                case 7:
                    meleeDefence += skill.UpgradeSkill;
                    rangedDefence += skill.UpgradeSkill;
                    magicDefence += skill.UpgradeSkill;
                    break;
                case 8:
                    regenHp += skill.UpgradeSkill;
                    break;
                case 9:
                    regenMp += skill.UpgradeSkill;
                    break;
                case 10:
                    passiveRegen += skill.UpgradeSkill;
                    break;
            }
        }

        _passive[PassiveType.HP] = hp;
        _passive[PassiveType.MP] = mp;
        _passive[PassiveType.MELEE_ATTACK] = meleeAttack;
        _passive[PassiveType.RANGED_ATTACK] = rangedAttack;
        _passive[PassiveType.MAGIC_ATTACK] = magicAttack;
        _passive[PassiveType.REGEN_HP] = regenHp;
        _passive[PassiveType.REGEN_MP] = regenMp;
        _passive[PassiveType.PASSIVE_REGEN] = passiveRegen;
        _passive[PassiveType.MELEE_DEFENCE] = meleeDefence;
        _passive[PassiveType.RANGED_DEFENCE] = rangedDefence;
        _passive[PassiveType.MAGIC_DEFENCE] = magicDefence;
        _passive[PassiveType.MELEE_HIRATE] = meleeHitrate;
        _passive[PassiveType.RANGED_HIRATE] = rangedHitrate;
        _passive[PassiveType.MELEE_DODGE] = meleeDodge;
        _passive[PassiveType.RANGED_DODGE] = rangedDodge;
    }

    public int MinDamage => _playerEntity.DamagesMinimum + _stats.GetValueOrDefault(Statistics.MIN_DAMAGE);
    public int MaxDamage => _playerEntity.DamagesMaximum + _stats.GetValueOrDefault(Statistics.MAX_DAMAGE);
    public int HitRate => _playerEntity.HitRate + _stats.GetValueOrDefault(Statistics.HITRATE);
    public int CriticalChance => _playerEntity.HitCriticalChance + _stats.GetValueOrDefault(Statistics.CRITICAL_CHANCE);
    public int CriticalDamage => _playerEntity.HitCriticalDamage + _stats.GetValueOrDefault(Statistics.CRITICAL_DAMAGE);
    public int SecondMinDamage => _playerEntity.SecondDamageMinimum + _stats.GetValueOrDefault(Statistics.SECOND_MIN_DAMAGE);
    public int SecondMaxDamage => _playerEntity.SecondDamageMaximum + _stats.GetValueOrDefault(Statistics.SECOND_MAX_DAMAGE);
    public int SecondHitRate => _playerEntity.SecondHitRate + _stats.GetValueOrDefault(Statistics.SECOND_HITRATE);
    public int SecondCriticalChance => _playerEntity.SecondHitCriticalChance + _stats.GetValueOrDefault(Statistics.SECOND_CRITICAL_CHANCE);
    public int SecondCriticalDamage => _playerEntity.SecondHitCriticalDamage + _stats.GetValueOrDefault(Statistics.SECOND_CRITICAL_DAMAGE);
    public int MeleeDefense => _playerEntity.MeleeDefence + _stats.GetValueOrDefault(Statistics.MELEE_DEFENSE);
    public int RangeDefense => _playerEntity.RangedDefence + _stats.GetValueOrDefault(Statistics.RANGE_DEFENSE);
    public int MagicDefense => _playerEntity.MagicDefence + _stats.GetValueOrDefault(Statistics.MAGIC_DEFENSE);
    public int MeleeDodge => _playerEntity.MeleeDodge + _stats.GetValueOrDefault(Statistics.MELEE_DODGE);
    public int RangeDodge => _playerEntity.RangedDodge + _stats.GetValueOrDefault(Statistics.RANGE_DODGE);
    public int FireResistance => _playerEntity.FireResistance + _stats.GetValueOrDefault(Statistics.FIRE_RESISTANCE);
    public int WaterResistance => _playerEntity.WaterResistance + _stats.GetValueOrDefault(Statistics.WATER_RESISTANCE);
    public int LightResistance => _playerEntity.LightResistance + _stats.GetValueOrDefault(Statistics.LIGHT_RESISTANCE);
    public int ShadowResistance => _playerEntity.DarkResistance + _stats.GetValueOrDefault(Statistics.SHADOW_RESISTANCE);

    public void RefreshPlayerStatistics()
    {
        _stats.Clear();
        int minDamage = 0;
        int maxDamage = 0;
        int hitRate = 0;
        int criticalChance = 0;
        int criticalDamage = 0;
        int secondMinDamage = 0;
        int secondMaxDamage = 0;
        int secondHitRate = 0;
        int secondCriticalChance = 0;
        int secondCriticalDamage = 0;
        int meleeDefense = 0;
        int rangeDefense = 0;
        int magicDefense = 0;
        int meleeDodge = 0;
        int rangeDodge = 0;
        int fireResistance = 0;
        int waterResistance = 0;
        int lightResistance = 0;
        int shadowResistance = 0;

        ClassType classType = _playerEntity.Class;

        byte playerLevel = _playerEntity.Level;

        IReadOnlyList<BCardDTO> bCards = _playerEntity.BCardComponent.GetAllBCards();
        IEnumerable<BCardDTO> minMaxDamageBCards = bCards.Where(x => x.Type == (short)BCardType.AttackPower);
        IEnumerable<BCardDTO> hitRateBCards = bCards.Where(x => x.Type == (short)BCardType.Target);
        IEnumerable<BCardDTO> criticalBCards = bCards.Where(x => x.Type == (short)BCardType.Critical);
        IEnumerable<BCardDTO> defenseBCards = bCards.Where(x => x.Type == (short)BCardType.Defence);
        IEnumerable<BCardDTO> dodgeBCards = bCards.Where(x => x.Type == (short)BCardType.DodgeAndDefencePercent);
        IEnumerable<BCardDTO> resistanceBCards = bCards.Where(x => x.Type == (short)BCardType.ElementResistance);

        foreach (BCardDTO bCard in minMaxDamageBCards)
        {
            int firstData = bCard.FirstDataValue(playerLevel);

            switch ((AdditionalTypes.AttackPower)bCard.SubType)
            {
                case AdditionalTypes.AttackPower.AllAttacksIncreased:
                    minDamage += firstData;
                    maxDamage += firstData;
                    secondMinDamage += firstData;
                    secondMaxDamage += firstData;
                    break;
                case AdditionalTypes.AttackPower.AllAttacksDecreased:
                    minDamage -= firstData;
                    maxDamage -= firstData;
                    secondMinDamage -= firstData;
                    secondMaxDamage -= firstData;
                    break;
                case AdditionalTypes.AttackPower.MeleeAttacksIncreased:
                    minDamage += classType switch
                    {
                        ClassType.Adventurer => firstData,
                        ClassType.Swordman => firstData,
                        ClassType.Wrestler => firstData,
                        _ => 0
                    };

                    maxDamage += classType switch
                    {
                        ClassType.Adventurer => firstData,
                        ClassType.Swordman => firstData,
                        ClassType.Wrestler => firstData,
                        _ => 0
                    };

                    secondMinDamage += classType switch
                    {
                        ClassType.Archer => firstData,
                        _ => 0
                    };

                    secondMaxDamage += classType switch
                    {
                        ClassType.Archer => firstData,
                        _ => 0
                    };
                    break;
                case AdditionalTypes.AttackPower.MeleeAttacksDecreased:
                    minDamage -= classType switch
                    {
                        ClassType.Adventurer => firstData,
                        ClassType.Swordman => firstData,
                        ClassType.Wrestler => firstData,
                        _ => 0
                    };

                    maxDamage -= classType switch
                    {
                        ClassType.Adventurer => firstData,
                        ClassType.Swordman => firstData,
                        ClassType.Wrestler => firstData,
                        _ => 0
                    };

                    secondMinDamage -= classType switch
                    {
                        ClassType.Archer => firstData,
                        _ => 0
                    };

                    secondMaxDamage -= classType switch
                    {
                        ClassType.Archer => firstData,
                        _ => 0
                    };
                    break;
                case AdditionalTypes.AttackPower.RangedAttacksIncreased:
                    minDamage += classType switch
                    {
                        ClassType.Archer => firstData,
                        _ => 0
                    };

                    maxDamage += classType switch
                    {
                        ClassType.Archer => firstData,
                        _ => 0
                    };

                    if (classType == ClassType.Archer)
                    {
                        break;
                    }

                    secondMinDamage += firstData;
                    secondMaxDamage += firstData;
                    break;
                case AdditionalTypes.AttackPower.RangedAttacksDecreased:
                    minDamage -= classType switch
                    {
                        ClassType.Archer => firstData,
                        _ => 0
                    };

                    maxDamage -= classType switch
                    {
                        ClassType.Archer => firstData,
                        _ => 0
                    };

                    if (classType == ClassType.Archer)
                    {
                        break;
                    }

                    secondMinDamage -= firstData;
                    secondMaxDamage -= firstData;
                    break;
                case AdditionalTypes.AttackPower.MagicalAttacksIncreased:
                    if (classType != ClassType.Magician)
                    {
                        break;
                    }

                    minDamage += firstData;
                    maxDamage += firstData;
                    break;
                case AdditionalTypes.AttackPower.MagicalAttacksDecreased:
                    if (classType != ClassType.Magician)
                    {
                        break;
                    }

                    minDamage -= firstData;
                    maxDamage -= firstData;
                    break;
            }
        }

        foreach (BCardDTO bCard in hitRateBCards)
        {
            int firstData = bCard.FirstDataValue(playerLevel);
            switch ((AdditionalTypes.Target)bCard.SubType)
            {
                case AdditionalTypes.Target.AllHitRateIncreased:
                    hitRate += firstData;
                    secondHitRate += firstData;
                    break;
                case AdditionalTypes.Target.AllHitRateDecreased:
                    hitRate -= firstData;
                    secondHitRate -= firstData;
                    break;
                case AdditionalTypes.Target.MeleeHitRateIncreased:
                    hitRate += classType switch
                    {
                        ClassType.Adventurer => firstData,
                        ClassType.Swordman => firstData,
                        ClassType.Wrestler => firstData,
                        _ => 0
                    };

                    secondHitRate += classType switch
                    {
                        ClassType.Archer => firstData,
                        _ => 0
                    };
                    break;
                case AdditionalTypes.Target.MeleeHitRateDecreased:
                    hitRate -= classType switch
                    {
                        ClassType.Adventurer => firstData,
                        ClassType.Swordman => firstData,
                        ClassType.Wrestler => firstData,
                        _ => 0
                    };

                    secondHitRate -= classType switch
                    {
                        ClassType.Archer => firstData,
                        _ => 0
                    };
                    break;
                case AdditionalTypes.Target.RangedHitRateIncreased:
                    hitRate += classType switch
                    {
                        ClassType.Archer => firstData,
                        _ => 0
                    };

                    if (classType == ClassType.Archer)
                    {
                        break;
                    }

                    secondHitRate += firstData;
                    break;
                case AdditionalTypes.Target.RangedHitRateDecreased:
                    hitRate -= classType switch
                    {
                        ClassType.Archer => firstData,
                        _ => 0
                    };

                    if (classType == ClassType.Archer)
                    {
                        break;
                    }

                    secondHitRate -= firstData;
                    break;
                case AdditionalTypes.Target.MagicalConcentrationIncreased:
                    if (classType != ClassType.Magician)
                    {
                        break;
                    }

                    hitRate += firstData;
                    break;
                case AdditionalTypes.Target.MagicalConcentrationDecreased:
                    if (classType != ClassType.Magician)
                    {
                        break;
                    }

                    hitRate -= firstData;
                    break;
            }
        }

        foreach (BCardDTO bCard in criticalBCards)
        {
            int firstData = bCard.FirstDataValue(playerLevel);

            switch ((AdditionalTypes.Critical)bCard.SubType)
            {
                case AdditionalTypes.Critical.InflictingIncreased:
                    criticalChance += classType switch
                    {
                        ClassType.Adventurer => firstData,
                        ClassType.Swordman => firstData,
                        ClassType.Archer => firstData,
                        ClassType.Wrestler => firstData,
                        _ => 0
                    };

                    secondCriticalChance += firstData;
                    break;
                case AdditionalTypes.Critical.InflictingReduced:
                    criticalChance -= classType switch
                    {
                        ClassType.Adventurer => firstData,
                        ClassType.Swordman => firstData,
                        ClassType.Archer => firstData,
                        ClassType.Wrestler => firstData,
                        _ => 0
                    };

                    secondCriticalChance -= firstData;
                    break;
                case AdditionalTypes.Critical.DamageIncreased:
                    criticalDamage += classType switch
                    {
                        ClassType.Adventurer => firstData,
                        ClassType.Swordman => firstData,
                        ClassType.Archer => firstData,
                        ClassType.Wrestler => firstData,
                        _ => 0
                    };

                    secondCriticalDamage += firstData;
                    break;
                case AdditionalTypes.Critical.DamageIncreasedInflictingReduced:
                    criticalDamage -= classType switch
                    {
                        ClassType.Adventurer => firstData,
                        ClassType.Swordman => firstData,
                        ClassType.Archer => firstData,
                        ClassType.Wrestler => firstData,
                        _ => 0
                    };

                    secondCriticalDamage -= firstData;
                    break;
            }
        }

        foreach (BCardDTO bCard in defenseBCards)
        {
            int firstData = bCard.FirstDataValue(playerLevel);

            switch ((AdditionalTypes.Defence)bCard.SubType)
            {
                case AdditionalTypes.Defence.AllIncreased:
                    meleeDefense += firstData;
                    rangeDefense += firstData;
                    magicDefense += firstData;
                    break;
                case AdditionalTypes.Defence.AllDecreased:
                    meleeDefense -= firstData;
                    rangeDefense -= firstData;
                    magicDefense -= firstData;
                    break;
                case AdditionalTypes.Defence.MeleeIncreased:
                    meleeDefense += firstData;
                    break;
                case AdditionalTypes.Defence.MeleeDecreased:
                    meleeDefense -= firstData;
                    break;
                case AdditionalTypes.Defence.RangedIncreased:
                    rangeDefense += firstData;
                    break;
                case AdditionalTypes.Defence.RangedDecreased:
                    rangeDefense -= firstData;
                    break;
                case AdditionalTypes.Defence.MagicalIncreased:
                    magicDefense += firstData;
                    break;
                case AdditionalTypes.Defence.MagicalDecreased:
                    magicDefense -= firstData;
                    break;
            }
        }

        foreach (BCardDTO bCard in dodgeBCards)
        {
            int firstData = bCard.FirstDataValue(playerLevel);

            switch ((AdditionalTypes.DodgeAndDefencePercent)bCard.SubType)
            {
                case AdditionalTypes.DodgeAndDefencePercent.DodgeIncreased:
                    meleeDodge += firstData;
                    rangeDodge += firstData;
                    break;
                case AdditionalTypes.DodgeAndDefencePercent.DodgeDecreased:
                    meleeDodge -= firstData;
                    rangeDodge -= firstData;
                    break;
                case AdditionalTypes.DodgeAndDefencePercent.DodgingMeleeIncreased:
                    meleeDodge += firstData;
                    break;
                case AdditionalTypes.DodgeAndDefencePercent.DodgingMeleeDecreased:
                    meleeDodge -= firstData;
                    break;
                case AdditionalTypes.DodgeAndDefencePercent.DodgingRangedIncreased:
                    rangeDodge += firstData;
                    break;
                case AdditionalTypes.DodgeAndDefencePercent.DodgingRangedDecreased:
                    rangeDodge -= firstData;
                    break;
            }
        }

        foreach (BCardDTO bCard in resistanceBCards)
        {
            int firstData = bCard.FirstDataValue(playerLevel);

            switch ((AdditionalTypes.ElementResistance)bCard.SubType)
            {
                case AdditionalTypes.ElementResistance.AllIncreased:
                    fireResistance += firstData;
                    waterResistance += firstData;
                    lightResistance += firstData;
                    shadowResistance += firstData;
                    break;
                case AdditionalTypes.ElementResistance.AllDecreased:
                    fireResistance -= firstData;
                    waterResistance -= firstData;
                    lightResistance -= firstData;
                    shadowResistance -= firstData;
                    break;
                case AdditionalTypes.ElementResistance.FireIncreased:
                    fireResistance += firstData;
                    break;
                case AdditionalTypes.ElementResistance.FireDecreased:
                    fireResistance -= firstData;
                    break;
                case AdditionalTypes.ElementResistance.WaterIncreased:
                    waterResistance += firstData;
                    break;
                case AdditionalTypes.ElementResistance.WaterDecreased:
                    waterResistance -= firstData;
                    break;
                case AdditionalTypes.ElementResistance.LightIncreased:
                    lightResistance += firstData;
                    break;
                case AdditionalTypes.ElementResistance.LightDecreased:
                    lightResistance -= firstData;
                    break;
                case AdditionalTypes.ElementResistance.DarkIncreased:
                    shadowResistance += firstData;
                    break;
                case AdditionalTypes.ElementResistance.DarkDecreased:
                    shadowResistance -= firstData;
                    break;
            }
        }

        minDamage += _playerEntity.GetMaxWeaponShellValue(ShellEffectType.DamageImproved, true);
        maxDamage += _playerEntity.GetMaxWeaponShellValue(ShellEffectType.DamageImproved, true);
        secondMinDamage += _playerEntity.GetMaxWeaponShellValue(ShellEffectType.DamageImproved, false);
        secondMaxDamage += _playerEntity.GetMaxWeaponShellValue(ShellEffectType.DamageImproved, false);

        criticalChance += _playerEntity.GetMaxWeaponShellValue(ShellEffectType.CriticalChance, true);
        criticalDamage += _playerEntity.GetMaxWeaponShellValue(ShellEffectType.CriticalDamage, true);
        secondCriticalChance += _playerEntity.GetMaxWeaponShellValue(ShellEffectType.CriticalChance, false);
        secondCriticalDamage += _playerEntity.GetMaxWeaponShellValue(ShellEffectType.CriticalDamage, false);

        meleeDefense += _playerEntity.GetMaxArmorShellValue(ShellEffectType.CloseDefence);
        rangeDefense += _playerEntity.GetMaxArmorShellValue(ShellEffectType.DistanceDefence);
        magicDefense += _playerEntity.GetMaxArmorShellValue(ShellEffectType.MagicDefence);

        fireResistance += _playerEntity.GetMaxArmorShellValue(ShellEffectType.IncreasedFireResistance);
        waterResistance += _playerEntity.GetMaxArmorShellValue(ShellEffectType.IncreasedWaterResistance);
        lightResistance += _playerEntity.GetMaxArmorShellValue(ShellEffectType.IncreasedLightResistance);
        shadowResistance += _playerEntity.GetMaxArmorShellValue(ShellEffectType.IncreasedDarkResistance);

        fireResistance += _playerEntity.GetMaxArmorShellValue(ShellEffectType.IncreasedAllResistance);
        waterResistance += _playerEntity.GetMaxArmorShellValue(ShellEffectType.IncreasedAllResistance);
        lightResistance += _playerEntity.GetMaxArmorShellValue(ShellEffectType.IncreasedAllResistance);
        shadowResistance += _playerEntity.GetMaxArmorShellValue(ShellEffectType.IncreasedAllResistance);

        fireResistance += _playerEntity.Family?.UpgradeValues?.GetOrDefault(FamilyUpgradeType.FIRE_RESISTANCE) ?? 0;
        waterResistance += _playerEntity.Family?.UpgradeValues?.GetOrDefault(FamilyUpgradeType.WATER_RESISTANCE) ?? 0;
        lightResistance += _playerEntity.Family?.UpgradeValues?.GetOrDefault(FamilyUpgradeType.LIGHT_RESISTANCE) ?? 0;
        shadowResistance += _playerEntity.Family?.UpgradeValues?.GetOrDefault(FamilyUpgradeType.DARK_RESISTANCE) ?? 0;

        _stats[Statistics.MIN_DAMAGE] = minDamage;
        _stats[Statistics.MAX_DAMAGE] = maxDamage;
        _stats[Statistics.HITRATE] = hitRate;
        _stats[Statistics.CRITICAL_CHANCE] = criticalChance;
        _stats[Statistics.CRITICAL_DAMAGE] = criticalDamage;
        _stats[Statistics.SECOND_MIN_DAMAGE] = secondMinDamage;
        _stats[Statistics.SECOND_MAX_DAMAGE] = secondMaxDamage;
        _stats[Statistics.SECOND_HITRATE] = secondHitRate;
        _stats[Statistics.SECOND_CRITICAL_CHANCE] = secondCriticalChance;
        _stats[Statistics.SECOND_CRITICAL_DAMAGE] = secondCriticalDamage;
        _stats[Statistics.MELEE_DEFENSE] = meleeDefense;
        _stats[Statistics.RANGE_DEFENSE] = rangeDefense;
        _stats[Statistics.MAGIC_DEFENSE] = magicDefense;
        _stats[Statistics.MELEE_DODGE] = meleeDodge;
        _stats[Statistics.RANGE_DODGE] = rangeDodge;
        _stats[Statistics.FIRE_RESISTANCE] = fireResistance;
        _stats[Statistics.WATER_RESISTANCE] = waterResistance;
        _stats[Statistics.LIGHT_RESISTANCE] = lightResistance;
        _stats[Statistics.SHADOW_RESISTANCE] = shadowResistance;
    }
}