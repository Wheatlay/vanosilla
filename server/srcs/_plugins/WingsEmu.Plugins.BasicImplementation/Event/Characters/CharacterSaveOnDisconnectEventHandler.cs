using System;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using PhoenixLib.Logging;
using PhoenixLib.ServiceBus;
using WingsAPI.Communication.ServerApi.Protocol;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsEmu.Game._enum;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Groups.Events;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Maps.Event;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Raids.Events;
using WingsEmu.Game.RainbowBattle.Event;
using WingsEmu.Game.TimeSpaces.Events;
using WingsEmu.Packets.Enums;
using WingsEmu.Plugins.DistributedGameEvents.PlayerEvents;

namespace WingsEmu.Plugins.BasicImplementations.Event.Characters;

public class CharacterSaveOnDisconnectEventHandler : IAsyncEventProcessor<CharacterDisconnectedEvent>
{
    private readonly IMessagePublisher<PlayerDisconnectedChannelMessage> _disconnectedPublisher;
    private readonly SerializableGameServer _gameServer;
    private readonly IRespawnDefaultConfiguration _respawnDefaultConfiguration;
    private readonly ISessionManager _sessionManager;
    private readonly IMateTransportFactory _transportFactory;

    public CharacterSaveOnDisconnectEventHandler(ISessionManager sessionManager, IMessagePublisher<PlayerDisconnectedChannelMessage> disconnectedPublisher, SerializableGameServer gameServer,
        IRespawnDefaultConfiguration respawnDefaultConfiguration, IMateTransportFactory transportFactory)
    {
        _sessionManager = sessionManager;
        _disconnectedPublisher = disconnectedPublisher;
        _gameServer = gameServer;
        _respawnDefaultConfiguration = respawnDefaultConfiguration;
        _transportFactory = transportFactory;
    }

    public async Task HandleAsync(CharacterDisconnectedEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;

        try
        {
            session.PlayerEntity.LifetimeStats.TotalTimeOnline += DateTime.UtcNow - session.PlayerEntity.GameStartDate;
            await session.CloseExchange();
            await session.EmitEventAsync(new LeaveGroupEvent());
            Log.Info("Group left");
            await session.EmitEventAsync(new RaidPartyLeaveEvent(false));
            Log.Info("Raid party left");
            await session.EmitEventAsync(new TimeSpaceLeavePartyEvent
            {
                RemoveLive = true,
                CheckFinished = false
            });
            Log.Info("Timespace left");
            await session.EmitEventAsync(new LeaveMapEvent());
            Log.Info("Rainbow left");
            await session.EmitEventAsync(new RainbowBattleLeaveEvent
            {
                SendMessage = true
            });
            Log.Info("Map left");
            await session.EmitEventAsync(new CharacterRemoveManagersEvent());
            Log.Info("Remove character from managers");
        }
        catch (Exception exc)
        {
            Log.Error("[DISCONNECT_HANDLER] Leave maps", exc);
        }

        try
        {
            _sessionManager.RemoveOnline(session.PlayerEntity.Name, session.PlayerEntity.Id);

            if (session.PlayerEntity.IsSeal && _gameServer.ChannelType == GameChannelType.ACT_4)
            {
                RespawnDefault respawn = _respawnDefaultConfiguration.GetReturn(session.PlayerEntity.Faction == FactionType.Angel ? RespawnType.ACT4_ANGEL_SPAWN : RespawnType.ACT4_DEMON_SPAWN);
                session.PlayerEntity.MapId = respawn.MapId;
                session.PlayerEntity.MapX = respawn.MapX;
                session.PlayerEntity.MapY = respawn.MapY;
            }
        }
        catch (Exception exc)
        {
            Log.Error("[DISCONNECT_HANDLER]", exc);
        }

        try
        {
            await session.EmitEventAsync(new SessionSaveEvent());
        }
        catch (Exception exc)
        {
            Log.Error("[DISCONNECT_HANDLER]", exc);
        }

        await _disconnectedPublisher.PublishAsync(new PlayerDisconnectedChannelMessage
        {
            ChannelId = _gameServer.ChannelId,
            CharacterId = session.PlayerEntity.Id,
            CharacterName = session.PlayerEntity.Name,
            DisconnectionTime = e.DisconnectionTime,
            FamilyId = session.PlayerEntity.Family?.Id
        });

        Log.Warn($"Character disconnected: {session.PlayerEntity.Name}:{session.PlayerEntity.Id}");
    }
}