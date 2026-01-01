using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsEmu.Core;
using WingsEmu.DTOs.Items;
using WingsEmu.Game;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Items;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.Event.Characters;

public class SpPerfectEventHandler : IAsyncEventProcessor<SpPerfectEvent>
{
    private readonly IGameLanguageService _gameLanguage;
    private readonly IRandomGenerator _randomGenerator;
    private readonly SpPerfectionConfiguration _spConfiguration;

    public SpPerfectEventHandler(IRandomGenerator randomGenerator, IGameLanguageService gameLanguageService, SpPerfectionConfiguration spConfiguration)
    {
        _randomGenerator = randomGenerator;
        _gameLanguage = gameLanguageService;
        _spConfiguration = spConfiguration;
    }

    public async Task HandleAsync(SpPerfectEvent e, CancellationToken cancellation)
    {
        if (e.InventoryItem.ItemInstance.Type != ItemInstanceType.SpecialistInstance)
        {
            return;
        }

        GameItemInstance sp = e.InventoryItem.ItemInstance;

        if (sp.GameItem.IsPartnerSpecialist)
        {
            return;
        }

        if (sp.Rarity == -2)
        {
            return;
        }

        if (sp.SpStoneUpgrade >= 100)
        {
            return;
        }

        IClientSession session = e.Sender;

        PerfUpgradeConfiguration configuration = _spConfiguration.PerfUpgradeConfigurations
            .FirstOrDefault(upgradeConfiguration =>
                upgradeConfiguration.SpPerfUpgradeRange.Minimum <= sp.SpStoneUpgrade
                && sp.SpStoneUpgrade <= upgradeConfiguration.SpPerfUpgradeRange.Maximum
            );

        if (configuration == null)
        {
            return;
        }

        int stoneVNum = GetStoneVnum(sp);

        if (!session.HasEnoughGold(configuration.GoldNeeded))
        {
            return;
        }

        if (!session.PlayerEntity.HasItem(stoneVNum, (short)configuration.StonesNeeded))
        {
            return;
        }

        session.PlayerEntity.RemoveGold(configuration.GoldNeeded);
        await session.RemoveItemFromInventory(stoneVNum, (short)configuration.StonesNeeded);

        int rnd = _randomGenerator.RandomNumber();

        var randomBag = new RandomBag<bool>(_randomGenerator);

        randomBag.AddEntry(true, configuration.SuccessChance);
        randomBag.AddEntry(false, 100 - configuration.SuccessChance);

        bool isSuccess = randomBag.GetRandom();


        if (isSuccess)
        {
            await PerfectSp(sp, session, configuration);
        }
        else
        {
            SendBothMessages(session, _gameLanguage.GetLanguage(GameDialogKey.PERFECTSP_MESSAGE_FAILURE, session.UserLanguage), true);
            await session.EmitEventAsync(new SpPerfectedEvent
            {
                Success = false,
                Sp = sp,
                SpPerfectionLevel = sp.SpStoneUpgrade
            });
        }

        session.SendInventoryAddPacket(e.InventoryItem);
        session.SendShopEndPacket(ShopEndType.Npc);
    }

    private async Task PerfectSp(ItemInstanceDTO spInstance, IClientSession session, PerfUpgradeConfiguration configuration)
    {
        Range<int> countRange = configuration.StatAmountRange;
        byte count = (byte)_randomGenerator.RandomNumber(countRange.Minimum, countRange.Maximum + 1);

        session.SendEffect(EffectType.UpgradeSuccess);

        var randomBag = new RandomBag<SpPerfStats>(_randomGenerator);
        foreach ((SpPerfStats stat, short chance) in _spConfiguration.StatProbabilityConfiguration)
        {
            byte statValue = stat switch
            {
                SpPerfStats.Attack => spInstance.SpDamage,
                SpPerfStats.Defense => spInstance.SpDefence,
                SpPerfStats.Element => spInstance.SpElement,
                SpPerfStats.HpMp => spInstance.SpHP,
                SpPerfStats.ResistanceFire => spInstance.SpFire,
                SpPerfStats.ResistanceWater => spInstance.SpWater,
                SpPerfStats.ResistanceLight => spInstance.SpLight,
                SpPerfStats.ResistanceDark => spInstance.SpDark
            };

            if (statValue >= 50)
            {
                continue;
            }

            randomBag.AddEntry(stat, chance);
        }

        SpPerfStats selectedStat = randomBag.GetRandom();
        GameDialogKey dialogSpUpgrade;

        switch (selectedStat)
        {
            case SpPerfStats.Attack:
                spInstance.SpDamage += count;
                dialogSpUpgrade = GameDialogKey.PERFECTSP_ATTACK;
                break;
            case SpPerfStats.Defense:
                spInstance.SpDefence += count;
                dialogSpUpgrade = GameDialogKey.PERFECTSP_DEFENSE;
                break;
            case SpPerfStats.Element:
                spInstance.SpElement += count;
                dialogSpUpgrade = GameDialogKey.PERFECTSP_ELEMENT;
                break;
            case SpPerfStats.HpMp:
                spInstance.SpHP += count;
                dialogSpUpgrade = GameDialogKey.PERFECTSP_HPMP;
                break;
            case SpPerfStats.ResistanceFire:
                spInstance.SpFire += count;
                dialogSpUpgrade = GameDialogKey.PERFECTSP_FIRE;
                break;
            case SpPerfStats.ResistanceWater:
                spInstance.SpWater += count;
                dialogSpUpgrade = GameDialogKey.PERFECTSP_WATER;
                break;
            case SpPerfStats.ResistanceLight:
                spInstance.SpLight += count;
                dialogSpUpgrade = GameDialogKey.PERFECTSP_LIGHT;
                break;
            case SpPerfStats.ResistanceDark:
                spInstance.SpDark += count;
                dialogSpUpgrade = GameDialogKey.PERFECTSP_SHADOW;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        SuccessMessageGenerator(session, GameDialogKey.PERFECTSP_MESSAGE_SUCCESS, dialogSpUpgrade, count);
        spInstance.SpStoneUpgrade++;
        await session.EmitEventAsync(new SpPerfectedEvent
        {
            Success = true,
            Sp = spInstance,
            SpPerfectionLevel = spInstance.SpStoneUpgrade
        });
    }

    private void SuccessMessageGenerator(IClientSession session, GameDialogKey baseDialog, GameDialogKey successType, byte count)
    {
        SendBothMessages(session, _gameLanguage.GetLanguageFormat(baseDialog, session.UserLanguage,
            _gameLanguage.GetLanguage(successType, session.UserLanguage), count.ToString()), false);
    }

    public void SendBothMessages(IClientSession session, string message, bool bad)
    {
        ChatMessageColorType color = bad ? ChatMessageColorType.Red : ChatMessageColorType.Green;

        session.SendChatMessage(message, color);
        session.SendMsg(message, MsgMessageType.Middle);
    }

    private int GetStoneVnum(GameItemInstance sp)
    {
        foreach (SpStoneLink spStonesLink in _spConfiguration.SpStonesLinks)
        {
            if (spStonesLink.SpVnums.Contains(sp.ItemVNum))
            {
                return spStonesLink.StoneVnum;
            }
        }

        return 0;
    }
}