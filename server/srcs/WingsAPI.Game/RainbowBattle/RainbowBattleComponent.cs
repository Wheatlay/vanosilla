using System;
using WingsAPI.Packets.Enums.Rainbow;

namespace WingsEmu.Game.RainbowBattle;

public interface IRainbowBattleComponent
{
    int Kills { get; set; }
    int Deaths { get; set; }
    bool IsInRainbowBattle { get; }
    bool IsFrozen { get; set; }
    DateTime? FrozenTime { get; set; }
    RainbowBattleParty RainbowBattleParty { get; }
    int ActivityPoints { get; set; }

    RainbowBattleTeamType Team { get; }

    void SetRainbowBattle(RainbowBattleParty rainbowBattleParty, RainbowBattleTeamType team);
    void RemoveRainbowBattle();
}

public class RainbowBattleComponent : IRainbowBattleComponent
{
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public bool IsInRainbowBattle => RainbowBattleParty != null;
    public bool IsFrozen { get; set; }
    public DateTime? FrozenTime { get; set; }

    public RainbowBattleParty RainbowBattleParty { get; private set; }
    public int ActivityPoints { get; set; }
    public RainbowBattleTeamType Team { get; private set; }

    public void SetRainbowBattle(RainbowBattleParty rainbowBattleParty, RainbowBattleTeamType team)
    {
        RainbowBattleParty = rainbowBattleParty;
        Team = team;
        Kills = 0;
        Deaths = 0;
        IsFrozen = false;
        ActivityPoints = 0;
    }

    public void RemoveRainbowBattle()
    {
        RainbowBattleParty = null;
        Kills = 0;
        Deaths = 0;
        IsFrozen = false;
        ActivityPoints = 0;
    }
}