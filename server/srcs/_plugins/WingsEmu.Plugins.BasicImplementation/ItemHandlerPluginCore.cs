using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PhoenixLib.Configuration;
using WingsAPI.Plugins;
using WingsEmu.Game._ItemUsage;
using WingsEmu.Game._ItemUsage.Configuration;
using WingsEmu.Game.Cellons;
using WingsEmu.Game.Managers;
using WingsEmu.Plugins.BasicImplementations.Algorithms.Shells;
using WingsEmu.Plugins.BasicImplementations.ItemUsage.Etc;
using WingsEmu.Plugins.BasicImplementations.Vehicles;

namespace WingsEmu.Plugins.BasicImplementations;

public class ItemHandlerPluginCore : IGameServerPlugin
{
    public string Name => nameof(ItemHandlerPluginCore);


    public void AddDependencies(IServiceCollection services, GameServerLoader gameServer)
    {
        services.AddHandlers<ItemHandlerPluginCore, IItemHandler>();
        services.AddHandlers<ItemHandlerPluginCore, IItemUsageByVnumHandler>();

        services.AddMultipleConfigurationOneFile<SpPartnerInfo>("sp_partner");
        services.AddSingleton<ISpPartnerConfiguration, SpPartnerConfiguration>();

        services.AddFileConfiguration<MateBuffConfiguration>("mates_buffs");
        services.AddSingleton<IMateBuffConfigsContainer, MateBuffConfigsContainer>();

        services.AddFileConfiguration<SpWingInfoConfiguration>("sp_wing_info");
        services.AddSingleton<ISpWingConfiguration, SpWingConfiguration>();

        services.AddFileConfiguration<CostumeMorphFileConfiguration>("costume_scroll_morphs");
        services.AddSingleton<ICostumeScrollConfiguration, CostumeScrollConfiguration>();

        services.AddSingleton<IItemHandlerContainer, ItemHandlerContainer>();
        services.AddSingleton<IItemUsageManager, ItemUsageManager>();

        services.AddFileConfiguration<CellonSystemConfiguration>("cellon_configuration");

        services.AddMultipleConfigurationOneFile<ShellOptionValues>("shell_option_type_configuration");
        services.AddSingleton<IShellOptionTypeConfiguration, ShellOptionTypeConfiguration>();

        services.AddFileConfiguration<ShellCategoryConfiguration>("shell_categories_config");

        services.AddMultipleConfigurationOneFile<ShellLevelEffect>("shell_level_effect_configuration");
        services.AddSingleton<IShellLevelEffectConfiguration, ShellLevelEffectConfiguration>();

        services.AddMultipleConfigurationOneFile<PerfumeConfiguration>("perfume_configuration");
        services.AddSingleton<IShellPerfumeConfiguration, ShellPerfumeConfiguration>();

        services.AddMultipleConfigurationOneFile<VehicleConfiguration>("vehicle");
        services.TryAddSingleton<IVehicleConfigurationProvider, VehicleConfigurationProvider>();

        services.AddFileConfiguration<CellaRefinerConfiguration>("cella_refiners_configuration");
    }
}