using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PhoenixLib.Configuration;
using PhoenixLib.Events;
using PhoenixLib.Logging;
using Plugin.Raids.RecurrentJob;
using Plugin.Raids.Scripting;
using Plugin.Raids.Scripting.Validator.Raid;
using WingsAPI.Communication.ServerApi.Protocol;
using WingsAPI.Plugins;
using WingsAPI.Scripting;
using WingsAPI.Scripting.LUA;
using WingsAPI.Scripting.ScriptManager;
using WingsEmu.Game.Raids;
using WingsEmu.Game.Raids.Configuration;

namespace Plugin.Raids;

public class RaidsPluginCore : IGameServerPlugin
{
    public string Name => nameof(RaidsPluginCore);

    public void AddDependencies(IServiceCollection services, GameServerLoader gameServer)
    {
        // TODO: Plz, when we have warmup we should move those "AddSingleton" down, so it only gets loaded when necessary
        services.AddSingleton<SRaidValidator>();

        // Raid Script Cache
        services.AddSingleton<RaidScriptManager>();
        services.AddSingleton<IRaidScriptManager>(s => s.GetRequiredService<RaidScriptManager>());

        if (gameServer.Type == GameChannelType.ACT_4)
        {
            Log.Debug("Not loading Raids plugin because this is an Act4 channel.");
            return;
        }

        services.AddEventHandlersInAssembly<RaidsPluginCore>();

        services.AddSingleton<IRaidManager, RaidManager>();
        services.AddHostedService<RaidSystem>();

        services.TryAddSingleton(x =>
        {
            IConfigurationPathProvider config = x.GetRequiredService<IConfigurationPathProvider>();
            return new ScriptFactoryConfiguration
            {
                RootDirectory = config.GetConfigurationPath("scripts"),
                LibDirectory = config.GetConfigurationPath("scripts/lib")
            };
        });

        // script factory
        services.TryAddSingleton<IScriptFactory, LuaScriptFactory>();

        // factory
        services.AddSingleton<IRaidFactory, RaidFactory>();

        services.AddFileConfiguration<RaidConfiguration>();
    }
}