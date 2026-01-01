// WingsEmu
// 
// Developed by NosWings Team

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PhoenixLib.DAL;
using ProtoBuf;
using WingsAPI.Data.Miniland;
using WingsAPI.Packets.Enums;
using WingsEmu.DTOs.Bonus;
using WingsEmu.DTOs.Buffs;
using WingsEmu.DTOs.Inventory;
using WingsEmu.DTOs.Mates;
using WingsEmu.DTOs.Quests;
using WingsEmu.DTOs.Quicklist;
using WingsEmu.DTOs.Respawns;
using WingsEmu.DTOs.Skills;
using WingsEmu.DTOs.Titles;
using WingsEmu.Game._enum;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Character;

namespace WingsAPI.Data.Character;

[ProtoContract]
public class CharacterDTO : ILongDto
{
    [ProtoMember(2)]
    public long AccountId { get; set; }

    [ProtoMember(3)]
    public int Act4Dead { get; set; }

    [ProtoMember(4)]
    public int Act4Kill { get; set; }

    [ProtoMember(5)]
    public int Act4Points { get; set; }

    [ProtoMember(6)]
    public int ArenaWinner { get; set; }

    [ProtoMember(7)]
    [MaxLength(255)]
    public string Biography { get; set; }

    [ProtoMember(8)]
    public bool BuffBlocked { get; set; }

    [ProtoMember(9)]
    public ClassType Class { get; set; }

    [ProtoMember(10)]
    public short Compliment { get; set; }

    [ProtoMember(11)]
    public float Dignity { get; set; }

    [ProtoMember(12)]
    public bool EmoticonsBlocked { get; set; }

    [ProtoMember(13)]
    public bool ExchangeBlocked { get; set; }

    [ProtoMember(14)]
    public FactionType Faction { get; set; }

    [ProtoMember(15)]
    public bool FamilyRequestBlocked { get; set; }

    [ProtoMember(16)]
    public bool FriendRequestBlocked { get; set; }

    [ProtoMember(17)]
    public GenderType Gender { get; set; }

    [ProtoMember(18)]
    public long Gold { get; set; }

    [ProtoMember(19)]
    public bool GroupRequestBlocked { get; set; }

    [ProtoMember(20)]
    public HairColorType HairColor { get; set; }

    [ProtoMember(21)]
    public HairStyleType HairStyle { get; set; }

    [ProtoMember(22)]
    public bool HeroChatBlocked { get; set; }

    [ProtoMember(23)]
    public byte HeroLevel { get; set; }

    [ProtoMember(24)]
    public long HeroXp { get; set; }

    [ProtoMember(25)]
    public int Hp { get; set; }

    [ProtoMember(26)]
    public bool HpBlocked { get; set; }

    [ProtoMember(27)]
    public bool IsPetAutoRelive { get; set; }

    [ProtoMember(28)]
    public bool IsPartnerAutoRelive { get; set; }

    [ProtoMember(29)]
    public byte JobLevel { get; set; }

    [ProtoMember(30)]
    public long JobLevelXp { get; set; }

    [ProtoMember(31)]
    public byte Level { get; set; }

    [ProtoMember(32)]
    public long LevelXp { get; set; }

    [ProtoMember(33)]
    public int MapId { get; set; }

    [ProtoMember(34)]
    public short MapX { get; set; }

    [ProtoMember(35)]
    public short MapY { get; set; }

    [ProtoMember(36)]
    public int MasterPoints { get; set; }

    [ProtoMember(37)]
    public int MasterTicket { get; set; }

    [ProtoMember(38)]
    public byte MaxPetCount { get; set; }

    [ProtoMember(39)]
    public byte MaxPartnerCount { get; set; }

    [ProtoMember(40)]
    public bool MinilandInviteBlocked { get; set; }

    [ProtoMember(41)]
    [MaxLength(255)]
    public string MinilandMessage { get; set; }

