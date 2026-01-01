using System;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsAPI.Game.Extensions.PacketGeneration;
using WingsAPI.Packets.Enums;
using WingsEmu.DTOs.Items;
using WingsEmu.Game;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Items;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Networking.Broadcasting;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.Event.Items;

public class GamblingEventHandler : IAsyncEventProcessor<GamblingEvent>
{
    private readonly IGamblingRarityConfiguration _gamblingRarityConfiguration;
    private readonly GamblingRarityInfo _gamblingRarityInfo;
    private readonly IGameLanguageService _gameLanguage;
    private readonly IRandomGenerator _randomGenerator;

    public GamblingEventHandler(IGameLanguageService gameLanguage, IRandomGenerator randomGenerator, IGamblingRarityConfiguration gamblingRarityConfiguration, GamblingRarityInfo gamblingRarityInfo)
    {
        _gameLanguage = gameLanguage;
        _randomGenerator = randomGenerator;
        _gamblingRarityConfiguration = gamblingRarityConfiguration;
        _gamblingRarityInfo = gamblingRarityInfo;
    }

    public async Task HandleAsync(GamblingEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;
        RarifyProtection protection = e.Protection;

        if (!session.HasCurrentMapInstance || e.Item.ItemInstance.Type != ItemInstanceType.WearableInstance)
        {
            return;
        }


        GameItemInstance item = e.Item.ItemInstance;
        GameItemInstance amulet = e.Amulet?.ItemInstance;

        const int cellaVnum = (int)ItemVnums.CELLA;
        const int scrollVnum = (int)ItemVnums.EQ_NORMAL_SCROLL;

        short originalRarity = item.Rarity;
        switch (e.Mode)
        {
            case RarifyMode.Increase:
                if (item.Rarity >= 8)
                {
                    session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.GAMBLING_MESSAGE_ALREADY_MAX_RARE, session.UserLanguage), ChatMessageColorType.Yellow);
                    return;
                }

