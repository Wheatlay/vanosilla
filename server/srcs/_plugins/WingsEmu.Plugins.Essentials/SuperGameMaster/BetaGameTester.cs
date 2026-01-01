using System;
using System.Linq;
using System.Threading.Tasks;
using Qmmands;
using WingsAPI.Game.Extensions.Families;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsAPI.Game.Extensions.Quicklist;
using WingsEmu.Commands.Checks;
using WingsEmu.Commands.Entities;
using WingsEmu.DTOs.Account;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Algorithm;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Families;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Networking.Broadcasting;
using WingsEmu.Game.Skills;
using WingsEmu.Packets.Enums.Character;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.Essentials.GameMaster;

[Name("Beta Game Tester")]
[Description("Module related to Beta Game Tester commands.")]
[RequireAuthority(AuthorityType.GameMaster)]
public class BetaGameTester : SaltyModuleBase
{
    private readonly IBattleEntityAlgorithmService _algorithm;
    private readonly ICharacterAlgorithm _characterAlgorithm;
    private readonly IGameItemInstanceFactory _gameItemInstance;
    private readonly IItemsManager _itemManager;
    private readonly IGameLanguageService _language;
    private readonly IRankingManager _rankingManager;
    private readonly IReputationConfiguration _reputationConfiguration;
    private readonly IServerManager _serverManager;
    private readonly ISkillsManager _skillsManager;

    public BetaGameTester(IItemsManager itemsManager, IGameLanguageService language, ISkillsManager skillsManager, IBattleEntityAlgorithmService algorithm, ICharacterAlgorithm characterAlgorithm,
        IReputationConfiguration reputationConfiguration, IServerManager serverManager, IGameItemInstanceFactory gameItemInstance, IRankingManager rankingManager)
    {
        _language = language;
        _itemManager = itemsManager;
        _skillsManager = skillsManager;
        _algorithm = algorithm;
        _characterAlgorithm = characterAlgorithm;
        _reputationConfiguration = reputationConfiguration;
        _serverManager = serverManager;
        _gameItemInstance = gameItemInstance;
        _rankingManager = rankingManager;
    }

    [Command("gold")]
    [Description("Set player gold")]
    public async Task<SaltyCommandResult> SetGold(
        [Description("Amount of gold.")] long gold)
    {
        IClientSession session = Context.Player;
        if (gold < 0)
        {
            return new SaltyCommandResult(false, "Wrong value!");
        }

        if (gold > _serverManager.MaxGold)
        {
            return new SaltyCommandResult(false, "Wrong value!");
        }

        session.PlayerEntity.Gold = gold;
        session.RefreshGold();
        return new SaltyCommandResult(true, $"Your gold: {gold}");
    }

    [Command("item")]
    [Description("Create an Item")]
    public async Task<SaltyCommandResult> CreateitemAsync(
        [Description("Item VNUM.")] short itemvnum,
        [Description("Amount.")] short amount)
    {
        IClientSession session = Context.Player;
        GameItemInstance newItem = _gameItemInstance.CreateItem(itemvnum, amount);
        await session.AddNewItemToInventory(newItem);
        return new SaltyCommandResult(true, $"Created item: {_language.GetLanguage(GameDataType.Item, newItem.GameItem.Name, Context.Player.UserLanguage)}");
    }

    [Command("item")]
    [Description("Create an Item with rare and upgrade.")]
    public async Task<SaltyCommandResult> CreateitemAsync(
        [Description("Item VNUM.")] short itemvnum,
        [Description("Amount.")] short amount,
        [Description("Item's rare.")] sbyte rare,
        [Description("Item's upgrade.")] byte upgrade)
    {
        IClientSession session = Context.Player;

        GameItemInstance newItem = _gameItemInstance.CreateItem(itemvnum, amount, upgrade, rare);
        await session.AddNewItemToInventory(newItem);
        return new SaltyCommandResult(true, $"Created item: {_language.GetLanguage(GameDataType.Item, newItem.GameItem.Name, Context.Player.UserLanguage)}");
    }

