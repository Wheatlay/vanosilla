using System;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.Groups;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game._ItemUsage.Configuration;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Networking;

namespace WingsEmu.Plugins.BasicImplementations.Event.Characters;

public class VehicleRemoveEventHandler : IAsyncEventProcessor<RemoveVehicleEvent>
{
    private readonly IGameLanguageService _gameLanguage;
    private readonly ISpPartnerConfiguration _spPartner;

    public VehicleRemoveEventHandler(IGameLanguageService gameLanguage, ISpPartnerConfiguration spPartner)
    {
        _gameLanguage = gameLanguage;
        _spPartner = spPartner;
    }

    public async Task HandleAsync(RemoveVehicleEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;

        if (!session.PlayerEntity.IsOnVehicle)
        {
            return;
        }

        if (e.ShowMates && !session.PlayerEntity.IsInvisible())
        {
            session.BroadcastInTeamMembers(_gameLanguage, _spPartner);
        }

        session.PlayerEntity.RandomMapTeleport = null;
        session.RefreshParty(_spPartner);

        Buff speedBooster = session.PlayerEntity.BuffComponent.GetBuff((short)BuffVnums.SPEED_BOOSTER);
        await session.PlayerEntity.RemoveBuffAsync(false, speedBooster);

        session.PlayerEntity.IsOnVehicle = false;
        await session.EmitEventAsync(new GetDefaultMorphEvent());
        session.RefreshStatChar();
        session.BroadcastEq();
        session.RefreshStat();
        session.SendCondPacket();
        session.PlayerEntity.LastSpeedChange = DateTime.UtcNow;
    }
}