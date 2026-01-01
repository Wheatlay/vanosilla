using System;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Managers;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.Event.Characters;

public class GenerateReputationEventHandler : IAsyncEventProcessor<GenerateReputationEvent>
{
    private readonly IGameLanguageService _languageService;
    private readonly GameMinMaxConfiguration _minMaxConfiguration;
    private readonly IRankingManager _rankingManager;
    private readonly IReputationConfiguration _reputationConfiguration;
    private readonly IServerManager _serverManager;

    public GenerateReputationEventHandler(IServerManager serverManager, IGameLanguageService languageService, GameMinMaxConfiguration minMaxConfiguration,
        IReputationConfiguration reputationConfiguration, IRankingManager rankingManager)
    {
        _serverManager = serverManager;
        _languageService = languageService;
        _minMaxConfiguration = minMaxConfiguration;
        _reputationConfiguration = reputationConfiguration;
        _rankingManager = rankingManager;
    }

    public async Task HandleAsync(GenerateReputationEvent e, CancellationToken cancellation)
    {
        IPlayerEntity character = e.Sender.PlayerEntity;
        long amount = e.Amount * _serverManager.ReputRate;

        if (character.Reput <= 0 && amount <= 0)
        {
            return;
        }

        long oldReput = character.Reput;
        character.Reput += amount;

        if (character.Reput < _minMaxConfiguration.MinReputation)
        {
            character.Reput = _minMaxConfiguration.MinReputation;
        }

        if (_minMaxConfiguration.MaxReputation < character.Reput)
        {
            character.Reput = _minMaxConfiguration.MaxReputation;
        }

        character.Session.RefreshReputation(_reputationConfiguration, _rankingManager.TopReputation);
        bool decrease = amount < 0;

        if (!e.SendMessage)
        {
            return;
        }

        character.Session.SendChatMessage(
            _languageService.GetLanguageFormat(decrease ? GameDialogKey.INFORMATION_CHATMESSAGE_REPUT_DECREASE : GameDialogKey.INFORMATION_CHATMESSAGE_REPUT_INCREASE, character.Session.UserLanguage,
                Math.Abs(oldReput - character.Reput)),
            decrease ? ChatMessageColorType.Red : ChatMessageColorType.Green);
    }
}