using System.Threading.Tasks;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Mates.Events;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.ClientPackets;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.PacketHandling.Game.Basic;

public class SitPacketHandler : GenericGamePacketHandlerBase<SitPacket>
{
    private readonly IMeditationManager _meditationManager;

    public SitPacketHandler(IMeditationManager meditationManager) => _meditationManager = meditationManager;

    protected override async Task HandlePacketAsync(IClientSession session, SitPacket packet)
    {
        if (_meditationManager.HasMeditation(session.PlayerEntity))
        {
            _meditationManager.RemoveAllMeditation(session.PlayerEntity);
        }

        if (packet?.Users == null)
        {
            return;
        }

        bool syncWithPlayer = false;
        foreach (SitSubPacket subPacket in packet.Users)
        {
            if (subPacket.VisualType == VisualType.Player)
            {
                await session.RestAsync();
                syncWithPlayer = true;
                continue;
            }

            IMateEntity mateEntity = session.PlayerEntity.MateComponent.GetMate(x => x.Id == subPacket.UserId);
            if (mateEntity == null)
            {
                continue;
            }

            await session.EmitEventAsync(new MateRestEvent
            {
                MateEntity = mateEntity,
                Rest = syncWithPlayer ? session.PlayerEntity.IsSitting : !mateEntity.IsSitting
            });
        }
    }
}