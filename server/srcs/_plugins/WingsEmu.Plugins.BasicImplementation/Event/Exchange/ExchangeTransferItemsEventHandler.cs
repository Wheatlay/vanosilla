using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsEmu.DTOs.Items;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Exchange.Event;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.Event.Exchange;

public class ExchangeTransferItemsEventHandler : IAsyncEventProcessor<ExchangeTransferItemsEvent>
{
    private readonly IBankReputationConfiguration _bankReputationConfiguration;
    private readonly IGameItemInstanceFactory _gameItemInstance;
    private readonly IGameLanguageService _gameLanguage;
    private readonly IRankingManager _rankingManager;
    private readonly IReputationConfiguration _reputationConfiguration;
    private readonly IServerManager _serverManager;

    public ExchangeTransferItemsEventHandler(IServerManager serverManager, IGameLanguageService gameLanguage, IGameItemInstanceFactory gameItemInstance,
        IReputationConfiguration reputationConfiguration, IBankReputationConfiguration bankReputationConfiguration, IRankingManager rankingManager)
    {
        _serverManager = serverManager;
        _gameLanguage = gameLanguage;
        _gameItemInstance = gameItemInstance;
        _reputationConfiguration = reputationConfiguration;
        _bankReputationConfiguration = bankReputationConfiguration;
        _rankingManager = rankingManager;
    }

