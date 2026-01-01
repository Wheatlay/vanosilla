using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsEmu.DTOs.Items;
using WingsEmu.DTOs.ServerDatas;
using WingsEmu.Game;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.Event.Characters;

public class RollItemBoxEventHandler : IAsyncEventProcessor<RollItemBoxEvent>
{
    private readonly IGameItemInstanceFactory _gameItemInstanceFactory;
    private readonly IGameLanguageService _gameLanguage;
    private readonly IItemBoxManager _itemBoxManager;
    private readonly IItemsManager _itemManager;
    private readonly IRandomGenerator _random;
    private readonly ISessionManager _sessionManager;

    public RollItemBoxEventHandler(IRandomGenerator random, IGameLanguageService gameLanguage, IItemsManager itemManager, ISessionManager sessionManager,
        IGameItemInstanceFactory gameItemInstanceFactory, IItemBoxManager itemBoxManager)
    {
        _random = random;
        _gameLanguage = gameLanguage;
        _itemManager = itemManager;
        _sessionManager = sessionManager;
        _gameItemInstanceFactory = gameItemInstanceFactory;
        _itemBoxManager = itemBoxManager;
    }

    public async Task HandleAsync(RollItemBoxEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;
        InventoryItem item = e.Item;

        // ItemBoxManager
        ItemBoxDto itemBox = _itemBoxManager.GetItemBoxByItemVnumAndDesign(item.ItemInstance.ItemVNum);
        if (itemBox == null)
        {
            return;
        }

        if (itemBox.ItemBoxType == ItemBoxType.BUNDLE)
        {
            foreach (ItemBoxItemDto rollItem in itemBox.Items)
            {
                GameItemInstance newItem =
                    _gameItemInstanceFactory.CreateItem(rollItem.ItemGeneratedVNum, rollItem.ItemGeneratedAmount, rollItem.ItemGeneratedUpgrade, (sbyte)item.ItemInstance.Rarity);
                await session.AddNewItemToInventory(newItem, true, sendGiftIsFull: true);
                if (itemBox.ShowsRaidBoxPanelOnOpen)
                {
                    session.SendRdiPacket(rollItem.ItemGeneratedVNum, rollItem.ItemGeneratedAmount);
                }
            }


            await session.RemoveItemFromInventory(item: item);
            return;
        }

        IReadOnlyCollection<ItemBoxItemDto> rewards = GetRandomRewards(session, itemBox);
        List<ItemInstanceDTO> obtainedItems = new();
        foreach (ItemBoxItemDto itemBoxItem in rewards)
        {
            byte upgrade = itemBoxItem.ItemGeneratedUpgrade;
            IGameItem createdGameItem = _itemManager.GetItem(itemBoxItem.ItemGeneratedVNum);

            if (createdGameItem == null)
            {
                continue;
            }

            sbyte rarity = 0;
            if (createdGameItem.ItemType != ItemType.Box)
            {
                rarity = (sbyte)item.ItemInstance.Rarity;
            }

            GameItemInstance newItem = _gameItemInstanceFactory.CreateItem(itemBoxItem.ItemGeneratedVNum, itemBoxItem.ItemGeneratedAmount, upgrade, rarity);
            obtainedItems.Add(newItem);
            await session.AddNewItemToInventory(newItem, true, sendGiftIsFull: true);

            if (itemBox.ShowsRaidBoxPanelOnOpen)
            {
                session.SendRdiPacket(itemBoxItem.ItemGeneratedVNum, itemBoxItem.ItemGeneratedAmount);
            }
        }

        await session.RemoveItemFromInventory(item: item);
        await session.EmitEventAsync(new BoxOpenedEvent
        {
            Box = e.Item.ItemInstance,
            Rewards = obtainedItems
        });
    }

    private IReadOnlyCollection<ItemBoxItemDto> GetRandomRewards(IClientSession session, ItemBoxDto box)
    {
        var rewards = new List<ItemBoxItemDto>();

        int minimumRoll = box.MinimumRewards ?? 1;
        int maximumRoll = box.MaximumRewards ?? minimumRoll;
        int rolls = _random.RandomNumber(minimumRoll, maximumRoll + 1);

        // We group them by category (probability)
        var possibleRewards = new Dictionary<short, List<ItemBoxItemDto>>();
        foreach (ItemBoxItemDto item in box.Items)
        {
            if (!possibleRewards.ContainsKey(item.Probability))
            {
                possibleRewards[item.Probability] = new List<ItemBoxItemDto>();
            }

            possibleRewards[item.Probability].Add(item);
        }

        var randomBag = new RandomBag<List<ItemBoxItemDto>>(_random);
        foreach ((short categoryChance, List<ItemBoxItemDto> items) in possibleRewards)
        {
            randomBag.AddEntry(items, categoryChance);
        }

        for (int i = 0; i < rolls; i++)
        {
            List<ItemBoxItemDto> randomCategory = randomBag.GetRandom();
            ItemBoxItemDto rolledItem = randomCategory.ElementAt(_random.RandomNumber(randomCategory.Count));
            rewards.Add(rolledItem);
        }

        return rewards;
    }
}