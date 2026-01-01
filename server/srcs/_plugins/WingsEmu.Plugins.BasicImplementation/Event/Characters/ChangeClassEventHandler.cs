using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.Families;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsAPI.Game.Extensions.Quicklist;
using WingsEmu.DTOs.Quicklist;
using WingsEmu.Game;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Algorithm;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Inventory.Event;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Networking.Broadcasting;
using WingsEmu.Game.Skills;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Character;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.Event.Characters;

public class ChangeClassEventHandler : IAsyncEventProcessor<ChangeClassEvent>
{
    private readonly IBattleEntityAlgorithmService _algorithm;
    private readonly ICharacterAlgorithm _characterAlgorithm;
    private readonly IGameItemInstanceFactory _gameItemInstance;
    private readonly IGameLanguageService _languageService;
    private readonly IRandomGenerator _randomNumberGenerator;
    private readonly IRankingManager _rankingManager;
    private readonly IReputationConfiguration _reputationConfiguration;

    public ChangeClassEventHandler(IGameLanguageService languageService, IRandomGenerator randomNumberGenerator,
        IBattleEntityAlgorithmService algorithm, ICharacterAlgorithm characterAlgorithm, IGameItemInstanceFactory gameItemInstance, IReputationConfiguration reputationConfiguration,
        IRankingManager rankingManager)
    {
        _languageService = languageService;
        _randomNumberGenerator = randomNumberGenerator;
        _algorithm = algorithm;
        _characterAlgorithm = characterAlgorithm;
        _gameItemInstance = gameItemInstance;
        _reputationConfiguration = reputationConfiguration;
        _rankingManager = rankingManager;
    }

