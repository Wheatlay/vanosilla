using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Communication.Families;
using WingsAPI.Communication.Player;
using WingsAPI.Packets.Enums.Families;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Families.Event;
using WingsEmu.Game.InterChannel;
using WingsEmu.Game.Managers;

namespace Plugin.FamilyImpl
{
    public class FamilyReceiveInviteEventHandler : IAsyncEventProcessor<FamilyReceiveInviteEvent>
    {
        private readonly IFamilyInvitationService _familyInvitation;
        private readonly IGameLanguageService _languageService;
        private readonly ISessionManager _sessionManager;

        public FamilyReceiveInviteEventHandler(IGameLanguageService languageService, IFamilyInvitationService familyInvitation, ISessionManager sessionManager)
        {
            _languageService = languageService;
            _familyInvitation = familyInvitation;
            _sessionManager = sessionManager;
        }

        public async Task HandleAsync(FamilyReceiveInviteEvent e, CancellationToken cancellation)
        {
            if (e.Sender.PlayerEntity.IsInFamily())
            {
                return;
            }

            if (e.Sender.PlayerEntity.FamilyRequestBlocked)
            {
                await e.Sender.EmitEventAsync(new InterChannelSendInfoByCharIdEvent(e.SenderCharacterId, GameDialogKey.FAMILY_INFO_INVITATION_NOT_ALLOWED));
                return;
            }

            if (e.Sender.PlayerEntity.IsBlocking(e.SenderCharacterId))
            {
                await e.Sender.EmitEventAsync(new InterChannelSendInfoByCharIdEvent(e.SenderCharacterId, GameDialogKey.BLACKLIST_INFO_BLOCKED));
                return;
            }

            await _familyInvitation.SaveFamilyInvitationAsync(new FamilyInvitationSaveRequest
            {
                Invitation = new FamilyInvitation
                {
                    SenderId = e.SenderCharacterId,
                    SenderFamilyId = e.FamilyId,
                    TargetId = e.Sender.PlayerEntity.Id
                }
            });

            ClusterCharacterInfo sender = await _sessionManager.GetOnlineCharacterById(e.SenderCharacterId);
            string message = _languageService.GetLanguageFormat(GameDialogKey.FAMILY_DIALOG_ASK_JOIN_FAMILY, e.Sender.UserLanguage, e.FamilyName, sender?.Name ?? "?");

            e.Sender.SendDialog($"gjoin {((byte)FamilyJoinType.PreAccepted).ToString()} {e.SenderCharacterId.ToString()}",
                $"gjoin {((byte)FamilyJoinType.Rejected).ToString()} {e.SenderCharacterId.ToString()}", message);
        }
    }
}