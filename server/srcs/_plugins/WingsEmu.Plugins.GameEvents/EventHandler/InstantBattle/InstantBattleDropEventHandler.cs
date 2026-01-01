using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using PhoenixLib.Scheduler;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Inventory.Event;
using WingsEmu.Game.Maps;
using WingsEmu.Plugins.GameEvents.Configuration.InstantBattle;
using WingsEmu.Plugins.GameEvents.Event.InstantBattle;

namespace WingsEmu.Plugins.GameEvents.InstantBattle
{
    public class InstantBattleDropEventHandler : IAsyncEventProcessor<InstantBattleDropEvent>
    {
        private readonly IAsyncEventPipeline _eventPipeline;
        private readonly IGameLanguageService _gameLanguage;
        private readonly IScheduler _scheduler;

        public InstantBattleDropEventHandler(IAsyncEventPipeline eventPipeline, IGameLanguageService gameLanguage, IScheduler scheduler)
        {
            _eventPipeline = eventPipeline;
            _gameLanguage = gameLanguage;
            _scheduler = scheduler;
        }

        public async Task HandleAsync(InstantBattleDropEvent e, CancellationToken cancellation)
        {
            IMapInstance map = e.Instance.MapInstance;

            if (0 < e.Instance.MapInstance.GetAliveMonsters(x => x.IsInstantBattle).Count || e.Wave.Drops == null || e.Wave.Drops.Count < 1)
            {
                return;
            }

            foreach (InstantBattleDrop drop in e.Wave.Drops)
            {
                for (int i = 0; i < drop.BunchCount; i++)
                {
                    Position position = map.GetRandomPosition();
                    await _eventPipeline.ProcessEventAsync(new DropMapItemEvent(map, position, drop.ItemVnum, drop.AmountPerBunch), cancellation);
                }
            }
        }
    }
}