    [Command("position", "pos")]
    [Description("Outputs your current position")]
    public async Task<SaltyCommandResult> WhereAmI() => new SaltyCommandResult(true,
        $"MapId: {Context.Player.CurrentMapInstance?.MapId} | X: {Context.Player.PlayerEntity.PositionX} | Y: {Context.Player.PlayerEntity.PositionY}");

    [Command("splevel", "splvl")]
    [Description("Set player job level")]
    public async Task<SaltyCommandResult> SetSpLevel(
        [Description("SP job.")] byte spLevel)
    {
        if (spLevel == 0)
        {
            return new SaltyCommandResult(false, "Wrong value!");
        }

        IClientSession session = Context.Player;
        if (session.PlayerEntity.Specialist == null)
        {
            return new SaltyCommandResult(false, "You need to wear Specialist Card!");
        }

        session.PlayerEntity.Specialist.SpLevel = spLevel;
        session.PlayerEntity.Specialist.Xp = 0;
        session.RefreshLevel(_characterAlgorithm);
        session.LearnSpSkill(_skillsManager, _language);
        foreach (IBattleEntitySkill skill in session.PlayerEntity.Skills)
        {
            skill.LastUse = DateTime.UtcNow.AddDays(-1);
        }

        session.BroadcastIn(_reputationConfiguration, _rankingManager.TopReputation, new ExceptSessionBroadcast(session));
        session.BroadcastGidx(session.PlayerEntity.Family, _language);
        session.BroadcastEffectInRange(EffectType.JobLevelUp);
        return new SaltyCommandResult(true, "Specialist Card SP Level has been updated.");
    }

    [Command("jlevel", "joblvl", "joblevel", "jlvl")]
    [Description("Set player job level")]
    public async Task<SaltyCommandResult> SetJobLevel(
        [Description("Joblevel.")] byte jobLevel)
    {
        if (jobLevel == 0)
        {
            return new SaltyCommandResult(false, "Wrong value!");
        }

        IClientSession session = Context.Player;

        if ((session.PlayerEntity.Class != 0 || jobLevel > 20) && (session.PlayerEntity.Class == 0 || jobLevel > 255) ||
            jobLevel <= 0)
        {
            return new SaltyCommandResult(false, "Wrong value!");
        }

        session.PlayerEntity.JobLevel = jobLevel;
        session.PlayerEntity.JobLevelXp = 0;
        session.PlayerEntity.CharacterSkills.Clear();

        session.RefreshLevel(_characterAlgorithm);

        session.BroadcastIn(_reputationConfiguration, _rankingManager.TopReputation, new ExceptSessionBroadcast(session));
        session.BroadcastGidx(session.PlayerEntity.Family, _language);
        session.BroadcastEffectInRange(EffectType.JobLevelUp);

        if (session.PlayerEntity.Class == ClassType.Wrestler)
        {
            session.PlayerEntity.CharacterSkills[1525] = new CharacterSkill
            {
                SkillVNum = 1525
            };

            session.PlayerEntity.CharacterSkills[1529] = new CharacterSkill
            {
                SkillVNum = 1529
            };

            session.PlayerEntity.CharacterSkills[1565] = new CharacterSkill
            {
                SkillVNum = 1565
            };
        }
        else
        {
            session.PlayerEntity.CharacterSkills[(short)(200 + 20 * (byte)session.PlayerEntity.Class)] = new CharacterSkill
            {
                SkillVNum = (short)(200 + 20 * (byte)session.PlayerEntity.Class)
            };
            session.PlayerEntity.CharacterSkills[(short)(201 + 20 * (byte)session.PlayerEntity.Class)] = new CharacterSkill
            {
                SkillVNum = (short)(201 + 20 * (byte)session.PlayerEntity.Class)
            };
            session.PlayerEntity.CharacterSkills[236] = new CharacterSkill
            {
                SkillVNum = 236
            };
        }

        session.PlayerEntity.SkillComponent.SkillUpgrades.Clear();

        session.RefreshSkillList();
        session.RefreshQuicklist();
        session.LearnAdventurerSkill(_skillsManager, _language);

        return new SaltyCommandResult(true, "Job Level has been updated.");
    }

