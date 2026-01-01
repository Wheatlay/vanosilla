using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.Warehouse;
using WingsEmu.DTOs.Bonus;
using WingsEmu.DTOs.Items;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Features;
using WingsEmu.Game.Items;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Warehouse;
using WingsEmu.Game.Warehouse.Events;

namespace WingsEmu.Plugins.BasicImplementations.Warehouse;

public class PartnerWarehouseAddItemEventHandler : IAsyncEventProcessor<PartnerWarehouseAddItemEvent>
{
    private readonly IGameFeatureToggleManager _gameFeatureToggleManager;
    private readonly IGameItemInstanceFactory _gameItemInstance;
    private readonly IGameLanguageService _gameLanguageService;

    public PartnerWarehouseAddItemEventHandler(IGameItemInstanceFactory gameItemInstance, IGameFeatureToggleManager gameFeatureToggleManager, IGameLanguageService gameLanguageService)
    {
        _gameItemInstance = gameItemInstance;
        _gameFeatureToggleManager = gameFeatureToggleManager;
        _gameLanguageService = gameLanguageService;
    }

    public async Task HandleAsync(PartnerWarehouseAddItemEvent e, CancellationToken cancellation)
    {
        bool disabled = await _gameFeatureToggleManager.IsDisabled(GameFeature.PartnerWarehouse);
        if (disabled)
        {
            e.Sender.SendInfo(_gameLanguageService.GetLanguage(GameDialogKey.GAME_FEATURE_DISABLED, e.Sender.UserLanguage));
            return;
        }

        IClientSession session = e.Sender;
        GameItemInstance itemInstance = e.ItemInstance;

        if (itemInstance == null)
        {
            return;
        }

        if (!session.PlayerEntity.HaveStaticBonus(StaticBonusType.PartnerBackpack))
        {
            return;
        }

        if (itemInstance.Amount > 999)
        {
            return;
        }

        IReadOnlyList<PartnerWarehouseItem> warehouseItems = session.PlayerEntity.PartnerWarehouseItems();
        if (itemInstance.Type == ItemInstanceType.NORMAL_ITEM)
        {
            // Find exactly the same item
            int vnum = itemInstance.GameItem.Id;
            PartnerWarehouseItem sameItem = warehouseItems.FirstOrDefault(x => x?.ItemInstance != null && x.ItemInstance.ItemVNum == vnum);
            if (sameItem == null)
            {
                byte? freeSlot = session.PlayerEntity.GetNextPartnerWarehouseSlot();
                if (!freeSlot.HasValue)
                {
                    return;
                }

                session.PlayerEntity.AddPartnerWarehouseItem(itemInstance, freeSlot.Value);
                PartnerWarehouseItem partnerWarehouseItem = session.PlayerEntity.GetPartnerWarehouseItem(freeSlot.Value);
                if (session.PlayerEntity.IsPartnerWarehouseOpen)
                {
                    session.SendAddPartnerWarehouseItem(partnerWarehouseItem);
                }

                return;
            }

            GameItemInstance anotherInstance = sameItem.ItemInstance;
            int amount = itemInstance.Amount;

            int total = amount + anotherInstance.Amount;
            if (total > 999)
            {
                amount = total - 999;

                // Try find in warehouse exactly the same item and add left amount to it
                foreach (PartnerWarehouseItem item in warehouseItems.Where(x => x?.ItemInstance.ItemVNum == vnum))
                {
                    if (item.ItemInstance == anotherInstance)
                    {
                        continue;
                    }

                    if (item.ItemInstance.Amount >= 999)
                    {
                        continue;
                    }

                    if (amount + item.ItemInstance.Amount > 999)
                    {
                        int left = 999 - item.ItemInstance.Amount;
                        amount -= left;
                        item.ItemInstance.Amount = 999;
                        continue;
                    }

                    item.ItemInstance.Amount += amount;
                    amount = 0;
                    break;
                }

                if (amount <= 0)
                {
                    anotherInstance.Amount = 999;
                    if (session.PlayerEntity.IsPartnerWarehouseOpen)
                    {
                        session.SendAddPartnerWarehouseItem(sameItem);
                    }

                    return;
                }

                // Find new slot and create new item
                byte? freeSlot = session.PlayerEntity.GetNextPartnerWarehouseSlot();
                if (!freeSlot.HasValue)
                {
                    // if partner warehouse is full give another item full stack
                    anotherInstance.Amount = 999;
                    if (session.PlayerEntity.IsPartnerWarehouseOpen)
                    {
                        session.SendAddPartnerWarehouseItem(sameItem);
                    }

                    return;
                }

                anotherInstance.Amount = 999;

                GameItemInstance newItem = _gameItemInstance.CreateItem(anotherInstance.ItemVNum, amount);
                session.PlayerEntity.AddPartnerWarehouseItem(newItem, freeSlot.Value);
                PartnerWarehouseItem partnerWarehouseItem = session.PlayerEntity.GetPartnerWarehouseItem(freeSlot.Value);
                if (session.PlayerEntity.IsPartnerWarehouseOpen)
                {
                    session.SendAddPartnerWarehouseItem(partnerWarehouseItem);
                }

                return;
            }

            anotherInstance.Amount += amount;
            if (session.PlayerEntity.IsPartnerWarehouseOpen)
            {
                session.SendAddPartnerWarehouseItem(sameItem);
            }

            return;
        }

        byte? freeItemSlot = session.PlayerEntity.GetNextPartnerWarehouseSlot();
        if (!freeItemSlot.HasValue)
        {
            return;
        }

        session.PlayerEntity.AddPartnerWarehouseItem(itemInstance, freeItemSlot.Value);
        PartnerWarehouseItem eqPartnerWarehouseItem = session.PlayerEntity.GetPartnerWarehouseItem(freeItemSlot.Value);
        if (session.PlayerEntity.IsPartnerWarehouseOpen)
        {
            session.SendAddPartnerWarehouseItem(eqPartnerWarehouseItem);
        }
    }
}