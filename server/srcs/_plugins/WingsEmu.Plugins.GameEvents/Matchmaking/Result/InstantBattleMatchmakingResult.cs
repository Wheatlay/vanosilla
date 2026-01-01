using System;
using System.Collections.Generic;
using WingsEmu.Game._i18n;
using WingsEmu.Game.GameEvent.Configuration;
using WingsEmu.Game.GameEvent.Matchmaking.Result;
using WingsEmu.Game.Networking;

namespace WingsEmu.Plugins.GameEvents.Matchmaking.Result
{
    public class InstantBattleMatchmakingResult : IMatchmakingResult
    {
        public InstantBattleMatchmakingResult(List<Tuple<IGameEventConfiguration, List<IClientSession>>> sessions, Dictionary<GameDialogKey, List<IClientSession>> refusedSessions)
        {
            Sessions = sessions;
            RefusedSessions = refusedSessions;
        }

        public List<Tuple<IGameEventConfiguration, List<IClientSession>>> Sessions { get; }

        public Dictionary<GameDialogKey, List<IClientSession>> RefusedSessions { get; }
    }
}