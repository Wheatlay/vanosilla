using System;
using Microsoft.Extensions.DependencyInjection;
using WingsAPI.Plugins;
using WingsEmu.Game.Algorithm;
using WingsEmu.Game.Buffs;

namespace WingsEmu.Plugins.BasicImplementations;

public class GameHelpersPlugin : IGamePlugin
{
    private readonly IServiceProvider _services;

    public GameHelpersPlugin(IServiceProvider services) => _services = services;

    public string Name => nameof(GameHelpersPlugin);

    public void OnLoad()
    {
        StaticCharacterAlgorithmService.Initialize(_services.GetService<ICharacterAlgorithm>());
        StaticBCardEffectHandlerService.Initialize(_services.GetService<IBCardEffectHandlerContainer>());
    }
}