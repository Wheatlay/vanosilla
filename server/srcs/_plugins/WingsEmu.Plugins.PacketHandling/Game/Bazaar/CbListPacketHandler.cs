using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WingsEmu.DTOs.Bonus;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Bazaar.Configuration;
using WingsEmu.Game.Bazaar.Events;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.ClientPackets;

namespace WingsEmu.Plugins.PacketHandling.Game.Bazaar;

public class CbListPacketHandler : GenericGamePacketHandlerBase<CbListPacket>
{
    private readonly BazaarConfiguration _bazaarConfiguration;
    private readonly IGameLanguageService _languageService;

    public CbListPacketHandler(IGameLanguageService languageService, BazaarConfiguration bazaarConfiguration)
    {
        _languageService = languageService;
        _bazaarConfiguration = bazaarConfiguration;
    }

    protected override async Task HandlePacketAsync(IClientSession session, CbListPacket packet)
    {
        if (session.IsActionForbidden())
        {
            session.CloseNosBazaarUi();
            return;
        }

        if (session.PlayerEntity.IsInExchange())
        {
            session.CloseNosBazaarUi();
            return;
        }

        if (session.PlayerEntity.HasShopOpened)
        {
            session.CloseNosBazaarUi();
            return;
        }

        if (session.PlayerEntity.IsShopping)
        {
            session.CloseNosBazaarUi();
            return;
        }

        INpcEntity getNosBazaarNpc = session.CurrentMapInstance.GetPassiveNpcs().FirstOrDefault(x => x.ShopNpc is { ShopType: (byte)NpcShopType.NOS_BAZAAR });
        if (!session.PlayerEntity.HaveStaticBonus(StaticBonusType.BazaarMedalSilver) && !session.PlayerEntity.HaveStaticBonus(StaticBonusType.BazaarMedalGold) && getNosBazaarNpc == null)
        {
            session.CloseNosBazaarUi();
            await session.NotifyStrangeBehavior(StrangeBehaviorSeverity.DANGER, "Tried to open NosBazaar without a medal.");
            return;
        }

        DateTime currentDate = DateTime.UtcNow;
        if (session.PlayerEntity.LastBuySearchBazaarRefresh > currentDate)
        {
            return;
        }

        session.PlayerEntity.LastBuySearchBazaarRefresh = currentDate.AddSeconds(_bazaarConfiguration.DelayServerBetweenRequestsInSecs);

        if (packet.ItemVNumFilter == null)
        {
            return;
        }

        string[] splitedString = packet.ItemVNumFilter.Split(' ');

        List<int> list = null;

        for (int i = 0; i < splitedString.Length; i++)
        {
            short value = Convert.ToInt16(splitedString[i]);
            if (i == 0)
            {
                i += value + 1;
                continue;
            }

            list ??= new List<int>();
            list.Add(value);
        }

        await session.EmitEventAsync(new BazaarSearchItemsEvent(packet.Index, packet.CategoryFilterType, packet.SubTypeFilter, packet.LevelFilter, packet.RareFilter, packet.UpgradeFilter,
            packet.OrderFilter, list));
    }
}