using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.DTOs.Maps;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Act4.Configuration;
using WingsEmu.Game.Act4.Event;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Maps.Event;
using WingsEmu.Packets.Enums;

namespace Plugin.Act4.Event;

public class PortalTriggerAct4EventHandler : IAsyncEventProcessor<PortalTriggerEvent>
{
    private readonly Act4Configuration _act4Configuration;
    private readonly IGameLanguageService _languageService;
    private readonly IMapManager _mapManager;

    public PortalTriggerAct4EventHandler(Act4Configuration act4Configuration, IGameLanguageService languageService, IMapManager mapManager)
    {
        _act4Configuration = act4Configuration;
        _languageService = languageService;
        _mapManager = mapManager;
    }

    public async Task HandleAsync(PortalTriggerEvent e, CancellationToken cancellation)
    {
        if (!e.Sender.CurrentMapInstance.HasMapFlag(MapFlags.ACT_4))
        {
            return;
        }

        if (e.Sender.CurrentMapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance)
        {
            return;
        }

        List<int> bannedMapIds = e.Sender.PlayerEntity.Faction == FactionType.Angel ? _act4Configuration.BannedMapIdsToAngels : _act4Configuration.BannedMapIdsToDemons;
        if (e.Portal.DestinationMapInstance != null && bannedMapIds.Contains(e.Portal.DestinationMapInstance.MapId))
        {
            e.Sender.SendInformationChatMessage(_languageService.GetLanguage(GameDialogKey.PORTAL_CHATMESSAGE_BLOCKED, e.Sender.UserLanguage));
            return;
        }

        e.Sender.PlayerEntity.LastPortal = DateTime.UtcNow;

        if (e.Portal.Type == PortalType.AngelRaid || e.Portal.Type == PortalType.DemonRaid)
        {
            await e.Sender.EmitEventAsync(new Act4DungeonEnterEvent
            {
                Confirmed = e.Confirmed
            });
            return;
        }

        if (e.Portal.DestinationMapInstance == null)
        {
            return;
        }

        //TP Logic
        if (e.Portal.DestinationX == -1 && e.Portal.DestinationY == -1)
        {
            await _mapManager.TeleportOnRandomPlaceInMapAsync(e.Sender, e.Portal.DestinationMapInstance);
            return;
        }

        if (e.Portal.DestinationMapInstance.Id == e.Sender.PlayerEntity.MapInstanceId && e.Portal.DestinationX.HasValue && e.Portal.DestinationY.HasValue)
        {
            e.Sender.PlayerEntity.TeleportOnMap(e.Portal.DestinationX.Value, e.Portal.DestinationY.Value, true);
            return;
        }

        e.Sender.ChangeMap(e.Portal.DestinationMapInstance, e.Portal.DestinationX, e.Portal.DestinationY);
    }
}