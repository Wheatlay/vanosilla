using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.DTOs.Maps;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Algorithm;
using WingsEmu.Game.Algorithm.Events;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Groups;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Mates.Events;
using WingsEmu.Game.TimeSpaces;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Character;
using WingsEmu.Packets.Enums.Chat;
using WingsEmu.Plugins.BasicImplementations.Algorithms;

namespace WingsEmu.Plugins.BasicImplementations.Event.Algorithm;

public class GenerateExperienceEventHandler : IAsyncEventProcessor<GenerateExperienceEvent>
{
    private readonly ICharacterAlgorithm _characterAlgorithm;
    private readonly IGameLanguageService _gameLanguageService;
    private readonly IServerManager _serverManager;
    private readonly ITimeSpaceManager _timeSpaceManager;

    public GenerateExperienceEventHandler(ICharacterAlgorithm characterAlgorithm, IServerManager serverManager, IGameLanguageService gameLanguageService, ITimeSpaceManager timeSpaceManager)
    {
        _characterAlgorithm = characterAlgorithm;
        _serverManager = serverManager;
        _gameLanguageService = gameLanguageService;
        _timeSpaceManager = timeSpaceManager;
    }

    public async Task HandleAsync(GenerateExperienceEvent e, CancellationToken cancellation)
    {
        IPlayerEntity character = e.Character;
        IMonsterEntity monsterEntity = e.MonsterEntity;
        long? monsterOwnerId = e.MonsterOwnerId;

        if (monsterEntity.IsAlive())
        {
            return;
        }

        if (monsterEntity.MapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance)
        {
            TimeSpaceParty timeSpace = _timeSpaceManager.GetTimeSpaceByMapInstanceId(monsterEntity.MapInstance.Id);
            if (timeSpace == null)
            {
                return;
            }

            if (timeSpace.IsEasyMode)
            {
                return;
            }
        }

        if (!character.IsInGroup())
        {
            if (!character.IsAlive())
            {
                return;
            }

            ExperienceInfo singleExpInfo = new()
            {
                BeforeCalculationPlayerLevel = character.Level
            };

            ExcessExperience getExtraExp = character.GetMoreExperience(_serverManager);
            getExtraExp.ExperienceInfo = singleExpInfo;
            bool decreaseXp = monsterOwnerId.HasValue && monsterOwnerId.Value != character.Id;

            await ProcessExperience(character, getExtraExp, monsterEntity, decreaseXp, decreaseXp);
            return;
        }

        bool decrease = true;
        PlayerGroup group = character.GetGroup();
        if (monsterOwnerId.HasValue)
        {
            if (group.Members.Any(entity => entity.Id == monsterOwnerId.Value))
            {
                decrease = false;
            }
        }

        ExperienceInfo experienceInfo = new()
        {
            MembersLevel = (short)group.Members.Where(x => x.MapInstance?.Id == character.MapInstance?.Id).Sum(x => x.Level),
            MembersOnMap = (byte)group.Members.Count(x => x.MapInstance?.Id == character.MapInstance?.Id)
        };

        foreach (IPlayerEntity member in character.GetGroup().Members)
        {
            if (!member.IsAlive())
            {
                continue;
            }

            if (member.MapInstance?.Id != character.MapInstance?.Id)
            {
                continue;
            }

            ExcessExperience memberExtraXp = member.GetMoreExperience(_serverManager);
            experienceInfo.BeforeCalculationPlayerLevel = member.Level;
            memberExtraXp.ExperienceInfo = experienceInfo;

            if (monsterOwnerId.HasValue)
            {
                await ProcessExperience(member, memberExtraXp, monsterEntity, decrease, member.Id == character.Id);
                continue;
            }

            await ProcessExperience(member, memberExtraXp, monsterEntity);
        }
    }

