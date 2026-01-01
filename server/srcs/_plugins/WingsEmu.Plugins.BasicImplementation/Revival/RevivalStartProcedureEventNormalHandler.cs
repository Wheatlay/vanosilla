using System;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Revival;

namespace WingsEmu.Plugins.BasicImplementations.Revival;

public class RevivalStartProcedureEventNormalHandler : IAsyncEventProcessor<RevivalStartProcedureEvent>
{
    private readonly PlayerRevivalConfiguration _revivalConfiguration;

    public RevivalStartProcedureEventNormalHandler(GameRevivalConfiguration gameRevivalConfiguration) => _revivalConfiguration = gameRevivalConfiguration.PlayerRevivalConfiguration;

    public async Task HandleAsync(RevivalStartProcedureEvent e, CancellationToken cancellation)
    {
        if (e.Sender.PlayerEntity.MapInstance.MapInstanceType != MapInstanceType.NormalInstance
            && e.Sender.PlayerEntity.MapInstance.MapInstanceType != MapInstanceType.EventGameInstance
            && e.Sender.PlayerEntity.MapInstance.MapInstanceType != MapInstanceType.Miniland)
        {
            return;
        }

        if (e.Sender.CurrentMapInstance.MapInstanceType is MapInstanceType.Act4Instance or MapInstanceType.Act4Dungeon)
        {
            return;
        }

        if (e.Sender.PlayerEntity.IsOnVehicle)
        {
            await e.Sender.EmitEventAsync(new RemoveVehicleEvent());
        }

        await e.Sender.PlayerEntity.RemoveBuffsOnDeathAsync();
        e.Sender.RefreshStat();

        e.Sender.PlayerEntity.UpdateRevival(DateTime.UtcNow + _revivalConfiguration.RevivalDialogDelay, RevivalType.DontPayRevival, ForcedType.Forced);
    }
}