    [Command("speed")]
    [Description("Set player speed")]
    public async Task<SaltyCommandResult> SetSpeed(
        [Description("Amount of speed (0-59).")]
        byte speed)
    {
        if (speed > 59 || speed == 0)
        {
            return new SaltyCommandResult(false, "Wrong value!");
        }

        IClientSession session = Context.Player;

        session.PlayerEntity.Speed = speed;
        session.SendCondPacket();
        session.PlayerEntity.IsCustomSpeed = true;
        return new SaltyCommandResult(true, $"Speed: {speed}");
    }

    [Command("speed")]
    [Description("Turn off your custom speed")]
    public async Task<SaltyCommandResult> SetSpeed()
    {
        Context.Player.PlayerEntity.IsCustomSpeed = false;
        Context.Player.PlayerEntity.RefreshCharacterStats();
        Context.Player.SendCondPacket();
        return new SaltyCommandResult(true);
    }

    [Command("reput")]
    [Description("Set reputation to the session")]
    public async Task<SaltyCommandResult> SetReput(
        [Description("Amount of reputation.")] long reput)
    {
        if (reput < 0)
        {
            return new SaltyCommandResult(false, "Wrong value!");
        }

        IClientSession session = Context.Player;

        session.PlayerEntity.Reput = reput;
        session.RefreshReputation(_reputationConfiguration, _rankingManager.TopReputation);
        return new SaltyCommandResult(true, $"Reputation: {reput}");
    }

    [Command("class", "changeclass")]
    [Description("Set character class.")]
    public async Task<SaltyCommandResult> SetClass(
        [Description("0 - Adv, 1 - Sword, 2 - Archer, 3 - Mage, 4 - MA.")]
        string classType)
    {
        if (!Enum.TryParse(classType, out ClassType classt))
        {
            return new SaltyCommandResult(false, "Wrong value!");
        }

        if (classt >= ClassType.Wrestler)
        {
            return new SaltyCommandResult(false, "This Class doesn't exist!");
        }

        IClientSession session = Context.Player;

        session.EmitEvent(new ChangeClassEvent { NewClass = classt, ShouldObtainBasicItems = false, ShouldObtainNewFaction = false });
        return new SaltyCommandResult(true, "Class has been changed.");
    }

    [Command("level", "lvl")]
    [Description("Set player level")]
    public async Task<SaltyCommandResult> SetLvl(
        [Description("Level.")] byte level, bool mates = false)
    {
        IClientSession session = Context.Player;

        if (level == 0)
        {
            return new SaltyCommandResult(false, "Wrong value!");
        }

        if (level == 150)
        {
            return new SaltyCommandResult(false, "Wrong value!");
        }

        session.PlayerEntity.Level = level;
        session.PlayerEntity.LevelXp = 0;
        session.PlayerEntity.RefreshCharacterStats();
        session.PlayerEntity.RefreshMaxHpMp(_algorithm);
        session.PlayerEntity.Hp = session.PlayerEntity.MaxHp;
        session.PlayerEntity.Mp = session.PlayerEntity.MaxMp;

        session.RefreshStat();
        session.RefreshStatInfo();
        session.RefreshStatChar();
        session.RefreshLevel(_characterAlgorithm);

        IFamily family = session.PlayerEntity.Family;

        session.BroadcastIn(_reputationConfiguration, _rankingManager.TopReputation, new ExceptSessionBroadcast(session));
        session.BroadcastGidx(family, _language);
        session.BroadcastEffectInRange(EffectType.NormalLevelUp);
        session.BroadcastEffectInRange(EffectType.NormalLevelUpSubEffect);

        if (mates)
        {
            foreach (IMateEntity mateEntity in session.PlayerEntity.MateComponent.GetMates())
            {
                mateEntity.Level = level;
                mateEntity.Hp = mateEntity.MaxHp;
                mateEntity.Mp = mateEntity.MaxMp;
            }

            session.RefreshMateStats();
        }

        return new SaltyCommandResult(true, "Level has been updated.");
    }

