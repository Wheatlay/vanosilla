using System.Threading.Tasks;
using WingsEmu.Game._NpcDialog;
using WingsEmu.Game._NpcDialog.Event;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.NpcDialogs;

public class JoinLodHandler : INpcDialogAsyncHandler
{
    public NpcRunType[] NpcRunTypes => new[] { NpcRunType.LAND_OF_CHAOS_ENTER };

    public async Task Execute(IClientSession session, NpcDialogEvent e)
    {
    }
}