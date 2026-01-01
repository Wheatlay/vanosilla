using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using WingsAPI.Data.Character;
using WingsAPI.Data.Miniland;
using WingsEmu.DTOs.Account;
using WingsEmu.DTOs.Bonus;
using WingsEmu.DTOs.Buffs;
using WingsEmu.DTOs.Inventory;
using WingsEmu.DTOs.Mates;
using WingsEmu.DTOs.Quests;
using WingsEmu.DTOs.Quicklist;
using WingsEmu.DTOs.Skills;
using WingsEmu.DTOs.Titles;
using WingsEmu.Game._enum;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Cheats;
using WingsEmu.Game.Entities;
using WingsEmu.Game.EntityStatistics;
using WingsEmu.Game.Exchange;
using WingsEmu.Game.Families;
using WingsEmu.Game.Groups;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Mails;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Quests;
using WingsEmu.Game.Quicklist;
using WingsEmu.Game.Raids;
using WingsEmu.Game.RainbowBattle;
using WingsEmu.Game.Relations;
using WingsEmu.Game.RespawnReturn;
using WingsEmu.Game.Revival;
using WingsEmu.Game.Shops;
using WingsEmu.Game.Skills;
using WingsEmu.Game.SnackFood;
using WingsEmu.Game.Specialists;
using WingsEmu.Game.TimeSpaces;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Character;

namespace WingsEmu.Game.Characters;

