using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Packets.Enums.Rainbow;
using WingsEmu.Game.RainbowBattle;
using WingsEmu.Game.RainbowBattle.Event;

namespace Plugin.RainbowBattle.EventHandlers
{
    public class RainbowBattleProcessFlagPointsEventHandler : IAsyncEventProcessor<RainbowBattleProcessFlagPointsEvent>
    {
        private readonly IAsyncEventPipeline _asyncEventPipeline;

        public RainbowBattleProcessFlagPointsEventHandler(IAsyncEventPipeline asyncEventPipeline) => _asyncEventPipeline = asyncEventPipeline;

        public async Task HandleAsync(RainbowBattleProcessFlagPointsEvent e, CancellationToken cancellation)
        {
            RainbowBattleParty rainbowParty = e.RainbowBattleParty;

            IReadOnlyDictionary<RainbowBattleFlagType, byte> redFlags = rainbowParty.RedFlags;
            IReadOnlyDictionary<RainbowBattleFlagType, byte> blueFlags = rainbowParty.BlueFlags;

            int redPointsToAdd = 0;
            int bluePointsToAdd = 0;

            foreach ((RainbowBattleFlagType flagType, byte count) in redFlags)
            {
                byte flagPoints = (byte)flagType;
                int counter = flagPoints * count;
                redPointsToAdd += counter;
            }

            foreach ((RainbowBattleFlagType flagType, byte count) in blueFlags)
            {
                byte flagPoints = (byte)flagType;
                int counter = flagPoints * count;
                bluePointsToAdd += counter;
            }

            rainbowParty.IncreaseRedPoints(redPointsToAdd);
            rainbowParty.IncreaseBluePoints(bluePointsToAdd);

            await _asyncEventPipeline.ProcessEventAsync(new RainbowBattleRefreshScoreEvent
            {
                RainbowBattleParty = rainbowParty
            });
        }
    }
}