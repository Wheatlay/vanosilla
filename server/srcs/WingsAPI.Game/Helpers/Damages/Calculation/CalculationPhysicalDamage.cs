namespace WingsEmu.Game.Helpers.Damages.Calculation;

public class CalculationPhysicalDamage
{
    // BCardDTO - Type, SubType
    public int CleanDamage { get; set; }

    public double DamagePercentage { get; set; } // 5, 5

    public double DamagePercentageSecond { get; set; } // 8, 1

    public double MultiplyDamage { get; set; } // 34, 1

    public double EndCriticalDamage { get; set; } // 38, 5

    public double IgnoreEnemyDefense { get; set; } // 84, 1

    public double VesselLoDDamage { get; set; } // 90, 3

    public double VesselGlacernonDamage { get; set; } // 90, 7

    public double IncreaseAllDamage { get; set; } // 103, 1

    public double IncreaseAllDamageAttackType { get; set; } // 103, 3-5-7

    public double IncreaseDamageMagicDefense { get; set; } // 108, 9

    public int IncreaseDamageRace { get; set; } // 24, 1

    public double IncreaseDamageRacePercentage { get; set; } // 71, 1

    public double IncreaseLoDDamage { get; set; } // 101, 1

    public double IncreaseVesselDamage { get; set; } // 101, 3

    public double IncreaseDamageFaction { get; set; } // 85, 7-9

    public int InvisibleDamage { get; set; } // 43, 7

    public double IncreaseDamageInPvP { get; set; } // 71, 9

    public double IncreaseAttack { get; set; } // 15, 1

    public double IncreaseAttackAttackType { get; set; } // 15, 3-5-7

    public double IncreaseDamageShadowFairy { get; set; } // 80, 9

    public double IncreaseAllAttacks { get; set; } // 44, 3

    public double IncreaseDamageByDebuffs { get; set; }

    public double IncreaseDamageHighMonsters { get; set; } // 86, 5

    public double IncreaseDamageVersusMonsters { get; set; }

    public double IncreaseAllAttacksVersusMonsters { get; set; }
}