// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PhoenixLib.Logging;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsEmu.Game;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game._ItemUsage;
using WingsEmu.Game._ItemUsage.Event;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.ItemUsage.Etc;

public class CellaRefinerConfiguration : List<RefinerConfiguration>
{
}

public class RefinerConfiguration
{
    public int ItemVnum { get; set; }
    public short MinimumCella { get; set; }
    public short MaximumCella { get; set; }
    public short ChanceOfOptionalItem { get; set; }

    public List<RefinerItem> Items { get; set; }
}

public class RefinerItem
{
    public int Vnum { get; set; }
    public short Quantity { get; set; }
    public int Chance { get; set; }
}

public class RefinerItemHandler : IItemHandler
{
    private readonly CellaRefinerConfiguration _config;
    private readonly IGameItemInstanceFactory _gameItemInstanceFactory;
    private readonly IGameLanguageService _gameLanguage;
    private readonly IItemsManager _itemsManager;
    private readonly IRandomGenerator _randomGenerator;

    public RefinerItemHandler(IRandomGenerator randomGenerator, IGameLanguageService gameLanguage, IItemsManager itemsManager, IGameItemInstanceFactory gameItemInstanceFactory,
        CellaRefinerConfiguration config)
    {
        _randomGenerator = randomGenerator;
        _gameLanguage = gameLanguage;
        _itemsManager = itemsManager;
        _gameItemInstanceFactory = gameItemInstanceFactory;
        _config = config;
    }

    public ItemType ItemType => ItemType.Main;
    public long[] Effects => new long[] { 10 };

    public async Task HandleAsync(IClientSession session, InventoryUseItemEvent e)
    {
        if (!session.PlayerEntity.HasItem((short)ItemVnums.GILLION))
        {
            session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.ITEM_CHATMESSAGE_NO_GILLION, session.UserLanguage), ChatMessageColorType.Yellow);
            return;
        }

        RefinerConfiguration config = _config.FirstOrDefault(s => s.ItemVnum == e.Item.ItemInstance.ItemVNum);
        if (config == null)
        {
            Log.Error($"No configuration for Cella refiner: {e.Item.ItemInstance.ItemVNum}", new Exception($"No configuration for Cella refiner: {e.Item.ItemInstance.ItemVNum}"));
            return;
        }

        short amountOfCella = (short)_randomGenerator.RandomNumber(config.MinimumCella, config.MaximumCella);
        GameItemInstance cella = _gameItemInstanceFactory.CreateItem((short)ItemVnums.CELLA, amountOfCella);
        await session.AddNewItemToInventory(cella, true, ChatMessageColorType.Yellow, true);
        await session.RemoveItemFromInventory((short)ItemVnums.GILLION);
        await session.RemoveItemFromInventory(item: e.Item);


        if (_randomGenerator.RandomNumber() > config.ChanceOfOptionalItem)
        {
            return;
        }

        var randomBag = new RandomBag<RefinerItem>(_randomGenerator);

        foreach (RefinerItem item in config.Items)
        {
            randomBag.AddEntry(item, item.Chance);
        }

        RefinerItem optionalItem = randomBag.GetRandom();


        GameItemInstance newItem = _gameItemInstanceFactory.CreateItem(optionalItem.Vnum, optionalItem.Quantity);
        await session.AddNewItemToInventory(newItem, true, sendGiftIsFull: true);
    }
}