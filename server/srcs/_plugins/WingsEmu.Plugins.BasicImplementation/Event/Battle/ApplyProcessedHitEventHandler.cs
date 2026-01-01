using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsEmu.DTOs.BCards;
using WingsEmu.DTOs.Skills;
using WingsEmu.Game;
using WingsEmu.Game._enum;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Entities.Event;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Mates;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Battle;
using WingsEmu.Packets.ServerPackets.Battle;

namespace WingsEmu.Plugins.BasicImplementations.Event.Battle;

public class ApplyProcessedHitEventHandler : IAsyncEventProcessor<ApplyHitEvent>
{
    private readonly IBCardEffectHandlerContainer _bCardHandlerContainer;
    private readonly IAsyncEventPipeline _eventPipeline;
    private readonly IMonsterEntityFactory _monsterEntityFactory;
    private readonly IRandomGenerator _randomGenerator;
    private readonly ISkillUsageManager _skillUsageManager;

    public ApplyProcessedHitEventHandler(IAsyncEventPipeline eventPipeline,
        IBCardEffectHandlerContainer bCardHandlerContainer, ISkillUsageManager skillUsageManager, IRandomGenerator randomGenerator, IMonsterEntityFactory monsterEntityFactory)
    {
        _eventPipeline = eventPipeline;
        _bCardHandlerContainer = bCardHandlerContainer;
        _skillUsageManager = skillUsageManager;
        _randomGenerator = randomGenerator;
        _monsterEntityFactory = monsterEntityFactory;
    }

