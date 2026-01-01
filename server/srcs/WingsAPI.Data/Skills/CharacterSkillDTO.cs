// WingsEmu
// 
// Developed by NosWings Team

using ProtoBuf;

namespace WingsEmu.DTOs.Skills;

[ProtoContract]
public class CharacterSkillDTO
{
    [ProtoMember(1)]
    public int SkillVNum { get; set; }
}