using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Data.Drops;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsEmu.DTOs.Maps;
using WingsEmu.DTOs.Quests;
using WingsEmu.Game;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Algorithm.Events;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Groups;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Inventory.Event;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Managers.ServerData;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Quests;
using WingsEmu.Game.Quests.Event;
using WingsEmu.Game.Raids;
using WingsEmu.Game.TimeSpaces;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.Event.Characters;

public sealed class KillBonusEventHandler : IAsyncEventProcessor<KillBonusEvent>
{
    private readonly IDropManager _dropManager;
    private readonly IDropRarityConfigurationProvider _dropRarityConfigurationProvider;
    private readonly IAsyncEventPipeline _eventPipeline;
    private readonly IGameItemInstanceFactory _gameItemInstance;
    private readonly IGameLanguageService _gameLanguage;
    private readonly IItemsManager _itemsManager;
    private readonly IRandomGenerator _randomGenerator;
    private readonly IRankingManager _rankingManager;
    private readonly IReputationConfiguration _reputationConfiguration;
    private readonly IServerManager _serverManager;
    private readonly ISessionManager _sessionManager;

    private readonly HashSet<QuestType> NormalDropQuestTypes = new() { QuestType.DROP_CHANCE, QuestType.DROP_CHANCE_2, QuestType.DROP_IN_TIMESPACE };

    public KillBonusEventHandler(IRandomGenerator randomGenerator,
        IDropManager dropManager, IServerManager serverManager, IGameLanguageService gameLanguage, ISessionManager sessionManager,
        IItemsManager itemsManager, IAsyncEventPipeline eventPipeline, IGameItemInstanceFactory gameItemInstance,
        IDropRarityConfigurationProvider dropRarityConfigurationProvider, IReputationConfiguration reputationConfiguration, IRankingManager rankingManager)
    {
        _randomGenerator = randomGenerator;
        _dropManager = dropManager;
        _serverManager = serverManager;
        _gameLanguage = gameLanguage;
        _sessionManager = sessionManager;
        _itemsManager = itemsManager;
        _eventPipeline = eventPipeline;
        _gameItemInstance = gameItemInstance;
        _dropRarityConfigurationProvider = dropRarityConfigurationProvider;
        _reputationConfiguration = reputationConfiguration;
        _rankingManager = rankingManager;
    }

    public async Task HandleAsync(KillBonusEvent e, CancellationToken cancellation)
    {
        IMonsterEntity monsterEntityToAttack = e.MonsterEntity;
        IPlayerEntity character = e.Sender.PlayerEntity;
        IClientSession session = e.Sender;

        if (monsterEntityToAttack == null || monsterEntityToAttack.IsStillAlive || monsterEntityToAttack.SummonerType is VisualType.Player)
        {
            return;
        }

        if (!ShouldMonsterDrop(monsterEntityToAttack))
        {
            return;
        }

        // owner set
        IPlayerEntity dropOwner = null;

        if (monsterEntityToAttack.Damagers.Count > 0)
        {
            IBattleEntity entityDropOwner = monsterEntityToAttack.Damagers.FirstOrDefault();
            if (entityDropOwner != null)
            {
                dropOwner = entityDropOwner switch
                {
                    IMonsterEntity monsterEntity
                        => monsterEntity.SummonerType != null && monsterEntity.SummonerId != null && monsterEntity.SummonerType == VisualType.Player
                            ? monsterEntity.MapInstance.GetCharacterById(monsterEntity.SummonerId.Value)
                            : null,
                    IPlayerEntity playerEntity => playerEntity,
                    IMateEntity mateEntity => mateEntity.Owner,
                    _ => null
                };
            }
        }

        // Check if owner is online and it's at the same map
        IClientSession firstAttacker = dropOwner != null ? _sessionManager.GetSessionByCharacterId(dropOwner.Id) : null;
        if (firstAttacker == null)
        {
            dropOwner = character;
        }
        else
        {
            dropOwner = firstAttacker.CurrentMapInstance?.Id == character.MapInstance.Id ? firstAttacker.PlayerEntity : character;
        }

        PlayerGroup playerGroup = null;
        if (dropOwner != null)
        {
            playerGroup = dropOwner.GetGroup();
        }

        // end owner set
        if (!session.HasCurrentMapInstance)
        {
            return;
        }

        await HandleExp(session, character, monsterEntityToAttack, dropOwner?.Id);
        await HandleGoldDrops(monsterEntityToAttack, playerGroup, dropOwner);
        await HandleDrops(monsterEntityToAttack, session.PlayerEntity, playerGroup, dropOwner);
    }

