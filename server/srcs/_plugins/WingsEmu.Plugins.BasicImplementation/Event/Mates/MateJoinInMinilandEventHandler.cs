using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.Groups;
using WingsAPI.Game.Extensions.Quicklist;
using WingsEmu.Game._i18n;
using WingsEmu.Game._ItemUsage.Configuration;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Mates.Events;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.BasicImplementations.Event.Mates;

public class MateJoinInMinilandEventHandler : IAsyncEventProcessor<MateJoinInMinilandEvent>
{
    private readonly IBuffFactory _buffFactory;
    private readonly IGameLanguageService _gameLanguage;
    private readonly IMateBuffConfigsContainer _mateBuffConfigsContainer;
    private readonly ISpPartnerConfiguration _spPartnerConfiguration;

    public MateJoinInMinilandEventHandler(IBuffFactory buffFactory, IMateBuffConfigsContainer mateBuffConfigsContainer, IGameLanguageService gameLanguage,
        ISpPartnerConfiguration spPartnerConfiguration)
    {
        _buffFactory = buffFactory;
        _mateBuffConfigsContainer = mateBuffConfigsContainer;
        _gameLanguage = gameLanguage;
        _spPartnerConfiguration = spPartnerConfiguration;
    }

    public async Task HandleAsync(MateJoinInMinilandEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;
        IMateEntity mateEntity = e.MateEntity;

        if (mateEntity == null)
        {
            return;
        }

        if (mateEntity.IsTeamMember)
        {
            return;
        }

        if (!session.IsGameMaster())
        {
            if (mateEntity.Level > session.PlayerEntity.Level)
            {
                session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.PET_SHOUTMESSAGE_HIGHER_LEVEL, session.UserLanguage), MsgMessageType.Middle);
                return;
            }

            if (session.PlayerEntity.GetDignityIco() == 6)
            {
                session.SendMsg(session.GetLanguage(GameDialogKey.PET_SHOUTMESSAGE_DIGNITY_LOW), MsgMessageType.Middle);
                return;
            }
        }

        IMateEntity teammate = session.PlayerEntity.MateComponent.GetMate(s => s.MateType == mateEntity.MateType && s.IsTeamMember);
        if (teammate != null)
        {
            await session.EmitEventAsync(new MateStayInsideMinilandEvent { MateEntity = teammate });
        }

        mateEntity.IsTeamMember = true;
        mateEntity.ChangePosition(new Position(mateEntity.MinilandX, mateEntity.MinilandY));

        session.SendScpStcPacket();

        switch (mateEntity.MateType)
        {
            case MateType.Pet:
                await session.AddPetBuff(mateEntity, _mateBuffConfigsContainer, _buffFactory);
                break;
            case MateType.Partner when mateEntity.MonsterSkills?.Count != 0:
                session.RefreshSkillList();
                session.RefreshQuicklist();
                break;
        }

        session.SendScnPackets();
        session.SendScpPackets();
        session.SendPClearPacket();
        session.SendScnPackets();
        session.SendScpPackets();
        session.RefreshParty(_spPartnerConfiguration);
    }
}