public interface IPlayerEntity : IBattleEntity, IEquipmentOptionContainer, IQuestContainer, ICharacterRevivalComponent, IFamilyComponent, IComboSkillComponent, ISkillCooldownComponent,
    IAngelElementBuffComponent, IScoutComponent, IRelationComponent, IGroupComponent, IInventoryComponent, IExchangeComponent, IBubbleComponent, IPartnerInventoryComponent,
    IRaidComponent, IFoodSnackComponent
{
    int AdditionalHp { get; set; }
    int AdditionalMp { get; set; }
    bool TriggerAmbush { get; set; }
    DateTime LastMapChange { get; set; }
    DateTime? LastSkillCombo { get; set; }
    AuthorityType Authority { get; set; }
    int CurrentMinigame { get; set; }
    int SecondDamageMinimum { get; }
    int SecondDamageMaximum { get; }
    int HitRate { get; }
    int HitCriticalChance { get; }
    int HitCriticalDamage { get; }
    int SecondHitRate { get; }
    int SecondHitCriticalChance { get; }
    int SecondHitCriticalDamage { get; }
    int MeleeDefence { get; }
    int RangedDefence { get; }
    int MagicDefence { get; }
    int MeleeDodge { get; }
    int RangedDodge { get; }
    int SpecialistElementRate { get; }
    DateTime GameStartDate { get; set; }
    bool HasShopOpened { get; set; }
    bool Invisible { get; }
    bool IsCustomSpeed { get; set; }
    bool IsShopping { get; set; }
    bool IsSitting { get; set; }
    bool IsOnVehicle { get; set; }
    bool IsMorphed { get; set; }
    (VisualType, long) LastEntity { get; set; }
    LastWalk LastWalk { get; set; }
    int LastNRunId { get; set; }
    int LastPulse { get; set; }
    DateTime LastPulseTick { get; set; }
    DateTime LastDefence { get; set; }
    DateTime LastItemUpgrade { get; set; }
    DateTime LastDeath { get; set; }
    DateTime LastEffect { get; set; }
    DateTime LastEffectMinigame { get; set; }
    DateTime LastHealth { get; set; }
    DateTime LastPortal { get; set; }
    DateTime LastPotion { get; set; }
    DateTime LastSnack { get; set; }
    DateTime LastFood { get; set; }
    DateTime LastSkillUse { get; set; }
    DateTime LastSpeedChange { get; set; }
    DateTime LastTransform { get; set; }
    DateTime LastDayNight { get; set; }
    DateTime? SpCooldownEnd { get; set; }
    DateTime Bubble { get; set; }
    DateTime SpyOutStart { get; set; }
    DateTime ItemsToRemove { get; set; }
    DateTime BonusesToRemove { get; set; }
    DateTime? RandomMapTeleport { get; set; }
    DateTime LastMove { get; set; }
    DateTime LastPutItem { get; set; }
    DateTime LastSentNote { get; set; }
    DateTime? CheckWeedingBuff { get; set; }
    DateTime LastPvPAttack { get; set; }
    DateTime LastRainbowArrowEffect { get; set; }
    DateTime LastRainbowEffects { get; set; }
    Guid MapInstanceId { get; set; }
    IMapInstance Miniland { get; set; }
    int Morph { get; set; }
    int MorphUpgrade { get; set; }
    int MorphUpgrade2 { get; set; }
    IClientSession Session { get; }
    ConcurrentDictionary<int, CharacterSkill> CharacterSkills { get; }
    ConcurrentDictionary<int, CharacterSkill> SkillsSp { get; set; }
    ConcurrentDictionary<long, int> HitsByMonsters { get; }
    bool UseSp { get; set; }
    byte VehicleSpeed { get; set; }
    byte VehicleMapSpeed { get; set; }
    int WareHouseSize { get; set; }
    DateTime LastBuySearchBazaarRefresh { get; set; }
    DateTime LastBuyBazaarRefresh { get; set; }
    DateTime LastListItemBazaar { get; set; }
    DateTime LastAdministrationBazaarRefresh { get; set; }
    DateTime LastMonsterCaught { get; set; }
    bool IsSeal { get; set; }
    bool IsRemovingSpecialistPoints { get; set; }
    bool IsWarehouseOpen { get; set; }
    bool IsPartnerWarehouseOpen { get; set; }
    bool IsCraftingItem { get; set; }
    bool IsBankOpen { get; set; }
    DateTime LastUnfreezedPlayer { get; set; }
    DateTime LastSpPacketSent { get; set; }
    DateTime LastSpRemovingProcess { get; set; }
    DateTime LastAttack { get; set; }
    bool InitialScpPacketSent { get; set; }
    long AccountId { get; set; }
    int Act4Dead { get; set; }
    int Act4Kill { get; set; }
    int Act4Points { get; set; }
    int ArenaWinner { get; set; }
    string Biography { get; set; }
    bool BuffBlocked { get; set; }
    bool ShowRaidDeathInfo { get; set; }
    ClassType Class { get; set; }
    short Compliment { get; set; }
    float Dignity { get; set; }
    bool EmoticonsBlocked { get; set; }
    bool ExchangeBlocked { get; set; }
    bool FamilyRequestBlocked { get; set; }
    bool FriendRequestBlocked { get; set; }
    GenderType Gender { get; set; }
    long Gold { get; set; }
    bool GroupRequestBlocked { get; set; }
    HairColorType HairColor { get; set; }
    HairStyleType HairStyle { get; set; }
    bool HeroChatBlocked { get; set; }
    byte HeroLevel { get; set; }
    long HeroXp { get; set; }
    bool HpBlocked { get; set; }
    bool IsPetAutoRelive { get; set; }
    bool IsPartnerAutoRelive { get; set; }
    byte JobLevel { get; set; }
    long JobLevelXp { get; set; }
    long LevelXp { get; set; }
    int MapId { get; set; }
    short MapX { get; set; }
    short MapY { get; set; }
    int MasterPoints { get; set; }
    int MasterTicket { get; set; }
    byte MaxPetCount { get; set; }
    byte MaxPartnerCount { get; set; }
    bool MinilandInviteBlocked { get; set; }
    string MinilandMessage { get; set; }
    short MinilandPoint { get; set; }
    MinilandState MinilandState { get; set; }
    bool MouseAimLock { get; set; }
    string Name { get; set; }
    bool QuickGetUp { get; set; }
    bool HideHat { get; set; }
    bool UiBlocked { get; set; }
    long Reput { get; set; }
    byte Slot { get; set; }
    int SpPointsBonus { get; set; }
    int SpPointsBasic { get; set; }
    int TalentLose { get; set; }
    int TalentSurrender { get; set; }
    int TalentWin { get; set; }
    bool WhisperBlocked { get; set; }
    int? LastMinilandProducedItem { get; set; }
    bool IsGettingLosingReputation { get; set; }
    byte DeathsOnAct4 { get; set; }
    long ArenaKills { get; set; }
    long ArenaDeaths { get; set; }
    TimeSpan? MuteRemainingTime { get; set; }
    DateTime LastMuteTick { get; set; }
    DateTime LastSitting { get; set; }
    DateTime? LastChatMuteMessage { get; set; }
    DateTime LastInventorySort { get; set; }
    DateTime? ArenaImmunity { get; set; }
    List<CharacterPartnerInventoryItemDto> PartnerInventory { get; set; }
    List<MateDTO> NosMates { get; set; }
    HashSet<long> CompletedTimeSpaces { get; set; }
    List<PartnerWarehouseItemDto> PartnerWarehouse { get; set; }
    List<CharacterStaticBonusDto> Bonus { get; set; }
    List<CharacterStaticBuffDto> StaticBuffs { get; set; }
    List<CharacterQuicklistEntryDto> Quicklist { get; set; }
    List<CharacterSkillDTO> LearnedSkills { get; set; }
    List<CharacterTitleDto> Titles { get; set; }
    List<CompletedScriptsDto> CompletedScripts { get; set; }
    List<CharacterQuestDto> CompletedQuests { get; set; }
    List<CharacterQuestDto> CompletedPeriodicQuests { get; set; }
    List<CharacterQuestDto> ActiveQuests { get; set; }
    List<CharacterMinilandObjectDto> MinilandObjects { get; set; }
    List<CharacterInventoryItemDto> Inventory { get; set; }
    List<CharacterInventoryItemDto> EquippedStuffs { get; set; }
    CharacterLifetimeStatsDto LifetimeStats { get; set; }
    CharacterRaidRestrictionDto RaidRestrictionDto { get; set; }
    RainbowBattleLeaverBusterDto RainbowBattleLeaverBusterDto { get; set; }

    IQuicklistComponent QuicklistComponent { get; }
    IMateComponent MateComponent { get; }
    IHomeComponent HomeComponent { get; }
    ISkillComponent SkillComponent { get; }
    ICheatComponent CheatComponent { get; }
    ISpecialistStatsComponent SpecialistComponent { get; }
    IPlayerStatisticsComponent StatisticsComponent { get; }
    ITimeSpaceComponent TimeSpaceComponent { get; }
    IShopComponent ShopComponent { get; }
    IMailNoteComponent MailNoteComponent { get; }
    IRainbowBattleComponent RainbowBattleComponent { get; }


    void RefreshCharacterStats(bool refreshHpMp = true);
    void AddStaticBonus(CharacterStaticBonusDto bonus);
    void AddStaticBonuses(IEnumerable<CharacterStaticBonusDto> bonuses);
    IReadOnlyList<CharacterStaticBonusDto> GetStaticBonuses();
    CharacterStaticBonusDto GetStaticBonus(Predicate<CharacterStaticBonusDto> predicate);
    int GetCp();
    int GetDignityIco();
    List<IPortalEntity> GetExtraPortal();
    void SetSession(IClientSession clientSession);
    int HealthHpLoad();
    int HealthMpLoad();
    bool HasBuff(BuffVnums buffVnum);
    void SetFaction(FactionType faction);
}