    private bool ShouldMonsterDrop(IMonsterEntity monsterEntityToAttack)
    {
        switch ((MonsterVnum)monsterEntityToAttack.MonsterVNum)
        {
            case MonsterVnum.TRAINING_STAKE:
            case MonsterVnum.DEMON_CAMP:
            case MonsterVnum.ANGEL_CAMP:
                return false;
        }

        return true;
    }

    private async Task HandleExp(IClientSession session, IPlayerEntity character, IMonsterEntity monsterEntityToAttack, long? dropOwner)
    {
        if (!character.IsAlive())
        {
            return;
        }

        await _eventPipeline.ProcessEventAsync(new GenerateExperienceEvent(character, monsterEntityToAttack, dropOwner));

        if (character.Level >= monsterEntityToAttack.Level || character.Dignity >= 100)
        {
            return;
        }

        character.Dignity += 1;
        session.RefreshReputation(_reputationConfiguration, _rankingManager.TopReputation);
        session.SendSuccessChatMessage(_gameLanguage.GetLanguageFormat(GameDialogKey.DIGNITY_CHATMESSAGE_RESTORE, session.UserLanguage, 1));
    }

    private async Task HandleDrops(IMonsterEntity monsterEntityToAttack, IPlayerEntity mainKiller, PlayerGroup playerGroup, IPlayerEntity firstAttacker)
    {
        IClientSession session = firstAttacker.Session;

        IReadOnlyList<DropDTO> monsterDrops = monsterEntityToAttack.Drops;
        var additionalDrop = new List<DropDTO>();
        IReadOnlyList<DropDTO> mapDrop = _dropManager.GetDropsByMapId(monsterEntityToAttack.MapInstance.MapId);
        IEnumerable<DropDTO> generalDrop = _dropManager.GetGeneralDrops();
        additionalDrop.AddRange(mapDrop);
        additionalDrop.AddRange(generalDrop);

        int secondChanceDropBCard = session.PlayerEntity.BCardComponent
            .GetAllBCardsInformation(BCardType.DropItemTwice, (byte)AdditionalTypes.DropItemTwice.DoubleDropChance, session.PlayerEntity.Level).firstData;
        bool secondChanceDrop = secondChanceDropBCard != 0 && _randomGenerator.RandomNumber() <= secondChanceDropBCard;

        #region Quests

        // Normal quest drops
        IEnumerable<CharacterQuest> characterQuests = session.PlayerEntity.GetCurrentQuestsByTypes(NormalDropQuestTypes);
        foreach (CharacterQuest characterQuest in characterQuests)
        {
            foreach (QuestObjectiveDto objective in characterQuest.Quest.Objectives)
            {
                if (monsterEntityToAttack.MonsterVNum != objective.Data0 && characterQuest.Quest.QuestType != QuestType.DROP_IN_TIMESPACE)
                {
                    continue;
                }

                if (characterQuest.Quest.QuestType == QuestType.DROP_IN_TIMESPACE)
                {
                    TimeSpaceParty timeSpace = session.PlayerEntity.TimeSpaceComponent.TimeSpace;
                    if (timeSpace == null || timeSpace.TimeSpaceId != objective.Data0)
                    {
                        continue;
                    }
                }

                float rndChance = _randomGenerator.RandomNumber();
                float chance = characterQuest.Quest.QuestType == QuestType.DROP_CHANCE ? objective.Data3 : objective.Data3 * 0.1f;
                if (rndChance > chance)
                {
                    continue;
                }

                await DropQuestItem(session, monsterEntityToAttack, playerGroup, objective.Data1);

                if (!secondChanceDrop)
                {
                    continue;
                }

                await DropQuestItem(session, monsterEntityToAttack, playerGroup, objective.Data1);
            }
        }

        // It has to be hardcoded, sorry T-T
        if (session.PlayerEntity.HasQuestWithId((int)QuestsVnums.LILIES_SP2))
        {
            if (monsterEntityToAttack.Level >= session.PlayerEntity.Level - 15 && monsterEntityToAttack.Level <= session.PlayerEntity.Level + 15 || monsterEntityToAttack.Level > 75)
            {
                float rndChance = _randomGenerator.RandomNumber();
                float chance = 25; // It has to be like this for now
                if (rndChance < chance)
                {
                    await DropQuestItem(session, monsterEntityToAttack, playerGroup, (int)ItemVnums.LILY_OF_PURITY);

                    if (secondChanceDrop)
                    {
                        await DropQuestItem(session, monsterEntityToAttack, playerGroup, (int)ItemVnums.LILY_OF_PURITY);
                    }
                }
            }
        }

        #endregion

        bool hasPenalty = HasLevelPenalty(mainKiller, monsterEntityToAttack);

        int rate = session.CurrentMapInstance.HasMapFlag(MapFlags.ACT_4) || session.CurrentMapInstance.HasMapFlag(MapFlags.HAS_DROP_DIRECTLY_IN_INVENTORY_ENABLED) ||
            monsterEntityToAttack.DropToInventory
                ? 1
                : _serverManager.MobDropRate;

        if (secondChanceDrop)
        {
            session.PlayerEntity.BroadcastEffectInRange(EffectType.DoubleChanceDrop);
        }

        for (int i = 0; i < rate; i++)
        {
            foreach (DropDTO drop in monsterDrops)
            {
                float rndChance = _randomGenerator.RandomNumber(0, 100000);
                float chance = drop.DropChance + drop.DropChance * _serverManager.MobDropChance * (hasPenalty ? 0.1f : 1.0f);
                if (rndChance > chance)
                {
                    continue;
                }

                await DropItem(session, monsterEntityToAttack, drop.ItemVNum, drop.Amount, playerGroup);

                if (!secondChanceDrop)
                {
                    continue;
                }

                await DropItem(session, monsterEntityToAttack, drop.ItemVNum, drop.Amount, playerGroup);
            }
        }

        if (monsterEntityToAttack.RaidDrop != null)
        {
            foreach (DropChance drop in monsterEntityToAttack.RaidDrop)
            {
                float rndChance = _randomGenerator.RandomNumber(0, 100000);
                float chance = drop.Chance + drop.Chance * _serverManager.MobDropChance;
                if (rndChance > chance)
                {
                    continue;
                }

                await DropItem(session, monsterEntityToAttack, drop.ItemVnum, drop.Amount, playerGroup);

                if (!secondChanceDrop)
                {
                    continue;
                }

                await DropItem(session, monsterEntityToAttack, drop.ItemVnum, drop.Amount, playerGroup);
            }
        }

        if (session.PlayerEntity.TimeSpaceComponent.TimeSpace != null && monsterEntityToAttack.MapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance)
        {
            TimeSpaceParty timeSpace = session.PlayerEntity.TimeSpaceComponent.TimeSpace;
            float rndChance = _randomGenerator.RandomNumber(0, 100000);
            int itemChance = timeSpace.Instance.BonusPointItemDropChance;
            float chance = itemChance + itemChance * _serverManager.MobDropChance * (hasPenalty ? 0.1f : 1.0f);
            if (rndChance <= chance)
            {
                await DropItem(session, monsterEntityToAttack, (short)ItemVnums.BONUS_POINTS, 1, playerGroup);

                if (secondChanceDrop)
                {
                    await DropItem(session, monsterEntityToAttack, (short)ItemVnums.BONUS_POINTS, 1, playerGroup);
                }
            }
        }

        IReadOnlyList<DropDTO> raceDrop = _dropManager.GetDropsByMonsterRace(monsterEntityToAttack.MonsterRaceType, monsterEntityToAttack.MonsterRaceSubType);
        for (int i = 0; i < rate; i++)
        {
            foreach (DropDTO drop in raceDrop)
            {
                float rndChance = _randomGenerator.RandomNumber(0, 10000);
                float chance = drop.DropChance + drop.DropChance * _serverManager.MobDropChance * (hasPenalty ? 0.1f : 1.0f);
                if (rndChance > chance)
                {
                    continue;
                }

                await DropItem(session, monsterEntityToAttack, drop.ItemVNum, drop.Amount, playerGroup);

                if (!secondChanceDrop)
                {
                    continue;
                }

                await DropItem(session, monsterEntityToAttack, drop.ItemVNum, drop.Amount, playerGroup);
            }
        }

        if (monsterEntityToAttack.MonsterRaceType is MonsterRaceType.Fixed or MonsterRaceType.Other)
        {
            return;
        }

        int genericRate = session.CurrentMapInstance.HasMapFlag(MapFlags.ACT_4) ? 1 : _serverManager.GenericDropRate;

        for (int i = 0; i < genericRate; i++)
        {
            foreach (DropDTO drop in additionalDrop)
            {
                float rndChance = _randomGenerator.RandomNumber(0, 10000);
                float chance = drop.DropChance + drop.DropChance * _serverManager.GenericDropChance * (hasPenalty ? 0.1f : 1.0f);
                if (rndChance > chance)
                {
                    continue;
                }

                await DropItem(session, monsterEntityToAttack, drop.ItemVNum, drop.Amount, playerGroup);

                if (!secondChanceDrop)
                {
                    continue;
                }

                await DropItem(session, monsterEntityToAttack, drop.ItemVNum, drop.Amount, playerGroup);
            }
        }
    }

