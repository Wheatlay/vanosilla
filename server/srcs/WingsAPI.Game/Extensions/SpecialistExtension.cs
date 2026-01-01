using System.Linq;
using WingsEmu.DTOs.Skills;
using WingsEmu.Game.Items;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Skills;

namespace WingsEmu.Game.Extensions;

public static class SpecialistExtension
{
    public static string GenerateSkillInfo(this GameItemInstance specialistInstance, byte type)
    {
        // type = 0 - in packet
        // type = 1 - pski packet
        // type = 2 - sc_n packet

        if (specialistInstance == null && type == 2)
        {
            return "-1 -1 -1";
        }

        if (specialistInstance?.PartnerSkills == null)
        {
            return type switch
            {
                0 => "0 0 0 ",
                1 => string.Empty,
                2 => "0.0 0.0 0.0",
                _ => string.Empty
            };
        }

        string generatePacket = string.Empty;
        var generateSkillsPacket = Enumerable.Repeat<PartnerSkill>(null, 3).ToList();

        for (int i = 0; i < 3; i++)
        {
            PartnerSkill skill = specialistInstance.PartnerSkills?.ElementAtOrDefault(i);
            if (skill == null)
            {
                continue;
            }

            generateSkillsPacket[skill.Slot] = skill;
        }

        for (int i = 0; i < 3; i++)
        {
            PartnerSkill skill = generateSkillsPacket.ElementAtOrDefault(i);
            if (skill == null)
            {
                generatePacket += type switch
                {
                    0 => "0 ",
                    1 => string.Empty,
                    2 => "0.0 ",
                    _ => string.Empty
                };

                continue;
            }

            generatePacket += type switch
            {
                0 => $"{skill.SkillId} ",
                1 => $"{skill.SkillId} ",
                2 => $"{skill.SkillId}.{skill.Rank} ",
                _ => "0 0 0"
            };
        }

        return generatePacket;
    }

    public static bool IsSpSkill(this GameItemInstance spInstance, SkillDTO ski) =>
        ski.UpgradeType == spInstance.GameItem.Morph && ski.SkillType == SkillType.NormalPlayerSkill && spInstance.SpLevel >= ski.LevelMinimum;

    public static void SendPartnerSpecialistInfo(this IClientSession session, GameItemInstance item) =>
        session.SendPacket(item.GeneratePslInfo());

    public static string GeneratePslInfo(this GameItemInstance item) =>
        "pslinfo " +
        $"{item.ItemVNum} " +
        $"{item.GameItem.Element} " +
        $"{item.GameItem.ElementRate} " +
        $"{item.GameItem.LevelMinimum} " +
        $"{item.GameItem.Speed} " +
        $"{item.GameItem.FireResistance} " +
        $"{item.GameItem.WaterResistance} " +
        $"{item.GameItem.LightResistance} " +
        $"{item.GameItem.DarkResistance} " +
        $"{item.GenerateSkillInfo(2)}";
}