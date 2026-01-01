using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Monster.Event;
using WingsEmu.Game.Networking;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.CharacterLifetimeStats;

public class TotalMonstersKilledEventHandler : IAsyncEventProcessor<MonsterDeathEvent>
{
    public async Task HandleAsync(MonsterDeathEvent e, CancellationToken cancellation)
    {
        switch (e.Killer)
        {
            case IPlayerEntity player:
                player.LifetimeStats.TotalMonstersKilled++;
                break;

            case IMateEntity mate:
                mate.Owner.LifetimeStats.TotalMonstersKilled++;
                break;
            case IMonsterEntity monster:
                if (!monster.SummonerId.HasValue || monster.IsMateTrainer)
                {
                    break;
                }

                if (monster.SummonerType != VisualType.Player)
                {
                    break;
                }

                IClientSession summoner = monster.MapInstance.GetCharacterById(monster.SummonerId.Value)?.Session;
                if (summoner == null)
                {
                    break;
                }

                summoner.PlayerEntity.LifetimeStats.TotalMonstersKilled++;
                break;
        }
    }
}