    private async Task DropQuestItem(IClientSession session, IMonsterEntity monsterEntityToAttack, PlayerGroup playerGroup, int itemVnum)
    {
        if (session.CurrentMapInstance.HasMapFlag(MapFlags.ACT_4) || monsterEntityToAttack.DropToInventory
            || session.CurrentMapInstance.HasMapFlag(MapFlags.HAS_DROP_DIRECTLY_IN_INVENTORY_ENABLED))
        {
            var alreadyGifted = new List<long>();
            foreach (IBattleEntity entity in monsterEntityToAttack.Damagers)
            {
                long charId = entity.Id;
                if (alreadyGifted.Contains(charId))
                {
                    continue;
                }

                IClientSession giftSession = _sessionManager.GetSessionByCharacterId(charId);
                if (giftSession == null)
                {
                    continue;
                }

                if (giftSession.PlayerEntity.MapInstance?.Id != monsterEntityToAttack.MapInstance?.Id)
                {
                    continue;
                }

                bool shouldReceiveDrop = ShouldReceiveDrop(giftSession, monsterEntityToAttack);
                if (!shouldReceiveDrop)
                {
                    continue;
                }

                if (giftSession.PlayerEntity.IsInGroup())
                {
                    foreach (IPlayerEntity member in giftSession.PlayerEntity.GetGroup().Members)
                    {
                        await member.Session.EmitEventAsync(new QuestItemPickUpEvent
                        {
                            ItemVnum = itemVnum,
                            Amount = 1,
                            SendMessage = true
                        });
                        alreadyGifted.Add(member.Id);
                    }
                }
                else
                {
                    await giftSession.EmitEventAsync(new QuestItemPickUpEvent
                    {
                        ItemVnum = itemVnum,
                        Amount = 1,
                        SendMessage = true
                    });
                    alreadyGifted.Add(giftSession.PlayerEntity.Id);
                }
            }

            return;
        }

        short newX = (short)(monsterEntityToAttack.PositionX + _randomGenerator.RandomNumber(-1, 2));
        short newY = (short)(monsterEntityToAttack.PositionY + _randomGenerator.RandomNumber(-1, 2));

        if (monsterEntityToAttack.MapInstance.IsBlockedZone(newX, newY))
        {
            newX = monsterEntityToAttack.PositionX;
            newY = monsterEntityToAttack.PositionY;
        }

        var newItemPosition = new Position(newX, newY);

        if (playerGroup == null)
        {
            var dropItem = new DropMapItemEvent(session.PlayerEntity.MapInstance, newItemPosition, (short)itemVnum, 1, ownerId: session.PlayerEntity.Id, isQuest: true);
            await _eventPipeline.ProcessEventAsync(dropItem);
            return;
        }

        string itemName;

        if (playerGroup.SharingMode == (byte)GroupSharingType.ByOrder)
        {
            long? dropOwner = playerGroup.GetNextOrderedCharacterId(session.PlayerEntity);

            if (!dropOwner.HasValue)
            {
                return;
            }

            foreach (IPlayerEntity s in playerGroup.Members)
            {
                itemName = _gameLanguage.GetItemName(_itemsManager.GetItem(itemVnum), s.Session);
                s.Session.SendChatMessage(_gameLanguage.GetLanguageFormat(GameDialogKey.GROUP_CHATMESSAGE_DROP_ORDERED, session.UserLanguage, 1,
                    itemName, playerGroup.Members.FirstOrDefault(c => c.Id == (long)dropOwner)?.Name), ChatMessageColorType.Yellow);
            }

            var dropItem = new DropMapItemEvent(session.PlayerEntity.MapInstance, newItemPosition, (short)itemVnum, 1, ownerId: dropOwner.Value, isQuest: true);
            await _eventPipeline.ProcessEventAsync(dropItem);
        }
        else
        {
            foreach (IPlayerEntity s in playerGroup.Members)
            {
                itemName = _gameLanguage.GetItemName(_itemsManager.GetItem(itemVnum), s.Session);
                s.Session.SendChatMessage(_gameLanguage.GetLanguageFormat(GameDialogKey.GROUP_CHATMESSAGE_DROP_SHARED, session.UserLanguage, 1, itemName), ChatMessageColorType.Yellow);
            }

            var dropItem = new DropMapItemEvent(session.PlayerEntity.MapInstance, newItemPosition, (short)itemVnum, 1, ownerId: session.PlayerEntity.Id, isQuest: true);
            await _eventPipeline.ProcessEventAsync(dropItem);
        }
    }

