using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Algorithm;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.Event.Characters;

public class ChangeFactionEventHandler : IAsyncEventProcessor<ChangeFactionEvent>
{
    private readonly ICharacterAlgorithm _characterAlgorithm;
    private readonly IGameLanguageService _gameLanguage;

    public ChangeFactionEventHandler(IGameLanguageService gameLanguage, ICharacterAlgorithm characterAlgorithm)
    {
        _gameLanguage = gameLanguage;
        _characterAlgorithm = characterAlgorithm;
    }

    public async Task HandleAsync(ChangeFactionEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;
        session.PlayerEntity.SetFaction(e.NewFaction);
        GameDialogKey factionMessageKey = session.PlayerEntity.Faction == 0
            ? GameDialogKey.INTERACTION_SHOUTMESSAGE_GET_PROTECTION_POWER_NEUTRAL
            : session.PlayerEntity.Faction == FactionType.Angel
                ? GameDialogKey.INTERACTION_SHOUTMESSAGE_GET_PROTECTION_POWER_ANGEL
                : GameDialogKey.INTERACTION_SHOUTMESSAGE_GET_PROTECTION_POWER_DEMON;
        session.SendMsg(_gameLanguage.GetLanguage(factionMessageKey, session.UserLanguage),
            MsgMessageType.Middle);
        session.SendPacket("scr 0 0 0 0 0 0");
        session.RefreshFaction();
        session.RefreshStatChar();
        session.SendEffect(session.PlayerEntity.Faction == FactionType.Demon ? EffectType.DemonProtection : EffectType.AngelProtection);
        session.SendCondPacket();
        session.RefreshLevel(_characterAlgorithm);
    }
}