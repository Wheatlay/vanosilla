using System;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.Families;
using WingsAPI.Game.Extensions.Groups;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game._ItemUsage.Configuration;
using WingsEmu.Game.Algorithm;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Families.Enum;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Character;
using WingsEmu.Packets.Enums.Chat;
using WingsEmu.Packets.Enums.Families;

namespace WingsEmu.Plugins.BasicImplementations.Event.Characters;

public class LevelUpEventHandler : IAsyncEventProcessor<LevelUpEvent>
{
    private readonly ICharacterAlgorithm _characterAlgorithm;
    private readonly IGameLanguageService _gameLanguage;
    private readonly IServerManager _serverManager;
    private readonly ISkillsManager _skillsManager;
    private readonly ISpPartnerConfiguration _spPartnerConfiguration;

    public LevelUpEventHandler(IServerManager serverManager,
        IGameLanguageService gameLanguage, ICharacterAlgorithm characterAlgorithm, ISkillsManager skillsManager, ISpPartnerConfiguration spPartnerConfiguration)
    {
        _serverManager = serverManager;
        _gameLanguage = gameLanguage;
        _characterAlgorithm = characterAlgorithm;
        _skillsManager = skillsManager;
        _spPartnerConfiguration = spPartnerConfiguration;
    }

    public async Task HandleAsync(LevelUpEvent e, CancellationToken cancellation)
    {
        IPlayerEntity character = e.Sender.PlayerEntity;

        switch (e.LevelType)
        {
            case LevelType.Level:
                await HandleLevelUp(character, e.Sender);
                break;
            case LevelType.JobLevel:
                await HandleJobLevelUp(character, e.Sender);
                break;
            case LevelType.SpJobLevel:
                await HandleSpJobLevelUp(character, e.Sender);
                break;
            case LevelType.Heroic:
                await HandleHeroicLevelUp(character, e.Sender);
                break;
            case LevelType.Fairy:
                await HandleFairyLevelUp(character, e.Sender);
                break;
        }

        e.Sender.RefreshLevel(_characterAlgorithm);
    }

