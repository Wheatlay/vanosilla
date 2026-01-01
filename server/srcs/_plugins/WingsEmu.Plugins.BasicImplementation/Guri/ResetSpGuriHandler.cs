using System.Threading.Tasks;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsAPI.Game.Extensions.ItemExtension.Item;
using WingsEmu.Game._enum;
using WingsEmu.Game._Guri;
using WingsEmu.Game._Guri.Event;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Algorithm;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.Guri;

public class ResetSpGuriHandler : IGuriHandler
{
    private readonly ICharacterAlgorithm _characterAlgorithm;

    private readonly IGameLanguageService _gameLanguageService;
    private readonly IItemsManager _itemsManager;
    private readonly ISessionManager _sessionManager;

    public ResetSpGuriHandler(IGameLanguageService gameLanguageService, ISessionManager sessionManager, ICharacterAlgorithm characterAlgorithm, IItemsManager itemsManager)
    {
        _gameLanguageService = gameLanguageService;
        _sessionManager = sessionManager;
        _characterAlgorithm = characterAlgorithm;
        _itemsManager = itemsManager;
    }

    public long GuriEffectId => 203;

    public async Task ExecuteAsync(IClientSession session, GuriEvent guriPacket)
    {
        if (guriPacket.Data != 0)
        {
            return;
        }

        if (session.PlayerEntity.IsSeal)
        {
            return;
        }

        bool hasPotion = session.PlayerEntity.HasItem((short)ItemVnums.RESET_SP_POINT);

        if (!hasPotion)
        {
            string itemName = _itemsManager.GetItem((short)ItemVnums.RESET_SP_POINT).GetItemName(_gameLanguageService, session.UserLanguage);
            session.SendChatMessage(_gameLanguageService.GetLanguageFormat(GameDialogKey.INVENTORY_SHOUTMESSAGE_NOT_ENOUGH_ITEMS, session.UserLanguage, 1, itemName), ChatMessageColorType.Yellow);
            return;
        }

        if (!session.PlayerEntity.UseSp)
        {
            session.SendChatMessage(_gameLanguageService.GetLanguage(GameDialogKey.SPECIALIST_CHATMESSAGE_TRANSFORMATION_NEEDED, session.UserLanguage), ChatMessageColorType.Yellow);
            return;
        }

        GameItemInstance specialistInstance = session.PlayerEntity.Specialist;
        if (specialistInstance == null || !session.PlayerEntity.UseSp)
        {
            await session.NotifyStrangeBehavior(StrangeBehaviorSeverity.NORMAL, "tried to reset SP points without SP.");
            return;
        }

        specialistInstance.SlDamage = 0;
        specialistInstance.SlDefence = 0;
        specialistInstance.SlElement = 0;
        specialistInstance.SlHP = 0;

        await session.RemoveItemFromInventory((short)ItemVnums.RESET_SP_POINT);
        session.SendCondPacket();
        session.SendSpecialistCardInfo(specialistInstance, _characterAlgorithm);
        session.RefreshLevel(_characterAlgorithm);
        session.RefreshStatChar();
        session.SendMsg(_gameLanguageService.GetLanguage(GameDialogKey.SPECIALIST_SHOUTMESSAGE_POINTS_RESET, session.UserLanguage), MsgMessageType.Middle);
    }
}