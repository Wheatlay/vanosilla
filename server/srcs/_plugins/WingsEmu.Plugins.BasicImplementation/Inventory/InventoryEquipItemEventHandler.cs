using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.DTOs.Items;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Algorithm;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Inventory.Event;
using WingsEmu.Game.Items;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.Inventory;

public class InventoryEquipItemEventHandler : IAsyncEventProcessor<InventoryEquipItemEvent>
{
    private readonly HashSet<EquipmentType> _bindItems = new()
    {
        EquipmentType.CostumeHat,
        EquipmentType.CostumeSuit,
        EquipmentType.WeaponSkin
    };

    private readonly ICharacterAlgorithm _characterAlgorithm;

    private readonly IGameLanguageService _gameLanguage;

    public InventoryEquipItemEventHandler(IGameLanguageService gameLanguage, ICharacterAlgorithm characterAlgorithm)
    {
        _gameLanguage = gameLanguage;
        _characterAlgorithm = characterAlgorithm;
    }

    public async Task HandleAsync(InventoryEquipItemEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;
        bool isSpecialType = e.IsSpecialType;
        InventoryType? inventoryType = e.InventoryType;
        bool bindItem = e.BoundItem;

        if (session.PlayerEntity.IsInExchange() || !session.HasCurrentMapInstance)
        {
            return;
        }

        if (session.PlayerEntity.HasShopOpened || session.PlayerEntity.ShopComponent.Items != null)
        {
            session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.ITEM_MESSAGE_CANT_USE, session.UserLanguage), ChatMessageColorType.Yellow);
            return;
        }

        InventoryItem inv;
        if (isSpecialType && inventoryType.HasValue)
        {
            inv = session.PlayerEntity.GetItemBySlotAndType(e.Slot, inventoryType.Value);
        }
        else
        {
            inv = session.PlayerEntity.GetItemBySlotAndType(e.Slot, InventoryType.Equipment);
        }

        if (inv == null)
        {
            return;
        }

        if (inv.ItemInstance.Type != ItemInstanceType.WearableInstance && inv.ItemInstance.Type != ItemInstanceType.SpecialistInstance)
        {
            return;
        }

        GameItemInstance item = inv.ItemInstance;
        EquipmentType equipmentType = item.GameItem.EquipmentSlot;
        ItemType itemType = item.GameItem.ItemType;

        if (equipmentType == EquipmentType.Sp)
        {
            if (item.GameItem.IsPartnerSpecialist)
            {
                return;
            }

            if (session.PlayerEntity.UseSp)
            {
                return;
            }

            if (item.Rarity == -2)
            {
                session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.ITEM_SHOUTMESSAGE_CANT_WEAR_SP_DESTROYED, session.UserLanguage), MsgMessageType.Middle);
                return;
            }
        }

        if (itemType != ItemType.Weapon && itemType != ItemType.Armor && itemType != ItemType.Fashion && itemType != ItemType.Jewelry && itemType != ItemType.Specialist)
        {
            session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.ITEM_MESSAGE_CANT_WEAR, session.UserLanguage), ChatMessageColorType.Yellow);
            return;
        }

        if (item.GameItem.LevelMinimum > (item.GameItem.IsHeroic ? session.PlayerEntity.HeroLevel : session.PlayerEntity.Level) ||
            item.GameItem.Sex != 0 && item.GameItem.Sex != ((byte)session.PlayerEntity.Gender + 1))
        {
            session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.ITEM_MESSAGE_CANT_WEAR, session.UserLanguage), ChatMessageColorType.Yellow);
            return;
        }

        if (itemType != ItemType.Jewelry && equipmentType != EquipmentType.Boots && equipmentType != EquipmentType.Gloves && (item.GameItem.Class >> (byte)session.PlayerEntity.Class & 1) != 1)
        {
            session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.ITEM_MESSAGE_CANT_WEAR, session.UserLanguage), ChatMessageColorType.Yellow);
            return;
        }

        GameItemInstance specialist = session.PlayerEntity.Specialist;
        if (session.PlayerEntity.UseSp && specialist != null)
        {
            if (specialist.GameItem.Element != 0 && equipmentType == EquipmentType.Fairy && item.GameItem.Element != specialist.GameItem.Element && item.GameItem.Element != (byte)ElementType.All)
            {
                session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.ITEM_SHOUTMESSAGE_FAIRY_WRONG_ELEMENT, session.UserLanguage), MsgMessageType.Middle);
                return;
            }
        }

        if (itemType == ItemType.Weapon || itemType == ItemType.Armor)
        {
            if (item.BoundCharacterId.HasValue && item.BoundCharacterId.Value != session.PlayerEntity.Id)
            {
                session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.ITEM_MESSAGE_CANT_WEAR, session.UserLanguage), ChatMessageColorType.Yellow);
                return;
            }
        }

        if (session.PlayerEntity.UseSp && equipmentType == EquipmentType.Sp)
        {
            session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.SPECIALIST_CHATMESSAGE_SP_BLOCKED, session.UserLanguage), ChatMessageColorType.Yellow);
            return;
        }

        if (session.PlayerEntity.JobLevel < item.GameItem.LevelJobMinimum)
        {
            session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.INFORMATION_MESSAGE_LOW_JOB, session.UserLanguage), ChatMessageColorType.Yellow);
            return;
        }

        if (item.IsBound && bindItem)
        {
            return;
        }

        if (!item.IsBound && (item.GameItem.ItemValidTime != 0 && (_bindItems.Contains(item.GameItem.EquipmentSlot) || item.GameItem.ItemType == ItemType.Jewelry)
                || equipmentType == EquipmentType.Fairy && (item.GameItem.MaxElementRate == 70 || item.GameItem.MaxElementRate == 80)))
        {
            if (!bindItem)
            {
                session.SendQnaPacket($"wear {inv.Slot} 0 1", _gameLanguage.GetLanguage(GameDialogKey.ITEM_DIALOG_ASK_BIND, session.UserLanguage));
                return;
            }

            item.BoundCharacterId = session.PlayerEntity.Id;

            if (item.GameItem.ItemValidTime == -1)
            {
                item.ItemDeleteTime = null;
            }
            else if (item.GameItem.ItemValidTime > 0)
            {
                item.ItemDeleteTime = DateTime.UtcNow.AddSeconds(item.GameItem.ItemValidTime);
            }
        }

        bool buffAmulet = false;
        if ((item.ItemDeleteTime.HasValue || item.DurabilityPoint != 0) && !_bindItems.Contains(item.GameItem.EquipmentSlot))
        {
            session.SendAmuletBuffPacket(item);
            buffAmulet = true;
        }

        bool removeAmuletBuff = false;
        InventoryItem itemInEquipment = session.PlayerEntity.GetInventoryItemFromEquipmentSlot(equipmentType);
        if (itemInEquipment == null)
        {
            session.SendInventoryRemovePacket(inv);
            session.PlayerEntity.EquipItem(inv, equipmentType);
        }
        else
        {
            if ((itemInEquipment.ItemInstance.ItemDeleteTime.HasValue || itemInEquipment.ItemInstance.DurabilityPoint != 0)
                && !_bindItems.Contains(itemInEquipment.ItemInstance.GameItem.EquipmentSlot))
            {
                removeAmuletBuff = true;
            }

            session.PlayerEntity.TakeOffItem(equipmentType, inv.Slot, isSpecialType && inventoryType.HasValue ? inventoryType.Value : InventoryType.Equipment);
            session.PlayerEntity.EquipItem(inv, equipmentType);
            session.PlayerEntity.RefreshEquipmentValues(itemInEquipment.ItemInstance, true);
        }

        if (removeAmuletBuff && !buffAmulet)
        {
            session.SendEmptyAmuletBuffPacket();
        }

        session.PlayerEntity.RefreshEquipmentValues(item);
        session.RefreshStatChar();
        session.RefreshEquipment();
        if (itemInEquipment != null)
        {
            session.SendInventoryAddPacket(itemInEquipment);
        }

        session.BroadcastEq();
        session.SendCondPacket();
        session.RefreshStat();
        session.SendIncreaseRange();

        switch (equipmentType)
        {
            case EquipmentType.Fairy:
                session.BroadcastPairy();
                break;
            case EquipmentType.Amulet:
                session.BroadcastEffectInRange(EffectType.EquipAmulet);
                break;
        }
    }
}