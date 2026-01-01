using System;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.Game;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Entities.Event;
using WingsEmu.Game.Entities.Extensions;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Monster.Event;

namespace WingsEmu.Plugins.BasicImplementations.Entities;

public class MapJoinMonsterEntityEventHandler : IAsyncEventProcessor<MapJoinMonsterEntityEvent>
{
    private readonly IAsyncEventPipeline _asyncEventPipeline;
    private readonly IRandomGenerator _randomGenerator;

    public MapJoinMonsterEntityEventHandler(IRandomGenerator randomGenerator, IAsyncEventPipeline asyncEventPipeline)
    {
        _randomGenerator = randomGenerator;
        _asyncEventPipeline = asyncEventPipeline;
    }

    public async Task HandleAsync(MapJoinMonsterEntityEvent e, CancellationToken cancellation)
    {
        IMonsterEntity monsterEntity = e.MonsterEntity;

        short x = e.MapX ?? monsterEntity.PositionX;
        short y = e.MapY ?? monsterEntity.PositionY;

        if (monsterEntity.MapInstance.IsBlockedZone(x, y))
        {
            Position randomPosition = monsterEntity.MapInstance.GetRandomPosition();
            x = randomPosition.X;
            y = randomPosition.Y;
        }

        monsterEntity.ChangePosition(new Position(x, y));
        monsterEntity.FirstX = x;
        monsterEntity.FirstY = y;
        monsterEntity.NextTick = DateTime.UtcNow;
        monsterEntity.NextAttackReady = DateTime.UtcNow;
        monsterEntity.OnFirstDamageReceive = true;

        monsterEntity.NextTick += TimeSpan.FromMilliseconds(_randomGenerator.RandomNumber(1000));

        monsterEntity.MapInstance.AddMonster(monsterEntity);
        monsterEntity.ModeDeathsSinceRespawn = monsterEntity.MapInstance.MonsterDeathsOnMap();

        if (monsterEntity.ModeIsHpTriggered)
        {
            monsterEntity.ModeIsActive = false;
        }
        else
        {
            monsterEntity.MapInstance.ActivateMode(monsterEntity);
        }

        if (!monsterEntity.IsStillAlive || !monsterEntity.IsAlive())
        {
            monsterEntity.IsStillAlive = true;
            monsterEntity.Hp = monsterEntity.MaxHp;
            monsterEntity.Mp = monsterEntity.MaxMp;
        }

        monsterEntity.MapInstance.Broadcast(monsterEntity.GenerateIn(e.ShowEffect));

        await _asyncEventPipeline.ProcessEventAsync(new MonsterRespawnedEvent
        {
            Monster = monsterEntity
        });
    }
}