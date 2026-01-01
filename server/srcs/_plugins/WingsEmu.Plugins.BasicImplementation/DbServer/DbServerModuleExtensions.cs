using Microsoft.Extensions.DependencyInjection;
using WingsEmu.Communication.gRPC.Extensions;

namespace WingsEmu.Plugins.BasicImplementations.DbServer;

public static class DbServerModuleExtensions
{
    public static void AddDbServerModule(this IServiceCollection services)
    {
        services.AddGrpcDbServerServiceClient();

        services.AddHostedService<CharacterSaveSystem>();
    }
}