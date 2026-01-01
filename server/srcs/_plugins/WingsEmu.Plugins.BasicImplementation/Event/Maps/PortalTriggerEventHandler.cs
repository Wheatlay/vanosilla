using System;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.DTOs.Maps;
using WingsEmu.Game;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Maps.Event;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Raids;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.Event.Maps;

public class PortalTriggerEventHandler : IAsyncEventProcessor<PortalTriggerEvent>
{
    private readonly IMapManager _mapManager;

    public PortalTriggerEventHandler(IMapManager mapManager) => _mapManager = mapManager;

    public async Task HandleAsync(PortalTriggerEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;

        switch (e.Portal.Type)
        {
            case PortalType.MapPortal:
            case PortalType.Open:
            case PortalType.Miniland:
            case PortalType.Exit:
            case PortalType.Effect:
            case PortalType.ShopTeleport:
            case PortalType.TSNormal when e.Sender.CurrentMapInstance.MapInstanceType != MapInstanceType.TimeSpaceInstance &&
                e.Sender.CurrentMapInstance.MapInstanceType != MapInstanceType.RaidInstance:
                break;
            default:
                return;
        }

        switch (session.CurrentMapInstance.MapInstanceType)
        {
            case MapInstanceType.RaidInstance:
                if (e.Portal.Type != PortalType.Open)
                {
                    break;
                }

                RaidParty raidParty = session.PlayerEntity.Raid;
                if (raidParty == null || session == raidParty.Leader)
                {
                    break;
                }

                if (e.Portal.DestinationMapInstance != null && raidParty.Leader.CurrentMapInstance.Id == e.Portal.DestinationMapInstance.Id)
                {
                    break;
                }

                if (e.Portal.DestinationMapInstance != null && raidParty.Instance?.RaidSubInstances != null)
                {
                    if (raidParty.Instance.RaidSubInstances.TryGetValue(e.Portal.DestinationMapInstance.Id, out RaidSubInstance subInstance)
                        && subInstance is { IsDiscoveredByLeader: true })
                        // avoid reteleporting leader on already discovered maps
                    {
                        break;
                    }
                }

                await ProcessTeleport(raidParty.Leader, e.Portal);

                break;
            case MapInstanceType.Miniland:
            case MapInstanceType.ArenaInstance:
            case MapInstanceType.EventGameInstance:
                session.ChangeToLastBaseMap();
                return;
            case MapInstanceType.TimeSpaceInstance:
            case MapInstanceType.Act4Instance:
                return;
        }

        if (session.CurrentMapInstance.HasMapFlag(MapFlags.ACT_4))
        {
            return;
        }

        if (e.Portal.DestinationMapInstance == null)
        {
            return;
        }

        e.Sender.PlayerEntity.LastPortal = DateTime.UtcNow;
        await ProcessTeleport(session, e.Portal);
    }

    private async Task ProcessTeleport(IClientSession session, IPortalEntity portal)
    {
        if (!session.CurrentMapInstance.HasMapFlag(MapFlags.IS_BASE_MAP) && portal.DestinationMapInstance != null && portal.DestinationMapInstance.HasMapFlag(MapFlags.IS_BASE_MAP))
        {
            session.ChangeToLastBaseMap();
            return;
        }

        if (portal.DestinationMapInstance?.Id == session.PlayerEntity.Miniland.Id)
        {
            session.ChangeMap(session.PlayerEntity.Miniland, portal.DestinationX, portal.DestinationY);
            return;
        }

        if (portal.DestinationX == -1 && portal.DestinationY == -1)
        {
            await _mapManager.TeleportOnRandomPlaceInMapAsync(session, portal.DestinationMapInstance);
            return;
        }

        if (portal.DestinationMapInstance?.Id == session.PlayerEntity.MapInstanceId && portal.DestinationX.HasValue && portal.DestinationY.HasValue)
        {
            session.PlayerEntity.TeleportOnMap(portal.DestinationX.Value, portal.DestinationY.Value, true);
            return;
        }

        session.ChangeMap(portal.DestinationMapInstance, portal.DestinationX, portal.DestinationY);
    }
}