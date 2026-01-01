using System;
using WingsAPI.Data.Families;
using WingsAPI.Packets.Enums.Shells;
using WingsEmu.Game._enum;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Helpers.Damages.Calculation;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Monster;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Battle;

namespace WingsEmu.Game.Extensions;

public static class DamageExtension
{
    private static readonly double[] _plus = { 0, 0.1, 0.15, 0.22, 0.32, 0.43, 0.54, 0.65, 0.9, 1.2, 2 };
    private static IRandomGenerator _randomGenerator => StaticRandomGenerator.Instance;

    public static bool IsMiss(this IBattleEntityDump attacker, IBattleEntityDump defender, CalculationBasicStatistics basicStatistics)
    {
        bool isPvP = attacker.IsPlayer() && defender.IsPlayer();

        int morale = basicStatistics.AttackerMorale;
        int attackerHitRate = basicStatistics.AttackerHitRate;
        int targetMorale = basicStatistics.DefenderMorale;
        int targetDodge = basicStatistics.DefenderDodge;

        if (defender.HasBCard(BCardType.SpecialDefence, (byte)AdditionalTypes.SpecialDefence.NoDefence))
        {
            return false;
        }

        if (isPvP && attacker.GetShellWeaponEffectValue(ShellEffectType.NeverMissInPVP) > 0)
        {
            return false;
        }

        if (attacker.IsMonster())
        {
            IBattleEntity monster = attacker.MapInstance.GetBattleEntity(attacker.Type, attacker.Id);
            if (monster != null && monster is IMonsterEntity mapMonster && mapMonster.MonsterRaceType == MonsterRaceType.Fixed && mapMonster.MonsterRaceSubType == 1)
            {
                return false;
            }
        }

        if (isPvP)
        {
            int shellChance = attacker.AttackType switch
            {
                AttackType.Melee => defender.GetShellArmorEffectValue(ShellEffectType.CloseDefenceDodgeInPVP),
                AttackType.Ranged => defender.GetShellArmorEffectValue(ShellEffectType.DistanceDefenceDodgeInPVP),
                AttackType.Magical => defender.GetShellArmorEffectValue(ShellEffectType.IgnoreMagicDamage),
                _ => 0
            };

            if (shellChance != 0 && _randomGenerator.RandomNumber() <= shellChance)
            {
                return true;
            }

            if (attacker.AttackType == AttackType.Magical)
            {
                return false;
            }

            shellChance = defender.GetShellArmorEffectValue(ShellEffectType.DodgeAllDamage);
            if (shellChance != 0 && _randomGenerator.RandomNumber() <= shellChance)
            {
                return true;
            }
        }

        if (attacker.AttackType == AttackType.Magical)
        {
            return false;
        }

        int attackerHitRateFinal = attackerHitRate + morale * 4;
        double targetDodgeFinal = targetDodge + targetMorale * 4;
        double difference = attackerHitRateFinal - targetDodgeFinal;

        // formula by friends111
        double chance = 100 / Math.PI * Math.Atan(0.015 * difference + 2) + 50;
        if (chance <= 40)
        {
            chance = 40;
        }

        double randomChance = _randomGenerator.RandomNumber() * 1.0;
        if (attacker.GetBCardInformation(BCardType.GuarantedDodgeRangedAttack, (byte)AdditionalTypes.GuarantedDodgeRangedAttack.AttackHitChance).count == 0)
        {
            if (chance <= randomChance)
            {
                return true;
            }
        }
        else
        {
            chance = attacker.GetBCardInformation(BCardType.GuarantedDodgeRangedAttack, (byte)AdditionalTypes.GuarantedDodgeRangedAttack.AttackHitChance).firstData;
            if (chance <= randomChance)
            {
                return true;
            }
        }

        return false;
    }