    [ProtoMember(42)]
    public short MinilandPoint { get; set; }

    [ProtoMember(43)]
    public MinilandState MinilandState { get; set; }

    [ProtoMember(44)]
    public bool MouseAimLock { get; set; }

    [ProtoMember(45)]
    public int Mp { get; set; }

    [ProtoMember(46)]
    [MaxLength(25)]
    public string Prefix { get; set; }

    [ProtoMember(47)]
    [MaxLength(30)]
    public string Name { get; set; }

    [ProtoMember(48)]
    public bool QuickGetUp { get; set; }

    [ProtoMember(49)]
    public bool HideHat { get; set; }

    [ProtoMember(50)]
    public bool UiBlocked { get; set; }

    [ProtoMember(51)]
    public long RagePoint { get; set; }

    [ProtoMember(52)]
    public long Reput { get; set; }

    [ProtoMember(53)]
    public byte Slot { get; set; }

    [ProtoMember(54)]
    public int SpPointsBonus { get; set; }

    [ProtoMember(55)]
    public int SpPointsBasic { get; set; }

    [ProtoMember(56)]
    public int TalentLose { get; set; }

    [ProtoMember(57)]
    public int TalentSurrender { get; set; }

    [ProtoMember(58)]
    public int TalentWin { get; set; }

    [ProtoMember(59)]
    public bool WhisperBlocked { get; set; }

    [ProtoMember(60)]
    public List<CharacterPartnerInventoryItemDto> PartnerInventory { get; set; } = new();

    [ProtoMember(61)]
    public List<MateDTO> NosMates { get; set; } = new();

    [ProtoMember(62)]
    public List<PartnerWarehouseItemDto> PartnerWarehouse { get; set; } = new();

    [ProtoMember(63)]
    public List<CharacterStaticBonusDto> Bonus { get; set; } = new();

    [ProtoMember(64)]
    public List<CharacterStaticBuffDto> StaticBuffs { get; set; } = new();

    [ProtoMember(65)]
    public List<CharacterQuicklistEntryDto> Quicklist { get; set; } = new();

    [ProtoMember(66)]
    public List<CharacterSkillDTO> LearnedSkills { get; set; } = new();

    [ProtoMember(67)]
    public List<CharacterTitleDto> Titles { get; set; } = new();

    [ProtoMember(68)]
    public List<CompletedScriptsDto> CompletedScripts { get; set; } = new();

    [ProtoMember(69)]
    public List<CharacterQuestDto> CompletedPeriodicQuests { get; set; } = new();

    [ProtoMember(70)]
    public List<CharacterQuestDto> ActiveQuests { get; set; } = new();

    [ProtoMember(71)]
    public List<CharacterMinilandObjectDto> MinilandObjects { get; set; } = new();

    [ProtoMember(72)]
    public RespawnType RespawnType { get; set; }

    [ProtoMember(73)]
    public CharacterReturnDto ReturnPoint { get; set; }

    [ProtoMember(74)]
    public List<CharacterInventoryItemDto> Inventory { get; set; } = new();

    [ProtoMember(75)]
    public List<CharacterInventoryItemDto> EquippedStuffs { get; set; } = new();

    [ProtoMember(76)]
    public CharacterLifetimeStatsDto LifetimeStats { get; set; } = new();

    [ProtoMember(77)]
    public List<CharacterQuestDto> CompletedQuests { get; set; } = new();

    [ProtoMember(78)]
    public HashSet<long> CompletedTimeSpaces { get; set; } = new();

    [ProtoMember(79)]
    public CharacterRaidRestrictionDto RaidRestrictionDto { get; set; } = new();

    [ProtoMember(80)]
    public Act5RespawnType Act5RespawnType { get; set; }

    [ProtoMember(81)]
    public RainbowBattleLeaverBusterDto RainbowBattleLeaverBusterDto { get; set; } = new();

    [ProtoMember(1)]
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }
}