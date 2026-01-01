using Microsoft.Extensions.DependencyInjection;
using PhoenixLib.Events;
using WingsAPI.Plugins;

namespace WingsEmu.Plugins.BasicImplementations;

public class GenericEventPluginCore : IGameServerPlugin
{
    public string Name => nameof(GenericEventPluginCore);

    public void AddDependencies(IServiceCollection services, GameServerLoader gameServer)
    {
        services.AddEventHandlersInAssembly<GenericEventPluginCore>();
    }
}