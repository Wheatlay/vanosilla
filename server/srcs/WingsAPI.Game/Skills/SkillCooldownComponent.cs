using System;
using System.Collections.Concurrent;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Game.Skills;

public class SkillCooldownComponent : ISkillCooldownComponent
{
    public SkillCooldownComponent()
    {
        SkillCooldowns = new ConcurrentQueue<(DateTime time, short castId)>();
        MatesSkillCooldowns = new ConcurrentQueue<(DateTime time, short castId, MateType mateType)>();
    }

    public ConcurrentQueue<(DateTime time, short castId)> SkillCooldowns { get; }
    public ConcurrentQueue<(DateTime time, short castId, MateType mateType)> MatesSkillCooldowns { get; }

    public void AddSkillCooldown(DateTime time, short castId)
    {
        SkillCooldowns.Enqueue((time, castId));
    }

    public void ClearSkillCooldowns()
    {
        SkillCooldowns.Clear();
    }

    public void AddMateSkillCooldown(DateTime time, short castId, MateType mateType)
    {
        MatesSkillCooldowns.Enqueue((time, castId, mateType));
    }

    public void ClearMateSkillCooldowns()
    {
        MatesSkillCooldowns.Clear();
    }
}