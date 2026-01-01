using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Communication.Families;
using WingsAPI.Game.Extensions.Families;
using WingsEmu.DTOs.Maps;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Families.Event;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums.Chat;
using WingsEmu.Packets.Enums.Families;

namespace Plugin.FamilyImpl
{
    public class FamilyLeaveEventHandler : IAsyncEventProcessor<FamilyLeaveEvent>
    {
        private readonly IFamilyService _familyService;
        private readonly IGameLanguageService _gameLanguage;

        public FamilyLeaveEventHandler(IGameLanguageService gameLanguage, IFamilyService familyService)
        {
            _gameLanguage = gameLanguage;
            _familyService = familyService;
        }

        public async Task HandleAsync(FamilyLeaveEvent e, CancellationToken cancellation)
        {
            IClientSession session = e.Sender;

            if (!session.PlayerEntity.IsInFamily())
            {
                session.SendInfo(_gameLanguage.GetLanguage(GameDialogKey.FAMILY_INFO_NO_FAMILY, session.UserLanguage));
                return;
            }

            if (session.PlayerEntity.IsHeadOfFamily())
            {
                session.SendInfo(_gameLanguage.GetLanguage(GameDialogKey.FAMILY_DIALOG_HEAD_CANNOT_LEAVE_FAMILY, session.UserLanguage));
                return;
            }

            if (!session.CurrentMapInstance.HasMapFlag(MapFlags.IS_BASE_MAP))
            {
                session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.INFORMATION_SHOUTMESSAGE_MUST_BE_IN_CLASSIC_MAP, session.UserLanguage), MsgMessageType.Middle);
                return;
            }

            await session.FamilyAddLogAsync(FamilyLogType.MemberLeave, session.PlayerEntity.Name);
            await session.EmitEventAsync(new FamilyLeftEvent
            {
                FamilyId = session.PlayerEntity.Family.Id
            });

            await _familyService.RemoveMemberToFamilyAsync(new FamilyRemoveMemberRequest
            {
                CharacterId = session.PlayerEntity.Id,
                FamilyId = session.PlayerEntity.Family.Id
            });

            session.SendInfo(_gameLanguage.GetLanguage(GameDialogKey.FAMILY_INFO_LEAVE, session.UserLanguage));
        }
    }
}