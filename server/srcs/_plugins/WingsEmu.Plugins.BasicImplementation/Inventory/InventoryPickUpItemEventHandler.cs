using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsAPI.Game.Extensions.ItemExtension.Item;
using WingsAPI.Packets.Enums.Shells;
using WingsEmu.DTOs.BCards;
using WingsEmu.DTOs.Bonus;
using WingsEmu.DTOs.Items;
using WingsEmu.Game;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Extensions.Mates;
using WingsEmu.Game.Groups;
using WingsEmu.Game.Helpers;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Inventory.Event;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Quests;
using WingsEmu.Game.Quests.Event;
using WingsEmu.Game.Raids;
using WingsEmu.Game.TimeSpaces.Events;
using WingsEmu.Game.Warehouse.Events;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.Inventory;

public class InventoryPickUpItemEventHandler : IAsyncEventProcessor<InventoryPickUpItemEvent>
{
    private readonly IBCardEffectHandlerContainer _bCardEffectHandler;
    private readonly IChestDropItemConfig _chestDropItemConfig;
    private readonly IDelayManager _delayManager;
    private readonly IGameItemInstanceFactory _gameItemInstanceFactory;
    private readonly IGameLanguageService _gameLanguage;
    private readonly IItemsManager _itemsManager;
    private readonly IRandomGenerator _randomGenerator;
    private readonly IDropRarityConfigurationProvider _rarityConfigurationProvider;
    private readonly IServerManager _serverManager;

    public InventoryPickUpItemEventHandler(IServerManager serverManager, IGameLanguageService gameLanguage, IItemsManager itemsManager,
        IGameItemInstanceFactory gameItemInstanceFactory, IDelayManager delayManager, IDropRarityConfigurationProvider rarityConfigurationProvider,
        IRandomGenerator randomGenerator, IChestDropItemConfig chestDropItemConfig,
        IBCardEffectHandlerContainer bCardEffectHandler)
    {
        _serverManager = serverManager;
        _gameLanguage = gameLanguage;
        _itemsManager = itemsManager;
        _gameItemInstanceFactory = gameItemInstanceFactory;
        _delayManager = delayManager;
        _rarityConfigurationProvider = rarityConfigurationProvider;
        _randomGenerator = randomGenerator;
        _chestDropItemConfig = chestDropItemConfig;
        _bCardEffectHandler = bCardEffectHandler;
    }

    public async Task HandleAsync(InventoryPickUpItemEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;
        if (!session.HasCurrentMapInstance)
        {
            return;
        }

        if (session.PlayerEntity.IsOnVehicle)
        {
            return;
        }

        if (session.PlayerEntity.IsSeal)
        {
            return;
        }

        if (session.PlayerEntity.IsInExchange())
        {
            return;
        }

        MapItem mapItem = session.CurrentMapInstance.GetDrop(e.DropId);

        if (mapItem == null)
        {
            return;
        }

        if (mapItem.Amount <= 0)
        {
            return;
        }

        bool canPickUpItem = false;
        var itemPosition = new Position(mapItem.PositionX, mapItem.PositionY);
        IMateEntity mateEntity = null;
        switch (e.PickerVisualType)
        {
            case VisualType.Player:
                canPickUpItem = session.PlayerEntity.Position.IsInRange(itemPosition, 5);
                break;
            case VisualType.Npc:
                mateEntity = session.PlayerEntity.MateComponent.GetMate(s => s.Id == e.PickerId);
                if (mateEntity == null)
                {
                    return;
                }

                canPickUpItem = mateEntity.MateType switch
                {
                    MateType.Partner => session.PlayerEntity.HaveStaticBonus(StaticBonusType.PartnerBackpack),
                    MateType.Pet => mateEntity.CanPickUp,
                    _ => false
                };

                break;
        }

        if (!canPickUpItem)
        {
            return;
        }

        if (mapItem.ItemVNum == (short)ItemVnums.GOLD)
        {
            await HandleGoldDrop(e.PickerVisualType, e.PickerId, session, mapItem);
            return;
        }

        switch (mapItem)
        {
            case ButtonMapItem button:
                if (button.CanBeMovedOnlyOnce.HasValue && button.CanBeMovedOnlyOnce.Value)
                {
                    session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.RAID_SHOUTMESSAGE_BUTTON_ALREADY_USED, session.UserLanguage), MsgMessageType.Middle);
                    return;
                }