    public static CalculationBasicStatistics CalculateBasicStatistics(this IBattleEntityDump attacker, IBattleEntityDump defender, SkillInfo skill)
    {
        bool isPvP = attacker.IsPlayer() && defender.IsPlayer();

        #region Attacker

        int attackerMorale = attacker.Morale;
        int attackerAttackUpgrade = attacker.AttackUpgrade;
        int attackerHitRate = attacker.HitRate;
        int attackerCriticalChance = attacker.CriticalChance;
        int attackerCriticalDamage = attacker.CriticalDamage;

        int attackerElementRate = attacker.ElementRate;

        #endregion

        #region Defender

        int defenderMorale = defender.Morale;
        int defenderDefenseUpgrade = defender.DefenseUpgrade;

        int defenderDefense = attacker.AttackType switch
        {
            AttackType.Melee => defender.MeleeDefense,
            AttackType.Ranged => defender.RangeDefense,
            AttackType.Magical => defender.MagicalDefense
        };

        int defenderDodge = attacker.AttackType switch
        {
            AttackType.Melee => defender.MeleeDodge,
            AttackType.Ranged => defender.RangeDodge,
            AttackType.Magical => 0
        };

        int defenderResistance = attacker.Element switch
        {
            ElementType.Neutral => 0,
            ElementType.Fire => defender.FireResistance,
            ElementType.Water => defender.WaterResistance,
            ElementType.Light => defender.LightResistance,
            ElementType.Shadow => defender.ShadowResistance
        };

        #endregion

        /* MORALE */

        if (!defender.HasBCard(BCardType.Morale, (byte)AdditionalTypes.Morale.LockMorale)) // 17 | 2
        {
            attackerMorale += attacker.GetBCardInformation(BCardType.Morale, (byte)AdditionalTypes.Morale.MoraleIncreased).firstData;
            attackerMorale -= attacker.GetBCardInformation(BCardType.Morale, (byte)AdditionalTypes.Morale.MoraleDecreased).firstData;
        }

        if (!attacker.HasBCard(BCardType.Morale, (byte)AdditionalTypes.Morale.IgnoreEnemyMorale)) // 17 | 4
        {
            defenderMorale += defender.GetBCardInformation(BCardType.Morale, (byte)AdditionalTypes.Morale.MoraleIncreased).firstData;
            defenderMorale -= defender.GetBCardInformation(BCardType.Morale, (byte)AdditionalTypes.Morale.MoraleDecreased).firstData;
        }

        if (attacker.HasBCard(BCardType.Morale, (byte)AdditionalTypes.Morale.MoraleHalved))
        {
            attackerMorale /= 2;
        }

        if (attacker.HasBCard(BCardType.Morale, (byte)AdditionalTypes.Morale.MoraleDoubled))
        {
            attackerMorale *= 2;
        }

        if (defender.HasBCard(BCardType.Morale, (byte)AdditionalTypes.Morale.MoraleHalved))
        {
            defenderMorale /= 2;
        }

        if (defender.HasBCard(BCardType.Morale, (byte)AdditionalTypes.Morale.MoraleDoubled))
        {
            defenderMorale *= 2;
        }

        /* ATTACK UPGRADE */

        attackerAttackUpgrade += attacker.GetBCardInformation(BCardType.AttackPower, (byte)AdditionalTypes.AttackPower.AttackLevelIncreased).firstData;
        attackerAttackUpgrade -= attacker.GetBCardInformation(BCardType.AttackPower, (byte)AdditionalTypes.AttackPower.AttackLevelDecreased).firstData;

        if (attacker.HasBCard(BCardType.CalculatingLevel, (byte)AdditionalTypes.CalculatingLevel.CalculatedAttackLevel) && attackerAttackUpgrade > 0)
        {
            attackerAttackUpgrade = 0;
        }

        /* DEFENSE UPGRADE */

        defenderDefenseUpgrade += defender.GetBCardInformation(BCardType.Defence, (byte)AdditionalTypes.Defence.DefenceLevelIncreased).firstData;
        defenderDefenseUpgrade -= defender.GetBCardInformation(BCardType.Defence, (byte)AdditionalTypes.Defence.DefenceLevelDecreased).firstData;

        if (defender.HasBCard(BCardType.CalculatingLevel, (byte)AdditionalTypes.CalculatingLevel.CalculatedDefenceLevel) && defenderDefenseUpgrade > 0)
        {
            defenderDefenseUpgrade = 0;
        }

        /* HITRATE */

        attackerHitRate += attacker.GetBCardInformation(BCardType.Target, (byte)AdditionalTypes.Target.AllHitRateIncreased).firstData;
        attackerHitRate -= attacker.GetBCardInformation(BCardType.Target, (byte)AdditionalTypes.Target.AllHitRateDecreased).firstData;
        attackerHitRate += attacker.AttackType switch
        {
            AttackType.Melee => attacker.GetBCardInformation(BCardType.Target, (byte)AdditionalTypes.Target.MeleeHitRateIncreased).firstData,
            AttackType.Ranged => attacker.GetBCardInformation(BCardType.Target, (byte)AdditionalTypes.Target.RangedHitRateIncreased).firstData,
            _ => 0
        };

        attackerHitRate -= attacker.AttackType switch
        {
            AttackType.Melee => attacker.GetBCardInformation(BCardType.Target, (byte)AdditionalTypes.Target.MeleeHitRateDecreased).firstData,
            AttackType.Ranged => attacker.GetBCardInformation(BCardType.Target, (byte)AdditionalTypes.Target.RangedHitRateDecreased).firstData,
            _ => 0
        };

        attackerHitRate += (int)(attackerHitRate * attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseSpPoints, (byte)AdditionalTypes.IncreaseSpPoints.AccuracyIncrease).firstData));
        attackerHitRate -= (int)(attackerHitRate * attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseSpPoints, (byte)AdditionalTypes.IncreaseSpPoints.AccuracyDecrease).firstData));
        attackerHitRate += (int)(attackerHitRate * attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.MagicShield, (byte)AdditionalTypes.MagicShield.AccuracyIncrease).firstData));

        /* CRITICAL CHANCE */

        attackerCriticalChance += attacker.GetBCardInformation(BCardType.Critical, (byte)AdditionalTypes.Critical.InflictingIncreased).firstData;
        attackerCriticalChance -= attacker.GetBCardInformation(BCardType.Critical, (byte)AdditionalTypes.Critical.InflictingReduced).firstData;

        attackerCriticalChance -= defender.GetBCardInformation(BCardType.Critical, (byte)AdditionalTypes.Critical.DamageIncreasedInflictingReduced).firstData;

        attackerCriticalChance += attacker.GetShellWeaponEffectValue(ShellEffectType.CriticalChance);
        attackerCriticalChance -= defender.GetShellArmorEffectValue(ShellEffectType.ReducedCritChanceRecive);

        attackerCriticalChance += defender.GetBCardInformation(BCardType.Critical, (byte)AdditionalTypes.Critical.ReceivingIncreased).firstData;
        attackerCriticalChance -= defender.GetBCardInformation(BCardType.Critical, (byte)AdditionalTypes.Critical.ReceivingDecreased).firstData;

        if (defender.IsMonster())
        {
            MonsterRaceType monsterRaceType = defender.MonsterRaceType;
            Enum monsterRaceSubType = defender.MonsterRaceSubType;

            if (attacker.HasBCard(BCardType.SpecialisationBuffResistance, (byte)AdditionalTypes.SpecialisationBuffResistance.IncreaseCriticalAgainst))
            {
                (int firstData, int secondData, int count) raceBCard =
                    attacker.GetBCardInformation(BCardType.SpecialisationBuffResistance, (byte)AdditionalTypes.SpecialisationBuffResistance.IncreaseCriticalAgainst);

                int monsterRace = raceBCard.firstData;
                var bCardRaceType = (MonsterRaceType)Math.Floor(monsterRace / 10.0);
                Enum bCardRaceSubType = attacker.GetRaceSubType(bCardRaceType, (byte)(monsterRace % 10));

                if (monsterRaceType == bCardRaceType && bCardRaceSubType != null && Equals(monsterRaceSubType, bCardRaceSubType))
                {
                    attackerCriticalChance += raceBCard.secondData;
                }
            }
        }

        if (defender.HasBCard(BCardType.SniperAttack, (byte)AdditionalTypes.SniperAttack.ReceiveCriticalFromSniper) && skill != null && skill.Vnum == (short)SkillsVnums.SNIPER)
        {
            attackerCriticalChance = defender.GetBCardInformation(BCardType.SniperAttack, (byte)AdditionalTypes.SniperAttack.ReceiveCriticalFromSniper).firstData;
        }

        if (defender.HasBCard(BCardType.SpecialCritical, (byte)AdditionalTypes.SpecialCritical.InflictingChancePercent))
        {
            attackerCriticalChance = defender.GetBCardInformation(BCardType.SpecialCritical, (byte)AdditionalTypes.SpecialCritical.InflictingChancePercent).firstData;
        }

        if (defender.HasBCard(BCardType.SpecialCritical, (byte)AdditionalTypes.SpecialCritical.ReceivingChancePercent))
        {
            attackerCriticalChance = defender.GetBCardInformation(BCardType.SpecialCritical, (byte)AdditionalTypes.SpecialCritical.ReceivingChancePercent).firstData;
        }

        if (attacker.HasBCard(BCardType.SpecialCritical, (byte)AdditionalTypes.SpecialCritical.AlwaysInflict))
        {
            attackerCriticalChance = 100;
        }

        if (defender.HasBCard(BCardType.SpecialCritical, (byte)AdditionalTypes.SpecialCritical.AlwaysReceives))
        {
            attackerCriticalChance = 100;
        }

        if (defender.HasBCard(BCardType.SpecialCritical, (byte)AdditionalTypes.SpecialCritical.NeverReceives))
        {
            attackerCriticalChance = 0;
        }

        if (attacker.HasBCard(BCardType.SpecialCritical, (byte)AdditionalTypes.SpecialCritical.NeverInflict))
        {
            attackerCriticalChance = 0;
        }

        /* CRITICAL DAMAGE */

        int damageCriticalIncrease = attacker.GetBCardInformation(BCardType.Critical, (byte)AdditionalTypes.Critical.DamageIncreased).firstData;

        if (damageCriticalIncrease != 0)
        {
            damageCriticalIncrease -= defender.GetBCardInformation(BCardType.Critical, (byte)AdditionalTypes.Critical.DamageIncreasedInflictingReduced).firstData;
        }

        attackerCriticalDamage += attacker.GetShellWeaponEffectValue(ShellEffectType.CriticalDamage);
        attackerCriticalDamage += damageCriticalIncrease;
        attackerCriticalDamage -= defender.GetJewelsEffectValue(CellonType.CriticalDamageDecrease);
        attackerCriticalDamage += defender.GetBCardInformation(BCardType.Critical, (byte)AdditionalTypes.Critical.DamageFromCriticalIncreased).firstData;
        attackerCriticalDamage -= defender.GetBCardInformation(BCardType.Critical, (byte)AdditionalTypes.Critical.DamageFromCriticalDecreased).firstData;

        if (defender.HasBCard(BCardType.StealBuff, (byte)AdditionalTypes.StealBuff.ReduceCriticalReceivedChance))
        {
            (int firstData, int secondData, _) = defender.GetBCardInformation(BCardType.StealBuff, (byte)AdditionalTypes.StealBuff.ReduceCriticalReceivedChance);
            if (defender.IsSucceededChance(firstData))
            {
                attackerCriticalDamage -= secondData;
            }
        }

        /* ELEMENT RATE */

        if (attacker.HasBCard(BCardType.IncreaseElementFairy, (byte)AdditionalTypes.IncreaseElementFairy.FairyElementIncreaseWhileAttackingChance) && attacker.Element != ElementType.Neutral)
        {
            (int firstData, int secondData, _) = attacker.GetBCardInformation(BCardType.IncreaseElementFairy, (byte)AdditionalTypes.IncreaseElementFairy.FairyElementIncreaseWhileAttackingChance);
            if (attacker.IsSucceededChance(firstData))
            {
                attackerElementRate += secondData;
            }
        }

        /* DEFENSE */

        defenderDefense += defender.GetBCardInformation(BCardType.Defence, (byte)AdditionalTypes.Defence.AllIncreased).firstData;
        defenderDefense -= defender.GetBCardInformation(BCardType.Defence, (byte)AdditionalTypes.Defence.AllDecreased).firstData;

        defenderDefense += attacker.AttackType switch
        {
            AttackType.Melee => defender.GetBCardInformation(BCardType.Defence, (byte)AdditionalTypes.Defence.MeleeIncreased).firstData,
            AttackType.Ranged => defender.GetBCardInformation(BCardType.Defence, (byte)AdditionalTypes.Defence.RangedIncreased).firstData,
            AttackType.Magical => defender.GetBCardInformation(BCardType.Defence, (byte)AdditionalTypes.Defence.MagicalIncreased).firstData
        };

        defenderDefense += attacker.AttackType switch
        {
            AttackType.Melee => defender.GetShellArmorEffectValue(ShellEffectType.CloseDefence),
            AttackType.Ranged => defender.GetShellArmorEffectValue(ShellEffectType.DistanceDefence),
            AttackType.Magical => defender.GetShellArmorEffectValue(ShellEffectType.MagicDefence)
        };

        defenderDefense -= attacker.AttackType switch
        {
            AttackType.Melee => defender.GetBCardInformation(BCardType.Defence, (byte)AdditionalTypes.Defence.MeleeDecreased).firstData,
            AttackType.Ranged => defender.GetBCardInformation(BCardType.Defence, (byte)AdditionalTypes.Defence.RangedDecreased).firstData,
            AttackType.Magical => defender.GetBCardInformation(BCardType.Defence, (byte)AdditionalTypes.Defence.MagicalDecreased).firstData
        };

        defenderDefense += (int)(defenderDefense * defender.GetMultiplier(defender.GetShellArmorEffectValue(ShellEffectType.PercentageTotalDefence)));
        defenderDefense += (int)(defenderDefense *
            defender.GetMultiplier(defender.GetBCardInformation(BCardType.DodgeAndDefencePercent, (byte)AdditionalTypes.DodgeAndDefencePercent.DefenceIncreased).firstData));
        defenderDefense -= (int)(defenderDefense *
            defender.GetMultiplier(defender.GetBCardInformation(BCardType.DodgeAndDefencePercent, (byte)AdditionalTypes.DodgeAndDefencePercent.DefenceReduced).firstData));

        bool nullifiedDefense = attacker.AttackType switch
        {
            AttackType.Melee => defender.HasBCard(BCardType.SpecialDefence, (byte)AdditionalTypes.SpecialDefence.MeleeDefenceNullified),
            AttackType.Ranged => defender.HasBCard(BCardType.SpecialDefence, (byte)AdditionalTypes.SpecialDefence.RangedDefenceNullified),
            AttackType.Magical => defender.HasBCard(BCardType.SpecialDefence, (byte)AdditionalTypes.SpecialDefence.MagicDefenceNullified)
        };

        if (nullifiedDefense)
        {
            defenderDefense = 0;
        }

        if (defender.HasBCard(BCardType.SpecialDefence, (byte)AdditionalTypes.SpecialDefence.AllDefenceNullified))
        {
            defenderDefense = 0;
        }

        /* DODGE */

        defenderDodge += defender.GetBCardInformation(BCardType.DodgeAndDefencePercent, (byte)AdditionalTypes.DodgeAndDefencePercent.DodgeIncreased).firstData;
        defenderDodge -= defender.GetBCardInformation(BCardType.DodgeAndDefencePercent, (byte)AdditionalTypes.DodgeAndDefencePercent.DodgeDecreased).firstData;

        defenderDodge += (int)(defenderDodge * defender.GetMultiplier(defender.GetBCardInformation(BCardType.MagicShield, (byte)AdditionalTypes.MagicShield.DodgeIncrease).firstData));

        defenderDodge += attacker.AttackType switch
        {
            AttackType.Melee => defender.GetBCardInformation(BCardType.DodgeAndDefencePercent, (byte)AdditionalTypes.DodgeAndDefencePercent.DodgingMeleeIncreased).firstData,
            AttackType.Ranged => defender.GetBCardInformation(BCardType.DodgeAndDefencePercent, (byte)AdditionalTypes.DodgeAndDefencePercent.DodgingRangedIncreased).firstData,
            AttackType.Magical => 0
        };

        defenderDodge -= attacker.AttackType switch
        {
            AttackType.Melee => defender.GetBCardInformation(BCardType.DodgeAndDefencePercent, (byte)AdditionalTypes.DodgeAndDefencePercent.DodgingMeleeDecreased).firstData,
            AttackType.Ranged => defender.GetBCardInformation(BCardType.DodgeAndDefencePercent, (byte)AdditionalTypes.DodgeAndDefencePercent.DodgingRangedDecreased).firstData,
            AttackType.Magical => 0
        };

        /* RESISTANCE */

        defenderResistance -= attacker.GetBCardInformation(BCardType.EnemyElementResistance, (byte)AdditionalTypes.EnemyElementResistance.AllDecreased).firstData;
        defenderResistance -= attacker.Element switch
        {
            ElementType.Neutral => 0,
            ElementType.Fire => attacker.GetBCardInformation(BCardType.EnemyElementResistance, (byte)AdditionalTypes.EnemyElementResistance.FireDecreased).firstData,
            ElementType.Water => attacker.GetBCardInformation(BCardType.EnemyElementResistance, (byte)AdditionalTypes.EnemyElementResistance.WaterDecreased).firstData,
            ElementType.Light => attacker.GetBCardInformation(BCardType.EnemyElementResistance, (byte)AdditionalTypes.EnemyElementResistance.LightDecreased).firstData,
            ElementType.Shadow => attacker.GetBCardInformation(BCardType.EnemyElementResistance, (byte)AdditionalTypes.EnemyElementResistance.DarkDecreased).firstData
        };

        defenderResistance += attacker.GetBCardInformation(BCardType.EnemyElementResistance, (byte)AdditionalTypes.EnemyElementResistance.AllIncreased).firstData;
        defenderResistance += attacker.Element switch
        {
            ElementType.Neutral => 0,
            ElementType.Fire => attacker.GetBCardInformation(BCardType.EnemyElementResistance, (byte)AdditionalTypes.EnemyElementResistance.FireIncreased).firstData,
            ElementType.Water => attacker.GetBCardInformation(BCardType.EnemyElementResistance, (byte)AdditionalTypes.EnemyElementResistance.WaterIncreased).firstData,
            ElementType.Light => attacker.GetBCardInformation(BCardType.EnemyElementResistance, (byte)AdditionalTypes.EnemyElementResistance.LightIncreased).firstData,
            ElementType.Shadow => attacker.GetBCardInformation(BCardType.EnemyElementResistance, (byte)AdditionalTypes.EnemyElementResistance.DarkIncreased).firstData
        };

        defenderResistance -= defender.GetBCardInformation(BCardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.AllDecreased).firstData;
        defenderResistance -= attacker.Element switch
        {
            ElementType.Neutral => 0,
            ElementType.Fire => defender.GetBCardInformation(BCardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.FireDecreased).firstData,
            ElementType.Water => defender.GetBCardInformation(BCardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.WaterDecreased).firstData,
            ElementType.Light => defender.GetBCardInformation(BCardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.LightDecreased).firstData,
            ElementType.Shadow => defender.GetBCardInformation(BCardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.DarkDecreased).firstData
        };

        defenderResistance += defender.GetBCardInformation(BCardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.AllIncreased).firstData;
        defenderResistance += attacker.Element switch
        {
            ElementType.Neutral => 0,
            ElementType.Fire => defender.GetBCardInformation(BCardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.FireIncreased).firstData,
            ElementType.Water => defender.GetBCardInformation(BCardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.WaterIncreased).firstData,
            ElementType.Light => defender.GetBCardInformation(BCardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.LightIncreased).firstData,
            ElementType.Shadow => defender.GetBCardInformation(BCardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.DarkIncreased).firstData
        };

        defenderResistance += defender.GetShellArmorEffectValue(ShellEffectType.IncreasedAllResistance);
        defenderResistance += attacker.Element switch
        {
            ElementType.Neutral => 0,
            ElementType.Fire => defender.GetShellArmorEffectValue(ShellEffectType.IncreasedFireResistance),
            ElementType.Water => defender.GetShellArmorEffectValue(ShellEffectType.IncreasedWaterResistance),
            ElementType.Light => defender.GetShellArmorEffectValue(ShellEffectType.IncreasedLightResistance),
            ElementType.Shadow => defender.GetShellArmorEffectValue(ShellEffectType.IncreasedDarkResistance)
        };

        defenderResistance += attacker.Element switch
        {
            ElementType.Neutral => 0,
            ElementType.Fire => defender.GetFamilyUpgradeValue(FamilyUpgradeType.FIRE_RESISTANCE),
            ElementType.Water => defender.GetFamilyUpgradeValue(FamilyUpgradeType.WATER_RESISTANCE),
            ElementType.Light => defender.GetFamilyUpgradeValue(FamilyUpgradeType.LIGHT_RESISTANCE),
            ElementType.Shadow => defender.GetFamilyUpgradeValue(FamilyUpgradeType.DARK_RESISTANCE)
        };

        defenderResistance += (int)(defenderResistance *
            defender.GetMultiplier(defender.GetBCardInformation(BCardType.IncreaseElementByResis, (byte)AdditionalTypes.IncreaseElementByResis.AllElementResisIncrease).firstData));
        defenderResistance -= (int)(defenderResistance *
            defender.GetMultiplier(defender.GetBCardInformation(BCardType.IncreaseElementByResis, (byte)AdditionalTypes.IncreaseElementByResis.AllElementResisDecrease).firstData));

        defenderResistance += attacker.Element switch
        {
            ElementType.Neutral => 0,
            ElementType.Fire => (int)(defenderResistance * defender.GetMultiplier(defender.GetBCardInformation(
                BCardType.IncreaseElementByResis, (byte)AdditionalTypes.IncreaseElementByResis.FireElementResisIncrease).firstData)),
            ElementType.Water => (int)(defenderResistance * defender.GetMultiplier(defender.GetBCardInformation(
                BCardType.IncreaseElementByResis, (byte)AdditionalTypes.IncreaseElementByResis.WaterElementResisIncrease).firstData)),
            ElementType.Light => (int)(defenderResistance * defender.GetMultiplier(defender.GetBCardInformation(
                BCardType.IncreaseElementByResis, (byte)AdditionalTypes.IncreaseElementByResis.LightElementResisIncrease).firstData)),
            ElementType.Shadow => (int)(defenderResistance * defender.GetMultiplier(defender.GetBCardInformation(
                BCardType.IncreaseElementByResis, (byte)AdditionalTypes.IncreaseElementByResis.ShadowElementResisIncrease).firstData))
        };

        defenderResistance -= attacker.Element switch
        {
            ElementType.Neutral => 0,
            ElementType.Fire => (int)(defenderResistance * defender.GetMultiplier(defender.GetBCardInformation(
                BCardType.IncreaseElementByResis, (byte)AdditionalTypes.IncreaseElementByResis.FireElementResisDecrease).firstData)),
            ElementType.Water => (int)(defenderResistance * defender.GetMultiplier(defender.GetBCardInformation(
                BCardType.IncreaseElementByResis, (byte)AdditionalTypes.IncreaseElementByResis.WaterElementResisDecrease).firstData)),
            ElementType.Light => (int)(defenderResistance * defender.GetMultiplier(defender.GetBCardInformation(
                BCardType.IncreaseElementByResis, (byte)AdditionalTypes.IncreaseElementByResis.LightElementResisDecrease).firstData)),
            ElementType.Shadow => (int)(defenderResistance * defender.GetMultiplier(defender.GetBCardInformation(
                BCardType.IncreaseElementByResis, (byte)AdditionalTypes.IncreaseElementByResis.ShadowElementResisDecrease).firstData))
        };

        if (isPvP)
        {
            defenderResistance -= (int)(defenderResistance * attacker.GetShellWeaponEffectValue(ShellEffectType.ReducesEnemyAllResistancesInPVP) * 0.01);
            defenderResistance -= attacker.Element switch
            {
                ElementType.Neutral => 0,
                ElementType.Fire => (int)(defenderResistance * attacker.GetShellWeaponEffectValue(ShellEffectType.ReducesEnemyFireResistanceInPVP) * 0.01),
                ElementType.Water => (int)(defenderResistance * attacker.GetShellWeaponEffectValue(ShellEffectType.ReducesEnemyWaterResistanceInPVP) * 0.01),
                ElementType.Light => (int)(defenderResistance * attacker.GetShellWeaponEffectValue(ShellEffectType.ReducesEnemyLightResistanceInPVP) * 0.01),
                ElementType.Shadow => (int)(defenderResistance * attacker.GetShellWeaponEffectValue(ShellEffectType.ReducesEnemyDarkResistanceInPVP) * 0.01)
            };
        }

        if (defender.HasBCard(BCardType.NoCharacteristicValue, (byte)AdditionalTypes.NoCharacteristicValue.AllResistancesNullified))
        {
            defenderResistance = 0;
        }

        bool removeResistance = attacker.Element switch
        {
            ElementType.Neutral => false,
            ElementType.Fire => defender.HasBCard(BCardType.NoCharacteristicValue, (byte)AdditionalTypes.NoCharacteristicValue.FireResistanceNullified),
            ElementType.Water => defender.HasBCard(BCardType.NoCharacteristicValue, (byte)AdditionalTypes.NoCharacteristicValue.WaterResistanceNullified),
            ElementType.Light => defender.HasBCard(BCardType.NoCharacteristicValue, (byte)AdditionalTypes.NoCharacteristicValue.LightResistanceNullified),
            ElementType.Shadow => defender.HasBCard(BCardType.NoCharacteristicValue, (byte)AdditionalTypes.NoCharacteristicValue.DarkResistanceNullified)
        };

        if (removeResistance)
        {
            defenderResistance = 0;
        }

        return new CalculationBasicStatistics
        {
            AttackerMorale = attackerMorale,
            AttackerAttackUpgrade = attackerAttackUpgrade,
            AttackerHitRate = attackerHitRate,
            AttackerCriticalChance = attackerCriticalChance,
            AttackerCriticalDamage = attackerCriticalDamage,
            AttackerElementRate = attackerElementRate,
            DefenderMorale = defenderMorale,
            DefenderDefenseUpgrade = defenderDefenseUpgrade,
            DefenderDefense = defenderDefense,
            DefenderDodge = defenderDodge,
            DefenderResistance = defenderResistance
        };
    }

    public static CalculationPhysicalDamage CalculatePhysicalDamage(this IBattleEntityDump attacker, IBattleEntityDump defender, SkillInfo skill)
    {
        bool isPvP = attacker.IsPlayer() && defender.IsPlayer();

        double damagePercentage = 1;
        double damagePercentageSecond = 0;
        double ignoreEnemyDefense = 1;
        double vesselLodDamage = 1;
        double vesselGlacernonDamage = 1;
        double increaseAllDamage = 1;
        double increaseAllDamageAttackType = 1;
        double increaseDamageMagicDefense = 0;
        int increaseDamageRace = 0;
        double increaseDamageRacePercentage = 1;
        double increaseLoDDamage = 1;
        double increaseVesselDamage = 1;
        double increaseDamageFaction = 1;
        double increaseDamageInPvP = 1;
        double increaseDamageVersusMonstersMapType = 1;
        double increaseAllDamageVersusMonsters = 1;

        /* DAMAGE */
        int damage = attacker.TryFindPartnerSkillInformation(BCardType.AttackPower, (byte)AdditionalTypes.AttackPower.AllAttacksIncreased, skill).firstData;
        damage -= attacker.GetBCardInformation(BCardType.AttackPower, (byte)AdditionalTypes.AttackPower.AllAttacksDecreased).firstData;

        damage += attacker.AttackType switch
        {
            AttackType.Melee => attacker.TryFindPartnerSkillInformation(BCardType.AttackPower, (byte)AdditionalTypes.AttackPower.MeleeAttacksIncreased, skill).firstData,
            AttackType.Ranged => attacker.TryFindPartnerSkillInformation(BCardType.AttackPower, (byte)AdditionalTypes.AttackPower.RangedAttacksIncreased, skill).firstData,
            AttackType.Magical => attacker.TryFindPartnerSkillInformation(BCardType.AttackPower, (byte)AdditionalTypes.AttackPower.MagicalAttacksIncreased, skill).firstData
        };

        damage -= attacker.AttackType switch
        {
            AttackType.Melee => attacker.GetBCardInformation(BCardType.AttackPower, (byte)AdditionalTypes.AttackPower.MeleeAttacksDecreased).firstData,
            AttackType.Ranged => attacker.GetBCardInformation(BCardType.AttackPower, (byte)AdditionalTypes.AttackPower.RangedAttacksDecreased).firstData,
            AttackType.Magical => attacker.GetBCardInformation(BCardType.AttackPower, (byte)AdditionalTypes.AttackPower.MagicalAttacksDecreased).firstData
        };

        if (isPvP)
        {
            damage += attacker.GetBCardInformation(BCardType.SpecialisationBuffResistance, (byte)AdditionalTypes.SpecialisationBuffResistance.IncreaseDamageInPVP).firstData;
            damage -= attacker.GetBCardInformation(BCardType.SpecialisationBuffResistance, (byte)AdditionalTypes.SpecialisationBuffResistance.DecreaseDamageInPVP).firstData;
        }

        /* DAMAGE PERCENTAGE */
        if (attacker.HasBCard(BCardType.Critical, (byte)AdditionalTypes.Critical.DamageIncreasingChance))
        {
            (int firstData, int secondData, _) = attacker.GetBCardInformation(BCardType.Critical, (byte)AdditionalTypes.Critical.DamageIncreasingChance);
            if (attacker.IsSucceededChance(firstData))
            {
                damagePercentage *= 1 + secondData * 0.01;
            }
        }

        /* DAMAGE PERCENTAGE - SOFT DAMAGE */
        if (attacker.HasBCard(BCardType.IncreaseDamage, (byte)AdditionalTypes.IncreaseDamage.IncreasingProbability))
        {
            (int firstData, int secondData, _) = attacker.GetBCardInformation(BCardType.IncreaseDamage, (byte)AdditionalTypes.IncreaseDamage.IncreasingProbability);
            if (attacker.IsSucceededChance(firstData))
            {
                damagePercentageSecond = secondData * 0.01;
            }
        }

        if (damagePercentageSecond != 0)
        {
            if (attacker.HasBCard(BCardType.IncreaseDamage, (byte)AdditionalTypes.IncreaseDamage.DecreasingProbability))
            {
                (int firstData, int secondData, _) = attacker.GetBCardInformation(BCardType.IncreaseDamage, (byte)AdditionalTypes.IncreaseDamage.DecreasingProbability);
                if (attacker.IsSucceededChance(firstData))
                {
                    damagePercentageSecond = secondData * 0.01 <= 0 ? 0 : damagePercentageSecond - secondData * 0.01;
                }
            }
        }

        /* MULTIPLY DAMAGE */
        double multiplyDamage = 1;
        int multiplyDamageInt = attacker.GetBCardInformation(BCardType.MultAttack, (byte)AdditionalTypes.MultAttack.AllAttackIncreased).firstData;
        multiplyDamageInt += attacker.AttackType switch
        {
            AttackType.Melee => attacker.GetBCardInformation(BCardType.MultAttack, (byte)AdditionalTypes.MultAttack.MeleeAttackIncreased).firstData,
            AttackType.Ranged => attacker.GetBCardInformation(BCardType.MultAttack, (byte)AdditionalTypes.MultAttack.RangedAttackIncreased).firstData,
            AttackType.Magical => attacker.GetBCardInformation(BCardType.MultAttack, (byte)AdditionalTypes.MultAttack.MagicalAttackIncreased).firstData
        };

        multiplyDamageInt -= attacker.GetBCardInformation(BCardType.MultAttack, (byte)AdditionalTypes.MultAttack.AllAttackDecreased).firstData;
        multiplyDamageInt -= attacker.AttackType switch
        {
            AttackType.Melee => attacker.GetBCardInformation(BCardType.MultAttack, (byte)AdditionalTypes.MultAttack.MeleeAttackDecreased).firstData,
            AttackType.Ranged => attacker.GetBCardInformation(BCardType.MultAttack, (byte)AdditionalTypes.MultAttack.RangedAttackDecreased).firstData,
            AttackType.Magical => attacker.GetBCardInformation(BCardType.MultAttack, (byte)AdditionalTypes.MultAttack.MagicalAttackDecreased).firstData
        };

        multiplyDamage = multiplyDamageInt == 0 ? 1 : multiplyDamageInt;

        /* INVISIBLE DAMAGE */
        int invisibleDamage = attacker.IsInvisible() ? attacker.GetBCardInformation(BCardType.LightAndShadow, (byte)AdditionalTypes.LightAndShadow.AdditionalDamageWhenHidden).firstData : 0;

        /* END CRITICAL DAMAGE */
        double endCriticalDamage = 1;
        endCriticalDamage += attacker.GetBCardInformation(BCardType.Count, (byte)AdditionalTypes.Count.IncreaseCritAttackOnEnd).firstData;

        /* INCREASE DAMAGE BY RACE */
        if (defender.IsMonster())
        {
            MonsterRaceType monsterRaceType = defender.MonsterRaceType;
            Enum monsterRaceSubType = defender.MonsterRaceSubType;

            int monsterRace;

            MonsterRaceType bCardRaceType;
            Enum bCardRaceSubType;
            if (attacker.HasBCard(BCardType.SpecialisationBuffResistance, (byte)AdditionalTypes.SpecialisationBuffResistance.IncreaseDamageAgainst))
            {
                (int firstData, int secondData, int count) raceBCard =
                    attacker.GetBCardInformation(BCardType.SpecialisationBuffResistance, (byte)AdditionalTypes.SpecialisationBuffResistance.IncreaseDamageAgainst);

                monsterRace = raceBCard.firstData;
                bCardRaceType = (MonsterRaceType)Math.Floor(monsterRace / 10.0);
                bCardRaceSubType = attacker.GetRaceSubType(bCardRaceType, (byte)(monsterRace % 10));

                if (monsterRaceType == bCardRaceType && bCardRaceSubType != null && Equals(monsterRaceSubType, bCardRaceSubType))
                {
                    increaseDamageRace += raceBCard.secondData;
                }
            }

            if (attacker.HasBCard(BCardType.LeonaPassiveSkill, (byte)AdditionalTypes.LeonaPassiveSkill.IncreaseDamageAgainst))
            {
                (int firstData, int secondData, int count) raceBCard =
                    attacker.GetBCardInformation(BCardType.LeonaPassiveSkill, (byte)AdditionalTypes.LeonaPassiveSkill.IncreaseDamageAgainst);

                monsterRace = raceBCard.firstData;
                bCardRaceType = (MonsterRaceType)Math.Floor(monsterRace / 10.0);
                bCardRaceSubType = attacker.GetRaceSubType(bCardRaceType, (byte)(monsterRace % 10));

                if (monsterRaceType == bCardRaceType && bCardRaceSubType != null && Equals(monsterRaceSubType, bCardRaceSubType))
                {
                    increaseDamageRacePercentage += raceBCard.secondData * 0.01;
                }
            }

            switch (monsterRaceSubType)
            {
                case MonsterSubRace.LowLevel.Animal:
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedSmallMonster) * 0.01;
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedAnimal) * 0.01;
                    break;
                case MonsterSubRace.HighLevel.Animal:
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedBigMonster) * 0.01;
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedAnimal) * 0.01;
                    break;
                case MonsterSubRace.LowLevel.Plant:
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedSmallMonster) * 0.01;
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedPlant) * 0.01;
                    break;
                case MonsterSubRace.HighLevel.Plant:
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedBigMonster) * 0.01;
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedPlant) * 0.01;
                    break;
                case MonsterSubRace.LowLevel.Monster:
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedSmallMonster) * 0.01;
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedEnemy) * 0.01;
                    break;
                case MonsterSubRace.HighLevel.Monster:
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedBigMonster) * 0.01;
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedEnemy) * 0.01;
                    break;
                case MonsterSubRace.Undead.LowLevelUndead:
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedSmallMonster) * 0.01;
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedUnDead) * 0.01;
                    break;
                case MonsterSubRace.Undead.HighLevelUndead:
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedBigMonster) * 0.01;
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedUnDead) * 0.01;
                    break;
                case MonsterSubRace.Undead.Vampire:
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedUnDead) * 0.01;
                    break;
                case MonsterSubRace.Spirits.LowLevelGhost:
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedSmallMonster) * 0.01;
                    break;
                case MonsterSubRace.Spirits.HighLevelGhost:
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedBigMonster) * 0.01;
                    break;
                case MonsterSubRace.Spirits.LowLevelSpirit:
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedSmallMonster) * 0.01;
                    break;
                case MonsterSubRace.Spirits.HighLevelSpirit:
                    increaseDamageRacePercentage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageIncreasedBigMonster) * 0.01;
                    break;
            }
        }

        /* DECREASE DAMAGE BY RACE */
        if (defender.IsPlayer() && defender.HasBCard(BCardType.SpecialisationBuffResistance, (byte)AdditionalTypes.SpecialisationBuffResistance.ReduceDamageAgainst))
        {
            (int firstData, int secondData, int count) raceBCard =
                defender.GetBCardInformation(BCardType.SpecialisationBuffResistance, (byte)AdditionalTypes.SpecialisationBuffResistance.ReduceDamageAgainst);

            int monsterRace = raceBCard.firstData;
            var bCardRaceType = (MonsterRaceType)Math.Floor(monsterRace / 10.0);
            Enum bCardRaceSubType = attacker.GetRaceSubType(bCardRaceType, (byte)(monsterRace % 10));

            if (attacker.MonsterRaceType == bCardRaceType && bCardRaceSubType != null && Equals(attacker.MonsterRaceSubType, bCardRaceSubType))
            {
                increaseDamageRace -= raceBCard.secondData;
            }
        }

        /* IGNORE DEFENDER DEFENSE */
        if (attacker.HasBCard(BCardType.StealBuff, (byte)AdditionalTypes.StealBuff.IgnoreDefenceChance))
        {
            (int firstData, int secondData, _) = attacker.GetBCardInformation(BCardType.StealBuff, (byte)AdditionalTypes.StealBuff.IgnoreDefenceChance);
            if (attacker.IsSucceededChance(firstData))
            {
                ignoreEnemyDefense -= secondData * 0.01;
                attacker.BroadcastEffect(EffectType.IgnoreDefence);
            }
        }

        /* INCREASE DAMAGE BY FACTION */
        if (isPvP)
        {
            (int firstData, int secondData, int count) factionBCard;

            switch (attacker.Faction)
            {
                case FactionType.Angel:
                    factionBCard = attacker.GetBCardInformation(BCardType.ChangingPlace, (byte)AdditionalTypes.ChangingPlace.IncreaseDamageVersusDemons);
                    increaseDamageFaction += factionBCard.firstData * 0.01;
                    break;
                case FactionType.Demon:
                    factionBCard = attacker.GetBCardInformation(BCardType.ChangingPlace, (byte)AdditionalTypes.ChangingPlace.IncreaseDamageVersusAngels);
                    increaseDamageFaction += factionBCard.firstData * 0.01;
                    break;
            }
        }

        /* INCREASE DAMAGE VS VESSEL MONSTERS */
        if (defender.IsMonster())
        {
            bool isVesselMonster = defender is NpcMonsterEntityDump { IsVesselMonster: true };
            if (isVesselMonster)
            {
                (int firstData, int secondData, int count) vesselBCard =
                    attacker.GetBCardInformation(BCardType.IncreaseDamageVersus, (byte)AdditionalTypes.IncreaseDamageVersus.VesselAndLodMobDamageIncrease);
                vesselLodDamage += vesselBCard.firstData * 0.01;

                vesselBCard = attacker.GetBCardInformation(BCardType.IncreaseDamageVersus, (byte)AdditionalTypes.IncreaseDamageVersus.VesselAndFrozenCrownMobDamageIncrease);
                vesselGlacernonDamage += vesselBCard.firstData * 0.01;

                vesselBCard = attacker.GetBCardInformation(BCardType.IncreaseDamageInLoD, (byte)AdditionalTypes.IncreaseDamageInLoD.VesselMonstersAttackIncrease);
                increaseVesselDamage += vesselBCard.firstData * 0.01;
            }
        }

        /* INCREASE DAMAGE VS LOD MONSTERS */
        if (attacker.MapInstance.MapId == (short)MapIds.LAND_OF_DEATH)
        {
            (int firstData, int secondData, int count)
                lodBCard = attacker.GetBCardInformation(BCardType.IncreaseDamageVersus, (byte)AdditionalTypes.IncreaseDamageVersus.VesselAndLodMobDamageIncrease);

            vesselLodDamage += lodBCard.secondData * 0.01;

            lodBCard = attacker.GetBCardInformation(BCardType.IncreaseDamageInLoD, (byte)AdditionalTypes.IncreaseDamageInLoD.LodMonstersAttackIncrease);
            increaseLoDDamage += lodBCard.firstData * 0.01;
        }

        /* INCREASE ALL DAMAGE */
        increaseAllDamage += attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseAllDamage, (byte)AdditionalTypes.IncreaseAllDamage.AllAttackIncrease).firstData);
        increaseAllDamage -= attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseAllDamage, (byte)AdditionalTypes.IncreaseAllDamage.AllAttackDecrease).firstData);

        /* INCREASE ALL DAMAGE BY ATTACK TYPE */
        increaseAllDamageAttackType += attacker.AttackType switch
        {
            AttackType.Melee => attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseAllDamage, (byte)AdditionalTypes.IncreaseAllDamage.MeleeAttackIncrease).firstData),
            AttackType.Ranged => attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseAllDamage, (byte)AdditionalTypes.IncreaseAllDamage.RangeAttackIncrease).firstData),
            AttackType.Magical => attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseAllDamage, (byte)AdditionalTypes.IncreaseAllDamage.MagicAttackIncrease).firstData)
        };

        increaseAllDamageAttackType -= attacker.AttackType switch
        {
            AttackType.Melee => attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseAllDamage, (byte)AdditionalTypes.IncreaseAllDamage.MeleeAttackDecrease).firstData),
            AttackType.Ranged => attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseAllDamage, (byte)AdditionalTypes.IncreaseAllDamage.RangeAttackDecrease).firstData),
            AttackType.Magical => attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseAllDamage, (byte)AdditionalTypes.IncreaseAllDamage.MagicAttackDecrease).firstData)
        };

        /* INCREASE ALL DAMAGE BY MAGIC DEFENSE */
        increaseDamageMagicDefense += attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.ReflectDamage, (byte)AdditionalTypes.ReflectDamage.AllAttackIncreasePerMagicDefense).firstData);

        /* INCREASE/DECREASE PVP DAMAGE */
        (int firstData, int secondData, int count) pvpBCard = attacker.GetBCardInformation(BCardType.LeonaPassiveSkill, (byte)AdditionalTypes.LeonaPassiveSkill.AttackIncreasedInPVP);
        if (isPvP)
        {
            increaseDamageInPvP += pvpBCard.firstData * 0.01;
            increaseDamageInPvP += attacker.GetShellWeaponEffectValue(ShellEffectType.PercentageDamageInPVP) * 0.01;
            if (attacker.MapInstance.MapInstanceType == MapInstanceType.RainbowBattle)
            {
                increaseDamageInPvP += attacker.GetBCardInformation(BCardType.IncreaseDamageVersus, (byte)AdditionalTypes.IncreaseDamageVersus.PvpDamageAndSpeedRainbowBattleIncrease).firstData * 0.01;
            }

            pvpBCard = attacker.GetBCardInformation(BCardType.LeonaPassiveSkill, (byte)AdditionalTypes.LeonaPassiveSkill.AttackDecreasedInPVP);
            increaseDamageInPvP -= pvpBCard.firstData * 0.01;
            increaseDamageInPvP -= defender.GetShellWeaponEffectValue(ShellEffectType.PercentageAllPVPDefence) * 0.01;
        }
        else
        {
            if (pvpBCard.secondData == 1)
            {
                increaseDamageInPvP -= pvpBCard.firstData * 0.01;
            }
        }

        /* INCREASE/DECRASE ATTACK */
        double increaseAttack = 1;
        (int firstData, int secondData, int count) attackBCard = defender.GetBCardInformation(BCardType.Damage, (byte)AdditionalTypes.Damage.DamageIncreased);
        increaseAttack += defender.GetMultiplier(attackBCard.firstData);

        attackBCard = defender.GetBCardInformation(BCardType.Damage, (byte)AdditionalTypes.Damage.DamageDecreased);
        increaseAttack -= defender.GetMultiplier(attackBCard.firstData);

        /* INCREASE/DECREASE ATTACK BY ATTACK TYPE*/
        double increaseAttackAttackType = 1;
        increaseAttackAttackType += attacker.AttackType switch
        {
            AttackType.Melee => defender.GetMultiplier(defender.GetBCardInformation(BCardType.Damage, (byte)AdditionalTypes.Damage.MeleeIncreased).firstData),
            AttackType.Ranged => defender.GetMultiplier(defender.GetBCardInformation(BCardType.Damage, (byte)AdditionalTypes.Damage.RangedIncreased).firstData),
            AttackType.Magical => defender.GetMultiplier(defender.GetBCardInformation(BCardType.Damage, (byte)AdditionalTypes.Damage.MagicalIncreased).firstData)
        };

        increaseAttackAttackType -= attacker.AttackType switch
        {
            AttackType.Melee => defender.GetMultiplier(defender.GetBCardInformation(BCardType.Damage, (byte)AdditionalTypes.Damage.MeleeDecreased).firstData),
            AttackType.Ranged => defender.GetMultiplier(defender.GetBCardInformation(BCardType.Damage, (byte)AdditionalTypes.Damage.RangedDecreased).firstData),
            AttackType.Magical => defender.GetMultiplier(defender.GetBCardInformation(BCardType.Damage, (byte)AdditionalTypes.Damage.MagicalDecreased).firstData)
        };

        /* INCREASE SHADOW ATTACK */
        double increaseDamageShadowFairy = 1;
        if (attacker.Element == ElementType.Shadow)
        {
            (int firstData, int secondData, int count) shadowBCard =
                attacker.GetBCardInformation(BCardType.DarkCloneSummon, (byte)AdditionalTypes.DarkCloneSummon.DarkElementDamageIncreaseChance);

            if (attacker.IsSucceededChance(shadowBCard.firstData))
            {
                increaseDamageShadowFairy += shadowBCard.secondData * 0.01;
            }
        }

        /* INCREASE ALL ATTACKS */
        double increaseAllAttacks = 1 + attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.Item, (byte)AdditionalTypes.Item.AttackIncreased).firstData);

        double markOfDeath = 1;
        double knockdown = 1;
        double loserSigh = 1;
        double comboSkill = 1;

        if (skill != null)
        {
            if (defender.HasBuff((int)BuffVnums.MARK_OF_DEATH) && skill.Vnum == (int)SkillsVnums.SPIRIT_SPLITTER)
            {
                markOfDeath = 3;
            }

            if (defender.HasBuff((int)BuffVnums.KNOCKDOWN) && skill.Vnum == (int)SkillsVnums.EXECUTION)
            {
                knockdown = 1.2;
            }

            if (defender.HasBuff((int)BuffVnums.LOSER_SIGH) && skill.Vnum == (int)SkillsVnums.EXECUTION)
            {
                loserSigh = 1.4;
            }

            if (skill.IsComboSkill && defender.HasBCard(BCardType.DamageConvertingSkill, (byte)AdditionalTypes.DamageConvertingSkill.AdditionalDamageCombo))
            {
                comboSkill += defender.GetMultiplier(defender.GetBCardInformation(BCardType.DamageConvertingSkill, (byte)AdditionalTypes.DamageConvertingSkill.AdditionalDamageCombo).firstData);
            }
        }

        double damageFromDebuff = markOfDeath * knockdown * loserSigh * comboSkill;

        /* INCREASE DAMAGE VS HIGH MONSTERS*/
        double increaseDamageHighMonsters = 0;
        if (defender.IsMonster() && defender.Level > attacker.Level && attacker.HasBCard(BCardType.EffectSummon, (byte)AdditionalTypes.EffectSummon.IfMobHigherLevelDamageIncrease))
        {
            (int firstData, int secondData, int count) increaseDamageMonsterBCard =
                attacker.GetBCardInformation(BCardType.EffectSummon, (byte)AdditionalTypes.EffectSummon.IfMobHigherLevelDamageIncrease);
            if (attacker.IsSucceededChance(increaseDamageMonsterBCard.firstData))
            {
                increaseDamageHighMonsters += attacker.GetMultiplier(increaseDamageMonsterBCard.secondData);
            }
        }

        /* INCREASE ALL ATTACKS VERSUS MONSTERS */
        if (!isPvP)
        {
            increaseAllDamageVersusMonsters += attacker.GetBCardInformation(BCardType.IncreaseElementFairy, (byte)AdditionalTypes.IncreaseElementFairy.DamageToMonstersIncrease).firstData * 0.01;
            increaseAllDamageVersusMonsters -= attacker.GetBCardInformation(BCardType.IncreaseElementFairy, (byte)AdditionalTypes.IncreaseElementFairy.DamageToMonstersDecrease).firstData * 0.01;
        }

        return new CalculationPhysicalDamage
        {
            CleanDamage = damage,
            DamagePercentage = damagePercentage,
            DamagePercentageSecond = damagePercentageSecond,
            MultiplyDamage = multiplyDamage,
            EndCriticalDamage = endCriticalDamage,
            IgnoreEnemyDefense = ignoreEnemyDefense,
            VesselLoDDamage = vesselLodDamage,
            VesselGlacernonDamage = vesselGlacernonDamage,
            IncreaseAllDamage = increaseAllDamage,
            IncreaseAllDamageAttackType = increaseAllDamageAttackType,
            IncreaseDamageMagicDefense = increaseDamageMagicDefense,
            IncreaseDamageRace = increaseDamageRace,
            IncreaseDamageRacePercentage = increaseDamageRacePercentage,
            IncreaseLoDDamage = increaseLoDDamage,
            IncreaseVesselDamage = increaseVesselDamage,
            IncreaseDamageFaction = increaseDamageFaction,
            InvisibleDamage = invisibleDamage,
            IncreaseDamageInPvP = increaseDamageInPvP,
            IncreaseAttack = increaseAttack,
            IncreaseAttackAttackType = increaseAttackAttackType,
            IncreaseDamageShadowFairy = increaseDamageShadowFairy,
            IncreaseAllAttacks = increaseAllAttacks,
            IncreaseDamageByDebuffs = damageFromDebuff,
            IncreaseDamageHighMonsters = increaseDamageHighMonsters,
            IncreaseDamageVersusMonsters = increaseDamageVersusMonstersMapType,
            IncreaseAllAttacksVersusMonsters = increaseAllDamageVersusMonsters
        };
    }

    public static CalculationElementDamage CalculateElementDamage(this IBattleEntityDump attacker, IBattleEntityDump defender, SkillInfo skill)
    {
        int element = 0;
        double elementMultiply = 0;

        if (attacker.HasBCard(BCardType.NoCharacteristicValue, (byte)AdditionalTypes.NoCharacteristicValue.AllPowersNullified))
        {
            return new CalculationElementDamage();
        }

        if (skill != null && skill.Element != (byte)attacker.Element)
        {
            return new CalculationElementDamage();
        }

        bool turnOffElement = attacker.Element switch
        {
            ElementType.Neutral => true,
            ElementType.Fire => attacker.HasBCard(BCardType.NoCharacteristicValue, (byte)AdditionalTypes.NoCharacteristicValue.FireElementNullified),
            ElementType.Water => attacker.HasBCard(BCardType.NoCharacteristicValue, (byte)AdditionalTypes.NoCharacteristicValue.WaterElementNullified),
            ElementType.Light => attacker.HasBCard(BCardType.NoCharacteristicValue, (byte)AdditionalTypes.NoCharacteristicValue.LightElementNullified),
            ElementType.Shadow => attacker.HasBCard(BCardType.NoCharacteristicValue, (byte)AdditionalTypes.NoCharacteristicValue.DarkElementNullified)
        };

        if (turnOffElement)
        {
            return new CalculationElementDamage();
        }

        switch (attacker.Element)
        {
            case ElementType.Fire:
                element += attacker.TryFindPartnerSkillInformation(BCardType.Element, (byte)AdditionalTypes.Element.FireIncreased, skill).firstData;
                element -= attacker.GetBCardInformation(BCardType.Element, (byte)AdditionalTypes.Element.FireDecreased).firstData;
                element += attacker.GetBCardInformation(BCardType.IncreaseDamage, (byte)AdditionalTypes.IncreaseDamage.FireIncreased).firstData;
                element -= attacker.GetBCardInformation(BCardType.IncreaseDamage, (byte)AdditionalTypes.IncreaseDamage.FireDecreased).firstData;
                element += attacker.GetShellWeaponEffectValue(ShellEffectType.IncreasedFireProperties);
                element += (int)(element *
                    attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseElementProcent, (byte)AdditionalTypes.IncreaseElementProcent.FireElementIncrease).firstData));
                element -= (int)(element *
                    attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseElementProcent, (byte)AdditionalTypes.IncreaseElementProcent.FireElementDecrease).firstData));
                break;
            case ElementType.Water:
                element += attacker.TryFindPartnerSkillInformation(BCardType.Element, (byte)AdditionalTypes.Element.WaterIncreased, skill).firstData;
                element -= attacker.GetBCardInformation(BCardType.Element, (byte)AdditionalTypes.Element.WaterDecreased).firstData;
                element += attacker.GetBCardInformation(BCardType.IncreaseDamage, (byte)AdditionalTypes.IncreaseDamage.WaterIncreased).firstData;
                element -= attacker.GetBCardInformation(BCardType.IncreaseDamage, (byte)AdditionalTypes.IncreaseDamage.WaterDecreased).firstData;
                element += attacker.GetShellWeaponEffectValue(ShellEffectType.IncreasedWaterProperties);
                element += (int)(element *
                    attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseElementProcent, (byte)AdditionalTypes.IncreaseElementProcent.WaterElementIncrease).firstData));
                element -= (int)(element *
                    attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseElementProcent, (byte)AdditionalTypes.IncreaseElementProcent.WaterElementDecrease).firstData));
                break;
            case ElementType.Light:
                element += attacker.TryFindPartnerSkillInformation(BCardType.Element, (byte)AdditionalTypes.Element.LightIncreased, skill).firstData;
                element -= attacker.GetBCardInformation(BCardType.Element, (byte)AdditionalTypes.Element.LightDecreased).firstData;
                element += attacker.GetBCardInformation(BCardType.IncreaseDamage, (byte)AdditionalTypes.IncreaseDamage.LightIncreased).firstData;
                element -= attacker.GetBCardInformation(BCardType.IncreaseDamage, (byte)AdditionalTypes.IncreaseDamage.LightDecreased).firstData;
                element += attacker.GetShellWeaponEffectValue(ShellEffectType.IncreasedLightProperties);
                element += (int)(element *
                    attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseElementProcent, (byte)AdditionalTypes.IncreaseElementProcent.LightElementIncrease).firstData));
                element -= (int)(element *
                    attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseElementProcent, (byte)AdditionalTypes.IncreaseElementProcent.LightElementDecrease).firstData));

                break;
            case ElementType.Shadow:
                element += attacker.TryFindPartnerSkillInformation(BCardType.Element, (byte)AdditionalTypes.Element.DarkIncreased, skill).firstData;
                element -= attacker.GetBCardInformation(BCardType.Element, (byte)AdditionalTypes.Element.DarkDecreased).firstData;
                element += attacker.GetBCardInformation(BCardType.IncreaseDamage, (byte)AdditionalTypes.IncreaseDamage.DarkIncreased).firstData;
                element -= attacker.GetBCardInformation(BCardType.IncreaseDamage, (byte)AdditionalTypes.IncreaseDamage.DarkDecreased).firstData;
                element += attacker.GetShellWeaponEffectValue(ShellEffectType.IncreasedDarkProperties);
                element += (int)(element *
                    attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseElementProcent, (byte)AdditionalTypes.IncreaseElementProcent.ShadowElementIncrease).firstData));
                element -= (int)(element *
                    attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseElementProcent, (byte)AdditionalTypes.IncreaseElementProcent.ShadowElementDecrease).firstData));
                break;
            case ElementType.Neutral:
                return new CalculationElementDamage();
        }

        element += attacker.GetBCardInformation(BCardType.Element, (byte)AdditionalTypes.Element.AllIncreased).firstData;
        element -= attacker.GetBCardInformation(BCardType.Element, (byte)AdditionalTypes.Element.AllDecreased).firstData;
        element += attacker.GetShellWeaponEffectValue(ShellEffectType.IncreasedElementalProperties);
        element += (int)(element * attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseElementProcent, (byte)AdditionalTypes.IncreaseElementProcent.AllElementIncrease).firstData));
        element -= (int)(element * attacker.GetMultiplier(attacker.GetBCardInformation(BCardType.IncreaseElementProcent, (byte)AdditionalTypes.IncreaseElementProcent.AllElementDecrease).firstData));

        elementMultiply = attacker.Element switch
        {
            ElementType.Fire => defender.Element switch
            {
                ElementType.Neutral => 1.3,
                ElementType.Fire => 1,
                ElementType.Water => 3,
                ElementType.Light => 1,
                ElementType.Shadow => 1.5
            },
            ElementType.Water => defender.Element switch
            {
                ElementType.Neutral => 1.3,
                ElementType.Fire => 3,
                ElementType.Water => 1,
                ElementType.Light => 1.5,
                ElementType.Shadow => 1
            },
            ElementType.Light => defender.Element switch
            {
                ElementType.Neutral => 1.3,
                ElementType.Fire => 1.5,
                ElementType.Water => 1,
                ElementType.Light => 1,
                ElementType.Shadow => 3
            },
            ElementType.Shadow => defender.Element switch
            {
                ElementType.Neutral => 1.3,
                ElementType.Fire => 1,
                ElementType.Water => 1.5,
                ElementType.Light => 3,
                ElementType.Shadow => 1
            },
            _ => elementMultiply
        };

        return new CalculationElementDamage
        {
            Element = element,
            ElementMultiply = elementMultiply
        };
    }

    public static CalculationDefense CalculationDefense(this IBattleEntityDump attacker, IBattleEntityDump defender)
    {
        double increaseDefense = 1;
        double increaseDefenseAttackType = 1;
        (int, double) increaseDefenseByLevel = (0, 1);
        (int, double) increaseDefenseByLevelAttackType = (0, 1);
        double increaseAllDefense = 1;
        double increaseDefenseInPve = 1;

        if (defender.HasBCard(BCardType.Block, (byte)AdditionalTypes.Block.ChanceAllIncreased))
        {
            (int firstData, int secondData, _) = defender.GetBCardInformation(BCardType.Block, (byte)AdditionalTypes.Block.ChanceAllIncreased);
            if (defender.IsSucceededChance(firstData))
            {
                increaseDefense += secondData * 0.01;
            }
        }

        if (defender.HasBCard(BCardType.Block, (byte)AdditionalTypes.Block.ChanceAllDecreased))
        {
            (int firstData, int secondData, _) = defender.GetBCardInformation(BCardType.Block, (byte)AdditionalTypes.Block.ChanceAllDecreased);
            if (defender.IsSucceededChance(firstData))
            {
                increaseDefense -= secondData * 0.01;
            }
        }

        (int firstData, int secondData, int count) increaseDefenseAttackTypeBCard = defender.GetBCardInformation(BCardType.Block, (byte)AdditionalTypes.Block.ChanceMeleeIncreased);
        switch (attacker.AttackType)
        {
            case AttackType.Melee when defender.HasBCard(BCardType.Block, (byte)AdditionalTypes.Block.ChanceMeleeIncreased):
                if (defender.IsSucceededChance(increaseDefenseAttackTypeBCard.firstData))
                {
                    increaseDefenseAttackType += increaseDefenseAttackTypeBCard.secondData * 0.01;
                    defender.BroadcastEffect(EffectType.MeleeDefense);
                }

                break;
            case AttackType.Ranged when defender.HasBCard(BCardType.Block, (byte)AdditionalTypes.Block.ChanceRangedIncreased):
                increaseDefenseAttackTypeBCard = defender.GetBCardInformation(BCardType.Block, (byte)AdditionalTypes.Block.ChanceRangedIncreased);
                if (defender.IsSucceededChance(increaseDefenseAttackTypeBCard.firstData))
                {
                    increaseDefenseAttackType += increaseDefenseAttackTypeBCard.secondData * 0.01;
                    defender.BroadcastEffect(EffectType.RangeDefense);
                }

                break;
            case AttackType.Magical when defender.HasBCard(BCardType.Block, (byte)AdditionalTypes.Block.ChanceMagicalIncreased):
                increaseDefenseAttackTypeBCard = defender.GetBCardInformation(BCardType.Block, (byte)AdditionalTypes.Block.ChanceMagicalIncreased);
                if (defender.IsSucceededChance(increaseDefenseAttackTypeBCard.firstData))
                {
                    increaseDefenseAttackType += increaseDefenseAttackTypeBCard.secondData * 0.01;
                    defender.BroadcastEffect(EffectType.MagicDefense);
                }

                break;
        }

        increaseDefenseAttackTypeBCard = defender.GetBCardInformation(BCardType.Block, (byte)AdditionalTypes.Block.ChanceMeleeDecreased);
        switch (attacker.AttackType)
        {
            case AttackType.Melee when defender.HasBCard(BCardType.Block, (byte)AdditionalTypes.Block.ChanceMeleeDecreased):
                if (defender.IsSucceededChance(increaseDefenseAttackTypeBCard.firstData))
                {
                    increaseDefenseAttackType -= increaseDefenseAttackTypeBCard.secondData * 0.01;
                    defender.BroadcastEffect(EffectType.MeleeDefense);
                }

                break;
            case AttackType.Ranged when defender.HasBCard(BCardType.Block, (byte)AdditionalTypes.Block.ChanceRangedDecreased):
                increaseDefenseAttackTypeBCard = defender.GetBCardInformation(BCardType.Block, (byte)AdditionalTypes.Block.ChanceRangedDecreased);
                if (defender.IsSucceededChance(increaseDefenseAttackTypeBCard.firstData))
                {
                    increaseDefenseAttackType -= increaseDefenseAttackTypeBCard.secondData * 0.01;
                    defender.BroadcastEffect(EffectType.RangeDefense);
                }

                break;
            case AttackType.Magical when defender.HasBCard(BCardType.Block, (byte)AdditionalTypes.Block.ChanceMagicalDecreased):
                increaseDefenseAttackTypeBCard = defender.GetBCardInformation(BCardType.Block, (byte)AdditionalTypes.Block.ChanceMagicalDecreased);
                if (defender.IsSucceededChance(increaseDefenseAttackTypeBCard.firstData))
                {
                    increaseDefenseAttackType -= increaseDefenseAttackTypeBCard.secondData * 0.01;
                    defender.BroadcastEffect(EffectType.MagicDefense);
                }

                break;
        }

        increaseDefenseByLevel.Item1 += defender.GetBCardInformation(BCardType.Absorption, (byte)AdditionalTypes.Absorption.AllAttackDecreased).firstData;
        increaseDefenseByLevel.Item1 -= defender.GetBCardInformation(BCardType.Absorption, (byte)AdditionalTypes.Absorption.AllAttackIncreased).firstData;

        increaseDefenseByLevel.Item2 -= defender.GetMultiplier(attacker.GetBCardInformation(BCardType.Absorption, (byte)AdditionalTypes.Absorption.AllAttackDecreased).secondData);
        increaseDefenseByLevel.Item2 += defender.GetMultiplier(attacker.GetBCardInformation(BCardType.Absorption, (byte)AdditionalTypes.Absorption.AllAttackIncreased).secondData);

        increaseDefenseByLevelAttackType.Item1 += attacker.AttackType switch
        {
            AttackType.Melee => defender.GetBCardInformation(BCardType.Absorption, (byte)AdditionalTypes.Absorption.MeleeAttackDecreased).firstData,
            AttackType.Ranged => defender.GetBCardInformation(BCardType.Absorption, (byte)AdditionalTypes.Absorption.RangedAttackDecreased).firstData,
            AttackType.Magical => defender.GetBCardInformation(BCardType.Absorption, (byte)AdditionalTypes.Absorption.MagicalAttacksDecreased).firstData
        };

        increaseDefenseByLevelAttackType.Item1 -= attacker.AttackType switch
        {
            AttackType.Melee => defender.GetBCardInformation(BCardType.Absorption, (byte)AdditionalTypes.Absorption.MeleeAttackIncreased).firstData,
            AttackType.Ranged => defender.GetBCardInformation(BCardType.Absorption, (byte)AdditionalTypes.Absorption.RangedAttackIncreased).firstData,
            AttackType.Magical => defender.GetBCardInformation(BCardType.Absorption, (byte)AdditionalTypes.Absorption.MagicalAttackIncreased).firstData
        };

        increaseDefenseByLevelAttackType.Item2 -= attacker.AttackType switch
        {
            AttackType.Melee => defender.GetMultiplier(defender.GetBCardInformation(BCardType.Absorption, (byte)AdditionalTypes.Absorption.MeleeAttackDecreased).secondData),
            AttackType.Ranged => defender.GetMultiplier(defender.GetBCardInformation(BCardType.Absorption, (byte)AdditionalTypes.Absorption.RangedAttackDecreased).secondData),
            AttackType.Magical => defender.GetMultiplier(defender.GetBCardInformation(BCardType.Absorption, (byte)AdditionalTypes.Absorption.MagicalAttacksDecreased).secondData)
        };

        increaseDefenseByLevelAttackType.Item2 += attacker.AttackType switch
        {
            AttackType.Melee => defender.GetMultiplier(defender.GetBCardInformation(BCardType.Absorption, (byte)AdditionalTypes.Absorption.MeleeAttackIncreased).secondData),
            AttackType.Ranged => defender.GetMultiplier(defender.GetBCardInformation(BCardType.Absorption, (byte)AdditionalTypes.Absorption.RangedAttackIncreased).secondData),
            AttackType.Magical => defender.GetMultiplier(defender.GetBCardInformation(BCardType.Absorption, (byte)AdditionalTypes.Absorption.MagicalAttackIncreased).secondData)
        };

        increaseAllDefense = 1 + defender.GetMultiplier(defender.GetBCardInformation(BCardType.Item, (byte)AdditionalTypes.Item.DefenceIncreased).firstData);

        int maximumCriticalDamage = defender.GetBCardInformation(BCardType.VulcanoElementBuff, (byte)AdditionalTypes.VulcanoElementBuff.CriticalDefence).firstData;

        bool isPvP = attacker.IsPlayer() && defender.IsPlayer();
        double defenseInPvP = 1;
        (int firstData, int secondData, int count) pvpBCard = defender.GetBCardInformation(BCardType.LeonaPassiveSkill, (byte)AdditionalTypes.LeonaPassiveSkill.DefenceIncreasedInPVP);
        if (isPvP)
        {
            defenseInPvP -= pvpBCard.firstData * 0.01;

            pvpBCard = attacker.GetBCardInformation(BCardType.LeonaPassiveSkill, (byte)AdditionalTypes.LeonaPassiveSkill.DefenceDecreasedInPVP);
            defenseInPvP += pvpBCard.firstData * 0.01;
            defenseInPvP += attacker.GetShellWeaponEffectValue(ShellEffectType.ReducesPercentageEnemyDefenceInPVP) * 0.01;
        }
        else
        {
            if (pvpBCard.secondData == 1)
            {
                defenseInPvP += pvpBCard.firstData * 0.01;
            }
        }

        int multiplyDefenseInt = defender.GetBCardInformation(BCardType.MultDefence, (byte)AdditionalTypes.MultDefence.AllDefenceIncreased).firstData;
        multiplyDefenseInt += attacker.AttackType switch
        {
            AttackType.Melee => defender.GetBCardInformation(BCardType.MultDefence, (byte)AdditionalTypes.MultDefence.MeleeDefenceIncreased).firstData,
            AttackType.Ranged => defender.GetBCardInformation(BCardType.MultDefence, (byte)AdditionalTypes.MultDefence.RangedDefenceIncreased).firstData,
            AttackType.Magical => defender.GetBCardInformation(BCardType.MultDefence, (byte)AdditionalTypes.MultDefence.MagicalDefenceIncreased).firstData
        };

        multiplyDefenseInt -= defender.GetBCardInformation(BCardType.MultDefence, (byte)AdditionalTypes.MultDefence.AllDefenceDecreased).firstData;
        multiplyDefenseInt -= attacker.AttackType switch
        {
            AttackType.Melee => defender.GetBCardInformation(BCardType.MultDefence, (byte)AdditionalTypes.MultDefence.MeleeDefenceDecreased).firstData,
            AttackType.Ranged => defender.GetBCardInformation(BCardType.MultDefence, (byte)AdditionalTypes.MultDefence.RangedDefenceDecreased).firstData,
            AttackType.Magical => defender.GetBCardInformation(BCardType.MultDefence, (byte)AdditionalTypes.MultDefence.MagicalDefenceDecreased).firstData
        };

        int multiplyDefense = multiplyDefenseInt == 0 ? 1 : multiplyDefenseInt;

        return new CalculationDefense
        {
            IncreaseDefense = increaseDefense,
            IncreaseDefenseAttackType = increaseDefenseAttackType,
            IncreaseDefenseByLevel = increaseDefenseByLevel,
            IncreaseDefenseByLevelAttackType = increaseDefenseByLevelAttackType,
            IncreaseAllDefense = increaseAllDefense,
            MaximumCriticalDamage = maximumCriticalDamage,
            DefenseInPvP = defenseInPvP,
            IncreaseDefenseInPve = increaseDefenseInPve,
            MultiplyDefense = multiplyDefense
        };
    }

    public static CalculationResult DamageResult(
        this IBattleEntityDump attacker,
        IBattleEntityDump defender,
        CalculationBasicStatistics baseStatistics,
        CalculationDefense defense,
        CalculationPhysicalDamage physicalDamage,
        CalculationElementDamage elementDamage,
        SkillInfo skill)
    {
        bool isCritical = false;

        bool isSoftDamage = false;
        bool isHighMonsterDamage = false;
        double softDamageMultiplier = 0;

        int distance = attacker.Position.GetDistance(defender.Position);

        #region Attacker Values

        int attackerMorale = baseStatistics.AttackerMorale;
        int attackerAttackUpgrade = baseStatistics.AttackerAttackUpgrade;
        int attackerCriticalChance = baseStatistics.AttackerCriticalChance;
        double attackerCriticalDamage = baseStatistics.AttackerCriticalDamage * 0.01;
        double attackerElementRate = baseStatistics.AttackerElementRate * 0.01;

        ElementType attackerElement = attacker.Element;
        AttackType attackType = attacker.AttackType;

        int basicDamage = _randomGenerator.RandomNumber(attacker.DamageMinimum + attacker.WeaponDamageMinimum, attacker.DamageMaximum + attacker.WeaponDamageMaximum);

        int weaponMin = attacker.WeaponDamageMinimum;
        int weaponMax = attacker.WeaponDamageMaximum;

        double shellDamagePercentage = attacker.GetShellWeaponEffectValue(ShellEffectType.PercentageTotalDamage) * 0.01;

        #endregion

        #region Defender Values

        int defenderMorale = baseStatistics.DefenderMorale;
        int defenderDefenseUpgrade = baseStatistics.DefenderDefenseUpgrade;
        int defenderDefense = baseStatistics.DefenderDefense;
        int defenderResistance = baseStatistics.DefenderResistance;

        #endregion

        #region Defense Values

        double increaseDefense = defense.IncreaseDefense; // 11, 1
        double increaseDefenseAttackType = defense.IncreaseDefenseAttackType; // 11, 3-5-7
        (int, double) increaseDefenseByLevel = defense.IncreaseDefenseByLevel; // 12, 1
        (int, double) increaseDefenseByLevelAttackType = defense.IncreaseDefenseByLevelAttackType; // 12, 3-5-7
        double increaseAllDefense = defense.IncreaseAllDefense; // 44, 4
        int maximumCriticalDamage = defense.MaximumCriticalDamage; // 66, 7
        double defenseInPvP = defense.DefenseInPvP; // 71, 7
        double increaseDefenseInPve = defense.IncreaseDefenseInPve;
        double multiplyDefense = defense.MultiplyDefense;

        #endregion

        #region Psychical Damage

        int cleanDamage = physicalDamage.CleanDamage;
        double damagePercentage = physicalDamage.DamagePercentage; // 5, 5
        double damagePercentageSecond = physicalDamage.DamagePercentageSecond; // 8, 5
        double multiplierDamage = physicalDamage.MultiplyDamage; // 34, 1
        double endCriticalDamage = physicalDamage.EndCriticalDamage; // 38, 5
        double ignoreEnemyDefense = physicalDamage.IgnoreEnemyDefense; // 84, 1
        double vesselLodDamage = physicalDamage.VesselLoDDamage; // 90, 3
        double vesselGlacernonDamage = physicalDamage.VesselGlacernonDamage; // 90, 7
        double increaseAllDamage = physicalDamage.IncreaseAllDamage; // 103, 1
        double increaseAllDamageAttackType = physicalDamage.IncreaseAllDamageAttackType; // 103, 3-5-7
        double increaseDamageMagicDefense = physicalDamage.IncreaseDamageMagicDefense; // 108, 9
        int increaseDamageRace = physicalDamage.IncreaseDamageRace; // 24, 1
        double increaseDamageRacePercentage = physicalDamage.IncreaseDamageRacePercentage; // 71, 1
        double increaseLodDamage = physicalDamage.IncreaseLoDDamage; // 101, 1
        double increaseVesselDamage = physicalDamage.IncreaseVesselDamage; // 101, 3
        double increaseDamageFaction = physicalDamage.IncreaseDamageFaction; // 85, 7-9
        int invisibleDamage = physicalDamage.InvisibleDamage; // 43, 7
        double increaseDamageInPvP = physicalDamage.IncreaseDamageInPvP; // 71, 9
        double increaseAttack = physicalDamage.IncreaseAttack; // 15, 1
        double increaseAttackAttackType = physicalDamage.IncreaseAttackAttackType; // 15, 3-5-7
        double increaseDamageShadowFairy = physicalDamage.IncreaseDamageShadowFairy; // 80, 9
        double increaseAllAttacks = physicalDamage.IncreaseAllAttacks; // 44, 3
        double increaseDamageByDebuffs = physicalDamage.IncreaseDamageByDebuffs;
        double increaseDamageHighMonsters = physicalDamage.IncreaseDamageHighMonsters; // 86, 5
        double increaseDamageVersusMonsters = physicalDamage.IncreaseDamageVersusMonsters;
        double increaseAllAttacksVersusMonsters = physicalDamage.IncreaseAllAttacksVersusMonsters;

        #endregion

        #region Element Damage

        int element = elementDamage.Element;
        double elementMultiply = elementDamage.ElementMultiply;

        #endregion

        if (skill != null && skill.Element != (byte)attackerElement)
        {
            attackerElementRate = 0;
        }

        if (attacker.HasBCard(BCardType.Mode, (byte)AdditionalTypes.Mode.EffectNoDamage))
        {
            return new CalculationResult(0, false, false);
        }

        basicDamage += cleanDamage;
        basicDamage += invisibleDamage;
        basicDamage += increaseDamageRace;

        basicDamage += attacker.GetShellWeaponEffectValue(ShellEffectType.DamageImproved);

        if (increaseDamageRacePercentage > 0)
        {
            basicDamage = (int)(basicDamage * increaseDamageRacePercentage);
        }

        double damageFromMap = vesselLodDamage * vesselGlacernonDamage;

        if (increaseDamageMagicDefense > 0 && attackType == AttackType.Magical)
        {
            basicDamage += (int)Math.Floor(attacker.MagicalDefense * increaseDamageMagicDefense);
        }

        if (increaseDamageHighMonsters > 0)
        {
            isHighMonsterDamage = true;
        }

        int plusDifference = attackerAttackUpgrade - defenderDefenseUpgrade;

        bool countWeaponPlus = false;
        int additionalDefense = 0;

        if (Math.Abs(plusDifference) > Math.Abs(increaseDefenseByLevel.Item1))
        {
            increaseDefenseByLevel.Item2 = 1;
        }

        if (Math.Abs(plusDifference) > Math.Abs(increaseDefenseByLevelAttackType.Item1))
        {
            increaseDefenseByLevelAttackType.Item2 = 1;
        }

        if (plusDifference > 0)
        {
            plusDifference = Math.Abs(plusDifference);
            if (plusDifference > 10)
            {
                plusDifference = 10;
            }

            weaponMin = (int)(weaponMin * _plus[plusDifference]);
            weaponMax = (int)(weaponMax * _plus[plusDifference]);
            countWeaponPlus = true;
        }
        else if (plusDifference < 0)
        {
            plusDifference = Math.Abs(plusDifference);
            if (plusDifference > 10)
            {
                plusDifference = 10;
            }

            additionalDefense += (int)Math.Floor(defenderDefense * _plus[plusDifference]);
        }

        if (attackType != AttackType.Magical && attackerCriticalChance != 0 && attacker.IsSucceededChance(attackerCriticalChance))
        {
            isCritical = true;
        }

        if (damagePercentageSecond > 0 && isHighMonsterDamage)
        {
            softDamageMultiplier = increaseDamageHighMonsters + damagePercentageSecond + increaseDamageHighMonsters * damagePercentageSecond;
            isSoftDamage = true;
        }
        else if (damagePercentageSecond > 0)
        {
            softDamageMultiplier = damagePercentageSecond;
            isSoftDamage = true;
        }
        else if (isHighMonsterDamage)
        {
            softDamageMultiplier = increaseDamageHighMonsters;
            isSoftDamage = true;
        }

        double defenderResistanceMultiplier = defenderResistance >= 100 ? 0 : 1.0 - defenderResistance * 0.01;

        int minDamage = basicDamage + (countWeaponPlus ? weaponMin : 0);
        int maxDamage = basicDamage + (countWeaponPlus ? weaponMax : 0);

        int calculatedDamage = _randomGenerator.RandomNumber(minDamage, maxDamage);

        #region Normal Damage

        int normalDmg = (int)((attacker.PhysicalDamageShell(calculatedDamage, shellDamagePercentage, increaseDefenseByLevel.Item2, increaseDefenseByLevelAttackType.Item2) +
            attacker.PhysicalDamageIncreaseDefenseByLevel(calculatedDamage, increaseDefenseByLevel.Item2, increaseDefenseByLevelAttackType.Item2) +
            attacker.PhysicalDamageIncreaseDefenseByLevelAttackType(calculatedDamage, increaseDefenseByLevelAttackType.Item2, increaseDefenseByLevel.Item2) +
            attacker.PhysicalDamageIncreaseAttack(calculatedDamage, defenderDefense, additionalDefense, increaseAttack, increaseDefenseByLevel.Item2, increaseDefenseByLevelAttackType.Item2) +
            attacker.PhysicalDamageIncreaseDefense(calculatedDamage, defenderDefense, additionalDefense, increaseDefenseAttackType, increaseDefenseByLevel.Item2,
                increaseDefenseByLevelAttackType.Item2) +
            attacker.PhysicalDamageMultiplier(calculatedDamage, multiplierDamage, increaseDefenseByLevel.Item2, increaseDefenseByLevelAttackType.Item2)) * increaseDefense);

        normalDmg = attacker.Penalties(defender, normalDmg, distance);

        normalDmg = (int)(normalDmg * damagePercentage);
        normalDmg = (int)(normalDmg * increaseAttack);
        normalDmg = (int)(normalDmg * increaseDefenseAttackType);

        #endregion

        #region Basic Damage

        int basicDmg = (int)(calculatedDamage * increaseAttack * increaseDefense);
        basicDmg = attacker.Penalties(defender, basicDmg, distance);

        #endregion

        #region Critical Damage

        int criticalDmg = 0;
        if (isCritical)
        {
            criticalDmg = (int)((attacker.PhysicalCriticalDamage(calculatedDamage, defenderDefense, attackerCriticalDamage, multiplierDamage) +
                attacker.PhysicalCriticalDamageShell(calculatedDamage, defenderDefense, shellDamagePercentage, increaseDefenseByLevel.Item2,
                    increaseDefenseByLevelAttackType.Item2, attackerCriticalDamage, multiplierDamage) +
                attacker.PhysicalCriticalDamageIncreaseDefenseByLevel(calculatedDamage, attackerCriticalDamage, increaseDefenseByLevel.Item2, increaseDefenseByLevelAttackType.Item2) +
                attacker.PhysicalCriticalDamageIncreaseDefenseByLevelAttackType(calculatedDamage, attackerCriticalDamage, increaseDefenseByLevel.Item2, increaseDefenseByLevelAttackType.Item2) +
                attacker.PhysicalCriticalIncreaseAttack(calculatedDamage, increaseAttack, attackerCriticalDamage, increaseDefenseByLevel.Item2, increaseDefenseByLevelAttackType.Item2,
                    multiplierDamage)) * increaseDefense);

            criticalDmg = attacker.Penalties(defender, criticalDmg, distance);

            criticalDmg = (int)(criticalDmg * damagePercentage);
            criticalDmg = (int)(criticalDmg * increaseAttack);
            criticalDmg = (int)(criticalDmg * increaseDefenseAttackType);
        }

        #endregion

        #region Element Damage

        int elementDmg = 0;
        if (attackerElement != ElementType.Neutral)
        {
            elementDmg =
                attacker.ElementFairyDamage(calculatedDamage, attackerElementRate) +
                attacker.ElementFairyDamageShell(calculatedDamage, shellDamagePercentage, attackerElementRate) +
                attacker.ElementFairyDamageMultiplier(calculatedDamage, multiplierDamage, attackerElementRate);
        }

        #endregion

        #region Physical Soft Damage

        int physicalSoft = 0;
        if (isSoftDamage)
        {
            physicalSoft = (int)((attacker.SoftDamageMultiplier(calculatedDamage, softDamageMultiplier, multiplierDamage) +
                attacker.SoftDamageShell(calculatedDamage, softDamageMultiplier, shellDamagePercentage, increaseDefenseByLevel.Item2, increaseDefenseByLevelAttackType.Item2) +
                attacker.SoftDamageIncreaseDefenseByLevel(calculatedDamage, softDamageMultiplier, increaseDefenseByLevel.Item2, increaseDefenseByLevelAttackType.Item2) +
                attacker.SoftDamageIncreaseDefenseByLevelAttackType(calculatedDamage, softDamageMultiplier, increaseDefenseByLevelAttackType.Item2, increaseDefenseByLevel.Item2) +
                attacker.SoftDamageIncreaseAttack(calculatedDamage, increaseAttack, softDamageMultiplier, increaseDefense, increaseDefenseAttackType)) * increaseDefense);

            physicalSoft = attacker.Penalties(defender, physicalSoft, distance);

            physicalSoft = (int)(physicalSoft * damagePercentage);
            physicalSoft = (int)(physicalSoft * increaseAttack);
            physicalSoft = (int)(physicalSoft * increaseDefenseAttackType);
        }

        #endregion

        #region Critical Soft Damage

        int criticalSoft = 0;
        if (isCritical && isSoftDamage)
        {
            criticalSoft = (int)((attacker.PhysicalSoftCriticalDamage(calculatedDamage, softDamageMultiplier, attackerCriticalDamage, multiplierDamage) +
                attacker.PhysicalSoftCriticalDamageShell(calculatedDamage, softDamageMultiplier, attackerCriticalDamage,
                    shellDamagePercentage, increaseDefenseByLevel.Item2, increaseDefenseByLevelAttackType.Item2) +
                attacker.PhysicalSoftCriticalIncreaseDefenseByLevel(calculatedDamage, softDamageMultiplier,
                    attackerCriticalDamage, increaseDefenseByLevel.Item2, increaseDefenseByLevelAttackType.Item2) +
                attacker.PhysicalSoftCriticalIncreaseDefenseByLevelAttackType(calculatedDamage, softDamageMultiplier,
                    attackerCriticalDamage, increaseDefenseByLevelAttackType.Item2, increaseDefenseByLevel.Item2) +
                attacker.PhysicalSoftCriticalIncreaseAttack(calculatedDamage, softDamageMultiplier, attackerCriticalDamage,
                    increaseAttack, increaseDefenseByLevel.Item2, increaseDefenseByLevelAttackType.Item2)) * increaseDefense);

            criticalSoft = attacker.Penalties(defender, criticalSoft, distance);

            criticalSoft = (int)(criticalSoft * damagePercentage);
            criticalSoft = (int)(criticalSoft * increaseAttack);
            criticalSoft = (int)(criticalSoft * increaseDefenseAttackType);
        }

        #endregion

        #region Element Soft Damage

        int elementSoft = 0;
        if (attackerElement != ElementType.Neutral && isSoftDamage)
        {
            elementSoft =
                attacker.SoftElementDamage(calculatedDamage, softDamageMultiplier, attackerElementRate, multiplierDamage) +
                attacker.SoftElementDamageShell(calculatedDamage, shellDamagePercentage, softDamageMultiplier, attackerElementRate);
        }

        #endregion

        /* FINAL PHYSICAL DAMAGE */

        int physicalDmg = (int)(attackerMorale + (basicDmg + normalDmg + criticalDmg + physicalSoft + criticalSoft) * endCriticalDamage * increaseAllDamageAttackType * increaseAllDamage *
            increaseAllAttacksVersusMonsters);
        double magicialDamageReduction = defender is PlayerBattleEntityDump playerBattleEntityDump && attackType == AttackType.Magical ? playerBattleEntityDump.DecreaseMagicDamage : 1;
        physicalDmg = (int)Math.Floor(physicalDmg * magicialDamageReduction);

        int defenceByLevel;
        if (defender.IsPlayer())
        {
            defenceByLevel = -15;
        }
        else
        {
            defenceByLevel = attacker.GetMonsterDamageBonus(defender.Level);
        }

        int defenderDefenseDamage = (int)(defenderMorale + defenceByLevel + (defenderDefense + additionalDefense) * ignoreEnemyDefense * increaseAllDefense * increaseDefenseInPve * multiplyDefense);

        physicalDmg -= defenderDefenseDamage <= 0 ? 0 : defenderDefenseDamage;


        /* FINAL ELEMENT DAMAGE */

        int elementalDamage = (int)(element + (elementDmg + elementSoft) * increaseAllDamageAttackType * increaseAllDamage);

        if (elementalDamage != 0)
        {
            elementalDamage = (int)Math.Floor(elementalDamage * increaseDefense);

            elementalDamage = (int)Math.Floor(elementalDamage * increaseDamageShadowFairy);

            elementalDamage = (int)Math.Floor(elementalDamage * elementMultiply);

            elementalDamage = (int)Math.Floor(elementalDamage * defenderResistanceMultiplier);
        }

        /* FINAL DAMAGE */

        int finalDamage = physicalDmg + elementalDamage;

        finalDamage = (int)Math.Floor(finalDamage * increaseAllAttacks);

        finalDamage = (int)Math.Floor(finalDamage * increaseDamageByDebuffs);

        finalDamage = (int)Math.Floor(finalDamage * damageFromMap);
        finalDamage = (int)Math.Floor(finalDamage * (1 + attacker.GetFamilyUpgradeValue(FamilyUpgradeType.INCREASE_ATTACK_DEFENSE) * 0.01));

        finalDamage = (int)Math.Floor(finalDamage * increaseDamageInPvP);

        finalDamage = (int)Math.Floor(finalDamage * increaseLodDamage);

        finalDamage = (int)Math.Floor(finalDamage * increaseVesselDamage);

        finalDamage = (int)Math.Floor(finalDamage * increaseDamageFaction);

        finalDamage = (int)Math.Floor(finalDamage * increaseDamageVersusMonsters);
        if (attacker is PlayerBattleEntityDump { IncreaseJajamaruDamage: true })
        {
            finalDamage = (int)Math.Floor(finalDamage * 1.5);
        }

        finalDamage = (int)Math.Floor(finalDamage * defenseInPvP);

        finalDamage = (int)Math.Floor(finalDamage * (1 - defender.GetFamilyUpgradeValue(FamilyUpgradeType.INCREASE_ATTACK_DEFENSE) * 0.01));

        if (isCritical && maximumCriticalDamage != 0)
        {
            finalDamage = maximumCriticalDamage;
        }

        if (attacker.HasBCard(BCardType.RecoveryAndDamagePercent, (byte)AdditionalTypes.RecoveryAndDamagePercent.DecreaseEnemyHP))
        {
            double maxHpPercentage = attacker.GetBCardInformation(BCardType.RecoveryAndDamagePercent, (byte)AdditionalTypes.RecoveryAndDamagePercent.DecreaseEnemyHP).firstData * 0.01;
            finalDamage = (int)(defender.MaxHp * maxHpPercentage);
        }

        if (defender.HasBCard(BCardType.RecoveryAndDamagePercent, (byte)AdditionalTypes.RecoveryAndDamagePercent.DecreaseSelfHP))
        {
            double maxHpPercentage = defender.GetBCardInformation(BCardType.RecoveryAndDamagePercent, (byte)AdditionalTypes.RecoveryAndDamagePercent.DecreaseSelfHP).firstData * 0.01;
            finalDamage = (int)(defender.MaxHp * maxHpPercentage);
        }

        if (finalDamage <= 0)
        {
            finalDamage = 1;
        }

        finalDamage += (int)(GetMonsterBaseDamage(attacker, defender) * increaseAttack * increaseDefense);

        return new CalculationResult(finalDamage, isCritical, isSoftDamage);
    }

    private static int GetMonsterBaseDamage(IBattleEntityDump attacker, IBattleEntityDump defender)
    {
        if (!attacker.IsMonster())
        {
            return 0;
        }

        int monsterLevel = attacker.Level;
        int multiplier = monsterLevel switch
        {
            < 30 => 0,
            <= 50 => 1,
            < 60 => 2,
            < 65 => 3,
            < 70 => 4,
            _ => 5
        };

        double damageReduction = defender is PlayerBattleEntityDump playerBattleEntityDump ? playerBattleEntityDump.MinimalDamageReduction : 1;

        return (int)(monsterLevel * multiplier * damageReduction);
    }

    private static int Penalties(this IBattleEntityDump attacker, IBattleEntityDump defender, int damage, int distance)
    {
        if (attacker.AttackType != AttackType.Ranged)
        {
            return damage;
        }

        bool hasRangePenalty = !attacker.HasBCard(BCardType.GuarantedDodgeRangedAttack, (byte)AdditionalTypes.GuarantedDodgeRangedAttack.NoPenalty);
        bool increaseDamageRangeDistance = attacker.HasBCard(BCardType.GuarantedDodgeRangedAttack, (byte)AdditionalTypes.GuarantedDodgeRangedAttack.DistanceDamageIncreasing);

        int returnDamage = damage;
        returnDamage = distance switch
        {
            <= 2 when hasRangePenalty => (int)(returnDamage * 0.7),
            > 2 when increaseDamageRangeDistance => (int)(returnDamage * (0.95 + 0.05 * distance)),
            _ => returnDamage
        };

        return returnDamage;
    }

    /*
     * Calculation:
     * 1. Clean damage
     * 2. Element damage
     * 3. Critical damage
     * 4. Soft Damage
     */

    // Calculate all bonuses for physical damage

    private static int PhysicalDamageShell(this IBattleEntityDump attacker, int cleanDamage, double shellPercentage, double increaseDefenseByLevel, double increaseDefenseByLevelAttackType)
        => (int)Math.Floor((cleanDamage + 15) * shellPercentage * increaseDefenseByLevel * increaseDefenseByLevelAttackType);

    private static int PhysicalDamageIncreaseAttack(this IBattleEntityDump attacker, int cleanDamage, int defense, int additionalDefense, double increaseAttack, double increaseDefenseByLevel,
        double increaseDefenseByLevelAttackType)
        => (int)Math.Floor((cleanDamage - defense - additionalDefense) / (1 / (increaseAttack - 1 < 0 ? 0 : increaseAttack - 1) + 1) * increaseDefenseByLevel * increaseDefenseByLevelAttackType);

    private static int PhysicalDamageIncreaseDefense(this IBattleEntityDump attacker, int cleanDamage, int defense, int additionalDefense, double increaseDefenseAttackType,
        double increaseDefenseByLevel,
        double increaseDefenseByLevelAttackType)
        => (int)Math.Floor((cleanDamage + 15 - defense - additionalDefense) / (1 / (increaseDefenseAttackType - 1 < 0 ? 0 : increaseDefenseAttackType - 1) + 1) * increaseDefenseByLevel *
            increaseDefenseByLevelAttackType);

    private static int PhysicalDamageIncreaseDefenseByLevel(this IBattleEntityDump attacker, int cleanDamage, double increaseDefenseByLevel, double increaseDefenseByLevelAttackType)
        => increaseDefenseByLevel == 1.0
            ? 0
            : (int)Math.Floor((cleanDamage + 15) / (1 / (increaseDefenseByLevel - 1 < 0 ? 0 : increaseDefenseByLevel - 1) + 1) * increaseDefenseByLevel * increaseDefenseByLevelAttackType);

    private static int PhysicalDamageIncreaseDefenseByLevelAttackType(this IBattleEntityDump attacker, int cleanDamage, double increaseDefenseByLevelAttackType, double increaseDefenseByLevel)
        => increaseDefenseByLevelAttackType == 1.0
            ? 0
            : (int)Math.Floor((cleanDamage + 15) / (1 / (increaseDefenseByLevelAttackType - 1 < 0 ? 0 : increaseDefenseByLevelAttackType - 1) + 1) * increaseDefenseByLevel *
                increaseDefenseByLevelAttackType);

    private static int PhysicalDamageMultiplier(this IBattleEntityDump attacker, int cleanDamage, double multiplyDamage, double increaseDefenseByLevel, double increaseDefenseByLevelAttackType)
        => (int)Math.Floor((cleanDamage + 15) * (multiplyDamage - 1) * increaseDefenseByLevel * increaseDefenseByLevelAttackType);

    // Calculate all bonuses for element damage

    private static int ElementFairyDamage(this IBattleEntityDump attacker, int cleanDamage, double fairyMultiplier) => (int)Math.Floor((cleanDamage + 115) * fairyMultiplier);

    private static int ElementFairyDamageShell(this IBattleEntityDump attacker, int cleanDamage, double shellPercentage, double fairyMultiplier) =>
        (int)Math.Floor((cleanDamage + 75) * shellPercentage * fairyMultiplier);

    private static int ElementFairyDamageMultiplier(this IBattleEntityDump attacker, int cleanDamage, double multiplierDamage, double fairyMultiplier) =>
        (int)Math.Floor((cleanDamage + 15) * fairyMultiplier * (multiplierDamage - 1));

    // Calculate all bonuses for critical damage

    private static int PhysicalCriticalDamage(this IBattleEntityDump attacker, int cleanDamage, int defense, double criticalMultiplier, double multiplierDamage)
        => (int)Math.Floor((cleanDamage + 15 - defense) * criticalMultiplier * multiplierDamage);

    private static int PhysicalCriticalDamageShell(this IBattleEntityDump attacker, int cleanDamage, int defense, double shellPercentage, double increaseDefenseByLevel,
        double increaseDefenseByLevelAttackType, double criticalMultiplier, double multiplierDamage)
        => (int)Math.Floor((cleanDamage + 15 - defense) * criticalMultiplier * shellPercentage * increaseDefenseByLevel * increaseDefenseByLevelAttackType * multiplierDamage);

    // Calculate all bonuses for critical soft damage

    private static int PhysicalCriticalIncreaseAttack(this IBattleEntityDump attacker, int cleanDamage, double increaseAttack, double criticalMultiplier, double increaseDefenseByLevel,
        double increaseDefenseByLevelAttackType,
        double multiplierDamage) => (int)Math.Floor(cleanDamage / (1 / (increaseAttack - 1 < 0 ? 0 : increaseAttack - 1) + 1) * criticalMultiplier * increaseDefenseByLevel *
        increaseDefenseByLevelAttackType * multiplierDamage);

    private static int PhysicalCriticalDamageIncreaseDefenseByLevel(this IBattleEntityDump attacker, int cleanDamage, double criticalMultiplier, double increaseDefenseByLevel,
        double increaseDefenseByLevelAttackType)
        => increaseDefenseByLevel == 1.0
            ? 0
            : (int)Math.Floor((cleanDamage + 15) / (1 / (increaseDefenseByLevel - 1 < 0 ? 0 : increaseDefenseByLevel - 1) + 1) * criticalMultiplier * increaseDefenseByLevel *
                increaseDefenseByLevelAttackType);

    private static int PhysicalCriticalDamageIncreaseDefenseByLevelAttackType(this IBattleEntityDump attacker, int cleanDamage, double criticalMultiplier, double increaseDefenseByLevel,
        double increaseDefenseByLevelAttackType)
        => increaseDefenseByLevelAttackType == 1.0
            ? 0
            : (int)Math.Floor((cleanDamage + 15) / (1 / (increaseDefenseByLevelAttackType - 1 < 0 ? 0 : increaseDefenseByLevelAttackType - 1) + 1) * criticalMultiplier * increaseDefenseByLevel *
                increaseDefenseByLevelAttackType);

    private static int PhysicalSoftCriticalDamage(this IBattleEntityDump attacker, int cleanDamage, double softDamageMultiplier, double criticalMultiplier, double damageMultiplier)
        => (int)Math.Floor((cleanDamage + 15) * softDamageMultiplier * criticalMultiplier * damageMultiplier);

    private static int PhysicalSoftCriticalDamageShell(this IBattleEntityDump attacker, int cleanDamage, double softDamageMultiplier, double criticalMultiplier, double shellPercentage,
        double increaseDefenseByLevel,
        double increaseDefenseByLevelAttackType) =>
        (int)Math.Floor((cleanDamage + 15) * softDamageMultiplier * criticalMultiplier * shellPercentage * increaseDefenseByLevel * increaseDefenseByLevelAttackType);

    private static int PhysicalSoftCriticalIncreaseAttack(this IBattleEntityDump attacker, int cleanDamage, double softDamageMultiplier, double criticalMultiplier, double increaseAttack,
        double increaseDefenseByLevel,
        double increaseDefenseByLevelAttackType) => (int)Math.Floor(cleanDamage / (1 / (increaseAttack - 1 < 0 ? 0 : increaseAttack - 1) + 1) * softDamageMultiplier * criticalMultiplier *
        increaseDefenseByLevel * increaseDefenseByLevelAttackType);

    private static int PhysicalSoftCriticalIncreaseDefenseByLevel(this IBattleEntityDump attacker, int cleanDamage, double softDamageMultiplier, double criticalMultiplier,
        double increaseDefenseByLevel, double increaseDefenseByLevelAttackType)
        => increaseDefenseByLevel == 1.0
            ? 0
            : (int)Math.Floor(cleanDamage / (1 / (increaseDefenseByLevel - 1) + 1) * softDamageMultiplier * criticalMultiplier * increaseDefenseByLevel * increaseDefenseByLevelAttackType);

    private static int PhysicalSoftCriticalIncreaseDefenseByLevelAttackType(this IBattleEntityDump attacker, int cleanDamage, double softDamageMultiplier, double criticalMultiplier,
        double increaseDefenseByLevelAttackType, double increaseDefenseByLevel)
        => increaseDefenseByLevelAttackType == 1.0
            ? 0
            : (int)Math.Floor(cleanDamage * increaseDefenseByLevelAttackType * softDamageMultiplier * criticalMultiplier * increaseDefenseByLevelAttackType * increaseDefenseByLevel);

    // Calculate all bonuses for physical soft damage

    private static int SoftDamageMultiplier(this IBattleEntityDump attacker, int cleanDamage, double softDamageMultiplier, double multiplierDamage)
        => (int)Math.Floor((cleanDamage + 15) * softDamageMultiplier * multiplierDamage);

    private static int SoftDamageShell(this IBattleEntityDump attacker, int cleanDamage, double softDamageMultiplier, double shellPercentage, double increaseDefenseByLevel,
        double increaseDefenseByLevelAttackType)
        => (int)Math.Floor((cleanDamage + 15) * softDamageMultiplier * shellPercentage * increaseDefenseByLevel * increaseDefenseByLevelAttackType);

    private static int SoftDamageIncreaseAttack(this IBattleEntityDump attacker, int cleanDamage, double increaseAttack, double softDamageMultiplier, double increaseDefense,
        double increaseDefenseAttackType)
        => (int)Math.Floor(cleanDamage * increaseAttack * softDamageMultiplier * increaseDefense * increaseDefenseAttackType);

    private static int SoftDamageIncreaseDefenseByLevel(this IBattleEntityDump attacker, int cleanDamage, double softDamageMultiplier, double increaseDefenseByLevel,
        double increaseDefenseByLevelAttackType)
        => increaseDefenseByLevel == 1.0
            ? 0
            : (int)Math.Floor((cleanDamage + 15) / (1 / (increaseDefenseByLevel - 1 < 0 ? 0 : increaseDefenseByLevel - 1) + 1) * softDamageMultiplier * increaseDefenseByLevel *
                increaseDefenseByLevelAttackType);

    private static int SoftDamageIncreaseDefenseByLevelAttackType(this IBattleEntityDump attacker, int cleanDamage, double softDamageMultiplier, double increaseDefenseByLevelAttackType,
        double increaseDefenseByLevel)
        => increaseDefenseByLevelAttackType == 1.0
            ? 0
            : (int)Math.Floor((cleanDamage + 15) / (1 / (increaseDefenseByLevelAttackType - 1 < 0 ? 0 : increaseDefenseByLevelAttackType - 1) + 1) * softDamageMultiplier * increaseDefenseByLevel *
                increaseDefenseByLevelAttackType);

    // Calculate all bonuses for element soft damage

    private static int SoftElementDamage(this IBattleEntityDump attacker, int cleanDamage, double softDamageMultiplier, double elementMultiplier, double multiplierDamage)
        => (int)Math.Floor((cleanDamage + 15) * softDamageMultiplier * elementMultiplier * multiplierDamage);

    private static int SoftElementDamageShell(this IBattleEntityDump attacker, int cleanDamage, double shellPercentage, double softDamageMultiplier, double elementMultiplier)
        => (int)Math.Floor((cleanDamage + 15) * softDamageMultiplier * shellPercentage * elementMultiplier);
}