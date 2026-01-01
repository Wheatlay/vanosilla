using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using PhoenixLib.Logging;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Raids;
using WingsEmu.Game.Raids.Events;
using WingsEmu.Game.Revival;

namespace Plugin.Raids;

public class RaidInstanceDestroyEventHandler : IAsyncEventProcessor<RaidInstanceDestroyEvent>
{
    private readonly IMapManager _mapManager;
    private readonly IRaidManager _raidManager;

    public RaidInstanceDestroyEventHandler(IRaidManager raidManager, IMapManager mapManager)
    {
        _raidManager = raidManager;
        _mapManager = mapManager;
    }

    public async Task HandleAsync(RaidInstanceDestroyEvent e, CancellationToken cancellation)
    {
        Log.Warn("Destroying raid instance");
        _raidManager.RemoveRaid(e.RaidParty);

        if (e.RaidParty.Instance == null)
        {
            return;
        }

        foreach (RaidSubInstance subInstance in e.RaidParty.Instance.RaidSubInstances.Values)
        {
            foreach (IClientSession session in subInstance.MapInstance.Sessions.ToList())
            {
                RaidPartyLeaveEventHandler.InternalLeave(session);

                if (!session.PlayerEntity.IsAlive())
                {
                    await session.EmitEventAsync(new RevivalReviveEvent());
                }

                session.ChangeToLastBaseMap();
            }

            _mapManager.RemoveMapInstance(subInstance.MapInstance.Id);
            subInstance.MapInstance.Destroy();
        }

        e.RaidParty.Destroy = true;
    }
}