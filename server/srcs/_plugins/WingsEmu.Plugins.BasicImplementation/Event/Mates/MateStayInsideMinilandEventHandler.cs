using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.Groups;
using WingsEmu.Game;
using WingsEmu.Game._ItemUsage.Configuration;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Extensions.Mates;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Mates.Events;
using WingsEmu.Game.Networking;

namespace WingsEmu.Plugins.BasicImplementations.Event.Mates;

public class MateStayInsideMinilandEventHandler : IAsyncEventProcessor<MateStayInsideMinilandEvent>
{
    private readonly IMateBuffConfigsContainer _mateBuffConfigsContainer;
    private readonly IRandomGenerator _randomGenerator;
    private readonly ISpPartnerConfiguration _spPartnerConfiguration;

    public MateStayInsideMinilandEventHandler(IRandomGenerator randomGenerator, IMateBuffConfigsContainer mateBuffConfigsContainer, ISpPartnerConfiguration spPartnerConfiguration)
    {
        _randomGenerator = randomGenerator;
        _mateBuffConfigsContainer = mateBuffConfigsContainer;
        _spPartnerConfiguration = spPartnerConfiguration;
    }

    public async Task HandleAsync(MateStayInsideMinilandEvent e, CancellationToken cancellation)
    {
        IClientSession session = e.Sender;
        IMateEntity mateEntity = e.MateEntity;

        if (mateEntity == null)
        {
            return;
        }

        if (session.PlayerEntity.Miniland == null)
        {
            return;
        }

        if (!mateEntity.IsAlive())
        {
            mateEntity.Hp = 1;
        }

        if (session.PlayerEntity.Miniland.IsBlockedZone(mateEntity.PositionX, mateEntity.PositionY))
        {
            Position cell = mateEntity.NewMinilandMapCell(_randomGenerator);
            mateEntity.MinilandX = cell.X;
            mateEntity.MinilandY = cell.Y;
        }

        if (session.PlayerEntity.Miniland.IsBlockedZone(mateEntity.MinilandX, mateEntity.MinilandY))
        {
            Position cell = mateEntity.NewMinilandMapCell(_randomGenerator);
            mateEntity.MinilandX = cell.X;
            mateEntity.MinilandY = cell.Y;
        }

        mateEntity.IsTeamMember = false;
        mateEntity.ChangePosition(new Position(mateEntity.MinilandX, mateEntity.MinilandY));

        if (!e.IsOnCharacterEnter)
        {
            await mateEntity.RemovePartnerSp();
            mateEntity.RefreshPartnerSkills();
            await mateEntity.RemoveAllBuffsAsync(true);
            session.RemovePetBuffs(mateEntity, _mateBuffConfigsContainer);
        }

        session.SendScpPackets();
        session.SendScnPackets();
        session.RefreshParty(_spPartnerConfiguration);
    }
}