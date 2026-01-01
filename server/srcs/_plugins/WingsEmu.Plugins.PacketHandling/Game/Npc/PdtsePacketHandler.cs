using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsAPI.Game.Extensions.ItemExtension.Item;
using WingsAPI.Game.Extensions.Quests;
using WingsEmu.DTOs.Maps;
using WingsEmu.DTOs.Quests;
using WingsEmu.DTOs.Recipes;
using WingsEmu.Game;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Entities.Event;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Managers.ServerData;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Npcs.Event;
using WingsEmu.Game.Portals;
using WingsEmu.Game.Quests;
using WingsEmu.Game.Quests.Event;
using WingsEmu.Packets.ClientPackets;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.PacketHandling.Game.Npc;

public class PdtsePacketHandler : GenericGamePacketHandlerBase<PdtsePacket>
{
    private readonly IGameItemInstanceFactory _gameItemInstanceFactory;
    private readonly IItemsManager _itemsManager;
    private readonly IItemUsageManager _itemUsageManager;
    private readonly IGameLanguageService _language;
    private readonly INpcEntityFactory _npcEntityFactory;
    private readonly INpcMonsterManager _npcMonsterManager;
    private readonly IRandomGenerator _randomGenerator;
    private readonly IRecipeManager _recipeManager;
    private readonly ITimeSpaceConfiguration _timeSpaceConfiguration;

    public PdtsePacketHandler(IGameLanguageService language, IRecipeManager recipeManager, IItemUsageManager itemUsageManager,
        IGameItemInstanceFactory gameItemInstanceFactory, IItemsManager itemsManager, IRandomGenerator randomGenerator,
        ITimeSpaceConfiguration timeSpaceConfiguration, INpcEntityFactory npcEntityFactory,
        INpcMonsterManager npcMonsterManager)
    {
        _language = language;
        _recipeManager = recipeManager;
        _itemUsageManager = itemUsageManager;
        _gameItemInstanceFactory = gameItemInstanceFactory;
        _itemsManager = itemsManager;
        _randomGenerator = randomGenerator;
        _timeSpaceConfiguration = timeSpaceConfiguration;
        _npcEntityFactory = npcEntityFactory;
        _npcMonsterManager = npcMonsterManager;
    }

