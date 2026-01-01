using PhoenixLib.MultiLanguage;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Items;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;

namespace WingsAPI.Game.Extensions.ItemExtension.Item
{
    public static class ItemExtension
    {
        public static bool IsTimeSpaceStone(this IGameItem item) => item.Data[0] == 900;

        public static bool IsTimeSpaceChest(this IGameItem gameItem) => gameItem.Data[0] == 4;

        public static string GetItemName(this IGameItem gameItem, IGameLanguageService gameLanguage, RegionLanguageType regionLanguageType)
            => gameLanguage.GetLanguage(GameDataType.Item, gameItem.Name, regionLanguageType);

        public static bool ShouldSendAmuletPacket(this IClientSession session, EquipmentType type) =>
            type != EquipmentType.CostumeHat && type != EquipmentType.CostumeSuit && type != EquipmentType.WeaponSkin;

        public static InventoryItem CreateInventoryItem(this IGameItemInstanceFactory instanceFactory, int vnum) => new()
        {
            ItemInstance = instanceFactory.CreateItem(vnum)
        };
    }
}