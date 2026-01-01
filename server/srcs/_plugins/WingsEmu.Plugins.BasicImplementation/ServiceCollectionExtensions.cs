using System;
using Microsoft.Extensions.DependencyInjection;
using PhoenixLib.Extensions;
using WingsEmu.Game._Guri;

namespace WingsEmu.Plugins.BasicImplementations;

public static class ServiceCollectionExtensions
{
    public static void AddGuriHandlers(this IServiceCollection services)
    {
        Type[] types = typeof(GuriPlugin).Assembly.GetTypesImplementingInterface<IGuriHandler>();
        foreach (Type handlerType in types)
        {
            services.AddTransient(handlerType);
        }

        services.AddSingleton<IGuriHandlerContainer, BaseGuriHandler>();
    }
}