    [Command("sex")]
    [Description("Change sex of character")]
    public async Task<SaltyCommandResult> ChangeGenderAsync()
    {
        IClientSession session = Context.Player;

        session.PlayerEntity.Gender = session.PlayerEntity.Gender == GenderType.Female ? GenderType.Male : GenderType.Female;
        session.SendMsg(_language.GetLanguage(GameDialogKey.INFORMATION_SHOUTMESSAGE_SEX_CHANGED, session.UserLanguage), MsgMessageType.Middle);

        session.SendEqPacket();
        session.SendGenderPacket();
        session.BroadcastIn(_reputationConfiguration, _rankingManager.TopReputation, new ExceptSessionBroadcast(session));
        session.BroadcastGidx(session.PlayerEntity.Family, _language);
        session.BroadcastCMode();
        session.BroadcastEffect(EffectType.Transform, new RangeBroadcast(session.PlayerEntity.PositionX, session.PlayerEntity.PositionY));
        return new SaltyCommandResult(true, "Gender has been changed.");
    }

    [Command("heal")]
    [Description("Heal yourself.")]
    public async Task<SaltyCommandResult> HealAsync()
    {
        IClientSession session = Context.Player;

        session.PlayerEntity.Hp = session.PlayerEntity.MaxHp;
        session.PlayerEntity.Mp = session.PlayerEntity.MaxMp;

        session.RefreshStat();

        return new SaltyCommandResult(true, "You have been healed.");
    }

    [Command("godmode")]
    [Description("Enable or disable godmode.")]
    public async Task<SaltyCommandResult> GodmodeAsync()
    {
        IClientSession session = Context.Player;

        session.PlayerEntity.CheatComponent.HasGodMode = !session.PlayerEntity.CheatComponent.HasGodMode;
        session.SendChatMessage($"GODMODE: {(session.PlayerEntity.CheatComponent.HasGodMode ? "ON" : "OFF")}", ChatMessageColorType.Yellow);
        return new SaltyCommandResult(true);
    }

    [Command("zoom")]
    [Description("Camera zoom.")]
    public async Task<SaltyCommandResult> ZoomAsync(
        [Description("Zoom value.")] byte valueZoom)
    {
        IClientSession session = Context.Player;

        session.PlayerEntity.SkillComponent.Zoom = valueZoom;
        session.RefreshZoom();
        return new SaltyCommandResult(true, $"Zoom updated: {valueZoom}");
    }

    [Command("clearchat")]
    [Description("Clear your chat")]
    public async Task<SaltyCommandResult> ClearchatAsync()
    {
        IClientSession session = Context.Player;

        for (int i = 0; i < 50; i++)
        {
            session.SendChatMessage(" ", ChatMessageColorType.Red);
        }

        return new SaltyCommandResult(true);
    }

    [Command("completed-ts")]
    [Description("Check completed Time-Spaces done by player")]
    public async Task<SaltyCommandResult> CompletedTs(IClientSession target)
    {
        if (!target.PlayerEntity.CompletedTimeSpaces.Any())
        {
            return new SaltyCommandResult(true, "Player didn't completed any Time-Space");
        }

        foreach (long tsId in target.PlayerEntity.CompletedTimeSpaces)
        {
            Context.Player.SendChatMessage($"Completed Time-Space: {tsId}", ChatMessageColorType.Yellow);
        }

        return new SaltyCommandResult(true);
    }
}