// WingsEmu
// 
// Developed by NosWings Team

using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game._ItemUsage;
using WingsEmu.Game._ItemUsage.Event;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Entities.Extensions;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Inventory;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.ItemUsage.Etc.Special;

public class FairyBoostHandler : IItemHandler
{
    private const int CARD_ID = 131;
    private readonly IBuffFactory _buffFactory;
    private readonly IAsyncEventPipeline _eventPipeline;
    private readonly IGameLanguageService _gameLanguage;

    public FairyBoostHandler(IGameLanguageService gameLanguage, IAsyncEventPipeline eventPipeline, IBuffFactory buffFactory)
    {
        _gameLanguage = gameLanguage;
        _eventPipeline = eventPipeline;
        _buffFactory = buffFactory;
    }

    public ItemType ItemType => ItemType.Special;
    public long[] Effects => new long[] { 250 };

    public async Task HandleAsync(IClientSession session, InventoryUseItemEvent e)
    {
        InventoryItem inv = e.Item;
        Buff buff = session.PlayerEntity.BuffComponent.GetBuff(CARD_ID);
        if (buff != null)
        {
            string buffName = _gameLanguage.GetLanguage(GameDataType.Card, buff.Name, session.UserLanguage);
            session.SendPacket(session.PlayerEntity.GenerateSayPacket(_gameLanguage.GetLanguageFormat(GameDialogKey.ITEM_CHATMESSAGE_CANT_USE_TWICE, session.UserLanguage, buffName),
                ChatMessageColorType.Yellow));
            return;
        }

        await session.RemoveItemFromInventory(inv.ItemInstance.ItemVNum);
        await session.PlayerEntity.AddBuffAsync(_buffFactory.CreateOneHourBuff(session.PlayerEntity, CARD_ID, BuffFlag.BIG_AND_KEEP_ON_LOGOUT));
        session.BroadcastPairy();
        session.RefreshFairy();
    }
}