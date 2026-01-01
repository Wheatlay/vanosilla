// WingsEmu
// 
// Developed by NosWings Team

using System.Threading.Tasks;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.ClientPackets;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.PacketHandling.Game.Banks;

public class BankManagementPacketHandler : GenericGamePacketHandlerBase<GboxPacket>
{
    private readonly IBankReputationConfiguration _bankReputationConfiguration;
    private readonly IGameLanguageService _gameLanguage;
    private readonly IRankingManager _rankingManager;
    private readonly IReputationConfiguration _reputationConfiguration;
    private readonly IServerManager _serverManager;

    public BankManagementPacketHandler(IGameLanguageService gameLanguage, IServerManager serverManager, IReputationConfiguration reputationConfiguration,
        IBankReputationConfiguration bankReputationConfiguration, IRankingManager rankingManager)
    {
        _gameLanguage = gameLanguage;
        _serverManager = serverManager;
        _reputationConfiguration = reputationConfiguration;
        _bankReputationConfiguration = bankReputationConfiguration;
        _rankingManager = rankingManager;
    }

    protected override async Task HandlePacketAsync(IClientSession session, GboxPacket packet)
    {
        if (session.PlayerEntity.IsInExchange())
        {
            return;
        }

        if (session.PlayerEntity.HasShopOpened)
        {
            return;
        }

        if (!session.PlayerEntity.IsBankOpen)
        {
            await session.NotifyStrangeBehavior(StrangeBehaviorSeverity.ABUSING, "Tried to withdraw/deposit gold without opened bank.");
            return;
        }

        switch (packet.Type)
        {
            case BankActionType.Deposit:
                if (packet.Option == 0)
                {
                    session.SendQnaPacket($"gbox 1 {packet.Amount} 1", _gameLanguage.GetLanguageFormat(GameDialogKey.BANK_DIALOG_ASK_DEPOSIT, session.UserLanguage, packet.Amount));
                    return;
                }

                if (packet.Option == 1)
                {
                    if (packet.Amount <= 0)
                    {
                        return;
                    }

                    if (session.Account.BankMoney + packet.Amount * 1000 > _serverManager.MaxBankGold)
                    {
                        session.SendInfo(_gameLanguage.GetLanguage(GameDialogKey.BANK_MESSAGE_MAX_GOLD_REACHED, session.UserLanguage));
                        session.SendSMemo(SmemoType.BankError, _gameLanguage.GetLanguage(GameDialogKey.BANK_MESSAGE_MAX_GOLD_REACHED, session.UserLanguage));
                        return;
                    }

                    if (session.PlayerEntity.Gold < packet.Amount * 1000)
                    {
                        session.SendInfo(_gameLanguage.GetLanguage(GameDialogKey.INTERACTION_MESSAGE_NOT_ENOUGH_GOLD, session.UserLanguage));
                        session.SendSMemo(SmemoType.BankError, _gameLanguage.GetLanguage(GameDialogKey.INTERACTION_MESSAGE_NOT_ENOUGH_GOLD, session.UserLanguage));
                        return;
                    }

                    session.PlayerEntity.Gold -= packet.Amount * 1000;
                    session.Account.BankMoney += packet.Amount * 1000;
                    session.RefreshGold();
                    session.SendGbPacket(BankType.Deposit, _reputationConfiguration, _bankReputationConfiguration, _rankingManager.TopReputation);
                    session.SendSMemo(SmemoType.BankInfo, _gameLanguage.GetLanguageFormat(GameDialogKey.BANK_MESSAGE_DEPOSIT, session.UserLanguage, $"{packet.Amount}000"));
                    session.SendChatMessage(_gameLanguage.GetLanguageFormat(GameDialogKey.BANK_MESSAGE_BALANCE, session.UserLanguage, session.Account.BankMoney), ChatMessageColorType.Green);
                    session.SendSMemo(SmemoType.BankBalance, _gameLanguage.GetLanguageFormat(GameDialogKey.BANK_MESSAGE_BALANCE, session.UserLanguage, session.Account.BankMoney));
                }

                break;
            case BankActionType.Withdraw:
                if (packet.Option == 0)
                {
                    session.SendQnaPacket($"gbox 2 {packet.Amount} 1", _gameLanguage.GetLanguageFormat(GameDialogKey.BANK_DIALOG_ASK_WITHDRAW, session.UserLanguage, packet.Amount));
                    return;
                }

                if (packet.Option == 1)
                {
                    if (packet.Amount <= 0)
                    {
                        return;
                    }

                    if (session.PlayerEntity.Gold + packet.Amount * 1000 > _serverManager.MaxGold)
                    {
                        session.SendInfo(_gameLanguage.GetLanguage(GameDialogKey.BANK_INFO_TOO_MUCH_GOLD, session.UserLanguage));
                        session.SendSMemo(SmemoType.BankError, _gameLanguage.GetLanguage(GameDialogKey.BANK_INFO_TOO_MUCH_GOLD, session.UserLanguage));
                        return;
                    }

                    if (session.Account.BankMoney < packet.Amount * 1000)
                    {
                        session.SendInfo(_gameLanguage.GetLanguage(GameDialogKey.BANK_INFO_NOT_ENOUGH_FUNDS, session.UserLanguage));
                        session.SendSMemo(SmemoType.BankError, _gameLanguage.GetLanguage(GameDialogKey.BANK_INFO_NOT_ENOUGH_FUNDS, session.UserLanguage));
                        return;
                    }

                    if (!session.HasEnoughGold(session.PlayerEntity.GetBankPenalty(_reputationConfiguration, _bankReputationConfiguration, _rankingManager.TopReputation)))
                    {
                        session.SendInfo(_gameLanguage.GetLanguage(GameDialogKey.INTERACTION_MESSAGE_NOT_ENOUGH_GOLD, session.UserLanguage));
                        session.SendSMemo(SmemoType.BankError, _gameLanguage.GetLanguage(GameDialogKey.INTERACTION_MESSAGE_NOT_ENOUGH_GOLD, session.UserLanguage));
                        return;
                    }

                    session.PlayerEntity.Gold -= session.PlayerEntity.GetBankPenalty(_reputationConfiguration, _bankReputationConfiguration, _rankingManager.TopReputation);
                    session.Account.BankMoney -= packet.Amount * 1000;
                    session.PlayerEntity.Gold += packet.Amount * 1000;
                    session.RefreshGold();
                    session.SendGbPacket(BankType.Withdraw, _reputationConfiguration, _bankReputationConfiguration, _rankingManager.TopReputation);
                    session.SendSMemo(SmemoType.BankInfo, _gameLanguage.GetLanguageFormat(GameDialogKey.BANK_LOG_WITHDRAW_BANK, session.UserLanguage, packet.Amount));
                    session.SendChatMessage(_gameLanguage.GetLanguageFormat(GameDialogKey.BANK_MESSAGE_BALANCE, session.UserLanguage, session.Account.BankMoney), ChatMessageColorType.Green);
                    session.SendSMemo(SmemoType.BankBalance, _gameLanguage.GetLanguageFormat(GameDialogKey.BANK_MESSAGE_BALANCE, session.UserLanguage, session.Account.BankMoney));
                }

                break;
        }
    }
}