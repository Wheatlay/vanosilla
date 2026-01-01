using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Exchange;
using WingsEmu.Game.Exchange.Event;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.ClientPackets;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.PacketHandling.Game.Inventory;

public class ExcListPacketHandler : GenericGamePacketHandlerBase<ExcListPacket>
{
    private readonly IBankReputationConfiguration _bankReputationConfiguration;
    private readonly IGameLanguageService _language;
    private readonly IRankingManager _rankingManager;
    private readonly IReputationConfiguration _reputationConfiguration;
    private readonly ISessionManager _sessionManager;

    public ExcListPacketHandler(IGameLanguageService language, ISessionManager sessionManager, IReputationConfiguration reputationConfiguration,
        IBankReputationConfiguration bankReputationConfiguration, IRankingManager rankingManager)
    {
        _sessionManager = sessionManager;
        _reputationConfiguration = reputationConfiguration;
        _bankReputationConfiguration = bankReputationConfiguration;
        _rankingManager = rankingManager;
        _language = language;
    }

    protected override async Task HandlePacketAsync(IClientSession session, ExcListPacket packet)
    {
        if (packet == null)
        {
            return;
        }

        if (session.IsActionForbidden())
        {
            return;
        }

        if (!session.PlayerEntity.IsInExchange())
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(packet.PacketData))
        {
            return;
        }

        string[] packetSplit = packet.PacketData.Split(' ');
        if (packetSplit.Length < 2)
        {
            return;
        }

        if (!int.TryParse(packetSplit[0], out int gold))
        {
            return;
        }

        if (!long.TryParse(packetSplit[1], out long bankGold))
        {
            return;
        }

        if (bankGold < 0 || bankGold * 1000 > session.Account.BankMoney)
        {
            return;
        }

        if (gold < 0 || gold > session.PlayerEntity.Gold)
        {
            return;
        }

        PlayerExchange playerExchange = session.PlayerEntity.GetExchange();
        if (playerExchange == null)
        {
            return;
        }

        IClientSession exchangeTarget = _sessionManager.GetSessionByCharacterId(playerExchange.TargetId);
        if (exchangeTarget == null)
        {
            return;
        }

        if (playerExchange.RegisteredItems)
        {
            return;
        }

        if (bankGold != 0 && !session.HasEnoughGold(session.PlayerEntity.GetBankPenalty(_reputationConfiguration, _bankReputationConfiguration, _rankingManager.TopReputation) + gold))
        {
            await session.CloseExchange();
            session.SendMsg(_language.GetLanguage(GameDialogKey.INTERACTION_MESSAGE_NOT_ENOUGH_GOLD, session.UserLanguage), MsgMessageType.Middle);
            return;
        }

        if (session.PlayerEntity.HasShopOpened || exchangeTarget.PlayerEntity.HasShopOpened)
        {
            await session.CloseExchange();
            return;
        }

        string packetList = string.Empty;

        var itemList = new List<(InventoryItem, short)>();
        for (int i = 2, j = 0; i < packetSplit.Length && j < 10; i += 3, j++)
        {
            if (i + 2 > packetSplit.Length)
            {
                return;
            }

            if (!Enum.TryParse(packetSplit[i], out InventoryType inventoryType))
            {
                return;
            }

            if (!short.TryParse(packetSplit[i + 1], out short slot))
            {
                return;
            }

            if (!short.TryParse(packetSplit[i + 2], out short itemAmount))
            {
                return;
            }

            InventoryItem item = session.PlayerEntity.GetItemBySlotAndType(slot, inventoryType);
            if (item == null)
            {
                await session.CloseExchange();
                return;
            }

            if (itemAmount <= 0)
            {
                await session.CloseExchange();
                return;
            }

            if (itemAmount > item.ItemInstance.Amount)
            {
                await session.CloseExchange();
                return;
            }

            if (!item.ItemInstance.GameItem.IsTradable)
            {
                await session.CloseExchange();
                session.SendMsg(_language.GetLanguage(GameDialogKey.TRADE_SHOUTMESSAGE_ITEM_NOT_TRADABLE, session.UserLanguage), MsgMessageType.Middle);
                return;
            }

            if (item.ItemInstance.IsBound)
            {
                await session.CloseExchange();
                session.SendMsg(_language.GetLanguage(GameDialogKey.TRADE_SHOUTMESSAGE_ITEM_NOT_TRADABLE, session.UserLanguage), MsgMessageType.Middle);
                return;
            }

            itemList.Add((item, itemAmount));
            if (inventoryType != InventoryType.Equipment)
            {
                packetList += $"{j}.{(byte)inventoryType}.{item.ItemInstance.ItemVNum}.{itemAmount}.0 ";
            }
            else
            {
                packetList += $"{j}.{(byte)inventoryType}.{item.ItemInstance.ItemVNum}.{item.ItemInstance.Rarity}.{item.ItemInstance.Upgrade}.{item.ItemInstance.GetRunesCount()} ";
            }
        }

        await session.EmitEventAsync(new ExchangeRegisterEvent
        {
            InventoryItems = itemList,
            BankGold = bankGold * 1000,
            Gold = gold,
            Packets = packetList
        });
    }
}