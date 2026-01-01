// WingsEmu
// 
// Developed by NosWings Team

using System;
using WingsEmu.Game._enum;

namespace WingsEmu.DTOs.BCards;

public class BCardDTO
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public byte SubType { get; set; }

    public short Type { get; set; }

    public int FirstData { get; set; }

    public int SecondData { get; set; }

    public int ProcChance { get; set; }

    public byte? TickPeriod { get; set; }

    public byte CastType { get; set; }

    public BCardScalingType FirstDataScalingType { get; set; }

    public BCardScalingType SecondDataScalingType { get; set; }

    public bool? IsSecondBCardExecution { get; set; }

    public int? CardId { get; set; }

    public int? ItemVNum { get; set; }

    public int? SkillVNum { get; set; }

    public int? NpcMonsterVNum { get; set; }

    public BCardNpcMonsterTriggerType? TriggerType { get; set; }

    public BCardNpcTriggerType? NpcTriggerType { get; set; }

    public bool IsMonsterMode { get; set; }
}