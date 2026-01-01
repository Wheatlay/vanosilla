/*
using System.Linq;
using System.Threading.Tasks;
using WingsEmu.Core.Logging;
using WingsAPI.Game._Guri;
using WingsAPI.Game._Guri.Event;
using WingsAPI.Game.Managers;
using WingsAPI.Game.Networking;
using WingsAPI.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.Guri
{
    public class IcebreakerEventGuriHandler : IGuriHandler
    {
        public long GuriEffectId => 501;

        private readonly IServerManager _serverManager;
        private readonly IMapManager _mapManager;

        public IcebreakerEventGuriHandler(IServerManager serverManager, IMapManager mapManager)
        {
            _serverManager = serverManager;
            _mapManager = mapManager;
        }       

        public async Task ExecuteAsync(IClientSession Session, GuriEvent guriPacket)
        {
            if (!_serverManager.IceBreakerInWaiting && IceBreaker.Map.Sessions.Count() > IceBreaker.MaxAllowedPlayers)
            {
                Logger.Log.Debug($"The maximum number of players has been reached. GurriEffectId {GuriEffectId}");
                return;
            }

            await _mapManager.TeleportOnRandomPlaceInMapAsync(Session, IceBreaker.Map.MapInstanceId);
            var group = new WingsAPI.Game.Group(GroupType.IceBreaker);
            if (Session.Character.Group == null)
            {
                group.Characters.Add(Session);
                goto IceBreaker;
            }

            foreach (IClientSession session in Session.Character.Group.Characters)
            {
                group.Characters.Add(session);
            }

            IceBreaker:
            IceBreaker.AddGroup(group);
        }
    }
} */