                DateTime dateOfEnd = await _delayManager.RegisterAction(session.PlayerEntity, DelayedActionType.ButtonSwitch,
                    button.CustomDanceDuration.HasValue ? TimeSpan.FromMilliseconds(button.CustomDanceDuration.Value) : default);
                session.SendDelay((int)(dateOfEnd - DateTime.UtcNow).TotalMilliseconds, GuriType.ButtonSwitch, $"git {button.TransportId.ToString()}");
                return;
            case TimeSpaceMapItem timeSpaceMapItem:

                if (!timeSpaceMapItem.DancingTime.HasValue)
                {
                    await session.EmitEventAsync(new TimeSpacePickUpItemEvent
                    {
                        TimeSpaceMapItem = timeSpaceMapItem,
                        MateEntity = mateEntity
                    });
                    return;
                }

                dateOfEnd = await _delayManager.RegisterAction(session.PlayerEntity, DelayedActionType.ButtonSwitch, TimeSpan.FromMilliseconds(timeSpaceMapItem.DancingTime.Value));
                session.SendDelay((int)(dateOfEnd - DateTime.UtcNow).TotalMilliseconds, GuriType.OpeningBox, $"git {timeSpaceMapItem.TransportId.ToString()}");
                return;
        }

        if (mateEntity is { MateType: MateType.Partner })
        {
            if (!session.PlayerEntity.HasSpaceForPartnerItemWarehouse(mapItem.ItemVNum, (short)mapItem.Amount) && mapItem is not MonsterMapItem { IsQuest: true })
            {
                session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.PARTNER_WAREHOUSE_MESSAGE_NO_ENOUGH_PLACE, session.UserLanguage), ChatMessageColorType.Yellow);
                session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.PARTNER_WAREHOUSE_MESSAGE_NO_ENOUGH_PLACE, session.UserLanguage), MsgMessageType.Middle);
                return;
            }
        }
        else
        {
            GameItemInstance instance = mapItem.GetItemInstance();
            bool isMap = instance.GameItem.ItemType == ItemType.Map;
            if (!isMap && !session.PlayerEntity.HasSpaceFor(mapItem.ItemVNum, (short)mapItem.Amount) && mapItem is not MonsterMapItem { IsQuest: true })
            {
                session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.INTERACTION_MESSAGE_NOT_ENOUGH_PLACE, session.UserLanguage), ChatMessageColorType.Yellow);
                session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.INTERACTION_MESSAGE_NOT_ENOUGH_PLACE, session.UserLanguage), MsgMessageType.Middle);
                return;
            }
        }

        GameItemInstance itemInstance = null;
        if (mapItem is MonsterMapItem item)
        {
            if (item.OwnerId.HasValue && item.OwnerId.Value != -1)
            {
                PlayerGroup group = session.PlayerEntity.GetGroup();
                if (group == null)
                {
                    if (item.CreatedDate?.AddSeconds(30) > DateTime.UtcNow && item.OwnerId != session.PlayerEntity.Id)
                    {
                        session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.INFORMATION_CHATMESSAGE_NOT_YOUR_ITEM, session.UserLanguage), ChatMessageColorType.Yellow);
                        return;
                    }
                }
                else
                {
                    switch (group.SharingMode)
                    {
                        case GroupSharingType.ByOrder:
                            if (item.CreatedDate?.AddSeconds(30) > DateTime.UtcNow && item.OwnerId != session.PlayerEntity.Id)
                            {
                                session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.INFORMATION_CHATMESSAGE_NOT_YOUR_ITEM, session.UserLanguage), ChatMessageColorType.Yellow);
                                return;
                            }

                            break;
                        case GroupSharingType.Everyone:
                            bool canPickUp = session.PlayerEntity.GetGroup().Members.Any(entity => entity.Id == item.OwnerId.Value);

                            if (!canPickUp && item.CreatedDate?.AddSeconds(30) > DateTime.UtcNow)
                            {
                                session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.INFORMATION_CHATMESSAGE_NOT_YOUR_ITEM, session.UserLanguage), ChatMessageColorType.Yellow);
                                return;
                            }

                            break;
                    }
                }
            }

            itemInstance = item.GetItemInstance();
            if (itemInstance != null && itemInstance.GameItem.Type == InventoryType.Equipment && itemInstance.GameItem.ItemType is ItemType.Weapon or ItemType.Armor &&
                itemInstance.Type == ItemInstanceType.WearableInstance)
            {
                short rarity = _rarityConfigurationProvider.GetRandomRarity(itemInstance.GameItem.ItemType);
                itemInstance.Rarity = rarity;
                itemInstance.SetRarityPoint(_randomGenerator);
            }
        }

        itemInstance ??= mapItem.GetItemInstance();
        if (itemInstance?.GameItem.ItemType == ItemType.Map)
        {
            await HandleMapItemDrop(e.PickerVisualType, e.PickerId, session, mapItem, itemInstance);
            await CheckCollectingQuests(session, mateEntity, mapItem, itemInstance);
            return;
        }

        if (!session.CurrentMapInstance.RemoveDrop(mapItem.TransportId))
        {
            return;
        }

        switch (e.PickerVisualType)
        {
            case VisualType.Player:
                session.BroadcastGetPacket(mapItem.TransportId);
                session.PlayerEntity.SendIconPacket(true, mapItem.ItemVNum);
                break;
            case VisualType.Npc:
                mateEntity?.BroadcastMateGetPacket(mapItem.TransportId);
                mateEntity?.Owner.Session.SendCondMate(mateEntity);
                mateEntity?.SendIconPacket(true, mapItem.ItemVNum);
                mateEntity?.Owner?.Session.SendPacket(mateEntity.GenerateEffectPacket(EffectType.PetPickUp));
                break;
        }

        await CheckCollectingQuests(session, mateEntity, mapItem, itemInstance);
    }

    private async Task CheckCollectingQuests(IClientSession session, IMateEntity mateEntity, MapItem mapItem, GameItemInstance mapItemInstance)
    {
        // Quest logic
        if (mapItem is CharacterMapItem or MonsterMapItem { IsQuest: false } && mapItemInstance.GameItem.ItemType != ItemType.Map)
        {
            if (mateEntity is { MateType: MateType.Partner })
            {
                await session.EmitEventAsync(new PartnerWarehouseAddItemEvent
                {
                    ItemInstance = mapItemInstance
                });

                await session.EmitEventAsync(new InventoryPickedUpItemEvent
                {
                    ItemVnum = mapItemInstance.ItemVNum,
                    Amount = mapItem.Amount,
                    Location = new Location(mapItem.MapInstance.MapId, mapItem.PositionX, mapItem.PositionY)
                });

                return;
            }

            await session.AddNewItemToInventory(mapItemInstance, true);
            if (mapItem is CharacterMapItem item)
            {
                await session.EmitEventAsync(new InventoryPickedUpPlayerItemEvent
                {
                    ItemInstance = item.GetItemInstance(),
                    Amount = item.ItemVNum,
                    Location = new Location(mapItem.MapInstance.MapId, mapItem.PositionX, mapItem.PositionY)
                });
                return;
            }

            await session.EmitEventAsync(new InventoryPickedUpItemEvent
            {
                ItemVnum = mapItemInstance.ItemVNum,
                Amount = mapItem.Amount,
                Location = new Location(mapItem.MapInstance.MapId, mapItem.PositionX, mapItem.PositionY)
            });
            return;
        }

        IEnumerable<CharacterQuest> characterQuests = session.PlayerEntity.GetCurrentQuests().Where(s =>
            s.Quest.QuestType is QuestType.DROP_CHANCE or QuestType.DROP_CHANCE_2 or QuestType.DROP_HARDCODED or QuestType.DROP_IN_TIMESPACE);

        bool wasQuestItem = characterQuests.Any(s => s.Quest.Objectives.Any(o => mapItem.ItemVNum == (s.Quest.QuestType == QuestType.DROP_HARDCODED ? o.Data0 : o.Data1)));
        if (!wasQuestItem)
        {
            return;
        }

        if (session.PlayerEntity.IsInGroup())
        {
            foreach (IPlayerEntity member in session.PlayerEntity.GetGroup().Members)
            {
                await member.Session.EmitEventAsync(new QuestItemPickUpEvent
                {
                    ItemVnum = mapItem.ItemVNum,
                    Amount = mapItem.Amount,
                    SendMessage = true
                });
            }
        }
        else
        {
            await session.EmitEventAsync(new QuestItemPickUpEvent
            {
                ItemVnum = mapItem.ItemVNum,
                Amount = mapItem.Amount,
                SendMessage = true
            });
        }
    }

    private async Task HandleMapItemDrop(VisualType type, long pickerId, IClientSession session, MapItem mapItem, GameItemInstance mapItemInstance)
    {
        if (mapItemInstance == null)
        {
            session.CurrentMapInstance.RemoveDrop(mapItem.TransportId);
            return;
        }

        if (mapItemInstance.GameItem.Effect == 71)
        {
            session.PlayerEntity.SpPointsBasic += mapItemInstance.GameItem.EffectValue;
            if (session.PlayerEntity.SpPointsBasic > _serverManager.MaxBasicSpPoints)
            {
                session.PlayerEntity.SpPointsBasic = _serverManager.MaxBasicSpPoints;
            }

            session.SendMsg(_gameLanguage.GetLanguageFormat(GameDialogKey.SPECIALIST_SHOUTMESSAGE_POINTS_ADDED, session.UserLanguage, mapItem.GetItemInstance().GameItem.EffectValue),
                MsgMessageType.Middle);
            session.RefreshSpPoint();
        }

        switch ((ItemVnums)mapItemInstance.ItemVNum)
        {
            case ItemVnums.WILD_SOUND_FLOWER:
                await session.EmitEventAsync(new AddSoundFlowerQuestEvent
                {
                    SoundFlowerType = SoundFlowerType.WILD_SOUND_FLOWER
                });
                break;

            case ItemVnums.FAKE_MIMIC_POTION:

                session.SendMsg(session.GetLanguage(GameDialogKey.ITEM_SHOUTMESSAGE_MIMIC_FAKE_POTION), MsgMessageType.Middle);

                foreach (BCardDTO bCard in mapItemInstance.GameItem.BCards)
                {
                    _bCardEffectHandler.Execute(session.PlayerEntity, session.PlayerEntity, bCard);
                }

                break;

            case ItemVnums.BONUS_POINTS:

                if (session.CurrentMapInstance.MapInstanceType != MapInstanceType.TimeSpaceInstance && !session.PlayerEntity.TimeSpaceComponent.IsInTimeSpaceParty)
                {
                    break;
                }

                await session.EmitEventAsync(new TimeSpaceIncreaseScoreEvent
                {
                    AmountToIncrease = _randomGenerator.RandomNumber(1, 11),
                    TimeSpaceParty = session.PlayerEntity.TimeSpaceComponent.TimeSpace
                });

                break;
        }

        if (mapItemInstance.GameItem.IsTimeSpaceChest())
        {
            switch (mapItemInstance.GameItem.Data[0])
            {
                case 4:

                    ChestDropItemConfiguration config = _chestDropItemConfig.GetChestByDataId(mapItemInstance.GameItem.Data[2]);

                    if (config?.PossibleItems == null)
                    {
                        break;
                    }

                    if (_randomGenerator.RandomNumber() > config.ItemChance)
                    {
                        session.SendMsg(session.GetLanguage(GameDialogKey.ITEM_SHOUTMESSAGE_CHEST_EMPTY), MsgMessageType.Middle);
                        break;
                    }

                    ChestDropItemDrop getRandomItem = config.PossibleItems[_randomGenerator.RandomNumber(config.PossibleItems.Count)];
                    if (getRandomItem == null)
                    {
                        break;
                    }

                    GameItemInstance item = _gameItemInstanceFactory.CreateItem(getRandomItem.ItemVnum, getRandomItem.Amount);
                    await session.AddNewItemToInventory(item, sendGiftIsFull: true);

                    string itemName = item.GameItem.GetItemName(_gameLanguage, session.UserLanguage);
                    session.SendMsg(session.GetLanguageFormat(GameDialogKey.INVENTORY_CHATMESSAGE_X_ITEM_ACQUIRED, getRandomItem.Amount, itemName), MsgMessageType.Middle);
                    break;
            }
        }

        session.CurrentMapInstance.RemoveDrop(mapItem.TransportId);
        switch (type)
        {
            case VisualType.Player:
                session.BroadcastGetPacket(mapItem.TransportId);
                break;
            case VisualType.Npc:
                IMateEntity mateEntity = session.PlayerEntity.MateComponent.GetMate(s => s.Id == pickerId);
                mateEntity?.BroadcastMateGetPacket(mapItem.TransportId);
                mateEntity?.Owner.Session.SendCondMate(mateEntity);
                mateEntity?.Owner?.Session.SendPacket(mateEntity.GenerateEffectPacket(EffectType.PetPickUp));
                break;
        }
    }

    private async Task HandleGoldDrop(VisualType type, long pickerId, IClientSession session, MapItem mapItem)
    {
        if (mapItem is not MonsterMapItem droppedGold)
        {
            return;
        }

        if (droppedGold.OwnerId.HasValue && droppedGold.OwnerId.Value != -1)
        {
            PlayerGroup group = session.PlayerEntity.GetGroup();
            if (group == null)
            {
                if (droppedGold.CreatedDate?.AddSeconds(30) > DateTime.UtcNow && droppedGold.OwnerId != session.PlayerEntity.Id)
                {
                    session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.INFORMATION_CHATMESSAGE_NOT_YOUR_ITEM, session.UserLanguage), ChatMessageColorType.Yellow);
                    return;
                }
            }
            else
            {
                switch (group.SharingMode)
                {
                    case GroupSharingType.ByOrder:
                        if (droppedGold.CreatedDate?.AddSeconds(30) > DateTime.UtcNow && droppedGold.OwnerId != session.PlayerEntity.Id)
                        {
                            session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.INFORMATION_CHATMESSAGE_NOT_YOUR_ITEM, session.UserLanguage), ChatMessageColorType.Yellow);
                            return;
                        }

                        break;
                    case GroupSharingType.Everyone:
                        bool canPickUp = session.PlayerEntity.GetGroup().Members.Any(entity => entity.Id == droppedGold.OwnerId.Value);

                        if (!canPickUp && droppedGold.CreatedDate?.AddSeconds(30) > DateTime.UtcNow)
                        {
                            session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.INFORMATION_CHATMESSAGE_NOT_YOUR_ITEM, session.UserLanguage), ChatMessageColorType.Yellow);
                            return;
                        }

                        break;
                }
            }
        }

        long maxGold = _serverManager.MaxGold;

        double multiplier = 1 + (session.PlayerEntity.BCardComponent.GetAllBCardsInformation(BCardType.Item, (byte)AdditionalTypes.Item.IncreaseEarnedGold, session.PlayerEntity.Level).firstData * 0.01
            + session.PlayerEntity.GetMaxWeaponShellValue(ShellEffectType.GainMoreGold, true) * 0.01);
        int basicDrop = droppedGold.Amount;
        int goldDropped = (int)(droppedGold.Amount * multiplier);

        if (session.PlayerEntity.Gold + goldDropped > maxGold)
        {
            session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.INFORMATION_MESSAGE_MAX_GOLD, session.UserLanguage), MsgMessageType.Middle);
            return;
        }

        if (!session.CurrentMapInstance.RemoveDrop(mapItem.TransportId))
        {
            return;
        }

        session.PlayerEntity.Gold += goldDropped;
        if (session.PlayerEntity.Gold > maxGold)
        {
            session.PlayerEntity.Gold = maxGold;
        }

        await session.EmitEventAsync(new InventoryPickedUpItemEvent
        {
            ItemVnum = mapItem.ItemVNum,
            Amount = goldDropped,
            Location = new Location(mapItem.MapInstance.MapId, mapItem.PositionX, mapItem.PositionY)
        });

        string itemName = _itemsManager.GetItem((short)ItemVnums.GOLD).GetItemName(_gameLanguage, session.UserLanguage);

        if (basicDrop != goldDropped)
        {
            session.SendChatMessage(_gameLanguage.GetLanguageFormat(GameDialogKey.INVENTORY_CHATMESSAGE_GOLD_BONUS_ACQUIRED, session.UserLanguage,
                basicDrop, itemName, goldDropped - basicDrop), ChatMessageColorType.Green);
        }
        else
        {
            session.SendChatMessage(_gameLanguage.GetLanguageFormat(GameDialogKey.INVENTORY_CHATMESSAGE_X_ITEM_ACQUIRED, session.UserLanguage, goldDropped, itemName), ChatMessageColorType.Green);
        }

        session.RefreshGold();

        switch (type)
        {
            case VisualType.Player:
                session.BroadcastGetPacket(mapItem.TransportId);
                break;
            case VisualType.Npc:
                IMateEntity mateEntity = session.PlayerEntity.MateComponent.GetMate(s => s.Id == pickerId);
                mateEntity?.BroadcastMateGetPacket(mapItem.TransportId);
                mateEntity?.Owner.Session.SendCondMate(mateEntity);
                mateEntity?.Owner?.Session.SendPacket(mateEntity.GenerateEffectPacket(EffectType.PetPickUp));
                break;
        }
    }
}