    private async Task ProcessExperience(IPlayerEntity character, ExcessExperience getExtraExp, IMonsterEntity monsterEntity, bool decreaseXp = false, bool showMessage = false)
    {
        await ProcessExp(character, LevelType.Level, getExtraExp, monsterEntity, decreaseXp);
        await ProcessExp(character, LevelType.JobLevel, getExtraExp, monsterEntity, decreaseXp);
        await ProcessExp(character, LevelType.SpJobLevel, getExtraExp, monsterEntity, decreaseXp);
        await ProcessExp(character, LevelType.Heroic, getExtraExp, monsterEntity, decreaseXp);
        await ProcessExp(character, LevelType.LevelMate, getExtraExp, monsterEntity, decreaseXp);
        await ProcessExp(character, LevelType.Fairy, getExtraExp, monsterEntity, decreaseXp);

        if (decreaseXp && showMessage && character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance &&
            !character.MapInstance.HasMapFlag(MapFlags.ACT_4)) // yes, MapInstanceType, not MapFlag
        {
            character.Session.SendChatMessage(_gameLanguageService.GetLanguage(GameDialogKey.INTERACTION_CHATMESSAGE_XP_NOT_FIRST_HIT, character.Session.UserLanguage), ChatMessageColorType.Yellow);
        }
    }

    private async Task ProcessExp(IPlayerEntity character, LevelType level, ExcessExperience getExtraExp, IMonsterEntity monsterEntity, bool decreaseXp = false)
    {
        long experience = 0;

        int monsterXp = monsterEntity.Xp;
        int monsterJobXp = monsterEntity.JobXp;

        switch (level)
        {
            case LevelType.Level:
                experience = (long)(Math.Floor(Math.Floor(Math.Ceiling(monsterXp * getExtraExp.Mates) * getExtraExp.Level) * GetPenalty(character, false, monsterEntity, getExtraExp.ExperienceInfo)) *
                    getExtraExp.LowLevel);
                break;
            case LevelType.JobLevel:
                experience = (long)(Math.Floor(Math.Floor(monsterJobXp * getExtraExp.JobLevel) * GetPenalty(character, true, monsterEntity, getExtraExp.ExperienceInfo)) * getExtraExp.LowJob);
                break;
            case LevelType.SpJobLevel:
                experience = (long)(Math.Floor(Math.Floor(monsterJobXp * getExtraExp.JobSpLevel) * GetPenalty(character, true, monsterEntity, getExtraExp.ExperienceInfo)) * getExtraExp.LowJobSp);
                break;
            case LevelType.Heroic:
                experience = (long)(Math.Floor(
                    Math.Floor(Math.Ceiling(monsterXp * getExtraExp.Mates) * getExtraExp.HeroLevel) * GetPenalty(character, false, monsterEntity, getExtraExp.ExperienceInfo)) * getExtraExp.LowLevel);
                break;
            case LevelType.LevelMate:
                if (!character.MateComponent.GetMates().Any(x => x.IsTeamMember))
                {
                    break;
                }

                foreach (IMateEntity mate in character.MateComponent.TeamMembers())
                {
                    if (!mate.IsAlive())
                    {
                        continue;
                    }

                    IMateEntity anotherMate = character.MateComponent.GetTeamMember(x => x.MateType != mate.MateType);

                    switch (mate.MateType)
                    {
                        case MateType.Partner:
                            if (anotherMate != null && anotherMate.IsAlive())
                            {
                                experience = (long)Math.Floor(Math.Floor(monsterXp * 0.2025 * getExtraExp.PartnerLevel) * GetPenalty(character, false, monsterEntity, getExtraExp.ExperienceInfo));
                            }
                            else
                            {
                                experience = (long)Math.Floor(Math.Floor(monsterXp * 0.1875 * getExtraExp.PartnerLevel) * GetPenalty(character, false, monsterEntity, getExtraExp.ExperienceInfo));
                            }

                            break;
                        case MateType.Pet:
                            if (anotherMate != null && anotherMate.IsAlive())
                            {
                                experience = (long)Math.Floor(Math.Floor(monsterXp * 0.0675 * getExtraExp.MatesLevel) * GetPenalty(character, false, monsterEntity, getExtraExp.ExperienceInfo));
                            }
                            else
                            {
                                experience = (long)Math.Floor(Math.Floor(monsterXp * 0.055 * getExtraExp.MatesLevel) * GetPenalty(character, false, monsterEntity, getExtraExp.ExperienceInfo));
                            }

                            break;
                    }

                    await character.Session.EmitEventAsync(new MateProcessExperienceEvent(mate, experience));
                }

                break;
        }

        if (decreaseXp)
        {
            experience /= 3;
        }

        if (monsterEntity.MapInstance.MapInstanceType == MapInstanceType.EventGameInstance && level != LevelType.Fairy)
        {
            experience /= 10;
        }

        if (experience <= 0 && level != LevelType.Fairy)
        {
            return;
        }

        await ProcessFinalExperience(character, level, experience, monsterEntity);
    }

