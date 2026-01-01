using System;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.DTOs.Bonus;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Bazaar.Events;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Maps;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.Bazaar;

public class BazaarOpenUiEventHandler : IAsyncEventProcessor<BazaarOpenUiEvent>
{
    private readonly IGameLanguageService _languageService;

    public BazaarOpenUiEventHandler(IGameLanguageService languageService) => _languageService = languageService;

    public async Task HandleAsync(BazaarOpenUiEvent e, CancellationToken cancellation)
    {
        if (e.Sender.CurrentMapInstance?.MapInstanceType == MapInstanceType.TimeSpaceInstance)
        {
            return;
        }

        CharacterStaticBonusDto medalBonus =
            e.Sender.PlayerEntity.GetStaticBonus(x => x.StaticBonusType == StaticBonusType.BazaarMedalGold || x.StaticBonusType == StaticBonusType.BazaarMedalSilver);

        if (e.ThroughMedal && medalBonus == null)
        {
            e.Sender.SendInfo(_languageService.GetLanguage(GameDialogKey.BAZAAR_INFO_NEEDS_MEDAL, e.Sender.UserLanguage));
            return;
        }

        MedalType medal = medalBonus == null ? MedalType.None : medalBonus.StaticBonusType == StaticBonusType.BazaarMedalGold ? MedalType.Gold : MedalType.Silver;

        int time = medalBonus == null ? 0 : medalBonus.DateEnd == null ? 999 : (int)(medalBonus.DateEnd.Value - DateTime.UtcNow).TotalHours;

        e.Sender.SendMsg(_languageService.GetLanguage(GameDialogKey.BAZAAR_SHOUTMESSAGE_NOTICE_BAZAAR, e.Sender.UserLanguage), MsgMessageType.Middle);
        e.Sender.OpenNosBazaarUi(medal, time);
    }
}