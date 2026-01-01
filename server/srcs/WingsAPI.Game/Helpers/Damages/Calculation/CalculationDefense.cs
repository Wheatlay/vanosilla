namespace WingsEmu.Game.Helpers.Damages.Calculation;

public class CalculationDefense
{
    // BCardDTO - Type, SubType
    public double IncreaseDefense { get; set; } // 11, 1

    public double IncreaseDefenseAttackType { get; set; } // 11, 3-5-7

    public (int, double) IncreaseDefenseByLevel { get; set; } // 12, 1

    public (int, double) IncreaseDefenseByLevelAttackType { get; set; } // 12, 3-5-7

    public double IncreaseAllDefense { get; set; } // 44, 4

    public int MaximumCriticalDamage { get; set; } // 66, 7

    public double DefenseInPvP { get; set; } // 71, 7-8

    public double IncreaseDefenseInPve { get; set; }

    public double MultiplyDefense { get; set; } // 35
}