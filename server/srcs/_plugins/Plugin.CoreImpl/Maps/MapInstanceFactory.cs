using PhoenixLib.Events;
using WingsEmu.Game;
using WingsEmu.Game._ECS;
using WingsEmu.Game._i18n;
using WingsEmu.Game._ItemUsage.Configuration;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Portals;
using WingsEmu.Game.Skills;

namespace Plugin.CoreImpl.Maps
{
    public class MapInstanceFactory : IMapInstanceFactory
    {
        private readonly IAsyncEventPipeline _asyncEventPipeline;
        private readonly IBCardEffectHandlerContainer _bCardEffectHandlerContainer;
        private readonly IBuffFactory _buffFactory;
        private readonly IGameItemInstanceFactory _gameItemInstanceFactory;
        private readonly GameMinMaxConfiguration _gameMinMaxConfiguration;
        private readonly IGameLanguageService _languageService;
        private readonly IMeditationManager _meditationManager;
        private readonly IMonsterTalkingConfig _monsterTalkingConfig;
        private readonly IPortalFactory _portalFactory;
        private readonly IRandomGenerator _randomGenerator;
        private readonly ISkillsManager _skillsManager;
        private readonly ISpPartnerConfiguration _spPartnerConfiguration;
        private readonly ISpyOutManager _spyOutManager;
        private readonly ITickManager _tickManager;

        public MapInstanceFactory(ITickManager tickManager, GameMinMaxConfiguration gameMinMaxConfiguration,
            ISpyOutManager spyOutManager, ISkillsManager skillsManager, IGameLanguageService languageService,
            IMeditationManager meditationManager, IAsyncEventPipeline asyncEventPipeline, IRandomGenerator randomGenerator, IBCardEffectHandlerContainer bCardEffectHandlerContainer,
            IBuffFactory buffFactory, IPortalFactory portalFactory, IGameItemInstanceFactory gameItemInstanceFactory, ISpPartnerConfiguration spPartnerConfiguration,
            IMonsterTalkingConfig monsterTalkingConfig)
        {
            _tickManager = tickManager;
            _gameMinMaxConfiguration = gameMinMaxConfiguration;
            _spyOutManager = spyOutManager;
            _skillsManager = skillsManager;
            _languageService = languageService;
            _meditationManager = meditationManager;
            _asyncEventPipeline = asyncEventPipeline;
            _randomGenerator = randomGenerator;
            _bCardEffectHandlerContainer = bCardEffectHandlerContainer;
            _buffFactory = buffFactory;
            _portalFactory = portalFactory;
            _gameItemInstanceFactory = gameItemInstanceFactory;
            _spPartnerConfiguration = spPartnerConfiguration;
            _monsterTalkingConfig = monsterTalkingConfig;
        }

        public IMapInstance CreateMap(Map map, MapInstanceType mapInstanceType) =>
            new MapInstance(map, mapInstanceType, _tickManager, _gameMinMaxConfiguration, _spyOutManager, _skillsManager, _languageService, _meditationManager,
                _asyncEventPipeline, _randomGenerator, _bCardEffectHandlerContainer, _buffFactory, _portalFactory, _gameItemInstanceFactory, _spPartnerConfiguration, _monsterTalkingConfig);
    }
}