    public async Task HandleAsync(ApplyHitEvent e, CancellationToken cancellation)
    {
        HitInformation hit = e.HitInformation;

        IBattleEntity caster = hit.Caster;
        IBattleEntity target = e.Target;
        DamageAlgorithmResult algorithmResult = e.ProcessResults;
        HitType hitType = algorithmResult.HitType;
        int totalDamage = algorithmResult.Damages;

        SkillInfo skill = hit.Skill;

        BCardDTO[] afterAttackAllTargets = skill.BCardsType.TryGetValue(SkillCastType.AFTER_ATTACK_ALL_TARGETS, out HashSet<BCardDTO> bCards) ? bCards.ToArray() : Array.Empty<BCardDTO>();

        if (hit.IsFirst)
        {
            switch (skill.TargetType)
            {
                case TargetType.Self when skill.HitType == TargetHitType.EnemiesInAffectedAoE:
                    caster.BroadcastSuPacket(caster, skill, 0, SuPacketHitMode.NoDamageFail, isFirst: hit.IsFirst);
                    break;
                case TargetType.NonTarget:
                    caster.BroadcastNonTargetSkill(e.HitInformation.Position, skill);
                    break;
            }
        }

        bool onyx = false;
        if (caster.BCardComponent.HasBCard(BCardType.StealBuff, (byte)AdditionalTypes.StealBuff.ChanceSummonOnyxDragon))
        {
            onyx = e.ProcessResults.OnyxEffect;
        }

        if (totalDamage <= 0 && caster is not IMonsterEntity { IsMateTrainer: true })
        {
            totalDamage = 1;
        }

        IMonsterEntity onyxMonsterEntity = null;
        if (onyx && skill.CastId != 0)
        {
            short x = caster.PositionX;
            short y = caster.PositionY;

            x += (short)_randomGenerator.RandomNumber(-3, 3);
            y += (short)_randomGenerator.RandomNumber(-3, 3);
            if (caster.MapInstance.IsBlockedZone(x, y))
            {
                x = caster.PositionX;
                y = caster.PositionY;
            }

            onyxMonsterEntity = _monsterEntityFactory.CreateMonster((int)MonsterVnum.ONYX_MONSTER, caster.MapInstance, new MonsterEntityBuilder
            {
                IsRespawningOnDeath = false,
                IsWalkingAround = false
            });

            await onyxMonsterEntity.EmitEventAsync(new MapJoinMonsterEntityEvent(onyxMonsterEntity, x, y));
            caster.MapInstance.Broadcast(caster.GenerateOnyxGuriPacket(x, y));
        }

        if (caster.IsPlayer())
        {
            var player = (IPlayerEntity)caster;
            player.SkillComponent.IsSkillInterrupted = false;
            player.SkillComponent.CanBeInterrupted = false;
        }

        SuPacketHitMode hitMode = GetHitMode(skill, hitType, hit.IsFirst);

        if (!caster.IsAlive())
        {
            hitMode = SuPacketHitMode.OutOfRange;
            caster.BroadcastSuPacket(caster, skill, 0, hitMode, isFirst: hit.IsFirst);
            Skip(caster, skill);
            return;
        }

        switch (target)
        {
            case IMonsterEntity monsterEntity:
                monsterEntity.MapInstance.MonsterRefreshTarget(monsterEntity, caster, DateTime.UtcNow, true);
                break;
            case INpcEntity npcEntity:
                npcEntity.MapInstance.NpcRefreshTarget(npcEntity, caster);
                break;
        }

        int monsterSize = target switch
        {
            IMonsterEntity monsterEntity => monsterEntity.CellSize,
            INpcEntity npcEntity => npcEntity.CellSize,
            IMateEntity mateEntity => mateEntity.CellSize,
            _ => 0
        };

        int cellSizeBonus = target switch
        {
            IPlayerEntity => 7,
            _ => 3
        };

        if (skill.Vnum != -1 && skill.CastId != -1 &&
            skill.HitType == TargetHitType.TargetOnly && !caster.Position.IsInRange(target.Position, skill.Range + monsterSize + cellSizeBonus) && skill.AttackType != AttackType.Dash)
        {
            hitMode = SuPacketHitMode.OutOfRange;
            caster.BroadcastSuPacket(target, skill, 0, hitMode, isFirst: hit.IsFirst);
            Skip(caster, skill);
            return;
        }

        if (target.BCardComponent.HasBCard(BCardType.AbsorptionAndPowerSkill, (byte)AdditionalTypes.AbsorptionAndPowerSkill.AddDamageToHP))
        {
            (int firstData, int secondData) bCard =
                target.BCardComponent.GetAllBCardsInformation(BCardType.AbsorptionAndPowerSkill, (byte)AdditionalTypes.AbsorptionAndPowerSkill.AddDamageToHP, target.Level);
            if (_randomGenerator.RandomNumber() <= bCard.firstData)
            {
                double toHealPercentage = bCard.secondData * 0.01;
                int toHeal = (int)(totalDamage * toHealPercentage);
                await target.EmitEventAsync(new BattleEntityHealEvent
                {
                    Entity = target,
                    HpHeal = toHeal
                });

                caster.BroadcastSuPacket(target, skill, 0, GetHitMode(skill, HitType.Miss, hit.IsFirst), isFirst: hit.IsFirst);
                Skip(caster, skill);
                return;
            }
        }

        if (hitType == HitType.Miss)
        {
            caster.BroadcastSuPacket(target, skill, 0, hitMode, isFirst: hit.IsFirst);
            Skip(caster, skill);
            return;
        }

        if (caster.IsPlayer())
        {
            var character = (IPlayerEntity)caster;
            if (algorithmResult.SoftDamageEffect && hit.IsFirst)
            {
                caster.MapInstance.Broadcast(character.GenerateEffectPacket(EffectType.BoostedAttack));
            }

            if (skill.Combos.Any())
            {
                double increaseDamageByComboState = 0;
                ComboState comboState = _skillUsageManager.GetComboState(caster.Id, target.Id);
                increaseDamageByComboState = 0.05 + 0.1 * comboState.Hit;

                totalDamage += (int)(totalDamage * increaseDamageByComboState);

                comboState.Hit++;
                ComboDTO combo = skill.Combos.FirstOrDefault(s => s.Hit == comboState.Hit);
                if (combo != null)
                {
                    skill.HitAnimation = combo.Animation;
                    skill.HitEffect = combo.Effect;
                }

                if (skill.Combos.Max(s => s.Hit) <= comboState.Hit)
                {
                    _skillUsageManager.ResetComboState(caster.Id);
                }
            }
        }

        if (caster.BCardComponent.HasBCard(BCardType.NoDefeatAndNoDamage, (byte)AdditionalTypes.NoDefeatAndNoDamage.NeverCauseDamage))
        {
            totalDamage = 0;
        }

        if (target.IsPlayer())
        {
            var character = (IPlayerEntity)target;
            if (character.SkillComponent.CanBeInterrupted && character.IsCastingSkill)
            {
                character.SkillComponent.CanBeInterrupted = false;
                character.SkillComponent.IsSkillInterrupted = true;
            }
        }

        // REFLECTION
        if (IsReflectionNoDamage(target))
        {
            await ReflectDamage(caster, target, algorithmResult, skill, hit);
            Skip(caster, skill);
            return;
        }

        if (IsReflectionWithDamage(target))
        {
            await ReflectDamage(caster, target, algorithmResult, skill, hit, true);
        }

        await _eventPipeline.ProcessEventAsync(new EntityDamageEvent
        {
            Damaged = target,
            Damager = caster,
            Damage = totalDamage,
            CanKill = true,
            SkillInfo = skill
        });

        caster.BroadcastSuPacket(target, skill, totalDamage, hitMode, isFirst: hit.IsFirst);

        if (hit.IsFirst)
        {
            hit.IsFirst = false;
        }

        if (target.IsAlive())
        {
            foreach (BCardDTO bCard in afterAttackAllTargets)
            {
                _bCardHandlerContainer.Execute(target, caster, bCard, skill);
            }
        }

        if (!target.IsAlive() && caster.IsPlayer())
        {
            (caster as IPlayerEntity).Session.SendCancelPacket(CancelType.NotInCombatMode);
        }

        switch (skill.Vnum)
        {
            case (short)SkillsVnums.HOLY_EXPLOSION when target.BuffComponent.HasBuff((short)BuffVnums.ILLUMINATING_POWDER):
            {
                await _eventPipeline.ProcessEventAsync(new EntityDamageEvent
                {
                    Damaged = target,
                    Damager = caster,
                    Damage = totalDamage,
                    CanKill = true,
                    SkillInfo = skill
                });

                caster.BroadcastSuPacket(target, skill, totalDamage, hitMode, true);
                Buff buff = target.BuffComponent.GetBuff((short)BuffVnums.ILLUMINATING_POWDER);
                await target.RemoveBuffAsync(false, buff);
                break;
            }
            case (short)SkillsVnums.CONVERT when target.BuffComponent.HasBuff((short)BuffVnums.CORRUPTION):
            {
                int convertDamage = totalDamage / 2;
                await _eventPipeline.ProcessEventAsync(new EntityDamageEvent
                {
                    Damaged = target,
                    Damager = caster,
                    Damage = convertDamage,
                    CanKill = true,
                    SkillInfo = skill
                });

                caster.BroadcastSuPacket(target, skill, convertDamage, hitMode, true);
                Buff buff = target.BuffComponent.GetBuff((short)BuffVnums.CORRUPTION);
                await target.RemoveBuffAsync(false, buff);
                break;
            }
        }

        if (skill.CastId != 0 && onyx)
        {
            int onyxDamage = totalDamage / 2;
            await _eventPipeline.ProcessEventAsync(new EntityDamageEvent
            {
                Damaged = target,
                Damager = onyxMonsterEntity,
                Damage = onyxDamage,
                CanKill = false,
                SkillInfo = skill
            });

            onyxMonsterEntity.BroadcastSuPacket(target, skill, onyxDamage, hitMode);
        }
    }

