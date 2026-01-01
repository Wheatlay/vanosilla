// WingsEmu
// 
// Developed by NosWings Team

using Mapster;
using WingsAPI.Data.Character;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Character;

namespace WingsEmu.Plugins.PacketHandling.Customization;

public class BaseCharacter
{
    public BaseCharacter() => Character = new CharacterDTO
    {
        Class = ClassType.Adventurer,
        Gender = GenderType.Male,
        HairColor = HairColorType.Black,
        HairStyle = HairStyleType.A,
        Hp = 221,
        JobLevel = 1,
        Level = 1,
        MapId = 1,
        MapX = 78,
        MapY = 109,
        Mp = 221,
        MaxPetCount = 10,
        MaxPartnerCount = 3,
        Gold = 0,
        SpPointsBasic = 10000,
        SpPointsBonus = 0,
        Name = "template",
        Slot = 0,
        AccountId = 0,
        MinilandMessage = string.Empty
    };

    public CharacterDTO Character { get; set; }

    public CharacterDTO GetCharacter() => Character.Adapt<CharacterDTO>();
}