    private async Task DropItem(IClientSession session, IMonsterEntity monsterEntityToAttack, int itemVnum, int amount, PlayerGroup playerGroup)
    {
        if (session.CurrentMapInstance.HasMapFlag(MapFlags.ACT_4) || session.CurrentMapInstance.HasMapFlag(MapFlags.HAS_DROP_DIRECTLY_IN_INVENTORY_ENABLED)
            || monsterEntityToAttack.DropToInventory)
        {
            var alreadyGifted = new HashSet<long>();
            foreach (IBattleEntity entity in monsterEntityToAttack.Damagers)
            {
                long charId = entity.Id;
                if (alreadyGifted.Contains(charId))
                {
                    continue;
                }

                IClientSession giftSession = _sessionManager.GetSessionByCharacterId(charId);

                if (giftSession == null)
                {
                    continue;
                }

                if (giftSession.PlayerEntity.MapInstance?.Id != monsterEntityToAttack.MapInstance?.Id)
                {
                    continue;
                }

                bool shouldReceiveDrop = ShouldReceiveDrop(giftSession, monsterEntityToAttack);
                if (!shouldReceiveDrop)
                {
                    continue;
                }

                IGameItem item = _itemsManager.GetItem(itemVnum);
                sbyte randomRarity = _dropRarityConfigurationProvider.GetRandomRarity(item.ItemType);

                GameItemInstance itemInstance = _gameItemInstance.CreateItem(itemVnum, amount, 0, randomRarity);

                if (item.ItemType == ItemType.Map)
                {
                    continue;
                }

                await giftSession.AddNewItemToInventory(itemInstance, true, ChatMessageColorType.Yellow, true);
                alreadyGifted.Add(charId);
            }

            return;
        }

        short newX = (short)(monsterEntityToAttack.PositionX + _randomGenerator.RandomNumber(-1, 2));
        short newY = (short)(monsterEntityToAttack.PositionY + _randomGenerator.RandomNumber(-1, 2));

        if (monsterEntityToAttack.MapInstance.IsBlockedZone(newX, newY))
        {
            newX = monsterEntityToAttack.PositionX;
            newY = monsterEntityToAttack.PositionY;
        }

        var newItemPosition = new Position(newX, newY);

        if (playerGroup == null)
        {
            var dropItem = new DropMapItemEvent(session.PlayerEntity.MapInstance, newItemPosition, (short)itemVnum, amount, ownerId: session.PlayerEntity.Id);
            await _eventPipeline.ProcessEventAsync(dropItem);
            return;
        }

        if (playerGroup.SharingMode == (byte)GroupSharingType.ByOrder)
        {
            long? dropOwner = playerGroup.GetNextOrderedCharacterId(session.PlayerEntity);

            if (!dropOwner.HasValue)
            {
                return;
            }

            foreach (IPlayerEntity s in playerGroup.Members)
            {
                string itemName = _gameLanguage.GetItemName(_itemsManager.GetItem(itemVnum), s.Session);
                s.Session.SendChatMessage(_gameLanguage.GetLanguageFormat(GameDialogKey.GROUP_CHATMESSAGE_DROP_ORDERED, s.Session.UserLanguage, amount,
                    itemName, playerGroup.Members.FirstOrDefault(c => c.Id == (long)dropOwner)?.Name), ChatMessageColorType.Yellow);
            }

            var dropItem = new DropMapItemEvent(session.PlayerEntity.MapInstance, newItemPosition, (short)itemVnum, amount, ownerId: dropOwner.Value);
            await _eventPipeline.ProcessEventAsync(dropItem);
        }
        else
        {
            foreach (IPlayerEntity s in playerGroup.Members)
            {
                string itemName = _gameLanguage.GetItemName(_itemsManager.GetItem(itemVnum), s.Session);
                s.Session.SendChatMessage(_gameLanguage.GetLanguageFormat(GameDialogKey.GROUP_CHATMESSAGE_DROP_SHARED, s.Session.UserLanguage, amount, itemName), ChatMessageColorType.Yellow);
            }

            var dropItem = new DropMapItemEvent(session.PlayerEntity.MapInstance, newItemPosition, (short)itemVnum, amount, ownerId: session.PlayerEntity.Id);
            await _eventPipeline.ProcessEventAsync(dropItem);
        }
    }

