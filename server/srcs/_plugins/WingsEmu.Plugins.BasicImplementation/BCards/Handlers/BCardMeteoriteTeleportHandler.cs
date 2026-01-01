// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Collections.Generic;
using PhoenixLib.Events;
using WingsEmu.DTOs.BCards;
using WingsEmu.Game;
using WingsEmu.Game._enum;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Families;
using WingsEmu.Game.Groups;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Monster.Event;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.BCards.Handlers;

public class BCardMeteoriteTeleportHandler : IBCardEffectAsyncHandler
{
    private readonly IAsyncEventPipeline _asyncEventPipeline;
    private readonly IRandomGenerator _randomGenerator;
    private readonly ITeleportManager _teleportManager;

    public BCardMeteoriteTeleportHandler(ITeleportManager teleportManager, IRandomGenerator randomGenerator, IAsyncEventPipeline asyncEventPipeline)
    {
        _teleportManager = teleportManager;
        _randomGenerator = randomGenerator;
        _asyncEventPipeline = asyncEventPipeline;
    }

    public BCardType HandledType => BCardType.MeteoriteTeleport;

    public void Execute(IBCardEffectContext ctx)
    {
        IBattleEntity sender = ctx.Sender;
        IBattleEntity target = ctx.Target;
        int firstData = ctx.BCard.FirstData;
        BCardDTO bCard = ctx.BCard;

        int firstDataValue = bCard.FirstDataValue(sender.Level);

        switch (ctx.BCard.SubType)
        {
            case (byte)AdditionalTypes.MeteoriteTeleport.CauseMeteoriteFall:
                if (!(sender is IPlayerEntity senderCharacter))
                {
                    return;
                }

                int meteoritesAmount = firstDataValue + 10;
                var meteorites = new List<ToSummon>();
                Position positionNonTarget = ctx.Position;

                for (int i = 0; i < meteoritesAmount; i++)
                {
                    int x = positionNonTarget.X + _randomGenerator.RandomNumber(-4, 4);
                    int y = positionNonTarget.Y + _randomGenerator.RandomNumber(-4, 4);

                    if (senderCharacter.MapInstance.IsBlockedZone(x, y))
                    {
                        x = positionNonTarget.X;
                        y = positionNonTarget.Y;
                    }

                    int vnum = _randomGenerator.RandomNumber((short)MonsterVnum.FIRST_METEORITE, (short)MonsterVnum.SECOND_METEORITE + 1);
                    var toSummon = new ToSummon
                    {
                        VNum = (short)vnum,
                        SpawnCell = new Position((short)x, (short)y),
                        Target = senderCharacter,
                        IsMoving = true,
                        IsHostile = true
                    };
                    meteorites.Add(toSummon);
                }

                _asyncEventPipeline.ProcessEventAsync(new MonsterSummonEvent(senderCharacter.MapInstance, meteorites, senderCharacter)).ConfigureAwait(false).GetAwaiter().GetResult();

                break;
            case (byte)AdditionalTypes.MeteoriteTeleport.TeleportYouAndGroupToSavedLocation:
                if (!(sender is IPlayerEntity character))
                {
                    return;
                }

                IFamily family = character.Family;

                if (!character.HasBuff(BuffVnums.MEMORIAL))
                {
                    _teleportManager.RemovePosition(character.Id);
                    return;
                }

                Position position = _teleportManager.GetPosition(character.Id);
                if (position.X == 0 && position.Y == 0)
                {
                    _teleportManager.SavePosition(character.Id, character.Position);
                    character.BroadcastEffectGround(EffectType.ArchmageTeleportSet, character.PositionX, character.PositionY, false);
                    character.SetSkillCooldown(ctx.Skill);
                    character.RemoveCastingSkill();
                    return;
                }

                short savedX = _teleportManager.GetPosition(character.Id).X;
                short savedY = _teleportManager.GetPosition(character.Id).Y;

                IEnumerable<IBattleEntity> allies = sender.GetAlliesInRange(target, ctx.Skill.AoERange);
                int counter = 0;
                foreach (IBattleEntity entity in allies)
                {
                    if (counter == firstData)
                    {
                        break;
                    }

                    if (entity.IsNpc() && !entity.IsMate())
                    {
                        continue;
                    }

                    if (entity.IsMate())
                    {
                        var mateEntity = (IMateEntity)entity;
                        IPlayerEntity mateOwner = mateEntity.Owner;
                        if (character.Id == mateOwner?.Id)
                        {
                            continue;
                        }

                        if (mateOwner == null)
                        {
                            continue;
                        }

                        PlayerGroup mateOwnerGroup = mateOwner.GetGroup();
                        PlayerGroup sessionGroup = character.GetGroup();

                        bool isInGroupMate = mateOwnerGroup?.GroupId == sessionGroup?.GroupId;
                        if (!isInGroupMate)
                        {
                            if (mateOwner.Family?.Id != family?.Id)
                            {
                                continue;
                            }
                        }

                        mateEntity.BroadcastEffectGround(EffectType.ArchmageTeleport, mateEntity.PositionX, mateEntity.PositionY, false);
                        mateEntity.MapInstance.Broadcast(mateEntity.GenerateEffectPacket(EffectType.ArchmageTeleportAfter));
                        mateEntity.TeleportOnMap(savedX, savedY);
                        counter++;
                        continue;
                    }

                    var anotherCharacter = (IPlayerEntity)entity;
                    if (character.Id == anotherCharacter.Id)
                    {
                        continue;
                    }

                    if (!anotherCharacter.IsInGroup() && !anotherCharacter.IsInFamily())
                    {
                        continue;
                    }

                    PlayerGroup anotherCharacterGroup = anotherCharacter.GetGroup();
                    PlayerGroup characterGroup = character.GetGroup();

                    bool isInGroup = anotherCharacterGroup?.GroupId == characterGroup?.GroupId;
                    if (!isInGroup)
                    {
                        if (anotherCharacter.Family?.Id != family?.Id)
                        {
                            continue;
                        }
                    }

                    anotherCharacter.BroadcastEffectGround(EffectType.ArchmageTeleport, anotherCharacter.PositionX, anotherCharacter.PositionY, false);
                    anotherCharacter.MapInstance.Broadcast(anotherCharacter.GenerateEffectPacket(EffectType.ArchmageTeleportAfter));
                    anotherCharacter.TeleportOnMap(savedX, savedY);
                    counter++;
                }

                character.BroadcastEffectGround(EffectType.ArchmageTeleportWhiteEffect, character.PositionX, character.PositionY, false);
                character.TeleportOnMap(savedX, savedY, true);
                _teleportManager.RemovePosition(character.Id);
                character.BroadcastEffectGround(EffectType.ArchmageTeleportSet, savedX, savedY, true);

                SkillInfo fakeTeleport = character.GetFakeTeleportSkill();

                Buff memorialBuff = character.BuffComponent.GetBuff((short)BuffVnums.MEMORIAL);
                character.RemoveBuffAsync(false, memorialBuff);
                character.SetSkillCooldown(fakeTeleport);
                character.RemoveCastingSkill();
                character.SkillComponent.SendTeleportPacket = DateTime.UtcNow;
                break;
        }
    }
}