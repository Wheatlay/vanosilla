using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.Game._Guri;
using WingsEmu.Game._Guri.Event;

namespace WingsEmu.Plugins.BasicImplementations.Event.Guri;

public class GuriEventHandler : IAsyncEventProcessor<GuriEvent>
{
    private readonly IGuriHandlerContainer _guriHandler;

    public GuriEventHandler(IGuriHandlerContainer guriHandler) => _guriHandler = guriHandler;

    public async Task HandleAsync(GuriEvent e, CancellationToken cancellation)
    {
        await Task.Run(() => _guriHandler.Handle(e.Sender, e), cancellation);
    }
}