    private async Task ProcessFinalExperience(IPlayerEntity character, LevelType level, long experience, IMonsterEntity monsterEntity)
    {
        if (monsterEntity.SummonerId != 0 && monsterEntity.SummonerType == VisualType.Player)
        {
            return;
        }

        long neededExperienceToLevel;
        switch (level)
        {
            case LevelType.Level:
                if (character.Level >= _serverManager.MaxLevel)
                {
                    return;
                }

                neededExperienceToLevel = _characterAlgorithm.GetLevelXp(character.Level);

                if (character.Level <= 20)
                {
                    // every 10% generate full HP/MP
                    if ((int)(character.LevelXp / (neededExperienceToLevel / 10)) < (int)((character.LevelXp + experience) / (neededExperienceToLevel / 10)))
                    {
                        character.Hp = character.MaxHp;
                        character.Mp = character.MaxMp;
                        character.Session.RefreshStat();
                        character.Session.SendEffect(EffectType.ShinyStars);
                    }
                }

                character.LevelXp += experience;
                if (character.LevelXp < neededExperienceToLevel)
                {
                    break;
                }

                await character.Session.EmitEventAsync(new LevelUpEvent
                {
                    LevelType = LevelType.Level
                });
                break;
            case LevelType.JobLevel:
                if (character.Class == ClassType.Adventurer)
                {
                    if (character.JobLevel > 19)
                    {
                        break;
                    }

                    neededExperienceToLevel = _characterAlgorithm.GetJobXp(character.JobLevel, true);
                    character.JobLevelXp += experience;
                    if (character.JobLevelXp < neededExperienceToLevel)
                    {
                        break;
                    }

                    await character.Session.EmitEventAsync(new LevelUpEvent
                    {
                        LevelType = LevelType.JobLevel
                    });
                    break;
                }

                if (character.JobLevel >= _serverManager.MaxJobLevel)
                {
                    break;
                }

                neededExperienceToLevel = _characterAlgorithm.GetJobXp(character.JobLevel);
                character.JobLevelXp += experience;
                if (character.JobLevelXp < neededExperienceToLevel)
                {
                    break;
                }

                await character.Session.EmitEventAsync(new LevelUpEvent
                {
                    LevelType = LevelType.JobLevel
                });
                break;
            case LevelType.SpJobLevel:
                if (character.Specialist == null || !character.UseSp)
                {
                    break;
                }

                if (character.Specialist.SpLevel >= _serverManager.MaxSpLevel)
                {
                    break;
                }

                neededExperienceToLevel = _characterAlgorithm.GetSpecialistJobXp(character.Specialist.SpLevel, character.Specialist.IsFunSpecialist());
                character.Specialist.Xp += experience;
                if (character.Specialist.Xp < neededExperienceToLevel)
                {
                    break;
                }

                await character.Session.EmitEventAsync(new LevelUpEvent
                {
                    LevelType = LevelType.SpJobLevel,
                    ItemVnum = character.Specialist.ItemVNum
                });
                break;
            case LevelType.Heroic:
                if (monsterEntity.MapInstance != null && (!monsterEntity.MapInstance.HasMapFlag(MapFlags.ACT_6_1) || monsterEntity.MapInstance.HasMapFlag(MapFlags.ACT_6_2)))
                {
                    break;
                }

                if (monsterEntity.VesselMonster)
                {
                    break;
                }

                if (character.HeroLevel == 0)
                {
                    break;
                }

                if (character.HeroLevel >= _serverManager.MaxHeroLevel)
                {
                    break;
                }

                neededExperienceToLevel = _characterAlgorithm.GetHeroLevelXp(character.HeroLevel);
                character.HeroXp += experience;
                if (character.HeroXp < neededExperienceToLevel)
                {
                    break;
                }

                await character.Session.EmitEventAsync(new LevelUpEvent
                {
                    LevelType = LevelType.Heroic
                });
                break;
            case LevelType.Fairy:
                GameItemInstance fairy = character.Fairy;
                if (fairy == null)
                {
                    return;
                }

                if (fairy.ElementRate + fairy.GameItem.ElementRate >= fairy.GameItem.MaxElementRate || character.Level > monsterEntity.Level + 15)
                {
                    return;
                }

                fairy.Xp += (int)(_serverManager.FairyXpRate *
                    (1 + character.BCardComponent.GetAllBCardsInformation(BCardType.FairyXPIncrease, (byte)AdditionalTypes.FairyXPIncrease.IncreaseFairyXPPoints, character.Level).firstData * 0.01));
                int fairyXp = _characterAlgorithm.GetFairyXp((short)(fairy.ElementRate + fairy.GameItem.ElementRate));

                if (fairy.Xp < fairyXp)
                {
                    return;
                }

                await character.Session.EmitEventAsync(new LevelUpEvent
                {
                    LevelType = LevelType.Fairy,
                    ItemVnum = fairy.ItemVNum
                });


                break;
        }

        character.Session.RefreshLevel(_characterAlgorithm);
    }

