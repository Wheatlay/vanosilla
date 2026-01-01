using System;
using System.Threading.Tasks;
using Qmmands;
using WingsEmu.Commands.Checks;
using WingsEmu.Commands.Entities;
using WingsEmu.DTOs.Account;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Families.Event;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums.Families;

namespace Plugin.FamilyImpl.Commands
{
    [Name("family")]
    [RequireAuthority(AuthorityType.User)]
    public sealed class FamilyNostaleUiCommandsModule : SaltyModuleBase
    {
        private readonly IGameLanguageService _gameLanguage;

        public FamilyNostaleUiCommandsModule(IGameLanguageService gameLanguage) => _gameLanguage = gameLanguage;

        [Command("familyshout")]
        public async Task<SaltyCommandResult> FamilyShoutAsync([Remainder] string message)
        {
            await Context.Player.EmitEventAsync(new FamilyShoutEvent(message));
            return new SaltyCommandResult(true);
        }

        [Command("familydismiss")]
        public async Task<SaltyCommandResult> FamilyDismissAsync([Remainder] string nickname)
        {
            await Context.Player.EmitEventAsync(new FamilyRemoveMemberEvent(nickname));
            return new SaltyCommandResult(true);
        }

        [Command("familyleave")]
        public async Task<SaltyCommandResult> FamilyLeaveAsync()
        {
            if (!Context.Player.PlayerEntity.IsInFamily())
            {
                Context.Player.SendInfo(_gameLanguage.GetLanguage(GameDialogKey.FAMILY_INFO_NO_FAMILY, Context.Player.UserLanguage));
                return new SaltyCommandResult(false);
            }

            Context.Player.SendQnaPacket("gleave", _gameLanguage.GetLanguage(GameDialogKey.FAMILY_INFO_ASK_FAMILY_LEAVE, Context.Player.UserLanguage));
            return new SaltyCommandResult(true);
        }

        [Command("familymembers")]
        public async Task<SaltyCommandResult> FamilyListMembersAsync()
        {
            await Context.Player.EmitEventAsync(new FamilyListMembersEvent());
            return new SaltyCommandResult(true);
        }

        [Command("notice")]
        public async Task<SaltyCommandResult> FamilyNoticeAsync()
        {
            await Context.Player.EmitEventAsync(new FamilyNoticeMessageEvent(string.Empty, true));
            return new SaltyCommandResult(true);
        }

        [Command("notice")]
        public async Task<SaltyCommandResult> FamilyNoticeAsync([Remainder] string message)
        {
            await Context.Player.EmitEventAsync(new FamilyNoticeMessageEvent(message));
            return new SaltyCommandResult(true);
        }

        [Command("gender")]
        public async Task<SaltyCommandResult> ResetSexAsync(byte gender)
        {
            await Context.Player.EmitEventAsync(new FamilyChangeSexEvent(gender));
            return new SaltyCommandResult(true);
        }

        [Command("title")]
        public async Task<SaltyCommandResult> TitleChangeAsync(string nickname, FamilyTitle familyTitle)
        {
            await Context.Player.EmitEventAsync(new FamilyChangeTitleEvent(nickname, familyTitle));
            return new SaltyCommandResult(true);
        }

        [Command("today")]
        public async Task<SaltyCommandResult> TodayMessageAsync([Remainder] string message)
        {
            await Context.Player.EmitEventAsync(new FamilyTodayEvent(message));
            return new SaltyCommandResult(true);
        }

        [Command("familyinvite")]
        [Description("Invite player to family.")]
        public async Task<SaltyCommandResult> InviteFamilyAsync(
            [Remainder] [Description("Player nickname")]
            string nickname)
        {
            await Context.Player.EmitEventAsync(new FamilySendInviteEvent(nickname));
            return new SaltyCommandResult(true);
        }

        [Command("familydeputy")]
        public async Task<SaltyCommandResult> FamilyDeputyAsync(string sourceName, [Remainder] string targetName)
        {
            await Context.Player.EmitEventAsync(new FamilyChangeDeputyEvent(sourceName, targetName));
            return new SaltyCommandResult(true);
        }

        [Command("familydeputy")]
        public async Task<SaltyCommandResult> FamilyDeputyAsync([Remainder] string targetName)
        {
            await Context.Player.EmitEventAsync(new FamilyChangeAuthorityEvent(FamilyAuthority.Deputy, 0, 0, targetName));
            return new SaltyCommandResult(true);
        }

        [Command("familyhead")]
        public async Task<SaltyCommandResult> FamilyHeadAsync([Remainder] string nickname)
        {
            await Context.Player.EmitEventAsync(new FamilyChangeAuthorityEvent(FamilyAuthority.Head, 0, 0, nickname));
            return new SaltyCommandResult(true);
        }

        [Command("familykeeper")]
        public async Task<SaltyCommandResult> FamilyKeeperAsync(string familyKeeperChange, [Remainder] string nickname)
        {
            if (!Enum.TryParse(familyKeeperChange, out FamilyKeeperChange change))
            {
                return new SaltyCommandResult(false);
            }

            FamilyAuthority authority = change switch
            {
                FamilyKeeperChange.Dismiss => FamilyAuthority.Member,
                FamilyKeeperChange.Appointment => FamilyAuthority.Keeper
            };

            await Context.Player.EmitEventAsync(new FamilyChangeAuthorityEvent(authority, 0, 0, nickname));
            return new SaltyCommandResult(true);
        }

        [Command("familykeeper")]
        public async Task<SaltyCommandResult> FamilyKeeperAsync([Remainder] string nickname)
        {
            await Context.Player.EmitEventAsync(new FamilyChangeAuthorityEvent(FamilyAuthority.Keeper, 0, 0, nickname));
            return new SaltyCommandResult(true);
        }

        [Command("familydisband")]
        public async Task<SaltyCommandResult> FamilyDisbandAsync()
        {
            IClientSession session = Context.Player;
            if (!session.PlayerEntity.IsInFamily())
            {
                session.SendInfo(_gameLanguage.GetLanguage(GameDialogKey.FAMILY_INFO_NOT_IN_FAMILY, session.UserLanguage));
                return new SaltyCommandResult(false);
            }

            if (session.PlayerEntity.GetFamilyAuthority() != FamilyAuthority.Head)
            {
                session.SendInfo(_gameLanguage.GetLanguage(GameDialogKey.FAMILY_INFO_NOT_FAMILY_HEAD, session.UserLanguage));
                return new SaltyCommandResult(false);
            }

            session.SendQnaPacket("glrm 1", _gameLanguage.GetLanguage(GameDialogKey.FAMILY_DIALOG_ASK_DISMISS_FAMILY, session.UserLanguage));
            return new SaltyCommandResult(true);
        }
    }
}