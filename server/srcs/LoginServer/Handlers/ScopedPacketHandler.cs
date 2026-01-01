using System;
using System.Threading.Tasks;
using LoginServer.Network;
using Microsoft.Extensions.DependencyInjection;
using WingsEmu.Packets;

namespace LoginServer.Handlers
{
    /// <summary>
    /// Wraps another IPacketHandler so it is resolved within a new DI scope per execution.
    /// This allows handlers with scoped dependencies to be registered once at startup.
    /// </summary>
    /// <typeparam name="THandler">Concrete packet handler type</typeparam>
    public class ScopedPacketHandler<THandler> : IPacketHandler where THandler : IPacketHandler
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ScopedPacketHandler(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task HandleAsync(LoginClientSession session, IPacket packet)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            THandler handler = scope.ServiceProvider.GetRequiredService<THandler>();
            await handler.HandleAsync(session, packet);
        }
    }
}
