// WingsEmu
// 
// Developed by NosWings Team

using System.Collections.Generic;
using WingsEmu.Game._enum;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Mates;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.BCards.Handlers;

public class BCardSpecialBehaviorHandler : IBCardEffectAsyncHandler
{
    private readonly IBuffFactory _buffFactory;

    public BCardSpecialBehaviorHandler(IBuffFactory buffFactory) => _buffFactory = buffFactory;

    public BCardType HandledType => BCardType.SpecialBehaviour;

    public void Execute(IBCardEffectContext ctx)
    {
        IBattleEntity target = ctx.Target;
        IBattleEntity sender = ctx.Sender;
        byte subType = ctx.BCard.SubType;
        int firstData = ctx.BCard.FirstData;
        int secondData = ctx.BCard.SecondData;

        switch ((AdditionalTypes.SpecialBehaviour)subType)
        {
            case AdditionalTypes.SpecialBehaviour.InflictOnTeam:
                IEnumerable<IBattleEntity> allies = target.Position.GetAlliesInRange(target, (byte)firstData);
                foreach (IBattleEntity entity in allies)
                {
                    if (entity.BuffComponent.HasBuff(secondData))
                    {
                        continue;
                    }

                    if (!ShouldGiveDebuff(target, entity))
                    {
                        continue;
                    }

                    Buff buff = _buffFactory.CreateBuff(secondData, sender);
                    entity.AddBuffAsync(buff);
                }

                break;
            case AdditionalTypes.SpecialBehaviour.TeleportRandom:

                Position newPosition = sender.MapInstance.GetRandomPosition();
                sender.TeleportOnMap(newPosition.X, newPosition.Y);

                break;
            case AdditionalTypes.SpecialBehaviour.JumpToEveryObject:

                if (sender is not IMonsterEntity monsterEntity)
                {
                    return;
                }

                if (monsterEntity.Target == null)
                {
                    return;
                }

                IBattleEntity monsterTarget = monsterEntity.Target;
                IEnumerable<IMonsterEntity> monsters = monsterEntity.MapInstance.GetAliveMonstersInRange(monsterEntity.Position, (byte)firstData);
                foreach (IMonsterEntity monster in monsters)
                {
                    if (monster.Id == monsterEntity.Id)
                    {
                        continue;
                    }

                    monster.MapInstance.AddEntityToTargets(monster, monsterTarget);
                    if (monsterTarget is not IPlayerEntity playerEntity)
                    {
                        continue;
                    }

                    playerEntity.Session.SendEffectEntity(monster, EffectType.TargetedByOthers);
                }

                break;
        }
    }

    private bool ShouldGiveDebuff(IBattleEntity debuffer, IBattleEntity receiver)
    {
        bool give = debuffer switch
        {
            IPlayerEntity player when receiver is IPlayerEntity receiverPlayer => player.IsInGroupOf(receiverPlayer)
                || player.Family != null && receiverPlayer.Family != null && player.Family.Id == receiverPlayer.Family.Id,
            IPlayerEntity player when receiver is IMateEntity mateEntity => player.IsInGroupOf(mateEntity.Owner)
                || player.Family != null && mateEntity.Owner.Family != null && player.Family.Id == mateEntity.Owner.Family.Id,
            IMateEntity mateEntity when receiver is IPlayerEntity receiverPlayer => mateEntity.Owner.Id == receiverPlayer.Id,
            IMateEntity mateEntity when receiver is IMateEntity mate => mateEntity.Owner.Id == mate.Owner.Id,
            INpcEntity => false,
            _ => true
        };

        return give;
    }
}