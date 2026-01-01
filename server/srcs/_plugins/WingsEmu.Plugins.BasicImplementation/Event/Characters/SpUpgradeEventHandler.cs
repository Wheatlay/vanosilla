using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.Families;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsAPI.Game.Extensions.ItemExtension.Item;
using WingsAPI.Packets.Enums;
using WingsEmu.DTOs.Items;
using WingsEmu.Game;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;
using WingsEmu.Packets.Enums.Families;

namespace WingsEmu.Plugins.BasicImplementations.Event.Characters;

public class SpUpgradeEventHandler : IAsyncEventProcessor<SpUpgradeEvent>
{
    private readonly IItemsManager _itemsManager;
    private readonly IGameLanguageService _languageService;
    private readonly IRandomGenerator _randomGenerator;
    private readonly SpUpgradeConfiguration _spConfiguration;
    private readonly SpPerfectEventHandler _spPerfectHandler;

    public SpUpgradeEventHandler(IRandomGenerator randomGenerator, SpUpgradeConfiguration spConfiguration, SpPerfectEventHandler spPerfectHandler, IGameLanguageService languageService,
        IItemsManager itemsManager)
    {
        _randomGenerator = randomGenerator;
        _spConfiguration = spConfiguration;
        _spPerfectHandler = spPerfectHandler;
        _languageService = languageService;
        _itemsManager = itemsManager;
    }

    public async Task HandleAsync(SpUpgradeEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;

        if (e.InventoryItem.ItemInstance.Type != ItemInstanceType.SpecialistInstance)
        {
            return;
        }

        GameItemInstance sp = e.InventoryItem.ItemInstance;

        if (sp.GameItem.IsPartnerSpecialist)
        {
            return;
        }

        if (sp.Rarity == -2)
        {
            return;
        }

        UpgradeConfiguration configuration = _spConfiguration.FirstOrDefault(upgradeConfiguration =>
            upgradeConfiguration.SpUpgradeRange.Minimum <= sp.Upgrade
            && sp.Upgrade < upgradeConfiguration.SpUpgradeRange.Maximum);

        if (configuration == null)
        {
            return;
        }

        if (e.IsFree)
        {
            await SpUpgrade(configuration, session, e.InventoryItem, sp, configuration.SpecialItemsNeeded, true, true);
            switch (sp.ItemVNum)
            {
                case (short)ItemVnums.CHICKEN_SP:
                    await session.RemoveItemFromInventory((short)ItemVnums.SCROLL_CHICKEN);
                    break;
                case (short)ItemVnums.PYJAMA_SP:
                    await session.RemoveItemFromInventory((short)ItemVnums.SCROLL_PYJAMA);
                    break;
                case (short)ItemVnums.PIRATE_SP:
                    await session.RemoveItemFromInventory((short)ItemVnums.SCROLL_PIRATE);
                    break;
            }

            return;
        }

        if (sp.SpLevel < configuration.SpLevelNeeded)
        {
            session.SendMsg(_languageService.GetLanguageFormat(GameDialogKey.INFORMATION_SHOUTMESSAGE_SP_LVL_LOW, session.UserLanguage, configuration.SpLevelNeeded.ToString()), MsgMessageType.Middle);
            return;
        }

        if (session.PlayerEntity.Gold < configuration.GoldNeeded)
        {
            return;
        }

        if (!session.PlayerEntity.HasItem((int)ItemVnums.ANGEL_FEATHER, (short)configuration.FeatherNeeded))
        {
            string itemName = _itemsManager.GetItem((int)ItemVnums.ANGEL_FEATHER).GetItemName(_languageService, session.UserLanguage);
            session.SendMsg(_languageService.GetLanguageFormat(GameDialogKey.INVENTORY_SHOUTMESSAGE_NOT_ENOUGH_ITEMS, session.UserLanguage, configuration.FeatherNeeded, itemName),
                MsgMessageType.Middle);
            return;
        }

        if (!session.PlayerEntity.HasItem((int)ItemVnums.FULL_MOON_CRYSTAL, (short)configuration.FullMoonsNeeded))
        {
            string itemName = _itemsManager.GetItem((int)ItemVnums.FULL_MOON_CRYSTAL).GetItemName(_languageService, session.UserLanguage);
            session.SendMsg(_languageService.GetLanguageFormat(GameDialogKey.INVENTORY_SHOUTMESSAGE_NOT_ENOUGH_ITEMS, session.UserLanguage, configuration.FullMoonsNeeded, itemName),
                MsgMessageType.Middle);
            return;
        }

        var specialItems = new List<SpecialItem>();

        foreach (SpecialItem specialItem in configuration.SpecialItemsNeeded)
        {
            if (specialItem.SpVnums.Count > 0 && !specialItem.SpVnums.Contains(sp.ItemVNum))
            {
                continue;
            }

            if (!session.PlayerEntity.HasItem(specialItem.ItemVnum, (short)specialItem.Amount))
            {
                session.SendMsg(_languageService.GetLanguageFormat(GameDialogKey.INVENTORY_SHOUTMESSAGE_NOT_ENOUGH_ITEMS, session.UserLanguage, configuration.SpLevelNeeded.ToString()),
                    MsgMessageType.Middle);
                return;
            }

            specialItems.Add(specialItem);
        }

        bool isProtected = e.UpgradeProtection == UpgradeProtection.Protected;

        if (isProtected && !session.PlayerEntity.HasItem(configuration.ScrollVnum))
        {
            return;
        }

        session.PlayerEntity.RemoveGold(configuration.GoldNeeded);
        await session.RemoveItemFromInventory((int)ItemVnums.ANGEL_FEATHER, (short)configuration.FeatherNeeded);
        await session.RemoveItemFromInventory((int)ItemVnums.FULL_MOON_CRYSTAL, (short)configuration.FullMoonsNeeded);

        if (isProtected)
        {
            await session.RemoveItemFromInventory(configuration.ScrollVnum);
        }

        await SpUpgrade(configuration, session, e.InventoryItem, sp, specialItems, isProtected, false);
    }