                if (IsChampion(amulet) && !item.GameItem.IsHeroic)
                {
                    session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.GAMBLING_CHATMESSAGE_ITEM_IS_NOT_HEROIC, session.UserLanguage), ChatMessageColorType.Yellow);
                    return;
                }

                if (!IsChampion(amulet) && item.GameItem.IsHeroic)
                {
                    session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.GAMBLING_MESSAGE_ITEM_IS_HEROIC, session.UserLanguage), ChatMessageColorType.Yellow);
                    return;
                }

                if (amulet == null)
                {
                    return;
                }

                item.Rarity += 1;
                item.SetRarityPoint(_randomGenerator);
                session.SendInventoryAddPacket(e.Item);
                session.NotifyRarifyResult(_gameLanguage, item.Rarity);
                await session.RemoveItemFromInventory(item: e.Amulet, isEquiped: true);
                session.RefreshEquipment();
                session.SendModal(_gameLanguage.GetLanguage(GameDialogKey.GAMBLING_INFO_AMULET_DESTROYED, session.UserLanguage), ModalType.Confirm);

                await session.EmitEventAsync(new ItemGambledEvent
                {
                    ItemVnum = item.ItemVNum,
                    Mode = e.Mode,
                    Protection = e.Protection,
                    Amulet = e.Amulet?.ItemInstance.ItemVNum,
                    Succeed = true,
                    OriginalRarity = originalRarity,
                    FinalRarity = item.Rarity
                });
                return;

            case RarifyMode.Normal:
                if (session.PlayerEntity.Gold < _gamblingRarityInfo.GoldPrice)
                {
                    return;
                }

                if (!session.PlayerEntity.HasItem(cellaVnum, _gamblingRarityInfo.CellaUsed))
                {
                    return;
                }


                switch (protection)
                {
                    case RarifyProtection.Scroll when !session.PlayerEntity.HasItem(scrollVnum):
                        return;
                    // Using normal amulet/scroll on heroic item
                    case RarifyProtection.Scroll or RarifyProtection.ProtectionAmulet or RarifyProtection.BlessingAmulet when item.GameItem.IsHeroic:
                        session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.GAMBLING_MESSAGE_ITEM_IS_HEROIC, session.UserLanguage), MsgMessageType.Middle);
                        return;
                    // Using heroic amulet on normal item
                    case RarifyProtection.HeroicAmulet or RarifyProtection.RandomHeroicAmulet when !item.GameItem.IsHeroic:
                        session.SendMsg(
                            _gameLanguage.GetLanguage(GameDialogKey.GAMBLING_CHATMESSAGE_ITEM_IS_NOT_HEROIC, session.UserLanguage), MsgMessageType.Middle);
                        return;
                }

                if (item.GameItem.IsHeroic && item.Rarity == 8)
                {
                    session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.GAMBLING_MESSAGE_ALREADY_MAX_RARE, session.UserLanguage), MsgMessageType.Middle);
                    return;
                }

                if (protection == RarifyProtection.Scroll)
                {
                    if (!session.PlayerEntity.HasItem(scrollVnum))
                    {
                        session.SendShopEndPacket(ShopEndType.SpecialistHolder);
                        return;
                    }

                    await session.RemoveItemFromInventory(scrollVnum);
                    session.SendShopEndPacket(ShopEndType.Player);
                }

                session.PlayerEntity.Gold -= _gamblingRarityInfo.GoldPrice;
                await session.RemoveItemFromInventory(cellaVnum, _gamblingRarityInfo.CellaUsed);
                session.RefreshGold();
                break;


            default:
                throw new ArgumentOutOfRangeException(nameof(e.Mode), e.Mode, "The selected RarifyMode is not handled");
        }

        if (GamblingSuccess(item, amulet))
        {
            short rarity = _gamblingRarityConfiguration.GetRandomRarity();
            if (protection == RarifyProtection.Scroll && rarity > item.Rarity || protection != RarifyProtection.Scroll)
            {
                session.NotifyRarifyResult(_gameLanguage, rarity);
                session.BroadcastEffectInRange(EffectType.UpgradeSuccess);
                item.Rarity = rarity;
            }

            else if (rarity <= item.Rarity)
            {
                session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.GAMBLING_MESSAGE_FAILED_ITEM_SAVED, session.UserLanguage), ChatMessageColorType.Red);
                session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.GAMBLING_MESSAGE_FAILED_ITEM_SAVED, session.UserLanguage), MsgMessageType.Middle);
                session.BroadcastEffectInRange(EffectType.UpgradeFail);

                await session.EmitEventAsync(new ItemGambledEvent
                {
                    ItemVnum = item.ItemVNum,
                    Mode = e.Mode,
                    Protection = e.Protection,
                    Amulet = e.Amulet?.ItemInstance.ItemVNum,
                    Succeed = false,
                    OriginalRarity = originalRarity,
                    FinalRarity = item.Rarity
                });
                return;
            }

            item.SetRarityPoint(_randomGenerator);
            session.SendInventoryAddPacket(e.Item);

            await session.EmitEventAsync(new ItemGambledEvent
            {
                ItemVnum = item.ItemVNum,
                Mode = e.Mode,
                Protection = e.Protection,
                Amulet = e.Amulet?.ItemInstance.ItemVNum,
                Succeed = true,
                OriginalRarity = originalRarity,
                FinalRarity = item.Rarity
            });
        }
        else
        {
            switch (protection)
            {
                case RarifyProtection.ProtectionAmulet:
                case RarifyProtection.BlessingAmulet:
                case RarifyProtection.HeroicAmulet:
                case RarifyProtection.RandomHeroicAmulet:
                    if (amulet == null)
                    {
                        return;
                    }

                    amulet.DurabilityPoint -= 1;
                    session.SendAmuletBuffPacket(amulet);
                    if (amulet.DurabilityPoint <= 0)
                    {
                        await session.RemoveItemFromInventory(item: e.Amulet, isEquiped: true);
                        session.RefreshEquipment();
                        session.SendModal(_gameLanguage.GetLanguage(GameDialogKey.GAMBLING_INFO_AMULET_DESTROYED, session.UserLanguage), ModalType.Confirm);
                    }

                    session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.GAMBLING_MESSAGE_AMULET_FAIL_SAVED, session.UserLanguage), ChatMessageColorType.Red);
                    session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.GAMBLING_MESSAGE_AMULET_FAIL_SAVED, session.UserLanguage), MsgMessageType.Middle);

                    await session.EmitEventAsync(new ItemGambledEvent
                    {
                        ItemVnum = item.ItemVNum,
                        Mode = e.Mode,
                        Protection = e.Protection,
                        Amulet = e.Amulet.ItemInstance.ItemVNum,
                        Succeed = false,
                        OriginalRarity = originalRarity,
                        FinalRarity = item.Rarity
                    });
                    return;

                case RarifyProtection.Scroll:
                    session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.GAMBLING_MESSAGE_FAILED_ITEM_SAVED, session.UserLanguage), ChatMessageColorType.Red);
                    session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.GAMBLING_MESSAGE_FAILED_ITEM_SAVED, session.UserLanguage), MsgMessageType.Middle);
                    session.BroadcastEffect(EffectType.UpgradeFail, new RangeBroadcast(session.PlayerEntity.PositionX, session.PlayerEntity.PositionY));

                    if (!session.PlayerEntity.HasItem(scrollVnum))
                    {
                        session.SendShopEndPacket(ShopEndType.SpecialistHolder);
                    }

                    await session.EmitEventAsync(new ItemGambledEvent
                    {
                        ItemVnum = item.ItemVNum,
                        Mode = e.Mode,
                        Protection = e.Protection,
                        Amulet = e.Amulet?.ItemInstance.ItemVNum,
                        Succeed = false,
                        OriginalRarity = originalRarity,
                        FinalRarity = item.Rarity
                    });
                    return;

                case RarifyProtection.None:
                    await session.RemoveItemFromInventory(item: e.Item);
                    session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.GAMBLING_MESSAGE_FAILED, session.UserLanguage), ChatMessageColorType.Red);
                    session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.GAMBLING_MESSAGE_FAILED, session.UserLanguage), MsgMessageType.Middle);

                    await session.EmitEventAsync(new ItemGambledEvent
                    {
                        ItemVnum = item.ItemVNum,
                        Mode = e.Mode,
                        Protection = e.Protection,
                        Amulet = e.Amulet?.ItemInstance.ItemVNum,
                        Succeed = false,
                        OriginalRarity = originalRarity
                    });
                    return;
            }
        }
    }

    private bool GamblingSuccess(GameItemInstance item, GameItemInstance amulet)
    {
        if (item.Rarity < 0)
        {
            return true;
        }

        RaritySuccess raritySuccess = _gamblingRarityConfiguration.GetRaritySuccess((byte)item.Rarity);
        if (raritySuccess == null)
        {
            return false;
        }

        int rnd = _randomGenerator.RandomNumber(10000);
        return rnd < (IsEnhanced(amulet) ? raritySuccess.SuccessChance + 1000 : raritySuccess.SuccessChance);
    }

    private bool IsChampion(GameItemInstance amulet) =>
        amulet.ItemVNum is (short)ItemVnums.CHAMPION_AMULET or (short)ItemVnums.CHAMPION_AMULET_RANDOM;

    private bool IsEnhanced(GameItemInstance amulet) =>
        amulet != null && amulet.ItemVNum is (short)ItemVnums.BLESSING_AMULET or (short)ItemVnums.BLESSING_AMULET_DOUBLE or (short)ItemVnums.CHAMPION_AMULET or (short)ItemVnums.CHAMPION_AMULET_RANDOM
            or (short)ItemVnums.PROTECTION_AMULET;
}