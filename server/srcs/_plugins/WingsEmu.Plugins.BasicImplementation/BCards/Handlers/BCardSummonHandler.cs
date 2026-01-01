// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Collections.Generic;
using PhoenixLib.Events;
using WingsEmu.Game;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Monster.Event;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.BCards.Handlers;

public class BCardSummonHandler : IBCardEffectAsyncHandler
{
    private readonly IAsyncEventPipeline _eventPipeline;
    private readonly INpcMonsterManager _manager;
    private readonly IRandomGenerator _randomGenerator;

    public BCardSummonHandler(IRandomGenerator randomGenerator, IAsyncEventPipeline eventPipeline, INpcMonsterManager manager)
    {
        _randomGenerator = randomGenerator;
        _eventPipeline = eventPipeline;
        _manager = manager;
    }

    public BCardType HandledType => BCardType.Summons;

    public void Execute(IBCardEffectContext ctx)
    {
        if (ctx.Sender == null)
        {
            return;
        }

        if (ctx.Target == null)
        {
            return;
        }

        IBattleEntity sender = ctx.Sender;
        byte subType = ctx.BCard.SubType;

        int firstData = ctx.BCard.FirstData;
        int secondData = ctx.BCard.SecondData;
        int procChance = ctx.BCard.ProcChance;

        var summons = new List<ToSummon>();

        Position entityPosition = sender.Position;

        switch ((AdditionalTypes.Summons)subType)
        {
            case AdditionalTypes.Summons.Summons:
                for (int i = 0; i < firstData; i++)
                {
                    IMonsterData monsterToSummon = _manager.GetNpc(secondData);
                    if (monsterToSummon == null)
                    {
                        continue;
                    }

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
                        VNum = monsterToSummon.MonsterVNum,
                        SpawnCell = position,
                        IsMoving = monsterToSummon.CanWalk,
                        IsHostile = true
                    });
                }

                _eventPipeline.ProcessEventAsync(new MonsterSummonEvent(sender.MapInstance, summons, sender, showEffect: true)).ConfigureAwait(false).GetAwaiter().GetResult();
                break;
            case AdditionalTypes.Summons.SummonningChance:

                short posX = sender.Position.X;
                short posY = sender.Position.Y;

                posX += (short)_randomGenerator.RandomNumber(-3, 3);
                posY += (short)_randomGenerator.RandomNumber(-3, 3);

                if (sender.MapInstance.IsBlockedZone(posX, posY))
                {
                    posX = entityPosition.X;
                    posY = entityPosition.Y;
                }

                var newPosition = new Position(posX, posY);

                summons.Add(new ToSummon
                {
                    VNum = (short)secondData,
                    SpawnCell = newPosition,
                    IsMoving = true,
                    IsHostile = true,
                    SummonChance = (byte)Math.Abs(firstData)
                });
                _eventPipeline.ProcessEventAsync(new MonsterSummonEvent(sender.MapInstance, summons, showEffect: true)).ConfigureAwait(false).GetAwaiter().GetResult();
                break;
            case AdditionalTypes.Summons.SummonTrainingDummy:
            {
                summons.Add(new ToSummon
                {
                    VNum = (short)secondData,
                    SpawnCell = entityPosition,
                    IsMoving = true,
                    IsHostile = true,
                    SummonChance = (byte)Math.Abs(procChance)
                });

                _eventPipeline.ProcessEventAsync(new MonsterSummonEvent(sender.MapInstance, summons, showEffect: true)).ConfigureAwait(false).GetAwaiter().GetResult();
                break;
            }
            case AdditionalTypes.Summons.SummonUponDeathChance:
                summons.Add(new ToSummon
                {
                    VNum = (short)secondData,
                    SpawnCell = new Position(sender.PositionX, sender.PositionY),
                    IsMoving = true,
                    IsHostile = true,
                    SummonChance = (byte)Math.Abs(firstData)
                });
                _eventPipeline.ProcessEventAsync(new MonsterSummonEvent(sender.MapInstance, summons)).ConfigureAwait(false).GetAwaiter().GetResult();
                break;
            case AdditionalTypes.Summons.SummonUponDeath:
                for (short i = 0; i < firstData; i++)
                {
                    short senderPositionX = sender.Position.X;
                    short senderPositionY = sender.Position.Y;

                    senderPositionX += (short)_randomGenerator.RandomNumber(-2, 2);
                    senderPositionY += (short)_randomGenerator.RandomNumber(-2, 2);

                    if (sender.MapInstance.IsBlockedZone(senderPositionX, senderPositionY))
                    {
                        senderPositionX = entityPosition.X;
                        senderPositionY = entityPosition.Y;
                    }

                    summons.Add(new ToSummon
                    {
                        VNum = (short)secondData,
                        SpawnCell = new Position(senderPositionX, senderPositionY),
                        IsMoving = true,
                        IsHostile = true
                    });
                }

                _eventPipeline.ProcessEventAsync(new MonsterSummonEvent(sender.MapInstance, summons, sender)).ConfigureAwait(false).GetAwaiter().GetResult();
                break;
        }
    }
}