    protected override async Task HandlePacketAsync(IClientSession session, PdtsePacket packet)
    {
        if (!session.HasCurrentMapInstance)
        {
            return;
        }

        short vNum = packet.VNum;
        int lastItem = session.PlayerEntity.LastMinilandProducedItem ?? _itemUsageManager.GetLastItemUsed(session.PlayerEntity.Id);
        int lastNpcId = session.PlayerEntity.LastNRunId;
        short? eqItemSlot = packet.EqItemSlot;

        IReadOnlyList<Recipe> producerItemRecipes = _recipeManager.GetRecipesByProducerItemVnum(lastItem);
        Recipe recipe = producerItemRecipes?.FirstOrDefault(x => x.ProducedItemVnum == vNum);

        bool isNpcRecipe = false;
        if (recipe == null)
        {
            IReadOnlyList<Recipe> npcRecipes = _recipeManager.GetRecipesByNpcId(lastNpcId) ??
                _recipeManager.GetRecipesByNpcMonsterVnum(session.CurrentMapInstance.GetNpcById(lastNpcId)?.MonsterVNum ?? 0);
            recipe = npcRecipes?.FirstOrDefault(x => x.ProducedItemVnum == vNum);
            isNpcRecipe = true;
        }

        if (recipe is not { Amount: > 0 })
        {
            session.SendEmptyRecipeCraftItem();
            session.PlayerEntity.IsCraftingItem = false;
            return;
        }

        if (packet.Type == 1)
        {
            session.SendRecipeCraftItemList(recipe);
            session.PlayerEntity.IsCraftingItem = true;
            return;
        }

        if (!recipe.Items.Any() || recipe.Items.Any(ite => !session.PlayerEntity.HasItem(ite.ItemVNum, ite.Amount)))
        {
            session.PlayerEntity.IsCraftingItem = false;
            return;
        }

        IGameItem producedItem = _itemsManager.GetItem(recipe.ProducedItemVnum);
        if (producedItem == null)
        {
            return;
        }

        if (!session.PlayerEntity.HasSpaceFor(recipe.ProducedItemVnum, (short)recipe.Amount) && !producedItem.IsTimeSpaceStone())
        {
            session.SendChatMessage(_language.GetLanguage(GameDialogKey.INTERACTION_MESSAGE_NOT_ENOUGH_PLACE, session.UserLanguage), ChatMessageColorType.Yellow);
            session.PlayerEntity.IsCraftingItem = false;
            return;
        }

        bool close = false;
        short rarity = 0;
        short upgrade = 0;

        if (!CanCraftTimeSpace(session, producedItem, _timeSpaceConfiguration))
        {
            session.SendShopEndPacket(ShopEndType.Player);
            return;
        }

        foreach (RecipeItemDTO recipeItem in recipe.Items)
        {
            InventoryItem getFirstItem = session.PlayerEntity.GetFirstItemByVnum(recipeItem.ItemVNum);
            IGameItem eqItem = _itemsManager.GetItem(recipeItem.ItemVNum);
            bool isEquipment = false;

            if (eqItem.Type == InventoryType.Equipment && recipeItem.Slot == 0)
            {
                close = true;
                isEquipment = true;
                getFirstItem = eqItemSlot.HasValue ? session.PlayerEntity.GetItemBySlotAndType(eqItemSlot.Value, InventoryType.Equipment) : null;
                getFirstItem ??= session.PlayerEntity.GetFirstItemByVnum(recipeItem.ItemVNum);
            }

            if (!isEquipment)
            {
                await session.RemoveItemFromInventory(recipeItem.ItemVNum, recipeItem.Amount);
            }
            else
            {
                await session.RemoveItemFromInventory(amount: recipeItem.Amount, item: getFirstItem);
            }

            if (session.PlayerEntity.HasItem(recipeItem.ItemVNum, recipeItem.Amount))
            {
                continue;
            }

            close = true;
        }

        GameItemInstance newItem = _gameItemInstanceFactory.CreateItem(recipe.ProducedItemVnum, recipe.Amount, (byte)upgrade, (sbyte)rarity);

        if (newItem.GameItem.IsTimeSpaceStone())
        {
            await CreateTimeSpaceNpc(session, newItem.GameItem);
            return;
        }

        await ProcessCraftingQuest(session, newItem);

        if (!isNpcRecipe && close || !session.PlayerEntity.IsCraftingItem)
        {
            session.SendShopEndPacket(ShopEndType.Player);
        }

        await session.EmitEventAsync(new ItemProducedEvent
        {
            ItemInstance = newItem,
            ItemAmount = recipe.Amount
        });

        session.SendSound(SoundType.CRAFTING_SUCCESS);
        string itemName = _language.GetLanguage(GameDataType.Item, newItem.GameItem.Name, session.UserLanguage);
        session.SendMsg(_language.GetLanguageFormat(GameDialogKey.ITEM_SHOUTMESSAGE_CRAFTED_OBJECT, session.UserLanguage, itemName, recipe.Amount.ToString()), MsgMessageType.Middle);
        _itemUsageManager.SetLastItemUsed(session.PlayerEntity.Id, 0);
    }

    private bool CanCraftTimeSpace(IClientSession session, IGameItem item, ITimeSpaceConfiguration timeSpaceConfiguration)
    {
        // If it's not TS crafted stone
        if (!item.IsTimeSpaceStone())
        {
            return true;
        }

        if (!session.CurrentMapInstance.HasMapFlag(MapFlags.IS_BASE_MAP))
        {
            session.SendMsg(session.GetLanguage(GameDialogKey.INFORMATION_SHOUTMESSAGE_MUST_BE_IN_CLASSIC_MAP), MsgMessageType.Middle);
            return false;
        }

        int mapVnum = item.Data[3];

        if (mapVnum == -1 && session.CurrentMapInstance.HasMapFlag(MapFlags.ACT_4))
        {
            session.SendMsg(session.GetLanguage(GameDialogKey.INFORMATION_SHOUTMESSAGE_MUST_BE_IN_CLASSIC_MAP), MsgMessageType.Middle);
            return false;
        }

        if (mapVnum != -1 && session.CurrentMapInstance.MapVnum != mapVnum)
        {
            session.SendMsg(session.GetLanguage(GameDialogKey.TIMESPACE_SHOUTMESSAGE_WRONG_MAP), MsgMessageType.Middle);
            return false;
        }

        // Can't create portal close to another one
        IPortalEntity portal = session.CurrentMapInstance.Portals.Concat(session.PlayerEntity.GetExtraPortal()).FirstOrDefault(s =>
            session.PlayerEntity.PositionY >= s.PositionY - 3 &&
            session.PlayerEntity.PositionY <= s.PositionY + 3 &&
            session.PlayerEntity.PositionX >= s.PositionX - 3 &&
            session.PlayerEntity.PositionX <= s.PositionX + 3);

        ITimeSpacePortalEntity tsPortal = session.CurrentMapInstance.TimeSpacePortals.FirstOrDefault(s =>
            session.PlayerEntity.PositionY >= s.Position.Y - 3 &&
            session.PlayerEntity.PositionY <= s.Position.Y + 3 &&
            session.PlayerEntity.PositionX >= s.Position.X - 3 &&
            session.PlayerEntity.PositionX <= s.Position.X + 3);

        INpcEntity anotherTimeSpaceNpc = session.CurrentMapInstance.GetPassiveNpcs().FirstOrDefault(s =>
            session.PlayerEntity.PositionY >= s.Position.Y - 3 &&
            session.PlayerEntity.PositionY <= s.Position.Y + 3 &&
            session.PlayerEntity.PositionX >= s.Position.X - 3 &&
            session.PlayerEntity.PositionX <= s.Position.X + 3 && s.TimeSpaceOwnerId.HasValue);

        if (portal != null || tsPortal != null || anotherTimeSpaceNpc != null)
        {
            session.SendMsg(session.GetLanguage(GameDialogKey.TIMESPACE_SHOUTMESSAGE_CLOSE_PORTAL), MsgMessageType.Middle);
            return false;
        }

        // Check if player created already Time-Space npc
        INpcEntity timeSpaceNpc = session.CurrentMapInstance.GetPassiveNpcs().FirstOrDefault(x => x.TimeSpaceOwnerId.HasValue && session.PlayerEntity.Id == x.TimeSpaceOwnerId.Value);
        if (timeSpaceNpc != null)
        {
            return false;
        }

        TimeSpaceFileConfiguration timeSpace = _timeSpaceConfiguration.GetTimeSpaceConfiguration(item.Data[2]);
        return timeSpace != null;
    }

