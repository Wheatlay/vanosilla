using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsAPI.Game.Extensions.MinilandExtensions;
using WingsEmu.DTOs.Bonus;
using WingsEmu.DTOs.Items;
using WingsEmu.DTOs.Maps;
using WingsEmu.Game;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game._ItemUsage;
using WingsEmu.Game._ItemUsage.Event;
using WingsEmu.Game.Act4.Event;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Configurations.Miniland;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.ItemUsage.Main.Special;

public class GeneralItemsHandler : IItemHandler
{
    private readonly IBankReputationConfiguration _bankReputationConfiguration;
    private readonly IBuffFactory _buffFactory;
    private readonly IGameItemInstanceFactory _gameItemInstanceFactory;
    private readonly IGameLanguageService _gameLanguage;
    private readonly MinigameConfiguration _minigameConfiguration;
    private readonly IRandomGenerator _randomGenerator;
    private readonly IRankingManager _rankingManager;
    private readonly IReputationConfiguration _reputationConfiguration;
    private readonly ISessionManager _sessionManager;

    private readonly HashSet<ItemVnums> _tarots = new()
    {
        ItemVnums.SEALED_TAROT_FOOL,
        ItemVnums.SEALED_TAROT_MAGICIAN,
        ItemVnums.SEALED_TAROT_LOVERS,
        ItemVnums.SEALED_TAROT_HERMIT,
        ItemVnums.SEALED_TAROT_DEATH,
        ItemVnums.SEALED_TAROT_DEVIL,
        ItemVnums.SEALED_TAROT_TOWER,
        ItemVnums.SEALED_TAROT_STAR,
        ItemVnums.SEALED_TAROT_MOON,
        ItemVnums.SEALED_TAROT_SUN
    };

    public GeneralItemsHandler(IGameLanguageService gameLanguage, IBuffFactory buffFactory, IRandomGenerator randomGenerator,
        ISessionManager sessionManager, MinigameConfiguration minigameConfiguration, IGameItemInstanceFactory gameItemInstanceFactory, IReputationConfiguration reputationConfiguration,
        IBankReputationConfiguration bankReputationConfiguration, IRankingManager rankingManager)
    {
        _gameLanguage = gameLanguage;
        _buffFactory = buffFactory;
        _randomGenerator = randomGenerator;
        _sessionManager = sessionManager;
        _minigameConfiguration = minigameConfiguration;
        _gameItemInstanceFactory = gameItemInstanceFactory;
        _reputationConfiguration = reputationConfiguration;
        _bankReputationConfiguration = bankReputationConfiguration;
        _rankingManager = rankingManager;
    }

    public ItemType ItemType => ItemType.Special;
    public long[] Effects => new long[] { 0 };

    public async Task HandleAsync(IClientSession session, InventoryUseItemEvent e)
    {
        switch ((ItemVnums)e.Item.ItemInstance.ItemVNum)
        {
            case ItemVnums.SOULSTONE_BLESSING:
            case ItemVnums.SOULSTONE_BLESSING_LIMITED:
                Buff itemBuff = session.PlayerEntity.BuffComponent.GetBuff((short)BuffVnums.SOULSTONE_BLESSING);
                if (itemBuff != null)
                {
                    string buffName = _gameLanguage.GetLanguage(GameDataType.Card, itemBuff.Name, session.UserLanguage);
                    session.SendChatMessage(_gameLanguage.GetLanguageFormat(GameDialogKey.ITEM_CHATMESSAGE_CANT_USE_TWICE, session.UserLanguage, buffName), ChatMessageColorType.Yellow);
                    return;
                }

                if (!session.PlayerEntity.UseSp)
                {
                    session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.INFORMATION_SHOUTMESSAGE_NO_SPECIALIST_CARD, session.UserLanguage), MsgMessageType.Middle);
                    return;
                }

                await session.PlayerEntity.AddBuffAsync(_buffFactory.CreateOneHourBuff(session.PlayerEntity, (short)BuffVnums.SOULSTONE_BLESSING, BuffFlag.BIG_AND_KEEP_ON_LOGOUT));
                await session.RemoveItemFromInventory(item: e.Item);
                break;
            case ItemVnums.PRODUCTION_COUPON:
                if (session.PlayerEntity.MinilandPoint >= _minigameConfiguration.Configuration.MaxmimumMinigamePoints)
                {
                    session.SendInfo(_gameLanguage.GetLanguage(GameDialogKey.MINIGAME_INFO_POINTS_FULL, session.UserLanguage));
                    return;
                }

                if (!session.PlayerEntity.HasItem(_minigameConfiguration.Configuration.ProductionCouponVnum))
                {
                    return;
                }

                await session.RemoveItemFromInventory(_minigameConfiguration.Configuration.ProductionCouponVnum);
                session.AddMinigamePoints(_minigameConfiguration.Configuration.ProductionCouponPointsAmount, _minigameConfiguration);
                break;
            case ItemVnums.SCROLL_CHICKEN:
                session.SendWopenPacket(WindowType.CHICKEN_FREE_SCROLL);
                break;
            case ItemVnums.SCROLL_PYJAMA:
                session.SendWopenPacket(WindowType.PAJAMA_FREE_SCROLL);
                break;
            case ItemVnums.SCROLL_PIRATE:
                session.SendWopenPacket(WindowType.PIRATE_FREE_SCROLL);
                break;
            case ItemVnums.FAIRY_EXPERIENCE_POTION:
            case ItemVnums.FAIRY_EXPERIENCE_POTION_LIMITED:
                Buff buff = session.PlayerEntity.BuffComponent.GetBuff((short)BuffVnums.FAIRYXP_POTION);
                if (buff != null)
                {
                    string buffName = _gameLanguage.GetLanguage(GameDataType.Card, buff.Name, session.UserLanguage);
                    session.SendChatMessage(_gameLanguage.GetLanguageFormat(GameDialogKey.ITEM_CHATMESSAGE_CANT_USE_TWICE, session.UserLanguage, buffName), ChatMessageColorType.Yellow);
                    return;
                }

