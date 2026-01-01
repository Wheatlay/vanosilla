using System.Collections.Generic;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsEmu.DTOs.Maps;
using WingsEmu.Game;
using WingsEmu.Game._i18n;
using WingsEmu.Game._ItemUsage;
using WingsEmu.Game._ItemUsage.Event;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Monster.Event;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.ItemUsage.Etc.Teacher;

public class NosMateTrainerHandler : IItemHandler
{
    private const int MAX_DOLLS = 10;
    private readonly IAsyncEventPipeline _eventPipeline;
    private readonly IGameLanguageService _gameLanguage;
    private readonly INpcMonsterManager _monsterManager;

    public NosMateTrainerHandler(IGameLanguageService gameLanguage, INpcMonsterManager monsterManager, IAsyncEventPipeline eventPipeline)
    {
        _gameLanguage = gameLanguage;
        _monsterManager = monsterManager;
        _eventPipeline = eventPipeline;
    }

    public ItemType ItemType => ItemType.PetPartnerItem;
    public long[] Effects => new long[] { 21, 20 };

    public async Task HandleAsync(IClientSession session, InventoryUseItemEvent e)
    {
        if (!session.PlayerEntity.IsAlive())
        {
            return;
        }

        if (session.PlayerEntity.IsInExchange())
        {
            return;
        }

        if (session.PlayerEntity.IsOnVehicle)
        {
            return;
        }

        if (session.PlayerEntity.HasShopOpened)
        {
            return;
        }

        int monsterVnum = e.Item.ItemInstance.GameItem.EffectValue;

        IMonsterData monster = _monsterManager.GetNpc(monsterVnum);
        if (monster == null)
        {
            return;
        }

        if (session.PlayerEntity.IsInMateDollZone())
        {
            session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.DOLL_SHOUTMESSAGE_NOT_IN_ZONE, session.UserLanguage), MsgMessageType.Middle);
            return;
        }

        int dolls = session.CurrentMapInstance.GetAliveMonsters(x => x != null && x.IsAlive() && x.SummonerId == session.PlayerEntity.Id && x.SummonerType == VisualType.Player && x.IsMateTrainer)
            .Count;
        if (dolls >= MAX_DOLLS)
        {
            session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.DOLL_SHOUTMESSAGE_DOLLS_LIMIT, session.UserLanguage), MsgMessageType.Middle);
            return;
        }

        FactionType factionType = FactionType.Neutral;
        if (session.CurrentMapInstance.HasMapFlag(MapFlags.ACT_4))
        {
            factionType = session.PlayerEntity.Faction == FactionType.Angel ? FactionType.Demon : FactionType.Angel;
        }

        var listToSummon = new List<ToSummon>
        {
            new()
            {
                VNum = (short)monsterVnum,
                SpawnCell = session.PlayerEntity.Position,
                IsMoving = false,
                IsMateTrainer = true,
                FactionType = factionType
            }
        };

        await _eventPipeline.ProcessEventAsync(new MonsterSummonEvent(session.CurrentMapInstance, listToSummon, session.PlayerEntity, false));
        await session.RemoveItemFromInventory(item: e.Item);
    }
}