using System.Collections.Generic;
using System.Text;
using WingsEmu.DTOs.BCards;
using WingsEmu.Game;
using WingsEmu.Game.Act4;
using WingsEmu.Game.Act4.Configuration;
using WingsEmu.Game.Act4.Entities;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.BCards.Handlers;

public class BCardCalvinasHandler : IBCardEffectAsyncHandler
{
    private readonly Act4DungeonsConfiguration _act4DungeonsConfiguration;
    private readonly IDungeonManager _dungeonManager;
    private readonly IRandomGenerator _randomGenerator;

    public BCardCalvinasHandler(IDungeonManager dungeonManager, Act4DungeonsConfiguration act4DungeonsConfiguration, IRandomGenerator randomGenerator)
    {
        _dungeonManager = dungeonManager;
        _act4DungeonsConfiguration = act4DungeonsConfiguration;
        _randomGenerator = randomGenerator;
    }

    public BCardType HandledType => BCardType.LordCalvinas;

    public void Execute(IBCardEffectContext ctx)
    {
        IBattleEntity sender = ctx.Sender;
        BCardDTO bCard = ctx.BCard;

        switch ((AdditionalTypes.LordCalvinas)bCard.SubType)
        {
            case AdditionalTypes.LordCalvinas.InflictDamageAtLocation:
                var dragonCord = new StringBuilder();
                List<CalvinasDragon> calvinasDragons = new();

                int amountOfDragons = _randomGenerator.RandomNumber(1, 3);

                for (int i = 0; i < amountOfDragons; i++)
                {
                    int at = _randomGenerator.RandomNumber(0, 11);
                    int axis = _randomGenerator.RandomNumber(0, 2);

                    var newDragon = new CalvinasDragon
                    {
                        Axis = axis == 0 ? CoordType.X : CoordType.Y,
                        Size = 3,
                        At = (short)(at * 5),
                        Start = -50,
                        End = 400
                    };

                    calvinasDragons.Add(newDragon);
                }

                foreach (CalvinasDragon dragon in calvinasDragons)
                {
                    if (dragon.Axis == CoordType.X)
                    {
                        dragonCord.Append($"{dragon.Start} {dragon.At} {dragon.End} {dragon.At} ");
                    }
                    else
                    {
                        dragonCord.Append($"{dragon.At} {dragon.Start} {dragon.At} {dragon.End} ");
                    }
                }

                if (calvinasDragons.Count == 1)
                {
                    dragonCord.Append("0 0 0 0");
                }

                sender.MapInstance.Broadcast(sender.GenerateDragonPacket((byte)calvinasDragons.Count) + dragonCord);
                _dungeonManager.AddCalvinasDragons(sender.MapInstance.Id, new CalvinasState
                {
                    CalvinasDragonsList = calvinasDragons,
                    CastTime = sender.GenerateSkillCastTime(ctx.Skill)
                });
                break;
        }
    }
}