using System.Threading.Tasks;
using WingsEmu.DTOs.Items;
using WingsEmu.Game._i18n;
using WingsEmu.Game._ItemUsage;
using WingsEmu.Game._ItemUsage.Event;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Items;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.ItemUsage.Equipment.Box;

public class RaidBoxHandler : IItemHandler
{
    private readonly IGameLanguageService _languageService;

    public RaidBoxHandler(IGameLanguageService languageService) => _languageService = languageService;

    public ItemType ItemType => ItemType.Box;
    public long[] Effects => new long[] { 999 };

    public async Task HandleAsync(IClientSession session, InventoryUseItemEvent e)
    {
        InventoryItem box = session.PlayerEntity.GetItemBySlotAndType(e.Item.Slot, InventoryType.Equipment);
        if (box == null)
        {
            return;
        }

        if (box.ItemInstance.Type != ItemInstanceType.BoxInstance)
        {
            return;
        }

        GameItemInstance boxItem = box.ItemInstance;

        if (boxItem.GameItem.ItemSubType != 3)
        {
            return;
        }

        if (e.Option == 0)
        {
            session.SendQnaPacket($"guri 300 8023 {e.Item.Slot}", _languageService.GetLanguage(GameDialogKey.ITEM_DIALOG_ASK_OPEN_BOX, session.UserLanguage));
            return;
        }

        await session.EmitEventAsync(new RollItemBoxEvent
        {
            Item = box
        });
    }
}