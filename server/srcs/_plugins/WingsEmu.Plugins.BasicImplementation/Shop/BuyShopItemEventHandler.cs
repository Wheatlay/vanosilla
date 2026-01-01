using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsEmu.DTOs.Shops;
using WingsEmu.Game;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Shops.Event;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.Shop;

public class BuyShopItemEventHandler : IAsyncEventProcessor<BuyShopItemEvent>
{
    private readonly IGameItemInstanceFactory _gameItemInstanceFactory;
    private readonly IGameLanguageService _gameLanguage;
    private readonly IItemsManager _itemsManager;
    private readonly IRandomGenerator _randomGenerator;
    private readonly IRankingManager _rankingManager;
    private readonly IReputationConfiguration _reputationConfiguration;

    public BuyShopItemEventHandler(IItemsManager itemsManager, IGameLanguageService gameLanguage, IRandomGenerator randomGenerator, IGameItemInstanceFactory gameItemInstanceFactory,
        IReputationConfiguration reputationConfiguration, IRankingManager rankingManager)
    {
        _itemsManager = itemsManager;
        _gameLanguage = gameLanguage;
        _randomGenerator = randomGenerator;
        _gameItemInstanceFactory = gameItemInstanceFactory;
        _reputationConfiguration = reputationConfiguration;
        _rankingManager = rankingManager;
    }

    public async Task HandleAsync(BuyShopItemEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;
        if (session.PlayerEntity.IsInExchange())
        {
            return;
        }

        short amount = e.Amount;
        short slot = e.Slot;
        long ownerId = e.OwnerId;

        INpcEntity npcEntity = session.CurrentMapInstance.GetNpcById(ownerId);
        ShopItemDTO item = npcEntity?.ShopNpc.ShopItems.FirstOrDefault(it => it.Slot == slot);

        if (item == null)
        {
            return;
        }

        switch (amount)
        {
            case <= 0:
            case > 999:
                return;
        }

        IGameItem gameItemInfo = _itemsManager.GetItem(item.ItemVNum);
        long price = (item.Price ?? gameItemInfo.Price) * amount;
        long reputationPrice = (item.Price ?? gameItemInfo.ReputPrice) * amount;
        double percent;
        switch (session.PlayerEntity.GetDignityIco())
        {
            case 3:
                percent = 1.10;
                break;

            case 4:
                percent = 1.20;
                break;

            case 5:
            case 6:
                percent = 1.5;
                break;

            default:
                percent = 1;
                break;
        }

        short rare = item.Rare;
        if (gameItemInfo.Type == InventoryType.Equipment)
        {
            amount = 1;
        }

        if (gameItemInfo.ReputPrice == 0)
        {
            if (price < 0)
            {
                session.SendSMemo(SmemoType.Error, _gameLanguage.GetLanguage(GameDialogKey.INTERACTION_MESSAGE_NOT_ENOUGH_GOLD, session.UserLanguage));
                return;
            }

            if (price * percent > session.PlayerEntity.Gold)
            {
                session.SendSMemo(SmemoType.Error, _gameLanguage.GetLanguage(GameDialogKey.INTERACTION_MESSAGE_NOT_ENOUGH_GOLD, session.UserLanguage));
                return;
            }
        }
        else
        {
            if (reputationPrice <= 0 || reputationPrice > session.PlayerEntity.Reput)
            {
                session.SendSMemo(SmemoType.Error, _gameLanguage.GetLanguage(GameDialogKey.INFORMATION_CHATMESSAGE_NOT_ENOUGH_REPUT, session.UserLanguage));
                return;
            }

            sbyte ra = (sbyte)_randomGenerator.RandomNumber();

            int[] rareProb = { 100, 100, 70, 50, 30, 15, 5, 1 };
            if (gameItemInfo.ReputPrice != 0)
            {
                for (int i = 0; i < rareProb.Length; i++)
                {
                    if (ra <= rareProb[i])
                    {
                        rare = (sbyte)i;
                    }
                }
            }
        }

        if (!session.PlayerEntity.HasSpaceFor(item.ItemVNum, amount))
        {
            session.SendSMemo(SmemoType.Error, _gameLanguage.GetLanguage(GameDialogKey.INTERACTION_MESSAGE_NOT_ENOUGH_PLACE, session.UserLanguage));
            return;
        }

        GameItemInstance newItem = _gameItemInstanceFactory.CreateItem(item.ItemVNum, amount, item.Upgrade, (sbyte)rare, item.Color);
        await session.AddNewItemToInventory(newItem);
        string itemName = _gameLanguage.GetLanguage(GameDataType.Item, gameItemInfo.Name, session.UserLanguage);

        session.SendSMemo(SmemoType.Balance, _gameLanguage.GetLanguageFormat(GameDialogKey.NPC_SHOP_LOG_PURCHASE, session.UserLanguage, itemName, amount));
        if (gameItemInfo.ReputPrice == 0)
        {
            long totalPrice = (long)(price * percent);
            session.PlayerEntity.Gold -= totalPrice;
            session.RefreshGold();
            await session.EmitEventAsync(new ShopNpcBoughtItemEvent
            {
                SellerId = ownerId,
                CurrencyType = CurrencyType.GOLD,
                TotalPrice = totalPrice,
                ItemInstance = newItem,
                Quantity = amount
            });
            return;
        }

        session.PlayerEntity.Reput -= reputationPrice;
        session.RefreshReputation(_reputationConfiguration, _rankingManager.TopReputation);
        session.SendChatMessage(_gameLanguage.GetLanguageFormat(GameDialogKey.INFORMATION_CHATMESSAGE_REPUT_DECREASE, session.UserLanguage, reputationPrice), ChatMessageColorType.Red);
        newItem.BoundCharacterId = session.PlayerEntity.Id;
        await session.EmitEventAsync(new ShopNpcBoughtItemEvent
        {
            SellerId = ownerId,
            CurrencyType = CurrencyType.REPUTATION,
            TotalPrice = reputationPrice,
            ItemInstance = newItem,
            Quantity = amount
        });
    }
}