    private void Skip(IBattleEntity entity, SkillInfo skillInfo)
    {
        if (!entity.IsPlayer())
        {
            return;
        }

        if (!skillInfo.Combos.Any())
        {
            return;
        }

        _skillUsageManager.ResetComboState(entity.Id);
    }

    private static bool IsReflectionNoDamage(IBattleEntity target) =>
        target.BCardComponent.HasBCard(BCardType.TauntSkill, (byte)AdditionalTypes.TauntSkill.ReflectsMaximumDamageFromNegated) ||
        target.BCardComponent.HasBCard(BCardType.TauntSkill, (byte)AdditionalTypes.TauntSkill.ReflectsMaximumDamageFrom);

    private static bool IsReflectionWithDamage(IBattleEntity target) =>
        target.BCardComponent.HasBCard(BCardType.DamageConvertingSkill, (byte)AdditionalTypes.DamageConvertingSkill.ReflectMaximumReceivedDamage);

    private async Task ReflectDamage(IBattleEntity caster, IBattleEntity target, DamageAlgorithmResult damageAlgorithmResult, SkillInfo skill, HitInformation hitInformation,
        bool shouldDamageCaster = false)
    {
        int totalDamage = damageAlgorithmResult.Damages;
        HitType hitType = damageAlgorithmResult.HitType;

        (int firstData, int secondData) reflection =
            target.BCardComponent.GetAllBCardsInformation(BCardType.TauntSkill, (byte)AdditionalTypes.TauntSkill.ReflectsMaximumDamageFromNegated, target.Level);
        if (reflection.firstData == 0)
        {
            reflection = target.BCardComponent.GetAllBCardsInformation(BCardType.TauntSkill, (byte)AdditionalTypes.TauntSkill.ReflectsMaximumDamageFrom, target.Level);
        }

        if (shouldDamageCaster)
        {
            reflection = target.BCardComponent.GetAllBCardsInformation(BCardType.DamageConvertingSkill, (byte)AdditionalTypes.DamageConvertingSkill.ReflectMaximumReceivedDamage, target.Level);
        }

        if (totalDamage > reflection.firstData)
        {
            totalDamage = reflection.firstData;
        }

        await _eventPipeline.ProcessEventAsync(new EntityDamageEvent
        {
            Damaged = caster,
            Damager = target,
            Damage = totalDamage,
            CanKill = false,
            SkillInfo = skill
        });

        SuPacketHitMode hitMode = GetHitMode(skill, hitType, hitInformation.IsFirst);
        target.BroadcastSuPacket(caster, skill, totalDamage, hitMode, true, hitInformation.IsFirst);

        if (!shouldDamageCaster)
        {
            if (skill.Vnum != (short)SkillsVnums.DOUBLE_RIPPER)
            {
                SuPacketHitMode reflectHitMode = skill.AoERange != 0 ? SuPacketHitMode.ReflectionAoeMiss : SuPacketHitMode.Miss;
                caster.BroadcastSuPacket(caster, skill, totalDamage, reflectHitMode, false, hitInformation.IsFirst); // Yes, it should be false for reflect.
            }

            if (hitInformation.IsFirst)
            {
                hitInformation.IsFirst = false;
            }
        }
    }

