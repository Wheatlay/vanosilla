using System;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.DAL.Redis.Locks;
using PhoenixLib.Events;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Networking;

namespace WingsEmu.Plugins.BasicImplementations.Event.Characters;

public class SpecialistRefreshEventHandler : IAsyncEventProcessor<SpecialistRefreshEvent>
{
    private readonly GameMinMaxConfiguration _gameMinMaxConfiguration;
    private readonly IExpirableLockService _lockService;

    public SpecialistRefreshEventHandler(IExpirableLockService lockService, GameMinMaxConfiguration gameMinMaxConfiguration)
    {
        _lockService = lockService;
        _gameMinMaxConfiguration = gameMinMaxConfiguration;
    }

    public async Task HandleAsync(SpecialistRefreshEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;
        bool canRefresh = await _lockService.TryAddTemporaryLockAsync($"game:locks:specialist-points-refresh:{session.PlayerEntity.Id}", DateTime.UtcNow.Date.AddDays(1));

        if (canRefresh == false && e.Force == false)
        {
            session.SendDebugMessage("Specialist Points already refreshed today.");
            return;
        }

        session.PlayerEntity.SpPointsBasic = _gameMinMaxConfiguration.MaxSpBasePoints;
        session.RefreshSpPoint();
    }
}