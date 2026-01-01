using System.Threading.Tasks;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsEmu.DTOs.Items;
using WingsEmu.Game._enum;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Items;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.ClientPackets;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.PacketHandling.Game.Inventory;

public class SpecialistHolderPacketHandler : GenericGamePacketHandlerBase<SpecialistHolderPacket>
{
    protected override async Task HandlePacketAsync(IClientSession session, SpecialistHolderPacket specialistHolderPacket)
    {
        InventoryItem specialist = session.PlayerEntity.GetItemBySlotAndType(specialistHolderPacket.Slot, InventoryType.Equipment);
        InventoryItem holder = session.PlayerEntity.GetItemBySlotAndType(specialistHolderPacket.HolderSlot, InventoryType.Equipment);
        if (specialist == null || holder == null)
        {
            return;
        }

        if (specialist.ItemInstance.Type != ItemInstanceType.SpecialistInstance)
        {
            return;
        }

        GameItemInstance spItem = specialist.ItemInstance;

        if (holder.ItemInstance.Type != ItemInstanceType.BoxInstance)
        {
            return;
        }

        GameItemInstance box = holder.ItemInstance;

        switch (specialistHolderPacket.HolderType)
        {
            case 0:
                if (box.ItemVNum != (short)ItemVnums.GOLDEN_SP_HOLDER)
                {
                    return;
                }

                if (spItem.GameItem.IsPartnerSpecialist)
                {
                    return;
                }

                box.HoldingVNum = spItem.ItemVNum;
                box.SlDamage = spItem.SlDamage;
                box.SlDefence = spItem.SlDefence;
                box.SlElement = spItem.SlElement;
                box.SlHP = spItem.SlHP;
                box.SpDamage = spItem.SpDamage;
                box.SpDark = spItem.SpDark;
                box.SpDefence = spItem.SpDefence;
                box.SpElement = spItem.SpElement;
                box.SpFire = spItem.SpFire;
                box.SpHP = spItem.SpHP;
                box.SpLevel = spItem.SpLevel;
                box.SpLight = spItem.SpLight;
                box.SpStoneUpgrade = spItem.SpStoneUpgrade;
                box.SpWater = spItem.SpWater;
                box.Upgrade = spItem.Upgrade;
                box.Xp = spItem.Xp;
                break;
            case 1:
                if (box.ItemVNum != (short)ItemVnums.PSP_HOLDER)
                {
                    return;
                }

                if (!spItem.GameItem.IsPartnerSpecialist)
                {
                    return;
                }

                box.HoldingVNum = spItem.ItemVNum;
                box.PartnerSkill1 = spItem.PartnerSkill1;
                box.PartnerSkill2 = spItem.PartnerSkill2;
                box.PartnerSkill3 = spItem.PartnerSkill3;
                box.SkillRank1 = spItem.SkillRank1;
                box.SkillRank2 = spItem.SkillRank2;
                box.SkillRank3 = spItem.SkillRank3;
                box.PartnerSkills = spItem.PartnerSkills;
                break;
        }

        session.SendShopEndPacket(ShopEndType.SpecialistHolder);
        await session.RemoveItemFromInventory(item: specialist);
    }
}