    private bool ShouldReceiveDrop(IClientSession giftSession, IMonsterEntity monsterEntityToAttack)
    {
        long? tankPlayer = null;
        int highestHits = 0;
        foreach (IBattleEntity damager in monsterEntityToAttack.Damagers)
        {
            if (damager is not IPlayerEntity playerEntity)
            {
                continue;
            }

            if (!playerEntity.HitsByMonsters.TryGetValue(monsterEntityToAttack.Id, out int hits))
            {
                continue;
            }

            if (highestHits > hits)
            {
                continue;
            }

            tankPlayer = playerEntity.Id;
            highestHits = hits;
        }

        if (tankPlayer != null && giftSession.PlayerEntity.Id == tankPlayer.Value)
        {
            return true;
        }

        IPlayerEntity player = giftSession.PlayerEntity;
        if (!monsterEntityToAttack.PlayersDamage.TryGetValue(player.Id, out int damage))
        {
            return false;
        }

        int damageToDealt = (int)(monsterEntityToAttack.MaxHp * 0.05);
        return damageToDealt <= damage;
    }

    private async Task HandleGoldDrops(IMonsterEntity monsterEntityToAttack, PlayerGroup playerGroup, IPlayerEntity firstAttacker)
    {
        if (monsterEntityToAttack.MonsterRaceType is MonsterRaceType.Fixed or MonsterRaceType.Other)
        {
            return;
        }

        if (monsterEntityToAttack.MapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance)
        {
            return;
        }

        if (HasLevelPenalty(firstAttacker, monsterEntityToAttack))
        {
            return;
        }

        int gold = GetGold(firstAttacker, monsterEntityToAttack);
        long maxGold = _serverManager.MaxGold;
        gold = gold > maxGold ? (int)maxGold : gold;

        int randomNumber = 0;

        int rate = _serverManager.GoldDropRate;

        for (int i = 0; i < rate; i++)
        {
            randomNumber += _randomGenerator.RandomNumber();
        }

        if (randomNumber >= 50 * _serverManager.GoldDropChance)
        {
            return;
        }

        if (gold <= 0)
        {
            return;
        }

        IClientSession session = firstAttacker.Session;

        if (session.CurrentMapInstance == null)
        {
            return;
        }

        int secondChanceDropBCard = session.PlayerEntity.BCardComponent
            .GetAllBCardsInformation(BCardType.DropItemTwice, (byte)AdditionalTypes.DropItemTwice.DoubleDropChance, session.PlayerEntity.Level).firstData;
        bool secondChanceDrop = secondChanceDropBCard != 0 && _randomGenerator.RandomNumber() <= secondChanceDropBCard;

        if (secondChanceDrop)
        {
            session.BroadcastEffectInRange(EffectType.DoubleChanceDrop);
        }

        if (session.CurrentMapInstance.HasMapFlag(MapFlags.ACT_4) || session.CurrentMapInstance.HasMapFlag(MapFlags.HAS_DROP_DIRECTLY_IN_INVENTORY_ENABLED)
            || monsterEntityToAttack.DropToInventory)
        {
            var alreadyGifted = new HashSet<long>();
            foreach (IBattleEntity entity in monsterEntityToAttack.Damagers)
            {
                long charId = entity.Id;
                if (alreadyGifted.Contains(charId))
                {
                    continue;
                }

                IClientSession giftSession = _sessionManager.GetSessionByCharacterId(charId);

                if (giftSession == null)
                {
                    continue;
                }

                if (giftSession.PlayerEntity.MapInstance?.Id != monsterEntityToAttack.MapInstance?.Id)
                {
                    continue;
                }

                bool shouldReceiveDrop = ShouldReceiveDrop(giftSession, monsterEntityToAttack);
                if (!shouldReceiveDrop)
                {
                    continue;
                }

                await giftSession.EmitEventAsync(new GenerateGoldEvent
                (
                    (long)(gold * (1 + giftSession.PlayerEntity.BCardComponent.GetAllBCardsInformation(BCardType.Item,
                        (byte)AdditionalTypes.Item.IncreaseEarnedGold, giftSession.PlayerEntity.Level).firstData * 0.01))
                ));

                if (secondChanceDrop)
                {
                    await giftSession.EmitEventAsync(new GenerateGoldEvent
                    (
                        (long)(gold * (1 + giftSession.PlayerEntity.BCardComponent.GetAllBCardsInformation(BCardType.Item,
                            (byte)AdditionalTypes.Item.IncreaseEarnedGold, giftSession.PlayerEntity.Level).firstData * 0.01))
                    ));
                }

                alreadyGifted.Add(charId);
            }

            return;
        }

        string itemName = _itemsManager.GetItem((short)ItemVnums.GOLD).Name;
        if (playerGroup == null)
        {
            var dropGold = new DropMapItemEvent(session.PlayerEntity.MapInstance, monsterEntityToAttack.Position, (short)ItemVnums.GOLD, gold, ownerId: firstAttacker.Id);
            await _eventPipeline.ProcessEventAsync(dropGold);

            if (secondChanceDrop)
            {
                await _eventPipeline.ProcessEventAsync(dropGold);
            }

            return;
        }

        if (playerGroup.SharingMode == (byte)GroupSharingType.ByOrder)
        {
            long? dropOwner = playerGroup.GetNextOrderedCharacterId(firstAttacker);

            if (!dropOwner.HasValue)
            {
                return;
            }

            foreach (IPlayerEntity s in playerGroup.Members)
            {
                string itemNameTranslated = _gameLanguage.GetLanguage(GameDataType.Item, itemName, s.Session.UserLanguage);
                s.Session.SendChatMessage(
                    s.Session.GetLanguageFormat(GameDialogKey.GROUP_CHATMESSAGE_DROP_ORDERED, gold, itemNameTranslated, playerGroup.Members.FirstOrDefault(c => c.Id == (long)dropOwner)?.Name),
                    ChatMessageColorType.Yellow);
            }

            var dropGold = new DropMapItemEvent(session.PlayerEntity.MapInstance, monsterEntityToAttack.Position, (short)ItemVnums.GOLD, gold, ownerId: dropOwner.Value);
            await _eventPipeline.ProcessEventAsync(dropGold);
            if (secondChanceDrop)
            {
                await _eventPipeline.ProcessEventAsync(dropGold);
            }
        }
        else
        {
            foreach (IPlayerEntity s in playerGroup.Members)
            {
                string itemNameTranslated = _gameLanguage.GetLanguage(GameDataType.Item, itemName, s.Session.UserLanguage);
                s.Session.SendChatMessage(s.Session.GetLanguageFormat(GameDialogKey.GROUP_CHATMESSAGE_DROP_SHARED, gold, itemNameTranslated), ChatMessageColorType.Yellow);
            }

            var dropGold = new DropMapItemEvent(session.PlayerEntity.MapInstance, monsterEntityToAttack.Position, (short)ItemVnums.GOLD, gold, ownerId: session.PlayerEntity.Id);
            await _eventPipeline.ProcessEventAsync(dropGold);
            if (secondChanceDrop)
            {
                await _eventPipeline.ProcessEventAsync(dropGold);
            }
        }
    }

    private int GetGold(IPlayerEntity playerEntity, IMonsterEntity monsterEntity)
    {
        if (!playerEntity.MapInstance.HasMapFlag(MapFlags.IS_BASE_MAP) && playerEntity.MapInstance.MapInstanceType != MapInstanceType.TimeSpaceInstance)
        {
            return 0;
        }

        int lowBaseGold = _randomGenerator.RandomNumber(6 * monsterEntity.Level, 12 * monsterEntity.Level);
        return lowBaseGold * _serverManager.GoldRate;
    }

    private bool HasLevelPenalty(IPlayerEntity playerEntity, IMonsterEntity monsterEntity)
    {
        if (monsterEntity.Level >= 70)
        {
            return false;
        }

        return playerEntity.Level - monsterEntity.Level > 10;
    }
}