    private async Task CreateTimeSpaceNpc(IClientSession session, IGameItem item)
    {
        session.SendSound(SoundType.CRAFTING_SUCCESS);
        session.SendShopEndPacket(ShopEndType.Player);
        _itemUsageManager.SetLastItemUsed(session.PlayerEntity.Id, 0);

        TimeSpaceFileConfiguration timeSpace = _timeSpaceConfiguration.GetTimeSpaceConfiguration(item.Data[2]);
        if (timeSpace == null)
        {
            return;
        }

        IMonsterData timeSpaceMonster = _npcMonsterManager.GetNpc((short)MonsterVnum.TIME_SPACE_NPC);
        if (timeSpaceMonster == null)
        {
            return;
        }

        INpcEntity newNpc = _npcEntityFactory.CreateNpc(timeSpaceMonster, session.CurrentMapInstance, null, new NpcAdditionalData
        {
            TimeSpaceInfo = timeSpace,
            TimeSpaceOwnerId = session.PlayerEntity.Id
        });

        await newNpc.EmitEventAsync(new MapJoinNpcEntityEvent(newNpc, session.PlayerEntity.Position.X, session.PlayerEntity.Position.Y));
        string packet = newNpc.GenerateEffectGround(EffectType.BlueTimeSpace, newNpc.PositionX, newNpc.PositionY, false);
        newNpc.MapInstance.Broadcast(x => packet);
    }

    private async Task ProcessCraftingQuest(IClientSession session, GameItemInstance itemCrafted)
    {
        IReadOnlyCollection<CharacterQuest> characterQuests = session.PlayerEntity.GetCurrentQuests()
            .Where(s => s.Quest.QuestType == QuestType.CRAFT_WITHOUT_KEEPING && s.Quest.Objectives.Any(o => o.Data0 == itemCrafted.ItemVNum)).ToList();

        if (!characterQuests.Any())
        {
            InventoryItem invItem = await session.AddNewItemToInventory(itemCrafted, sendGiftIsFull: true);
            GameItemInstance item = invItem.ItemInstance;
            if (item.GameItem.EquipmentSlot is EquipmentType.Armor or EquipmentType.MainWeapon or EquipmentType.SecondaryWeapon)
            {
                item.SetRarityPoint(_randomGenerator);
            }

            session.SendPdtiPacket(PdtiType.ItemHasBeenProduced, itemCrafted.ItemVNum, (short)itemCrafted.Amount, invItem.Slot, itemCrafted.Upgrade, itemCrafted.Rarity);
            return;
        }

        foreach (CharacterQuest quest in characterQuests)
        {
            IEnumerable<QuestObjectiveDto> objectives = quest.Quest.Objectives;
            foreach (QuestObjectiveDto objective in objectives)
            {
                if (objective.Data0 != itemCrafted.ItemVNum)
                {
                    continue;
                }

                CharacterQuestObjectiveDto questObjectiveDto = quest.ObjectiveAmount[objective.ObjectiveIndex];

                int objectiveAmount = Math.Min(itemCrafted.Amount, quest.ObjectiveAmount[objective.ObjectiveIndex].RequiredAmount);
                questObjectiveDto.CurrentAmount += objectiveAmount;

                await session.EmitEventAsync(new QuestObjectiveUpdatedEvent
                {
                    CharacterQuest = quest
                });

                if (session.PlayerEntity.IsQuestCompleted(quest))
                {
                    await session.EmitEventAsync(new QuestCompletedEvent(quest));
                }
            }
        }
    }
}