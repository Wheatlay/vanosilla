using System.Threading.Tasks;
using PhoenixLib.Logging;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsEmu.DTOs.Items;
using WingsEmu.Game._enum;
using WingsEmu.Game._Guri;
using WingsEmu.Game._Guri.Event;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;
using WingsEmu.Plugins.BasicImplementations.Algorithms.Shells;

namespace WingsEmu.Plugins.BasicImplementations.Guri;

public class PerfumeGuriHandler : IGuriHandler
{
    private readonly IGameLanguageService _gameLanguageService;
    private readonly IItemsManager _itemsManager;

    private readonly IShellPerfumeConfiguration _perfumeConfiguration;

    public PerfumeGuriHandler(IShellPerfumeConfiguration perfumeConfiguration, IGameLanguageService gameLanguageService, IItemsManager itemsManager)
    {
        _perfumeConfiguration = perfumeConfiguration;
        _gameLanguageService = gameLanguageService;
        _itemsManager = itemsManager;
    }

    public long GuriEffectId => 205;

    public async Task ExecuteAsync(IClientSession session, GuriEvent guriPacket)
    {
        if (guriPacket.User == null)
        {
            return;
        }

        const int perfumeVnum = (short)ItemVnums.PERFUME;
        var perfumeInventoryType = (InventoryType)guriPacket.Data;
        InventoryItem eq = session.PlayerEntity.GetItemBySlotAndType((short)guriPacket.User.Value, perfumeInventoryType);

        if (eq == null)
        {
            return;
        }

        if (eq.ItemInstance.Type != ItemInstanceType.WearableInstance)
        {
            return;
        }

        GameItemInstance eqItem = eq.ItemInstance;
        if (!eqItem.BoundCharacterId.HasValue)
        {
            return;
        }

        // Item already yours
        if (eqItem.BoundCharacterId == session.PlayerEntity.Id)
        {
            return;
        }

        int? perfumesNeeded = _perfumeConfiguration.GetPerfumesByLevelAndRarity(eqItem.GameItem.LevelMinimum, (byte)eqItem.Rarity, eqItem.GameItem.IsHeroic);
        if (perfumesNeeded == null)
        {
            Log.Debug($"[ERROR] A valid perfume configuration for LV: {eqItem.GameItem.LevelMinimum}, Rarity: {eqItem.Rarity}, IsHeroic: {eqItem.GameItem.IsHeroic} was not found.");
            return;
        }

        if (!session.PlayerEntity.HasItem(perfumeVnum, (short)perfumesNeeded))
        {
            session.SendInfo(_gameLanguageService.GetLanguage(GameDialogKey.INVENTORY_SHOUTMESSAGE_NOT_ENOUGH_ITEMS, session.UserLanguage));
            return;
        }

        int? goldNeeded = _perfumeConfiguration.GetGoldByLevel(eqItem.GameItem.LevelMinimum, eqItem.GameItem.IsHeroic);
        if (goldNeeded == null)
        {
            Log.Debug($"[ERROR] A valid gold configuration for LV: {eqItem.GameItem.LevelMinimum}, IsHeroic: {eqItem.GameItem.IsHeroic} was not found.");
            return;
        }

        if (session.PlayerEntity.Gold < goldNeeded)
        {
            session.SendInfo(_gameLanguageService.GetLanguage(GameDialogKey.INTERACTION_MESSAGE_NOT_ENOUGH_GOLD, session.UserLanguage));
            return;
        }


        session.PlayerEntity.Gold -= (long)goldNeeded;
        session.RefreshGold();
        await session.RemoveItemFromInventory(perfumeVnum, (short)perfumesNeeded);
        eqItem.BoundCharacterId = session.PlayerEntity.Id;
    }
}