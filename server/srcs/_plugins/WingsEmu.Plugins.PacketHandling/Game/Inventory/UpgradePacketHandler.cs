using System;
using System.Threading.Tasks;
using WingsEmu.DTOs.Items;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Inventory.Event;
using WingsEmu.Game.Items;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.ClientPackets;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;
using WingsEmu.Plugins.BasicImplementations.Event.Characters;

namespace WingsEmu.Plugins.PacketHandling.Game.Inventory;

public class UpgradePacketHandler : GenericGamePacketHandlerBase<UpgradePacket>
{
    private readonly IGameLanguageService _language;

    public UpgradePacketHandler(IGameLanguageService language) => _language = language;

    protected override async Task HandlePacketAsync(IClientSession session, UpgradePacket upgradePacket)
    {
        if (session.PlayerEntity.IsInExchange())
        {
            return;
        }

        if (session.PlayerEntity.HasShopOpened)
        {
            return;
        }

        if (session.PlayerEntity.LastItemUpgrade.AddSeconds(4) > DateTime.UtcNow)
        {
            session.PlayerEntity.BroadcastEndDancingGuriPacket();
            return;
        }

        InventoryType inventoryType = upgradePacket.InventoryType;
        if (!Enum.TryParse(upgradePacket.UpgradeType.ToString(), out UpgradePacketType upType))
        {
            session.PlayerEntity.BroadcastEndDancingGuriPacket();
            return;
        }

        byte slot = upgradePacket.Slot;
        InventoryItem inventory;
        InventoryItem specialist2 = session.PlayerEntity.GetItemBySlotAndType(slot, inventoryType);
        session.PlayerEntity.LastItemUpgrade = DateTime.UtcNow;
        switch (upType)
        {
            case UpgradePacketType.FREE_CHICKEN_UPGRADE:
                // chicken SP
                inventory = session.PlayerEntity.GetItemBySlotAndType(slot, inventoryType);

                if (inventory?.ItemInstance.ItemVNum != (short)ItemVnums.CHICKEN_SP)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (specialist2 == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (specialist2.ItemInstance.GameItem.EquipmentSlot != EquipmentType.Sp)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (specialist2.ItemInstance.Rarity == -2)
                {
                    session.SendMsg(_language.GetLanguage(GameDialogKey.ITEM_SHOUTMESSAGE_CANT_UPGRADE_DESTROYED_SP, session.UserLanguage), MsgMessageType.Middle);
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                await session.EmitEventAsync(new SpUpgradeEvent(UpgradeProtection.Protected, specialist2, true));
                break;
            case UpgradePacketType.FREE_PAJAMA_UPGRADE:
                // sp pyj
                inventory = session.PlayerEntity.GetItemBySlotAndType(slot, inventoryType);

                if (inventory?.ItemInstance.ItemVNum != (short)ItemVnums.PYJAMA_SP)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (specialist2 == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (specialist2.ItemInstance.GameItem.EquipmentSlot != EquipmentType.Sp)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (specialist2.ItemInstance.Rarity == -2)
                {
                    session.SendMsg(_language.GetLanguage(GameDialogKey.ITEM_SHOUTMESSAGE_CANT_UPGRADE_DESTROYED_SP, session.UserLanguage), MsgMessageType.Middle);
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                await session.EmitEventAsync(new SpUpgradeEvent(UpgradeProtection.Protected, specialist2, true));
                break;
            case UpgradePacketType.FREE_PIRATE_UPGRADFE:
                inventory = session.PlayerEntity.GetItemBySlotAndType(slot, inventoryType);

                if (inventory?.ItemInstance.ItemVNum != (short)ItemVnums.PIRATE_SP)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (specialist2 == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (specialist2.ItemInstance.GameItem.EquipmentSlot != EquipmentType.Sp)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (specialist2.ItemInstance.Rarity == -2)
                {
                    session.SendMsg(_language.GetLanguage(GameDialogKey.ITEM_SHOUTMESSAGE_CANT_UPGRADE_DESTROYED_SP, session.UserLanguage), MsgMessageType.Middle);
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                await session.EmitEventAsync(new SpUpgradeEvent(UpgradeProtection.Protected, specialist2, true));
                break;
            case UpgradePacketType.PLAYER_ITEM_TO_PARTNER:
                await session.EmitEventAsync(new PlayerItemToPartnerItemEvent(slot, inventoryType));
                break;
            case UpgradePacketType.ITEM_UPGRADE:
                inventory = session.PlayerEntity.GetItemBySlotAndType(slot, inventoryType);

                if (inventory == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (inventory.ItemInstance.GameItem.EquipmentSlot != EquipmentType.Armor && inventory.ItemInstance.GameItem.EquipmentSlot != EquipmentType.MainWeapon &&
                    inventory.ItemInstance.GameItem.EquipmentSlot != EquipmentType.SecondaryWeapon)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                GameItemInstance amulet1 = session.PlayerEntity.Amulet;
                FixedUpMode hasAmulet1 = amulet1?.GameItem.Effect == 793 ? FixedUpMode.HasAmulet : FixedUpMode.None;

                await session.EmitEventAsync(new UpgradeItemEvent
                {
                    Inv = inventory,
                    Mode = UpgradeMode.Normal,
                    Protection = UpgradeProtection.None,
                    HasAmulet = hasAmulet1
                });

                break;
            case UpgradePacketType.CELLON_UPGRADE:
                if (upgradePacket.InventoryType2 == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                inventory = session.PlayerEntity.GetItemBySlotAndType((byte)upgradePacket.InventoryType2.Value, upgradePacket.InventoryType);

                if (inventory == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (inventory.ItemInstance.GameItem.EquipmentSlot != EquipmentType.Necklace
                    && inventory.ItemInstance.GameItem.EquipmentSlot != EquipmentType.Ring
                    && inventory.ItemInstance.GameItem.EquipmentSlot != EquipmentType.Bracelet)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (upgradePacket.CellonSlot == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (upgradePacket.CellonInventoryType == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (upgradePacket.CellonInventoryType.Value != InventoryType.Main)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                InventoryItem cellon = session.PlayerEntity.GetItemBySlotAndType(upgradePacket.CellonSlot.Value, upgradePacket.CellonInventoryType.Value);
                if (cellon == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (cellon.ItemInstance.GameItem.Effect != 100)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (inventory.ItemInstance.Type != ItemInstanceType.WearableInstance)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                await session.EmitEventAsync(new CellonUpgradeEvent(cellon, inventory.ItemInstance));
                break;
            case UpgradePacketType.ITEM_RARITY:
                inventory = session.PlayerEntity.GetItemBySlotAndType(slot, inventoryType);

                if (inventory == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (inventory.ItemInstance.GameItem.EquipmentSlot != EquipmentType.Armor && inventory.ItemInstance.GameItem.EquipmentSlot != EquipmentType.MainWeapon &&
                    inventory.ItemInstance.GameItem.EquipmentSlot != EquipmentType.SecondaryWeapon)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                InventoryItem amulet7 = session.PlayerEntity.GetInventoryItemFromEquipmentSlot(EquipmentType.Amulet);

                await session.EmitRarifyEvent(inventory, amulet7);
                session.SendShopEndPacket(ShopEndType.Npc);
                break;
            case UpgradePacketType.ITEM_SUM:
                inventory = session.PlayerEntity.GetItemBySlotAndType(slot, inventoryType);
                if (upgradePacket.InventoryType2 == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (upgradePacket.Slot2 == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (inventory == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                InventoryItem inventory2 = session.PlayerEntity.GetItemBySlotAndType((byte)upgradePacket.Slot2, (InventoryType)upgradePacket.InventoryType2);
                if (inventory2 == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                await session.EmitEventAsync(new ItemSumEvent(inventory, inventory2));
                break;

            case UpgradePacketType.SP_UPGRADE:
                InventoryItem specialist = session.PlayerEntity.GetItemBySlotAndType(slot, inventoryType);

                if (specialist == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (specialist.ItemInstance.GameItem.EquipmentSlot != EquipmentType.Sp)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                await session.EmitEventAsync(new SpUpgradeEvent(UpgradeProtection.None, specialist));
                break;

            case UpgradePacketType.ITEM_UPGRADE_SCROLL:
                inventory = session.PlayerEntity.GetItemBySlotAndType(slot, inventoryType);

                if (inventory == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                GameItemInstance amulet9 = session.PlayerEntity.Amulet;
                FixedUpMode hasAmulet9 = amulet9?.GameItem.Effect == 793 ? FixedUpMode.HasAmulet : FixedUpMode.None;

                if (inventory.ItemInstance.GameItem.EquipmentSlot != EquipmentType.Armor
                    && inventory.ItemInstance.GameItem.EquipmentSlot != EquipmentType.MainWeapon
                    && inventory.ItemInstance.GameItem.EquipmentSlot != EquipmentType.SecondaryWeapon)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                await session.EmitEventAsync(new UpgradeItemEvent
                {
                    Inv = inventory,
                    Mode = UpgradeMode.Normal,
                    Protection = UpgradeProtection.Protected,
                    HasAmulet = hasAmulet9
                });
                break;

            case UpgradePacketType.ITEM_RARITY_SCROLL:
                inventory = session.PlayerEntity.GetItemBySlotAndType(slot, inventoryType);

                if (inventory == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (inventory.ItemInstance.GameItem.EquipmentSlot != EquipmentType.Armor
                    && inventory.ItemInstance.GameItem.EquipmentSlot != EquipmentType.MainWeapon
                    && inventory.ItemInstance.GameItem.EquipmentSlot != EquipmentType.SecondaryWeapon)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                await session.EmitRarifyEvent(inventory, null, isScroll: true);
                break;

            case UpgradePacketType.SP_UPGRADE_SCROLL_BLUE:
            case UpgradePacketType.SP_UPGRADE_SCROLL_RED:
                specialist = session.PlayerEntity.GetItemBySlotAndType(slot, inventoryType);

                if (specialist == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (specialist.ItemInstance.GameItem.EquipmentSlot != EquipmentType.Sp)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                await session.EmitEventAsync(new SpUpgradeEvent(UpgradeProtection.Protected, specialist));
                break;

            case UpgradePacketType.SP_PERFECTION:
                specialist = session.PlayerEntity.GetItemBySlotAndType(slot, inventoryType);
                if (specialist == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (specialist.ItemInstance.GameItem.EquipmentSlot != EquipmentType.Sp)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                if (specialist.ItemInstance.Rarity == -2)
                {
                    session.SendMsg(_language.GetLanguage(GameDialogKey.ITEM_SHOUTMESSAGE_CANT_UPGRADE_DESTROYED_SP, session.UserLanguage), MsgMessageType.Middle);
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                await session.EmitEventAsync(new SpPerfectEvent(specialist));
                break;

            case UpgradePacketType.ITEM_UPGRADE_GOLD_SCROLL:
                inventory = session.PlayerEntity.GetItemBySlotAndType(slot, inventoryType);
                if (inventory == null)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                GameItemInstance amulet43 = session.PlayerEntity.Amulet;
                FixedUpMode hasAmulet43 = amulet43?.GameItem.Effect == 793 ? FixedUpMode.HasAmulet : FixedUpMode.None;

                if (inventory.ItemInstance.GameItem.EquipmentSlot != EquipmentType.Armor
                    && inventory.ItemInstance.GameItem.EquipmentSlot != EquipmentType.MainWeapon
                    && inventory.ItemInstance.GameItem.EquipmentSlot != EquipmentType.SecondaryWeapon)
                {
                    session.PlayerEntity.BroadcastEndDancingGuriPacket();
                    return;
                }

                await session.EmitEventAsync(new UpgradeItemEvent
                {
                    Inv = inventory,
                    Mode = UpgradeMode.Reduced,
                    Protection = UpgradeProtection.Protected,
                    HasAmulet = hasAmulet43
                });
                break;
        }
    }
}