using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace WingsEmu.Game.Skills;

public interface ISkillComponent
{
    public bool CanBeInterrupted { get; set; }
    public bool IsSkillInterrupted { get; set; }
    public DateTime? SendTeleportPacket { get; set; }
    public long? BombEntityId { get; set; }
    public DateTime? ResetSkillCooldowns { get; set; }
    public DateTime? ResetSpSkillCooldowns { get; set; }
    public byte Zoom { get; set; }
    public Dictionary<short, HashSet<IBattleEntitySkill>> SkillUpgrades { get; }
    public ConcurrentDictionary<int, byte> SkillsResets { get; }
    public int? LastSkillVnum { get; set; }
}

public class SkillComponent : ISkillComponent
{
    public bool CanBeInterrupted { get; set; }
    public bool IsSkillInterrupted { get; set; }
    public DateTime? SendTeleportPacket { get; set; }
    public long? BombEntityId { get; set; }
    public DateTime? ResetSkillCooldowns { get; set; }
    public DateTime? ResetSpSkillCooldowns { get; set; }
    public byte Zoom { get; set; }
    public Dictionary<short, HashSet<IBattleEntitySkill>> SkillUpgrades { get; } = new();
    public ConcurrentDictionary<int, byte> SkillsResets { get; } = new();
    public int? LastSkillVnum { get; set; }
}