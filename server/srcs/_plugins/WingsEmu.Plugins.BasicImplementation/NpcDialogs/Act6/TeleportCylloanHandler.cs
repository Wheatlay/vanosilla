using System.Threading.Tasks;
using WingsEmu.Game._i18n;
using WingsEmu.Game._NpcDialog;
using WingsEmu.Game._NpcDialog.Event;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.NpcDialogs.Act6;

public class TeleportCylloanHandler : INpcDialogAsyncHandler
{
    private readonly IGameLanguageService _gameLanguage;
    private readonly IMapManager _mapManager;

    public TeleportCylloanHandler(IMapManager mapManager, IGameLanguageService gameLanguage)
    {
        _mapManager = mapManager;
        _gameLanguage = gameLanguage;
    }

    public NpcRunType[] NpcRunTypes => new[] { NpcRunType.ACT61_TELEPORT_CYLLOAN };

    public async Task Execute(IClientSession session, NpcDialogEvent e)
    {
        /*INpcEntity npcEntity = session.CurrentMapInstance.GetNpcById(e.NpcId);
        if (npcEntity == null)
        {
            return;
        }
        
        if (session.CurrentMapInstance.MapInstanceType != MapInstanceType.BaseMapInstance)
        {
            return;
        }

        if (session.PlayerEntity.Level < 88 && session.PlayerEntity.HeroLevel < 1)
        {
            session.SendInformationChatMessage(_gameLanguage.GetLanguage(GameDialogKey.TOO_LOW_LVL, session.UserLanguage));
            return;
        }

        session.ChangeMap(228, 85, 104);*/
    }
}