    public async Task HandleAsync(ChangeClassEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;
        if (e.ShouldResetJobLevel)
        {
            session.PlayerEntity.JobLevel = 1;
        }

        session.PlayerEntity.JobLevelXp = 0;
        session.SendPacket("npinfo 0");
        session.SendPClearPacket();

        if (e.NewClass == (byte)ClassType.Adventurer)
        {
            session.PlayerEntity.HairStyle =
                (byte)session.PlayerEntity.HairStyle > 1 ? 0 : session.PlayerEntity.HairStyle;

            if (session.PlayerEntity.JobLevel > 20)
            {
                session.PlayerEntity.JobLevel = 20;
            }
        }

        session.SendCondPacket();
        session.PlayerEntity.Class = e.NewClass;
        session.PlayerEntity.RefreshMaxHpMp(_algorithm);
        session.PlayerEntity.Hp = session.PlayerEntity.MaxHp;
        session.PlayerEntity.Mp = session.PlayerEntity.MaxMp;
        session.BroadcastTitleInfo();
        session.RefreshStat();
        session.RefreshLevel(_characterAlgorithm);
        session.SendEqPacket();
        session.BroadcastEffectInRange(EffectType.JobLevelUp);
        session.SendMsg(_languageService.GetLanguage(GameDialogKey.INFORMATION_SHOUTMESSAGE_CLASS_CHANGED, session.UserLanguage), MsgMessageType.Middle);
        session.BroadcastEffectInRange(EffectType.Transform);
        if (e.ShouldObtainNewFaction && !session.PlayerEntity.IsInFamily())
        {
            await session.EmitEventAsync(new ChangeFactionEvent
            {
                NewFaction = (FactionType)_randomNumberGenerator.RandomNumber(1, 3)
            });
        }

        session.BroadcastCMode();
        session.BroadcastIn(_reputationConfiguration, _rankingManager.TopReputation, new ExceptSessionBroadcast(session));
        session.BroadcastGidx(session.PlayerEntity.Family, _languageService);
        session.BroadcastEffectInRange(EffectType.NormalLevelUp);
        session.BroadcastEffectInRange(EffectType.NormalLevelUpSubEffect);
        session.PlayerEntity.Skills.Clear();
        List<CharacterSkill> passivesToRemove = new();
        foreach (CharacterSkill skill in session.PlayerEntity.CharacterSkills.Values)
        {
            if (skill.Skill.IsPassiveSkill())
            {
                int skillMinimumLevel = 0;
                if (skill.Skill.MinimumSwordmanLevel == 0 && skill.Skill.MinimumArcherLevel == 0 && skill.Skill.MinimumMagicianLevel == 0)
                {
                    skillMinimumLevel = skill.Skill.MinimumAdventurerLevel;
                }
                else
                {
                    skillMinimumLevel = session.PlayerEntity.Class switch
                    {
                        ClassType.Adventurer => skill.Skill.MinimumAdventurerLevel,
                        ClassType.Swordman => skill.Skill.MinimumSwordmanLevel,
                        ClassType.Archer => skill.Skill.MinimumArcherLevel,
                        ClassType.Magician => skill.Skill.MinimumMagicianLevel,
                        _ => skillMinimumLevel
                    };
                }

                if (skillMinimumLevel == 0)
                {
                    passivesToRemove.Add(skill);
                }

                continue;
            }

            session.PlayerEntity.CharacterSkills.TryRemove(skill.SkillVNum, out CharacterSkill value);
            if (session.PlayerEntity.SkillComponent.SkillUpgrades.TryGetValue((short)skill.SkillVNum, out HashSet<IBattleEntitySkill> upgrades))
            {
                upgrades.Clear();
            }
        }

        foreach (CharacterSkill passive in passivesToRemove)
        {
            session.PlayerEntity.CharacterSkills.TryRemove(passive.Skill.Id, out _);
        }

        CharacterSkill newSkill;
        var skillsToAdd = new List<IBattleEntitySkill>();

        if (session.PlayerEntity.Class != ClassType.Wrestler)
        {
            int skillCatch = session.PlayerEntity.Class switch
            {
                ClassType.Adventurer => 209,
                ClassType.Swordman => 235,
                ClassType.Archer => 236,
                ClassType.Magician => 237
            };

            newSkill = new CharacterSkill
            {
                SkillVNum = (short)(200 + 20 * (byte)session.PlayerEntity.Class)
            };

            skillsToAdd.Add(newSkill);

            session.PlayerEntity.CharacterSkills[(short)(200 + 20 * (byte)session.PlayerEntity.Class)] = newSkill;

            newSkill = new CharacterSkill
            {
                SkillVNum = (short)(201 + 20 * (byte)session.PlayerEntity.Class)
            };

            skillsToAdd.Add(newSkill);

            session.PlayerEntity.CharacterSkills[(short)(201 + 20 * (byte)session.PlayerEntity.Class)] = newSkill;

            newSkill = new CharacterSkill
            {
                SkillVNum = skillCatch
            };

            skillsToAdd.Add(newSkill);

            session.PlayerEntity.CharacterSkills[skillCatch] = newSkill;
        }
        else
        {
            newSkill = new CharacterSkill
            {
                SkillVNum = 1525
            };

            skillsToAdd.Add(newSkill);

            session.PlayerEntity.CharacterSkills[1525] = newSkill;

            newSkill = new CharacterSkill
            {
                SkillVNum = 1529
            };

            skillsToAdd.Add(newSkill);

            session.PlayerEntity.CharacterSkills[1529] = newSkill;

            newSkill = new CharacterSkill
            {
                SkillVNum = 1565
            };

            skillsToAdd.Add(newSkill);

            session.PlayerEntity.CharacterSkills[1565] = newSkill;
        }

        session.PlayerEntity.ClearSkillCooldowns();

        foreach (CharacterQuicklistEntryDto remove in session.PlayerEntity.QuicklistComponent.GetQuicklist().Where(x => x.Morph == 0).ToList())
        {
            session.PlayerEntity.QuicklistComponent.RemoveQuicklist(remove.QuicklistTab, remove.QuicklistSlot, 0);
        }

        session.PlayerEntity.Skills.AddRange(skillsToAdd);
        session.RefreshPassiveBCards();
        session.RefreshSkillList();
        session.RefreshQuicklist();

        if (!e.ShouldObtainBasicItems)
        {
            return;
        }

        GameItemInstance mainWeapon = _gameItemInstance.CreateItem((short)(4 + (byte)e.NewClass * 14));
        InventoryItem item = await session.AddNewItemToInventory(mainWeapon);
        await session.EmitEventAsync(new InventoryEquipItemEvent(item.Slot));

        GameItemInstance secondWeapon;
        GameItemInstance otherItem;

        switch (e.NewClass)
        {
            case ClassType.Swordman:
                secondWeapon = _gameItemInstance.CreateItem(68);
                item = await session.AddNewItemToInventory(secondWeapon);
                await session.EmitEventAsync(new InventoryEquipItemEvent(item.Slot));

                otherItem = _gameItemInstance.CreateItem(2082, 10);
                await session.AddNewItemToInventory(otherItem);
                break;

            case ClassType.Archer:
                secondWeapon = _gameItemInstance.CreateItem(78);
                item = await session.AddNewItemToInventory(secondWeapon);
                await session.EmitEventAsync(new InventoryEquipItemEvent(item.Slot));

                otherItem = _gameItemInstance.CreateItem(2083, 10);
                await session.AddNewItemToInventory(otherItem);
                break;

            case ClassType.Magician:
                secondWeapon = _gameItemInstance.CreateItem(86);
                item = await session.AddNewItemToInventory(secondWeapon);
                await session.EmitEventAsync(new InventoryEquipItemEvent(item.Slot));
                break;
        }

        GameItemInstance armor = _gameItemInstance.CreateItem((short)(81 + (byte)e.NewClass * 13));
        item = await session.AddNewItemToInventory(armor);
        await session.EmitEventAsync(new InventoryEquipItemEvent(item.Slot));
    }
}