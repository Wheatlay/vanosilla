using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.ItemExtension;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsAPI.Game.Extensions.ItemExtension.Item;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Shops;
using WingsEmu.Game.Shops.Event;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.Shop;

public class ShopPlayerBuyItemEventHandler : IAsyncEventProcessor<ShopPlayerBuyItemEvent>
{
    private static readonly SemaphoreSlim SemaphoreSlim = new(1, 1);
    private readonly IGameItemInstanceFactory _gameItemInstanceFactory;
    private readonly IGameLanguageService _language;
    private readonly IServerManager _serverManager;
    private readonly ISessionManager _sessionManager;

    public ShopPlayerBuyItemEventHandler(IGameLanguageService language, ISessionManager sessionManager, IServerManager serverManager, IGameItemInstanceFactory gameItemInstanceFactory)
    {
        _language = language;
        _sessionManager = sessionManager;
        _serverManager = serverManager;
        _gameItemInstanceFactory = gameItemInstanceFactory;
    }

    public async Task HandleAsync(ShopPlayerBuyItemEvent e, CancellationToken cancellation)
    {
        await SemaphoreSlim.WaitAsync(cancellation);
        try
        {
            IClientSession session = e.Sender;
            short amount = e.Amount;
            short slot = e.Slot;
            long ownerId = e.OwnerId;

            if (session.IsActionForbidden())
            {
                return;
            }

            if (amount is < 1 or > 999 || ownerId == e.Sender.PlayerEntity.Id)
            {
                return;
            }

            IPlayerEntity owner = session.CurrentMapInstance.GetCharacterById(ownerId);
            IEnumerable<ShopPlayerItem> shop = owner?.ShopComponent.Items;
            if (shop == null)
            {
                return;
            }

            ShopPlayerItem playerItem = owner.ShopComponent.GetItem(slot);

            if (playerItem == null || playerItem.SellAmount < amount
                || playerItem.InventoryItem.InventoryType != InventoryType.Equipment && playerItem.InventoryItem.InventoryType != InventoryType.Etc &&
                playerItem.InventoryItem.InventoryType != InventoryType.Main)
            {
                return;
            }

            IClientSession shopOwner = owner.Session;

            long goldDifference = Math.Abs(playerItem.PricePerUnit * amount);

            if (goldDifference + shopOwner.PlayerEntity.Gold > _serverManager.MaxGold)
            {
                session.SendSMemo(SmemoType.Error, _language.GetLanguage(GameDialogKey.SHOP_INFO_TARGET_MAX_GOLD, session.UserLanguage));
                return;
            }

            if (!session.PlayerEntity.HasSpaceFor(playerItem.InventoryItem.ItemInstance.ItemVNum, amount))
            {
                session.SendSMemo(SmemoType.Error, _language.GetLanguage(GameDialogKey.INTERACTION_MESSAGE_NOT_ENOUGH_PLACE, session.UserLanguage));
                return;
            }

            if (!session.PlayerEntity.RemoveGold(goldDifference))
            {
                session.SendSMemo(SmemoType.Error, _language.GetLanguage(GameDialogKey.INTERACTION_MESSAGE_NOT_ENOUGH_GOLD, session.UserLanguage));
                return;
            }

            shopOwner.PlayerEntity.Gold += goldDifference;
            shopOwner.PlayerEntity.ShopComponent.Sell += goldDifference;
            shopOwner.RefreshGold();

            string itemNameOwner = playerItem.InventoryItem.ItemInstance.GameItem.GetItemName(_language, shopOwner.UserLanguage);
            string itemNameClient = playerItem.InventoryItem.ItemInstance.GameItem.GetItemName(_language, session.UserLanguage);


            shopOwner.SendSMemo(SmemoType.Balance,
                _language.GetLanguageFormat(GameDialogKey.PERSONAL_SHOP_LOG_PURCHASE_OWNER, shopOwner.UserLanguage, session.PlayerEntity.Name, itemNameOwner, amount));
            session.SendSMemo(SmemoType.Balance,
                _language.GetLanguageFormat(GameDialogKey.PERSONAL_SHOP_LOG_PURCHASE_BUYER, session.UserLanguage, itemNameClient, amount, shopOwner.PlayerEntity.Name));

            InventoryItem inventoryItem = playerItem.InventoryItem;
            if (playerItem.InventoryItem.InventoryType != InventoryType.Equipment)
            {
                GameItemInstance newItem = _gameItemInstanceFactory.CreateItem(inventoryItem.ItemInstance.ItemVNum, amount);
                await shopOwner.RemoveItemFromInventory(amount: amount, item: inventoryItem);
                await session.AddNewItemToInventory(newItem);
                playerItem.SellAmount -= amount;
            }
            else
            {
                await shopOwner.RemoveItemFromInventory(amount: amount, item: inventoryItem);
                await session.AddNewItemToInventory(inventoryItem.ItemInstance);
                playerItem.SellAmount = 0;
            }

            if (playerItem.SellAmount == 0)
            {
                shopOwner.PlayerEntity.ShopComponent.RemoveShopItem(playerItem);
            }

            shopOwner.SendSellList(shopOwner.PlayerEntity.ShopComponent.Sell, playerItem.ShopSlot, amount, playerItem.SellAmount);
            await session.EmitEventAsync(new ShopPlayerBoughtItemEvent
            {
                Quantity = amount,
                ItemInstance = inventoryItem.ItemInstance,
                TotalPrice = goldDifference,
                SellerId = shopOwner.PlayerEntity.Id,
                SellerName = shopOwner.PlayerEntity.Name
            });

            if (shopOwner.PlayerEntity.ShopComponent.Items.All(x => x == null))
            {
                await shopOwner.EmitEventAsync(new ShopPlayerCloseEvent());
                return;
            }

            session.SendShopContent(ownerId, shop);
        }
        finally
        {
            SemaphoreSlim.Release();
        }
    }
}