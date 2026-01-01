using System;
using PhoenixLib.Extensions;
using PhoenixLib.Logging;
using WingsAPI.Plugins;
using WingsEmu.Game._ItemUsage;

namespace WingsEmu.Plugins.BasicImplementations;

public class ItemHandlerPlugin : IGamePlugin
{
    private readonly IServiceProvider _container;
    private readonly IItemHandlerContainer _handlers;

    public ItemHandlerPlugin(IItemHandlerContainer handlers, IServiceProvider container)
    {
        _handlers = handlers;
        _container = container;
    }

    public string Name => nameof(ItemHandlerPlugin);


    public void OnLoad()
    {
        foreach (Type handlerType in typeof(ItemHandlerPlugin).Assembly.GetTypesImplementingInterface<IItemHandler>())
        {
            try
            {
                object tmp = _container.GetService(handlerType);
                if (!(tmp is IItemHandler real))
                {
                    continue;
                }

                Log.Debug($"[ITEM_USAGE][ADD_HANDLER] {handlerType}");
                _handlers.RegisterItemHandler(real).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Log.Error("[ITEM_USAGE][FAIL_ADD]", e);
            }
        }

        foreach (Type handlerType in typeof(ItemHandlerPlugin).Assembly.GetTypesImplementingInterface<IItemUsageByVnumHandler>())
        {
            try
            {
                object tmp = _container.GetService(handlerType);
                if (!(tmp is IItemUsageByVnumHandler real))
                {
                    continue;
                }

                Log.Debug($"[ITEM_USAGE][ADD_HANDLER_VNUM] {handlerType}");
                _handlers.RegisterItemHandler(real).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Log.Error("[ITEM_USAGE][FAIL_ADD_VNUM]", e);
            }
        }
    }
}