// WingsEmu
// 
// Developed by NosWings Team

using System.Threading.Tasks;
using PhoenixLib.MultiLanguage;
using Qmmands;
using WingsEmu.Commands.Checks;
using WingsEmu.Commands.Entities;
using WingsEmu.DTOs.Account;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Groups.Events;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Relations;
using WingsEmu.Packets.ClientPackets;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.Essentials.Account;

[Name("Account")]
[Description("Module related to account commands.")]
[RequireAuthority(AuthorityType.User)]
public class AccountModule : SaltyModuleBase
{
    private readonly IGameLanguageService _language;
    private readonly IServerManager _manager;
    private readonly ISessionManager _sessionManager;

    public AccountModule(IServerManager manager, IGameLanguageService language, ISessionManager sessionManager)
    {
        _manager = manager;
        _language = language;
        _sessionManager = sessionManager;
    }

    [Command("language", "lang", "getlang", "getlanguage")]
    [Description("Check your language")]
    public async Task<SaltyCommandResult> GetLanguage()
    {
        IClientSession player = Context.Player;
        player.SendChatMessage($"{_language.GetLanguage(GameDialogKey.COMMAND_CHATMESSAGE_LANGUAGE_CURRENT, player.UserLanguage)} {player.UserLanguage.ToString()}", ChatMessageColorType.Yellow);
        return new SaltyCommandResult(true, "");
    }

    [Command("language", "lang", "setlang", "setlanguage")]
    [Description("Sets your language")]
    public async Task<SaltyCommandResult> SetAccountLanguage([Description("EN, FR, CZ, PL, DE, IT, ES, TR")] RegionLanguageType languageType)
    {
        IClientSession player = Context.Player;
        player.Account.ChangeLanguage(languageType);
        player.SendChatMessage($"{_language.GetLanguage(GameDialogKey.COMMAND_CHATMESSAGE_LANGUAGE_CHANGED, player.UserLanguage)} {languageType.ToString()}", ChatMessageColorType.Yellow);

        return new SaltyCommandResult(true, "");
    }

    [Command("invite")]
    public async Task<SaltyCommandResult> InviteAsync([Remainder] string nickname)
    {
        await Context.Player.EmitEventAsync(new InviteJoinMinilandEvent(nickname, true));
        return new SaltyCommandResult(true);
    }

    [Command("fl")]
    [Description("Send friend request to the player")]
    public async Task<SaltyCommandResult> FriendRequestAsync([Remainder] [Description("Player nickname")] string nickname)
    {
        IClientSession session = Context.Player;
        IPlayerEntity target = _sessionManager.GetSessionByCharacterName(nickname)?.PlayerEntity;
        if (target == null)
        {
            session.SendMsg(_language.GetLanguage(GameDialogKey.INFORMATION_MESSAGE_USER_NOT_FOUND, session.UserLanguage), MsgMessageType.Middle);
            return new SaltyCommandResult(true);
        }

        await session.EmitEventAsync(new RelationFriendEvent
        {
            RequestType = FInsPacketType.INVITE,
            CharacterId = target.Id
        });

        return new SaltyCommandResult(true);
    }

    [Command("bl")]
    [Description("Block the player.")]
    public async Task<SaltyCommandResult> BlockRequestAsync([Remainder] [Description("Player nickname")] string nickname)
    {
        IClientSession session = Context.Player;
        IPlayerEntity target = _sessionManager.GetSessionByCharacterName(nickname)?.PlayerEntity;
        if (target == null)
        {
            session.SendMsg(_language.GetLanguage(GameDialogKey.INFORMATION_MESSAGE_USER_NOT_FOUND, session.UserLanguage), MsgMessageType.Middle);
            return new SaltyCommandResult(true);
        }

        await session.EmitEventAsync(new RelationBlockEvent
        {
            CharacterId = target.Id
        });

        return new SaltyCommandResult(true);
    }

    [Command("pinv")]
    [Description("Send group request to the player")]
    public async Task<SaltyCommandResult> GroupRequestAsync([Remainder] [Description("Player's nickname")] string nickname)
    {
        IClientSession session = Context.Player;
        IPlayerEntity target = _sessionManager.GetSessionByCharacterName(nickname)?.PlayerEntity;
        if (target == null)
        {
            session.SendMsg(_language.GetLanguage(GameDialogKey.INFORMATION_MESSAGE_USER_NOT_FOUND, session.UserLanguage), MsgMessageType.Middle);
            return new SaltyCommandResult(true);
        }

        await session.EmitEventAsync(new GroupActionEvent
        {
            RequestType = GroupRequestType.Requested,
            CharacterId = target.Id
        });

        return new SaltyCommandResult(true);
    }

    [Command("fcancel")]
    public async Task<SaltyCommandResult> CancelFightMode()
    {
        Context.Player.PlayerEntity.CancelCastingSkill();
        return new SaltyCommandResult(true);
    }
}