using System.Collections.Generic;
using PhoenixLib.Events;
using WingsEmu.Game;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Monster.Event;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.BCards.Handlers;

public class BCardDestroyerHandler : IBCardEffectAsyncHandler
{
    private readonly IAsyncEventPipeline _eventPipeline;
    private readonly IRandomGenerator _randomGenerator;

    public BCardDestroyerHandler(IRandomGenerator randomGenerator, IAsyncEventPipeline eventPipeline)
    {
        _randomGenerator = randomGenerator;
        _eventPipeline = eventPipeline;
    }

    public BCardType HandledType => BCardType.SecondSPCard;

    public async void Execute(IBCardEffectContext ctx)
    {
        IBattleEntity sender = ctx.Sender;

        if (!(ctx.Sender is IPlayerEntity character))
        {
            return;
        }

        byte subType = ctx.BCard.SubType;

        int firstData = ctx.BCard.FirstData;
        int secondData = ctx.BCard.SecondData;

        var summons = new List<ToSummon>();

        Position entityPosition = sender.Position;

        switch ((AdditionalTypes.SecondSPCard)subType)
        {
            case AdditionalTypes.SecondSPCard.PlantBomb:
                summons.Add(new ToSummon
                {
                    VNum = (short)secondData,
                    SpawnCell = character.Position,
                    IsMoving = false,
                    IsHostile = false
                });
                _eventPipeline.ProcessEventAsync(new MonsterSummonEvent(character.MapInstance, summons, character)).ConfigureAwait(false).GetAwaiter().GetResult();
                break;
            case AdditionalTypes.SecondSPCard.PlantSelfDestructionBomb:
                for (int i = 0; i < firstData; i++)
                {
                    short x = entityPosition.X;
                    short y = entityPosition.Y;

                    x += (short)_randomGenerator.RandomNumber(-3, 3);
                    y += (short)_randomGenerator.RandomNumber(-3, 3);
                    if (sender.MapInstance.IsBlockedZone(x, y))
                    {
                        x = entityPosition.X;
                        y = entityPosition.Y;
                    }

                    var position = new Position(x, y);
                    summons.Add(new ToSummon
                    {
                        VNum = (short)secondData,
                        SpawnCell = position,
                        IsHostile = true,
                        IsMoving = true
                    });
                }

                await _eventPipeline.ProcessEventAsync(new MonsterSummonEvent(character.MapInstance, summons, character));
                break;
        }
    }
}