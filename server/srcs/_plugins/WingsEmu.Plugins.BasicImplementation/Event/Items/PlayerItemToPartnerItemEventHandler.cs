using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsEmu.DTOs.Items;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Inventory.Event;
using WingsEmu.Game.Items;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.Event.Items;

public class PlayerItemToPartnerItemEventHandler : IAsyncEventProcessor<PlayerItemToPartnerItemEvent>
{
    private static readonly int[] _partnerItems =
    {
        (int)ItemVnums.PARTNER_WEAPON_MELEE, (int)ItemVnums.PARTNER_WEAPON_RANGED, (int)ItemVnums.PARTNER_WEAPON_MAGIC, (int)ItemVnums.PARTNER_ARMOR_MAGIC, (int)ItemVnums.PARTNER_ARMOR_RANGED,
        (int)ItemVnums.PARTNER_ARMOR_MELEE
    };

    private readonly IGameLanguageService _languageService;

    public PlayerItemToPartnerItemEventHandler(IGameLanguageService languageService) => _languageService = languageService;

    public async Task HandleAsync(PlayerItemToPartnerItemEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;

        InventoryItem item = session.PlayerEntity.GetItemBySlotAndType(e.Slot, e.InventoryType);
        if (item == null)
        {
            return;
        }

        if (item.ItemInstance.Type != ItemInstanceType.WearableInstance)
        {
            return;
        }

        GameItemInstance itemToTransform = item.ItemInstance;

        const ItemVnums donaVNum = ItemVnums.DONA_RIVER_SAND;
        int price = 300 * itemToTransform.GameItem.LevelMinimum + 2000; // Formula by friends111 :peepoLove:

        if (itemToTransform.GameItem.EquipmentSlot != EquipmentType.Armor
            && itemToTransform.GameItem.EquipmentSlot != EquipmentType.MainWeapon
            && itemToTransform.GameItem.EquipmentSlot != EquipmentType.SecondaryWeapon)
        {
            await session.NotifyStrangeBehavior(StrangeBehaviorSeverity.NORMAL, "Tried to transform a PlayerItem to a PartnerItem, and that PlayerItem is not the 'transformable' type" +
                "(in theory there is a Client-side check for that)");
            return;
        }

        if (_partnerItems.Contains(itemToTransform.ItemVNum))
        {
            await session.NotifyStrangeBehavior(StrangeBehaviorSeverity.NORMAL, "Tried to transform a PartnerItem to a PartnerItem (not possible with normal client)");
            return;
        }

        if (session.PlayerEntity.Gold < price)
        {
            await session.NotifyStrangeBehavior(StrangeBehaviorSeverity.ABUSING, "Seems like the balance the client thinks he has is superior to what the server has. (not sufficient" +
                " gold to pay the PlayerItem to PartnerItem transformation)");
            return;
        }

        if (!session.PlayerEntity.HasItem((short)donaVNum, itemToTransform.GameItem.LevelMinimum))
        {
            await session.NotifyStrangeBehavior(StrangeBehaviorSeverity.ABUSING, "Tried to transform a PlayerItem to a PartnerItem without sufficient amount of" +
                $" 'Dona River Sand' ({((short)donaVNum).ToString()}), and the Client-side check should have prevented that.");
            return;
        }

        if (itemToTransform.EquipmentOptions != null && itemToTransform.EquipmentOptions.Any())
        {
            session.SendChatMessage(_languageService.GetLanguage(GameDialogKey.ITEM_CHATMESSAGE_CANNOT_BE_WITH_SHELL, session.UserLanguage), ChatMessageColorType.Red);
            session.SendShopEndPacket(ShopEndType.Npc);
            return;
        }

        GameItemInstance newItem = itemToTransform;
        switch (itemToTransform.GameItem.EquipmentSlot)
        {
            case EquipmentType.Armor:
                switch (itemToTransform.GameItem.Class)
                {
                    case (int)ItemClassType.Swordsman:
                    case (int)ItemClassType.MartialArtist:
                        newItem.ItemVNum = (int)ItemVnums.PARTNER_ARMOR_MELEE;
                        break;
                    case (int)ItemClassType.Archer:
                        newItem.ItemVNum = (int)ItemVnums.PARTNER_ARMOR_RANGED;
                        break;
                    case (int)ItemClassType.Mage:
                        newItem.ItemVNum = (int)ItemVnums.PARTNER_ARMOR_MAGIC;
                        break;
                    default:
                        session.SendShopEndPacket(ShopEndType.Npc);
                        return;
                }

                break;
            case EquipmentType.SecondaryWeapon:
            case EquipmentType.MainWeapon:
                switch (itemToTransform.GameItem.Class)
                {
                    case (int)ItemClassType.Swordsman:
                        newItem.ItemVNum = itemToTransform.GameItem.EquipmentSlot == (int)EquipmentType.MainWeapon ? (int)ItemVnums.PARTNER_WEAPON_MELEE : (int)ItemVnums.PARTNER_WEAPON_RANGED;
                        break;
                    case (int)ItemClassType.Archer:
                        newItem.ItemVNum = itemToTransform.GameItem.EquipmentSlot == (int)EquipmentType.MainWeapon ? (int)ItemVnums.PARTNER_WEAPON_RANGED : (int)ItemVnums.PARTNER_WEAPON_MELEE;
                        break;
                    case (int)ItemClassType.Mage:
                        newItem.ItemVNum = itemToTransform.GameItem.EquipmentSlot == (int)EquipmentType.MainWeapon ? (int)ItemVnums.PARTNER_WEAPON_MAGIC : (int)ItemVnums.PARTNER_WEAPON_RANGED;
                        break;
                    case (int)ItemClassType.MartialArtist:
                        newItem.ItemVNum = (int)ItemVnums.PARTNER_WEAPON_MELEE;
                        break;
                    default:
                        session.SendShopEndPacket(ShopEndType.Npc);
                        return;
                }

                break;
            default:
                session.SendShopEndPacket(ShopEndType.Npc);
                return;
        }

        if (newItem.Type == ItemInstanceType.WearableInstance)
        {
            newItem.EquipmentOptions?.Clear();
            newItem.OriginalItemVnum = item.ItemInstance.GameItem.Id;
            newItem.BoundCharacterId = null;
        }

        await session.RemoveItemFromInventory((short)donaVNum, itemToTransform.GameItem.LevelMinimum);
        session.PlayerEntity.Gold -= price;
        InventoryItem getItem = session.PlayerEntity.GetItemBySlotAndType(e.Slot, e.InventoryType);
        await session.RemoveItemFromInventory(item: getItem);
        await session.AddNewItemToInventory(newItem);
        session.RefreshGold();
        session.SendShopEndPacket(ShopEndType.Npc);
        session.SendMsg(_languageService.GetLanguage(GameDialogKey.INFORMATION_SHOUTMESSAGE_PARTNER_ITEM_DONE, session.UserLanguage), MsgMessageType.Middle);
    }
}