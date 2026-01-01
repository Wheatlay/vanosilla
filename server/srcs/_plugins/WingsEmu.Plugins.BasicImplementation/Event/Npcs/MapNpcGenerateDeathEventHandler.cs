using System;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Battle.Managers;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Npcs.Event;
using WingsEmu.Game.TimeSpaces;
using WingsEmu.Game.TimeSpaces.Enums;
using WingsEmu.Game.TimeSpaces.Events;
using WingsEmu.Game.Triggers;

namespace WingsEmu.Plugins.BasicImplementations.Event.Npcs;

public class MapNpcGenerateDeathEventHandler : IAsyncEventProcessor<MapNpcGenerateDeathEvent>
{
    private readonly IAsyncEventPipeline _asyncEventPipeline;
    private readonly IPhantomPositionManager _phantomPositionManager;
    private readonly ITimeSpaceManager _timeSpaceManager;

    public MapNpcGenerateDeathEventHandler(ITimeSpaceManager timeSpaceManager, IAsyncEventPipeline asyncEventPipeline, IPhantomPositionManager phantomPositionManager)
    {
        _timeSpaceManager = timeSpaceManager;
        _asyncEventPipeline = asyncEventPipeline;
        _phantomPositionManager = phantomPositionManager;
    }

    public async Task HandleAsync(MapNpcGenerateDeathEvent e, CancellationToken cancellation)
    {
        INpcEntity npc = e.NpcEntity;
        DateTime currentTime = DateTime.UtcNow;
        npc.IsStillAlive = false;
        npc.Hp = 0;
        npc.Mp = 0;
        npc.Death = currentTime;
        await npc.RemoveAllBuffsAsync(true);
        npc.Target = null;
        npc.Killer = e.Killer;

        if (npc.IsPhantom())
        {
            _phantomPositionManager.AddPosition(npc.UniqueId, npc.Position);
        }

        await npc.TriggerEvents(BattleTriggers.OnDeath);

        if (npc.MapInstance.MapInstanceType != MapInstanceType.TimeSpaceInstance)
        {
            return;
        }

        if (!npc.IsProtected)
        {
            return;
        }

        TimeSpaceParty timeSpace = _timeSpaceManager.GetTimeSpaceByMapInstanceId(npc.MapInstance.Id);
        if (timeSpace == null)
        {
            return;
        }

        timeSpace.Instance.KilledProtectedNpcs++;

        if (!timeSpace.Instance.TimeSpaceObjective.ProtectNPC)
        {
            return;
        }

        await _asyncEventPipeline.ProcessEventAsync(new TimeSpaceInstanceFinishEvent(timeSpace, TimeSpaceFinishType.NPC_DIED));
    }
}