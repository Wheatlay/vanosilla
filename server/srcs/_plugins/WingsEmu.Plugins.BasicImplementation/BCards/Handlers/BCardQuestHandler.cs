using WingsEmu.Game;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Entities.Event;
using WingsEmu.Game.Managers.StaticData;

namespace WingsEmu.Plugins.BasicImplementations.BCards.Handlers;

public class BCardQuestHandler : IBCardEffectAsyncHandler
{
    private readonly IMonsterEntityFactory _monsterEntityFactory;
    private readonly INpcMonsterManager _monsterManager;
    private readonly IRandomGenerator _randomGenerator;

    public BCardQuestHandler(INpcMonsterManager monsterManager, IRandomGenerator randomGenerator, IMonsterEntityFactory monsterEntityFactory)
    {
        _monsterManager = monsterManager;
        _randomGenerator = randomGenerator;
        _monsterEntityFactory = monsterEntityFactory;
    }

    public BCardType HandledType => BCardType.Quest;

    public void Execute(IBCardEffectContext ctx)
    {
        IMonsterData sender;
        if (ctx.Sender is IMonsterEntity monsterEntity)
        {
            sender = monsterEntity;
        }
        else
        {
            return;
        }

        for (int i = 0; i < sender.VNumRequired; i++)
        {
            int posX = ctx.Sender.PositionX + _randomGenerator.RandomNumber(-1, 1);
            int posY = ctx.Sender.PositionY + _randomGenerator.RandomNumber(-1, 1);

            IMonsterEntity mapMonster = _monsterEntityFactory.CreateMonster(sender.SpawnMobOrColor, ctx.Sender.MapInstance, new MonsterEntityBuilder
            {
                IsHostile = true,
                IsWalkingAround = true
            });
            mapMonster.EmitEventAsync(new MapJoinMonsterEntityEvent(mapMonster, (short)posX, (short)posY, true));
        }
    }
}