using System;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using PhoenixLib.Logging;
using WingsAPI.Communication;
using WingsAPI.Communication.Bazaar;
using WingsAPI.Game.Extensions.Bazaar;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsAPI.Packets.Enums.Bazaar;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Bazaar;
using WingsEmu.Game.Bazaar.Configuration;
using WingsEmu.Game.Bazaar.Events;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Managers;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.Bazaar;

public class BazaarItemChangePriceEventHandler : IAsyncEventProcessor<BazaarItemChangePriceEvent>
{
    private readonly BazaarConfiguration _bazaarConfiguration;
    private readonly IBazaarManager _bazaarManager;
    private readonly IBazaarService _bazaarService;
    private readonly IGameLanguageService _languageService;
    private readonly IServerManager _serverManager;

    public BazaarItemChangePriceEventHandler(IBazaarManager bazaarManager, IBazaarService bazaarService, IServerManager serverManager, IGameLanguageService languageService,
        BazaarConfiguration bazaarConfiguration)
    {
        _bazaarManager = bazaarManager;
        _bazaarService = bazaarService;
        _serverManager = serverManager;
        _languageService = languageService;
        _bazaarConfiguration = bazaarConfiguration;
    }

    public async Task HandleAsync(BazaarItemChangePriceEvent e, CancellationToken cancellation)
    {
        await MainMethod(e);

        //TODO: Check official implementation
        await e.Sender.EmitEventAsync(new BazaarGetListedItemsEvent(0, BazaarListedItemType.All));
    }

    private async Task MainMethod(BazaarItemChangePriceEvent e)
    {
        BazaarItem cachedItem = await _bazaarManager.GetBazaarItemById(e.BazaarItemId);
        if (cachedItem == null)
        {
            return;
        }

        if (cachedItem.BazaarItemDto.CharacterId != e.Sender.PlayerEntity.Id)
        {
            await e.Sender.NotifyStrangeBehavior(StrangeBehaviorSeverity.ABUSING,
                $"Tried to change the price of an item that the character doesn't own. BazaarItemId: {cachedItem.BazaarItemDto.Id.ToString()}");
            return;
        }

        if (cachedItem.BazaarItemDto.SoldAmount > 0 || cachedItem.BazaarItemDto.GetBazaarItemStatus() != BazaarListedItemType.ForSale)
        {
            e.Sender.SendInfo(_languageService.GetLanguage(GameDialogKey.BAZAAR_INFO_ITEM_CHANGED, e.Sender.UserLanguage));
            return;
        }

        if (BazaarExtensions.PriceOrAmountExceeds(cachedItem.BazaarItemDto.UsedMedal, e.NewPricePerItem, cachedItem.BazaarItemDto.Amount))
        {
            e.Sender.SendInfo(_languageService.GetLanguage(GameDialogKey.BAZAAR_INFO_PRICE_EXCEEDS_LIMITS, e.Sender.UserLanguage));
            return;
        }

        long tax = cachedItem.BazaarItemDto.UsedMedal
            ? BazaarExtensions.MedalTax(e.NewPricePerItem * cachedItem.BazaarItemDto.Amount, cachedItem.BazaarItemDto.DayExpiryAmount)
            : BazaarExtensions.NormalTax(e.NewPricePerItem * cachedItem.BazaarItemDto.Amount);

        if (!e.Confirmed)
        {
            e.Sender.SendQnaPacket($"c_mod {e.BazaarItemId.ToString()} 0 0 {e.NewPricePerItem.ToString()} 1",
                _languageService.GetLanguageFormat(GameDialogKey.BAZAAR_QNA_CHANGE_PRICE_FEE, e.Sender.UserLanguage, tax.ToString()));
            return;
        }

        DateTime currentDate = DateTime.UtcNow;
        if (e.Sender.PlayerEntity.LastAdministrationBazaarRefresh > currentDate)
        {
            return;
        }

        e.Sender.PlayerEntity.LastAdministrationBazaarRefresh = currentDate.AddSeconds(_bazaarConfiguration.DelayServerBetweenRequestsInSecs);

        if (!e.Sender.HasEnoughGold(tax))
        {
            e.Sender.SendInfo(_languageService.GetLanguage(GameDialogKey.INTERACTION_MESSAGE_NOT_ENOUGH_GOLD, e.Sender.UserLanguage));
            return;
        }

        e.Sender.PlayerEntity.RemoveGold(tax);

        BazaarItemResponse response = null;
        try
        {
            response = await _bazaarService.ChangeItemPriceFromBazaar(new BazaarChangeItemPriceRequest
            {
                ChannelId = _serverManager.ChannelId,
                BazaarItemDto = cachedItem.BazaarItemDto,
                ChangerCharacterId = e.Sender.PlayerEntity.Id,
                NewPrice = e.NewPricePerItem,
                NewSaleFee = cachedItem.BazaarItemDto.UsedMedal ? 0 : (long)(e.NewPricePerItem * 0.05),
                SunkGold = tax
            });
        }
        catch (Exception ex)
        {
            Log.Error(nameof(BazaarItemChangePriceEventHandler), ex);
        }

        if (response?.ResponseType != RpcResponseType.SUCCESS)
        {
            e.Sender.SendInfo(
                response?.ResponseType == RpcResponseType.MAINTENANCE_MODE
                    ? _languageService.GetLanguage(GameDialogKey.BAZAAR_INFO_MAINTENANCE_MODE, e.Sender.UserLanguage)
                    : _languageService.GetLanguage(GameDialogKey.BAZAAR_INFO_RESYNC, e.Sender.UserLanguage));

            await e.Sender.EmitEventAsync(new GenerateGoldEvent(tax, sendMessage: false));
            return;
        }

        e.Sender.SendMsg(_languageService.GetLanguage(GameDialogKey.BAZAAR_SHOUTMESSAGE_ITEM_PRICE_CHANGED, e.Sender.UserLanguage), MsgMessageType.Middle);
    }
}