    private async Task HandleLevelUp(IPlayerEntity character, IClientSession session)
    {
        character.LevelXp -= _characterAlgorithm.GetLevelXp(character.Level);
        character.Level++;

        if (character.Level >= _serverManager.MaxLevel)
        {
            character.Level = (byte)_serverManager.MaxLevel;
            character.LevelXp = 0;
        }

        if (character.Level == _serverManager.HeroicStartLevel && character.HeroLevel == 0)
        {
            character.HeroLevel = 1;
            character.HeroXp = 0;
        }

        character.Session.RefreshStatChar();

        character.Hp = character.MaxHp;
        character.Mp = character.MaxMp;

        character.Session.RefreshStat();

        if (character.Level > 20 && (character.Level % 10) == 0)
        {
            await session.FamilyAddLogAsync(FamilyLogType.LevelUp, character.Name, character.Level.ToString());
            await session.FamilyAddExperience(character.Level * 20, FamXpObtainedFromType.LevelUp);
        }
        else if (character.Level > 80)
        {
            await session.FamilyAddLogAsync(FamilyLogType.LevelUp, character.Name, character.Level.ToString());
        }

        session.SendLevelUp();
        session.RefreshGroupLevelUi(_spPartnerConfiguration);
        session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.INFORMATION_SHOUTMESSAGE_LEVELUP, session.UserLanguage), MsgMessageType.Middle);
        session.BroadcastEffectInRange(EffectType.NormalLevelUp);
        session.BroadcastEffectInRange(EffectType.NormalLevelUpSubEffect);
    }

    private async Task HandleJobLevelUp(IPlayerEntity character, IClientSession session)
    {
        bool isAdventurer = character.Class == ClassType.Adventurer;
        character.JobLevelXp -= _characterAlgorithm.GetJobXp(character.JobLevel, isAdventurer);
        character.JobLevel++;

        if (character.JobLevel >= 20 && character.Class == ClassType.Adventurer)
        {
            character.JobLevel = 20;
            character.JobLevelXp = 0;
        }
        else if (character.JobLevel >= _serverManager.MaxJobLevel)
        {
            character.JobLevel = (byte)_serverManager.MaxJobLevel;
            character.JobLevelXp = 0;
        }

        session.SendLevelUp();
        session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.INFORMATION_SHOUTMESSAGE_JOB_LEVELUP, session.UserLanguage), MsgMessageType.Middle);
        session.BroadcastEffectInRange(EffectType.JobLevelUp);
        character.SkillComponent.ResetSkillCooldowns = DateTime.UtcNow;
    }

    private async Task HandleHeroicLevelUp(IPlayerEntity character, IClientSession session)
    {
        character.HeroXp -= _characterAlgorithm.GetHeroLevelXp(character.HeroLevel);
        character.HeroLevel++;

        if (character.HeroLevel >= _serverManager.MaxHeroLevel)
        {
            character.HeroLevel = (byte)_serverManager.MaxHeroLevel;
            character.HeroXp = 0;
        }

        character.Hp = character.MaxHp;
        character.Mp = character.MaxMp;

        character.Session.RefreshStat();

        if (character.HeroLevel > 1 && (character.HeroLevel % 10) == 0)
        {
            await session.FamilyAddLogAsync(FamilyLogType.HeroLevelUp, character.Name, character.HeroLevel.ToString());
            await session.FamilyAddExperience(character.HeroLevel * 20, FamXpObtainedFromType.LevelUp);
        }
        else if (character.HeroLevel > 50)
        {
            await session.FamilyAddLogAsync(FamilyLogType.HeroLevelUp, character.Name, character.HeroLevel.ToString());
        }

        session.SendLevelUp();
        session.RefreshGroupLevelUi(_spPartnerConfiguration);
        session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.INFORMATION_HERO_LEVELUP, session.UserLanguage), MsgMessageType.Middle);
        session.BroadcastEffectInRange(EffectType.NormalLevelUp);
        session.BroadcastEffectInRange(EffectType.NormalLevelUpSubEffect);
    }

    private async Task HandleSpJobLevelUp(IPlayerEntity character, IClientSession session)
    {
        character.Specialist.Xp -= _characterAlgorithm.GetSpecialistJobXp(character.Specialist.SpLevel, character.Specialist.IsFunSpecialist());
        character.Specialist.SpLevel++;

        if (character.Specialist.SpLevel >= _serverManager.MaxSpLevel)
        {
            character.Specialist.SpLevel = (byte)_serverManager.MaxSpLevel;
            character.Specialist.Xp = 0;
        }

        session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.SPECIALIST_SHOUTMESSAGE_LEVELUP, session.UserLanguage), MsgMessageType.Middle);
        session.BroadcastEffectInRange(EffectType.JobLevelUp);
        character.SkillComponent.ResetSpSkillCooldowns = DateTime.UtcNow;
    }

    private async Task HandleFairyLevelUp(IPlayerEntity character, IClientSession session)
    {
        GameItemInstance fairy = character.Fairy;
        if (fairy == null)
        {
            return;
        }

        int fairyXp = _characterAlgorithm.GetFairyXp((short)(fairy.ElementRate + fairy.GameItem.ElementRate));
        fairy.Xp -= fairyXp;
        fairy.ElementRate++;

        string fairyName = _gameLanguage.GetLanguage(GameDataType.Item, fairy.GameItem.Name, session.UserLanguage);
        if ((fairy.ElementRate + fairy.GameItem.ElementRate) == fairy.GameItem.MaxElementRate)
        {
            fairy.Xp = 0;
            session.SendMsg(_gameLanguage.GetLanguageFormat(GameDialogKey.INFORMATION_SHOUTMESSAGE_FAIRYMAX, session.UserLanguage, fairyName), MsgMessageType.Middle);
        }
        else
        {
            session.SendMsg(_gameLanguage.GetLanguageFormat(GameDialogKey.INFORMATION_SHOUTMESSAGE_FAIRY_LEVELUP, session.UserLanguage, fairyName), MsgMessageType.Middle);
        }

        session.RefreshFairy();
    }
}