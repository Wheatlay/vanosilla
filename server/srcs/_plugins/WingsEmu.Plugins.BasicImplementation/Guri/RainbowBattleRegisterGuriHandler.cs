using System;
using System.Linq;
using System.Threading.Tasks;
using WingsEmu.Game._Guri;
using WingsEmu.Game._Guri.Event;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Networking;
using WingsEmu.Game.RainbowBattle;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.Guri;

public class RainbowBattleRegisterGuriHandler : IGuriHandler
{
    private readonly IRainbowBattleManager _rainbowBattleManager;

    public RainbowBattleRegisterGuriHandler(IRainbowBattleManager rainbowBattleManager) => _rainbowBattleManager = rainbowBattleManager;

    public long GuriEffectId => 503;

    public async Task ExecuteAsync(IClientSession session, GuriEvent e)
    {
        if (!_rainbowBattleManager.IsRegistrationActive)
        {
            return;
        }

        if (_rainbowBattleManager.RegisteredPlayers.Contains(session.PlayerEntity.Id))
        {
            return;
        }

        _rainbowBattleManager.RegisterPlayer(session.PlayerEntity.Id);

        double timeLeft = (_rainbowBattleManager.RegistrationStartTime - DateTime.UtcNow).TotalSeconds;
        session.SendBsInfoPacket(BsInfoType.OpenWindow, GameType.RainbowBattle, (ushort)timeLeft, QueueWindow.WaitForEntry);
    }
}