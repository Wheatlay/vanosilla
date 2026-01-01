using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.PacketGeneration;
using WingsEmu.Game;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Families.Enum;
using WingsEmu.Game.Families.Event;
using WingsEmu.Game.GameEvent.InstantBattle;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Portals;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;
using WingsEmu.Plugins.GameEvents.Configuration.InstantBattle;
using WingsEmu.Plugins.GameEvents.DataHolder;
using WingsEmu.Plugins.GameEvents.Event.InstantBattle;

namespace WingsEmu.Plugins.GameEvents.EventHandler.InstantBattle
{
    public class InstantBattleCompleteEventHandler : IAsyncEventProcessor<InstantBattleCompleteEvent>
    {
        private readonly IGameLanguageService _gameLanguage;
        private readonly GameMinMaxConfiguration _minMaxConfiguration;
        private readonly IPortalFactory _portalFactory;
        private readonly IRankingManager _rankingManager;
        private readonly IReputationConfiguration _reputationConfiguration;

        public InstantBattleCompleteEventHandler(IGameLanguageService gameLanguage, GameMinMaxConfiguration minMaxConfiguration, IReputationConfiguration reputationConfiguration,
            IPortalFactory portalFactory, IRankingManager rankingManager)
        {
            _gameLanguage = gameLanguage;
            _minMaxConfiguration = minMaxConfiguration;
            _reputationConfiguration = reputationConfiguration;
            _portalFactory = portalFactory;
            _rankingManager = rankingManager;
        }

        public async Task HandleAsync(InstantBattleCompleteEvent e, CancellationToken cancellation)
        {
            IMapInstance map = e.Instance.MapInstance;
            InstantBattleReward reward = e.Instance.InternalConfiguration.Reward;
            InstantBattleInstance instance = e.Instance;

            e.Instance.Finished = true;

            await map.BroadcastAsync(async x =>
            {
                string message = _gameLanguage.GetLanguage(GameDialogKey.INSTANT_COMBAT_SHOUTMESSAGE_SUCCEEDED, x.UserLanguage);
                return x.GenerateMsgPacket(message, MsgMessageType.Middle);
            });

            foreach (IClientSession session in map.Sessions)
            {
                IPlayerEntity character = session.PlayerEntity;
                bool isHeroic = 0 < instance.InternalConfiguration.Requirements.HeroicLevel?.Minimum;

                long gold = reward.GoldMultiplier * (isHeroic ? character.HeroLevel : character.Level);
                long reputation = reward.ReputationMultiplier * (isHeroic ? character.HeroLevel : character.Level);
                int specialistPoint = reward.SpPointsMultiplier * (isHeroic ? character.HeroLevel : character.Level);
                int familyExperience = reward.FamilyExperience;
                int dignity = reward.Dignity;

                await session.EmitEventAsync(new InstantBattleWonEvent());
                await session.EmitEventAsync(new GenerateGoldEvent(gold));

                await session.EmitEventAsync(new GenerateReputationEvent
                {
                    Amount = (int)reputation,
                    SendMessage = true
                });

                character.SpPointsBonus += specialistPoint;
                if (character.Family != null)
                {
                    await character.Session.EmitEventAsync(new FamilyAddExperienceEvent(familyExperience, FamXpObtainedFromType.InstantCombat));
                }

                character.AddDignity(dignity, _minMaxConfiguration, _gameLanguage, _reputationConfiguration, _rankingManager.TopReputation);
                session.SendChatMessage(session.GetLanguageFormat(GameDialogKey.INFORMATION_CHATMESSAGE_WIN_SP_POINT, specialistPoint), ChatMessageColorType.Green);

                character.Hp = session.PlayerEntity.MaxHp;
                character.Mp = session.PlayerEntity.MaxMp;

                if (character.SpPointsBonus > _minMaxConfiguration.MaxSpAdditionalPoints)
                {
                    character.SpPointsBonus = _minMaxConfiguration.MaxSpAdditionalPoints;
                }

                session.RefreshSpPoint();
                session.RefreshStat();
                session.RefreshStatInfo();
            }

            var pos = new Position(e.Instance.InternalConfiguration.ReturnPortalX, e.Instance.InternalConfiguration.ReturnPortalY);
            IPortalEntity portal = _portalFactory.CreatePortal(PortalType.TSNormal, map, pos, map, pos);
            map.AddPortalToMap(portal);
        }
    }
}