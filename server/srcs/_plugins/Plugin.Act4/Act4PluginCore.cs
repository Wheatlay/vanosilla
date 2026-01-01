using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PhoenixLib.Configuration;
using PhoenixLib.Events;
using PhoenixLib.Logging;
using Plugin.Act4.RecurrentJob;
using Plugin.Act4.Scripting.Validator;
using WingsAPI.Communication.ServerApi.Protocol;
using WingsAPI.Plugins;
using WingsAPI.Scripting;
using WingsAPI.Scripting.LUA;
using WingsAPI.Scripting.ScriptManager;
using WingsEmu.Game.Act4;
using WingsEmu.Game.Act4.Configuration;
using WingsEmu.Game.Act4.Entities;
using WingsEmu.Game.Helpers;
using WingsEmu.Game.Managers;

namespace Plugin.Act4;

public class Act4PluginCore : IGameServerPlugin
{
    public string Name => nameof(Act4PluginCore);

    public void AddDependencies(IServiceCollection services, GameServerLoader gameServer)
    {
        if (gameServer.Type != GameChannelType.ACT_4)
        {
            Log.Debug("Not loading Act4 plugin because this is not an Act4 channel.");
            // Register null/default implementations for services that other handlers depend on
            services.TryAddSingleton<IAct4FlagManager, NullAct4FlagManager>();
            services.TryAddSingleton<IDungeonManager, NullDungeonManager>();
            services.TryAddSingleton(new Act4DungeonsConfiguration());
            return;
        }

        // TODO: Plz, when we have warmup we should move those "AddSingleton" down, so it only gets loaded when necessary
        // Dungeon Script Cache
        services.AddScoped<SDungeonValidator>();
        services.AddSingleton<IDungeonScriptManager, DungeonScriptManager>();
        services.AddSingleton<IDungeonManager, DungeonManager>();
        services.AddFileConfiguration<Act4DungeonsConfiguration>();
        services.AddSingleton<IAct4FlagManager, Act4FlagManager>();

        services.AddEventHandlersInAssembly<Act4PluginCore>();

        services.AddFileConfiguration<Act4Configuration>();

        services.AddSingleton<IAct4Manager, Act4Manager>();
        services.AddHostedService<Act4System>();

        services.AddSingleton<IAct4DungeonManager, Act4DungeonManager>();
        services.AddHostedService<Act4DungeonSystem>();

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
        services.AddSingleton<IDungeonFactory, DungeonFactory>();
    }
}

// Null implementations for non-Act4 channels
internal class NullAct4FlagManager : IAct4FlagManager
{
    public MapLocation AngelFlag => null;
    public MapLocation DemonFlag => null;
    public void SetAngelFlag(MapLocation mapLocation) { }
    public void SetDemonFlag(MapLocation mapLocation) { }
    public void RemoveAngelFlag() { }
    public void RemoveDemonFlag() { }
}

internal class NullDungeonManager : IDungeonManager
{
    public void AddNewHatusState(Guid mapInstanceId, HatusState hatusState) { }
    public HatusState GetHatusState(Guid mapInstanceId) => null;
    public void RemoveHatusState(Guid mapInstanceId) { }
    public void AddCalvinasDragons(Guid mapInstanceId, CalvinasState calvinasState) { }
    public CalvinasState GetCalvinasDragons(Guid mapInstanceId) => null;
    public void RemoveCalvinasDragons(Guid mapInstanceId) { }
}