                await session.RemoveItemFromInventory(item: e.Item);
                session.PlayerEntity.AddBuffAsync(_buffFactory.CreateOneHourBuff(session.PlayerEntity, (short)BuffVnums.FAIRYXP_POTION, BuffFlag.BIG_AND_KEEP_ON_LOGOUT))
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                break;
            case ItemVnums.PERFUME:
                session.SendGuriPacket(18, 1);
                break;
            case ItemVnums.RAINBOW_PEARL:
                session.SendGuriPacket(18);
                break;
            case ItemVnums.MAGIC_ERASER:
                if (e.Packet == null)
                {
                    return;
                }

                if (e.Packet.Length < 9)
                {
                    // MODIFIED PACKET
                    return;
                }

                if (!short.TryParse(e.Packet[9], out short eqSlot) ||
                    !Enum.TryParse(e.Packet[8], out InventoryType eqType))
                {
                    return;
                }

                InventoryItem eq = session.PlayerEntity.GetItemBySlotAndType(eqSlot, eqType);
                if (eq == null)
                {
                    // PACKET MODIFIED
                    return;
                }

                if (eq.ItemInstance.Type != ItemInstanceType.WearableInstance)
                {
                    return;
                }

                GameItemInstance eqItem = eq.ItemInstance;

                if (eqItem.GameItem.ItemType != ItemType.Armor && eqItem.GameItem.ItemType != ItemType.Weapon)
                {
                    return;
                }

                if (eqItem.EquipmentOptions == null || !eqItem.EquipmentOptions.Any())
                {
                    session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.ITEM_MESSAGE_ERASER_NO_SHELL, session.UserLanguage), ChatMessageColorType.Red);
                    session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.ITEM_MESSAGE_ERASER_NO_SHELL, session.UserLanguage), MsgMessageType.Middle);
                    return;
                }

                eqItem.EquipmentOptions.Clear();
                eqItem.ShellRarity = null;
                await session.RemoveItemFromInventory(item: e.Item);
                session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.SHELLS_SHOUTMESSAGE_ERASED, session.UserLanguage), MsgMessageType.Middle);
                session.SendGuriPacket(17, 1);
                break;
            case ItemVnums.TAROT_CARD_GAME:
                var tarots = _tarots.ToList();
                for (int i = 0; i < 5; i++)
                {
                    int rndIndex = _randomGenerator.RandomNumber(tarots.Count);
                    int newItemVnum = (int)tarots[rndIndex];

                    GameItemInstance newItem = _gameItemInstanceFactory.CreateItem(newItemVnum);
                    await session.AddNewItemToInventory(newItem, sendGiftIsFull: true);
                    tarots.RemoveAt(rndIndex);
                }

                await session.RemoveItemFromInventory(item: e.Item);
                break;
            case ItemVnums.CUARRY_BANK_SAVINGS_BOOK:
                session.SendGbPacket(BankType.BankMoney, _reputationConfiguration, _bankReputationConfiguration, _rankingManager.TopReputation);
                session.SendSMemo(SmemoType.BankInfo, _gameLanguage.GetLanguageFormat(GameDialogKey.BANK_MESSAGE_BALANCE, session.UserLanguage, session.Account.BankMoney));
                break;
            case ItemVnums.ANGEL_BASE_FLAG:
            case ItemVnums.DEMON_BASE_FLAG:
                if (!session.CurrentMapInstance.HasMapFlag(MapFlags.ACT_4))
                {
                    return;
                }

                await session.EmitEventAsync(new Act4PutFlagEvent
                {
                    InventoryItem = e.Item
                });
                break;
            case ItemVnums.CUARRY_BANK_VIP_10:
            case ItemVnums.CUARRY_BANK_VIP_30:
                if (session.PlayerEntity.HaveStaticBonus(StaticBonusType.CuarryBankMedal))
                {
                    return;
                }

                DateTime dateEnd = DateTime.UtcNow.AddDays(e.Item.ItemInstance.GameItem.Id == (short)ItemVnums.CUARRY_BANK_VIP_10 ? 10 : 30);
                await session.EmitEventAsync(new AddStaticBonusEvent(new CharacterStaticBonusDto
                {
                    DateEnd = dateEnd,
                    ItemVnum = e.Item.ItemInstance.GameItem.Id,
                    StaticBonusType = StaticBonusType.CuarryBankMedal
                }));

                await session.RemoveItemFromInventory(item: e.Item);
                string name = _gameLanguage.GetLanguage(GameDataType.Item, e.Item.ItemInstance.GameItem.Name, session.UserLanguage);
                session.SendChatMessage(_gameLanguage.GetLanguageFormat(GameDialogKey.ITEM_CHATMESSAGE_EFFECT_ACTIVATED, session.UserLanguage, name), ChatMessageColorType.Green);
                break;
        }
    }
}