    private double GetPenalty(IPlayerEntity character, bool isJob, IMonsterEntity monsterEntity, ExperienceInfo experienceInfo)
    {
        int actHeroPenalty = character.MapInstance.HasMapFlag(MapFlags.ACT_6_1) ? 5 : 0;
        bool isInGroup = character.IsInGroup();

        double levelPenalty = 1;
        double jobPenalty = 1;
        double monsterPenalty;
        int difference = character.Level - monsterEntity.Level;

        if (difference <= 5 + actHeroPenalty)
        {
            monsterPenalty = 1;
        }
        else if (difference == (6 + actHeroPenalty))
        {
            monsterPenalty = 0.9;
        }
        else if (difference == (7 + actHeroPenalty))
        {
            monsterPenalty = 0.7;
        }
        else if (difference == (8 + actHeroPenalty))
        {
            monsterPenalty = 0.5;
        }
        else if (difference == (9 + actHeroPenalty))
        {
            monsterPenalty = 0.3;
        }
        else
        {
            monsterPenalty = 0.1;
        }

        int groupMembers = experienceInfo.MembersOnMap ?? 1;

        if (isInGroup && experienceInfo.MembersOnMap != null)
        {
            byte membersOnTheSameMap = experienceInfo.MembersOnMap.Value;
            switch (membersOnTheSameMap)
            {
                case 2:
                    levelPenalty = 69.0 / 120.0;
                    jobPenalty = 75.0 / 120.0;
                    break;
                case 3:
                    levelPenalty = 52.0 / 120.0;
                    jobPenalty = 60.0 / 120.0;
                    break;
            }
        }

        double groupLevel = experienceInfo.MembersLevel ?? experienceInfo.BeforeCalculationPlayerLevel;

        if (!isJob)
        {
            return levelPenalty / (groupLevel / (experienceInfo.BeforeCalculationPlayerLevel * groupMembers)) * monsterPenalty;
        }

        if (monsterEntity.Level >= 70)
        {
            monsterPenalty = 1;
        }

        return jobPenalty / (groupLevel / (experienceInfo.BeforeCalculationPlayerLevel * groupMembers)) * monsterPenalty;
    }
}

public class ExcessExperience
{
    public ExcessExperience(double level, double jobLevel, double jobSpLevel, double heroLevel, double matesLevel, double partnerLevel, double lowLevel, double lowJob, double lowJobSp, double mates)
    {
        Level = level;
        JobLevel = jobLevel;
        JobSpLevel = jobSpLevel;
        HeroLevel = heroLevel;
        MatesLevel = matesLevel;
        PartnerLevel = partnerLevel;
        LowLevel = lowLevel;
        LowJob = lowJob;
        LowJobSp = lowJobSp;
        Mates = mates;
    }

    public double Level { get; }
    public double JobLevel { get; }
    public double JobSpLevel { get; }
    public double HeroLevel { get; }
    public double MatesLevel { get; }
    public double PartnerLevel { get; }

    public double LowLevel { get; }
    public double LowJob { get; }
    public double LowJobSp { get; }
    public double Mates { get; }

    public ExperienceInfo ExperienceInfo { get; set; }
}

public struct ExperienceInfo
{
    public byte BeforeCalculationPlayerLevel { get; set; }

    public short? MembersLevel { get; init; }
    public byte? MembersOnMap { get; init; }
}