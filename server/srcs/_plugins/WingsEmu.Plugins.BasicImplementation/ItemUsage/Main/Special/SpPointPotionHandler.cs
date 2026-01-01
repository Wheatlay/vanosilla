using System.Threading.Tasks;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsEmu.Game._i18n;
using WingsEmu.Game._ItemUsage;
using WingsEmu.Game._ItemUsage.Event;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.ItemUsage.Main.Special;

public class SpPointPotionHandler : IItemHandler
{
    private readonly IGameLanguageService _languageService;
    private readonly IServerManager _serverManager;

    public SpPointPotionHandler(IGameLanguageService languageService, IServerManager serverManager)
    {
        _languageService = languageService;
        _serverManager = serverManager;
    }

    public ItemType ItemType => ItemType.Special;
    public long[] Effects => new long[] { 150, 152 };

    public async Task HandleAsync(IClientSession session, InventoryUseItemEvent e)
    {
        if (session.PlayerEntity.SpPointsBonus == _serverManager.MaxAdditionalSpPoints)
        {
            session.SendChatMessage(_languageService.GetLanguage(GameDialogKey.ITEM_MESSAGE_CANT_USE, session.UserLanguage), ChatMessageColorType.Yellow);
            return;
        }

        int points = e.Item.ItemInstance.GameItem.Data[2];
        session.PlayerEntity.SpPointsBonus += points;
        if (session.PlayerEntity.SpPointsBonus > _serverManager.MaxAdditionalSpPoints)
        {
            session.PlayerEntity.SpPointsBonus = _serverManager.MaxAdditionalSpPoints;
        }

        session.SendMsg(_languageService.GetLanguageFormat(GameDialogKey.SPECIALIST_SHOUTMESSAGE_POINTS_ADDED, session.UserLanguage, points), MsgMessageType.Middle);
        session.RefreshSpPoint();
        await session.RemoveItemFromInventory(item: e.Item);
    }
}