    public async Task HandleAsync(ExchangeTransferItemsEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;
        IClientSession target = e.Target;
        bool transformGold = true;

        long maxGold = _serverManager.MaxGold;
        long maxBankGold = _serverManager.MaxBankGold;

        int senderGold = e.SenderGold;
        long senderBankGold = e.SenderBankGold;

        int targetGold = e.TargetGold;
        long targetBankGold = e.TargetBankGold;

        if (senderGold + target.PlayerEntity.Gold > maxGold)
        {
            transformGold = false;
        }

        if (targetGold + session.PlayerEntity.Gold > maxGold)
        {
            transformGold = false;
        }

        if (senderBankGold + target.Account.BankMoney > maxBankGold)
        {
            transformGold = false;
        }

        if (targetBankGold + session.Account.BankMoney > maxBankGold)
        {
            transformGold = false;
        }

        if (senderBankGold != 0)
        {
            if (!session.HasEnoughGold(session.PlayerEntity.GetBankPenalty(_reputationConfiguration, _bankReputationConfiguration, _rankingManager.TopReputation)))
            {
                await session.CloseExchange();
                session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.INTERACTION_MESSAGE_NOT_ENOUGH_GOLD, session.UserLanguage), MsgMessageType.Middle);
                return;
            }
        }

        if (targetBankGold != 0)
        {
            if (!target.HasEnoughGold(target.PlayerEntity.GetBankPenalty(_reputationConfiguration, _bankReputationConfiguration, _rankingManager.TopReputation)))
            {
                await target.CloseExchange();
                target.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.INTERACTION_MESSAGE_NOT_ENOUGH_GOLD, target.UserLanguage), MsgMessageType.Middle);
                return;
            }
        }

        if (!transformGold)
        {
            await session.CloseExchange();
            session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.INFORMATION_MESSAGE_MAX_GOLD, session.UserLanguage), MsgMessageType.Middle);
            target.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.INFORMATION_MESSAGE_MAX_GOLD, target.UserLanguage), MsgMessageType.Middle);
            return;
        }

        List<(InventoryItem, short)> senderItems = e.SenderItems;
        List<(InventoryItem, short)> targetItems = e.TargetItems;

        bool targetCanReceiveSenderItems = CanReceive(target.PlayerEntity, senderItems);

        if (!targetCanReceiveSenderItems)
        {
            await session.CloseExchange();
            target.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.INTERACTION_MESSAGE_NOT_ENOUGH_PLACE, target.UserLanguage), MsgMessageType.Middle);
            return;
        }

        bool senderCanReceiveTargetItems = CanReceive(session.PlayerEntity, targetItems);

        if (!senderCanReceiveTargetItems)
        {
            await session.CloseExchange();
            session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.INTERACTION_MESSAGE_NOT_ENOUGH_PLACE, target.UserLanguage), MsgMessageType.Middle);
            return;
        }

        if (senderBankGold != 0)
        {
            session.PlayerEntity.Gold -= session.PlayerEntity.GetBankPenalty(_reputationConfiguration, _bankReputationConfiguration, _rankingManager.TopReputation);
        }

        if (targetBankGold != 0)
        {
            target.PlayerEntity.Gold -= target.PlayerEntity.GetBankPenalty(_reputationConfiguration, _bankReputationConfiguration, _rankingManager.TopReputation);
        }

        session.PlayerEntity.Gold -= senderGold;
        target.PlayerEntity.Gold += senderGold;

        target.PlayerEntity.Gold -= targetGold;
        session.PlayerEntity.Gold += targetGold;

        session.Account.BankMoney -= senderBankGold;
        target.Account.BankMoney += senderBankGold;

        target.Account.BankMoney -= targetBankGold;
        session.Account.BankMoney += targetBankGold;

        session.RefreshGold();
        target.RefreshGold();

        foreach ((InventoryItem inventoryItem, short amount) in senderItems)
        {
            bool asGift = false;
            if (inventoryItem.ItemInstance.Type != ItemInstanceType.NORMAL_ITEM)
            {
                GameItemInstance deepCopy = _gameItemInstance.DuplicateItem(inventoryItem.ItemInstance);
                if (deepCopy.Amount != 1)
                {
                    deepCopy.Amount = 1;
                }

                if (!target.PlayerEntity.HasSpaceFor(deepCopy.ItemVNum))
                {
                    asGift = true;
                }

                await target.AddNewItemToInventory(deepCopy, sendGiftIsFull: asGift);
            }
            else
            {
                GameItemInstance newItem = _gameItemInstance.CreateItem(inventoryItem.ItemInstance.ItemVNum, amount);

                if (!target.PlayerEntity.HasSpaceFor(newItem.ItemVNum, amount))
                {
                    asGift = true;
                }

                await target.AddNewItemToInventory(newItem, sendGiftIsFull: asGift);
            }

            await session.RemoveItemFromInventory(item: inventoryItem, amount: amount);
        }

        foreach ((InventoryItem inventoryItem, short amount) in targetItems)
        {
            bool asGift = false;
            if (inventoryItem.ItemInstance.Type != ItemInstanceType.NORMAL_ITEM)
            {
                GameItemInstance deepCopy = _gameItemInstance.DuplicateItem(inventoryItem.ItemInstance);
                if (deepCopy.Amount != 1)
                {
                    deepCopy.Amount = 1;
                }

                if (!session.PlayerEntity.HasSpaceFor(deepCopy.ItemVNum))
                {
                    asGift = true;
                }

                await session.AddNewItemToInventory(deepCopy, sendGiftIsFull: asGift);
            }
            else
            {
                GameItemInstance newItem = _gameItemInstance.CreateItem(inventoryItem.ItemInstance.ItemVNum, amount);

                if (!session.PlayerEntity.HasSpaceFor(newItem.ItemVNum, amount))
                {
                    asGift = true;
                }

                await session.AddNewItemToInventory(newItem, sendGiftIsFull: asGift);
            }

            await target.RemoveItemFromInventory(item: inventoryItem, amount: amount);
        }

        await session.EmitEventAsync(new ExchangeCompletedEvent
        {
            Target = target,
            SenderGold = senderGold,
            SenderBankGold = senderBankGold,
            SenderItems = senderItems.Select(s => (_gameItemInstance.CreateDto(s.Item1.ItemInstance), s.Item2)).ToList(),
            TargetItems = targetItems.Select(s => (_gameItemInstance.CreateDto(s.Item1.ItemInstance), s.Item2)).ToList(),
            TargetGold = targetGold,
            TargetBankGold = targetBankGold
        });

        await session.CloseExchange(ExcCloseType.Successful);
    }

    private bool CanReceive(IPlayerEntity playerEntity, List<(InventoryItem, short)> items)
    {
        var dictionary = new Dictionary<InventoryType, short>();
        int counter = 0;

        if (!items.Any())
        {
            return true;
        }

        foreach ((InventoryItem item, short amount) in items)
        {
            InventoryType type = item.ItemInstance.GameItem.Type;
            short slots = playerEntity.GetInventorySlots(false, type);

            for (short i = 0; i < slots; i++)
            {
                if (type != InventoryType.Etc && type != InventoryType.Main && type != InventoryType.Equipment)
                {
                    return false;
                }

                if (dictionary.TryGetValue(type, out short slot))
                {
                    if (i == slot)
                    {
                        continue;
                    }
                }

                InventoryItem freeSlot = playerEntity.GetItemBySlotAndType(i, type);
                if (freeSlot?.ItemInstance != null)
                {
                    continue;
                }

                counter++;
                dictionary[item.InventoryType] = i;
                break;
            }
        }

        return counter != 0 && counter == items.Count;
    }
}