    private async Task SpUpgrade(UpgradeConfiguration configuration, IClientSession session, InventoryItem spItem, ItemInstanceDTO sp, List<SpecialItem> specialItems, bool isProtected, bool isFree)
    {
        byte originalUpgrade = sp.Upgrade;

        var randomBag = new RandomBag<SpUpgradeResult>(_randomGenerator);

        randomBag.AddEntry(SpUpgradeResult.Succeed, configuration.SuccessChance);
        randomBag.AddEntry(SpUpgradeResult.Fail, 100 - configuration.SuccessChance - configuration.DestroyChance);
        if (configuration.DestroyChance > 0)
        {
            randomBag.AddEntry(SpUpgradeResult.Break, configuration.DestroyChance);
        }

        SpUpgradeResult upgradeResult = randomBag.GetRandom();

        switch (upgradeResult)
        {
            case SpUpgradeResult.Break when isProtected:
                session.SendEffect(EffectType.UpgradeFail);
                _spPerfectHandler.SendBothMessages(session, _languageService.GetLanguage(GameDialogKey.UPGRADE_MESSAGE_SP_FAILED_SAVED, session.UserLanguage), true);
                break;
            case SpUpgradeResult.Break:
                sp.Rarity = -2;
                session.SendInventoryAddPacket(spItem);
                session.SendShopEndPacket(ShopEndType.Npc);
                _spPerfectHandler.SendBothMessages(session, _languageService.GetLanguage(GameDialogKey.UPGRADE_MESSAGE_SP_DESTROYED, session.UserLanguage), true);
                await RemoveSpecialItems(session, specialItems);
                break;
            case SpUpgradeResult.Succeed:
            {
                session.SendEffect(EffectType.UpgradeSuccess);
                sp.Upgrade++;
                session.SendInventoryAddPacket(spItem);
                _spPerfectHandler.SendBothMessages(session, _languageService.GetLanguage(GameDialogKey.UPGRADE_MESSAGE_SP_SUCCESS, session.UserLanguage), false);

                if (sp.Upgrade > 7)
                {
                    await session.FamilyAddLogAsync(FamilyLogType.ItemUpgraded, session.PlayerEntity.Name, sp.ItemVNum.ToString(), sp.Upgrade.ToString());
                }

                await RemoveSpecialItems(session, specialItems);
                break;
            }

            case SpUpgradeResult.Fail:
            {
                _spPerfectHandler.SendBothMessages(session, _languageService.GetLanguage(GameDialogKey.UPGRADE_MESSAGE_SP_FAILED, session.UserLanguage), true);
                if (!isProtected)
                {
                    await RemoveSpecialItems(session, specialItems);
                }

                break;
            }
        }

        await session.EmitEventAsync(new SpUpgradedEvent
        {
            IsProtected = isProtected,
            Sp = sp,
            OriginalUpgrade = originalUpgrade,
            UpgradeMode = isFree ? UpgradeMode.Free : UpgradeMode.Normal,
            UpgradeResult = upgradeResult
        });

        if (isProtected)
        {
            session.SendShopEndPacket(ShopEndType.SpecialistHolder);
            return;
        }

        session.SendShopEndPacket(ShopEndType.Npc);
    }

    private async Task RemoveSpecialItems(IClientSession session, List<SpecialItem> specialItems)
    {
        foreach (SpecialItem specialItem in specialItems)
        {
            await session.RemoveItemFromInventory(specialItem.ItemVnum, (short)specialItem.Amount);
        }
    }
}