    private SuPacketHitMode GetHitMode(SkillInfo skill, HitType hitType, bool isFirst)
    {
        if (skill.TargetType == TargetType.Self && (skill.HitType == TargetHitType.EnemiesInAffectedAoE || skill.HitType == TargetHitType.AlliesInAffectedAoE))
        {
            switch (hitType)
            {
                case HitType.Miss:
                    return SuPacketHitMode.MissAoe;
                case HitType.Normal:
                    return SuPacketHitMode.AttackedInAoe;
                case HitType.Critical:
                    return SuPacketHitMode.AttackedInAoeCrit;
            }
        }

        if (isFirst)
        {
            switch (hitType)
            {
                case HitType.Miss:
                    return SuPacketHitMode.Miss;
                case HitType.Normal:
                    return skill.TargetType == TargetType.NonTarget ? SuPacketHitMode.AttackedInAoe : SuPacketHitMode.SuccessAttack;
                case HitType.Critical:
                    return SuPacketHitMode.CriticalAttack;
            }
        }
        else
        {
            switch (hitType)
            {
                case HitType.Miss:
                    return SuPacketHitMode.MissAoe;
                case HitType.Normal:
                    return SuPacketHitMode.AttackedInAoe;
                case HitType.Critical:
                    return SuPacketHitMode.AttackedInAoeCrit;
            }
        }

        return SuPacketHitMode.SuccessAttack;
    }
}