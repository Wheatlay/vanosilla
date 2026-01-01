using System.Collections.Generic;
using System.Text;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Shops;
using WingsEmu.Packets.Enums;

namespace WingsAPI.Game.Extensions.ItemExtension
{
    public static class BuyExtension
    {
        public static string GenerateSellList(this IClientSession session, long gold, short slot, short amount, short sellAmount) => $"sell_list {gold} {slot}.{amount}.{sellAmount}";

        public static void SendSellList(this IClientSession session, long gold, short slot, short amount, short sellAmount) =>
            session.SendPacket(session.GenerateSellList(gold, slot, amount, sellAmount));

        public static void SendShopContent(this IClientSession receiverSession, long ownerCharacterId, IEnumerable<ShopPlayerItem> items)
        {
            var packetToSend = new StringBuilder($"n_inv 1 {ownerCharacterId.ToString()} 0 0");

            foreach (ShopPlayerItem item in items)
            {
                packetToSend.Append(item.GenerateShopContentSubPacket());
            }

            packetToSend.Append(
                " -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1 -1");

            receiverSession.SendPacket(packetToSend.ToString());
        }

        private static string GenerateShopContentSubPacket(this ShopPlayerItem shopPlayerItem)
        {
            if (shopPlayerItem == null)
            {
                return " -1";
            }

            if (shopPlayerItem.InventoryItem.InventoryType == InventoryType.Equipment)
            {
                return $" {((byte)shopPlayerItem.InventoryItem.InventoryType).ToString()}.{shopPlayerItem.ShopSlot.ToString()}.{shopPlayerItem.InventoryItem.ItemInstance.ItemVNum.ToString()}" +
                    $".{shopPlayerItem.InventoryItem.ItemInstance.Rarity.ToString()}.{shopPlayerItem.InventoryItem.ItemInstance.Upgrade.ToString()}.{shopPlayerItem.PricePerUnit.ToString()}" +
                    $".{shopPlayerItem.InventoryItem.ItemInstance.GetRunesCount().ToString()}.";
            }

            return $" {((byte)shopPlayerItem.InventoryItem.InventoryType).ToString()}.{shopPlayerItem.ShopSlot.ToString()}.{shopPlayerItem.InventoryItem.ItemInstance.ItemVNum.ToString()}" +
                $".{shopPlayerItem.SellAmount.ToString()}.{shopPlayerItem.PricePerUnit.ToString()}.-1.-1.-1";
        }
    }
}