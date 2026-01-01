using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsAPI.Game.Extensions.ItemExtension.Item;
using WingsAPI.Game.Extensions.Quicklist;
using WingsEmu.DTOs.Skills;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Skills;
using WingsEmu.Packets.ClientPackets;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Battle;
using WingsEmu.Packets.Enums.Chat;

namespace WingsEmu.Plugins.PacketHandling.Game.Battle;

public class UseSkillPacketHandler : GenericGamePacketHandlerBase<UseSkillPacket>
{
    private readonly ICardsManager _cardsManager;
    private readonly IDelayManager _delayManager;
    private readonly IGameLanguageService _gameLanguage;
    private readonly IItemsManager _items;
    private readonly RainbowBattleConfiguration _rainbowBattleConfiguration;
    private readonly ISkillsManager _skillsManager;

    public UseSkillPacketHandler(IGameLanguageService gameLanguage, IDelayManager delayManager, IItemsManager items, ICardsManager cardsManager, ISkillsManager skillsManager,
        RainbowBattleConfiguration rainbowBattleConfiguration)
    {
        _gameLanguage = gameLanguage;
        _delayManager = delayManager;
        _items = items;
        _cardsManager = cardsManager;
        _skillsManager = skillsManager;
        _rainbowBattleConfiguration = rainbowBattleConfiguration;
    }

    protected override async Task HandlePacketAsync(IClientSession session, UseSkillPacket packet)
    {
        session.SendDebugMessage("[U_S] Start u_s");
        IPlayerEntity character = session.PlayerEntity;

        if (!BasicChecks(session, packet))
        {
            return;
        }

        List<IBattleEntitySkill> skills = session.PlayerEntity.Skills;
        var characterSkill = skills.FirstOrDefault(s => s.Skill?.CastId == packet.CastId
            && (s.Skill?.UpgradeSkill == 0 || s.Skill?.SkillType == SkillType.NormalPlayerSkill) && s.Skill.TargetType != TargetType.NonTarget) as CharacterSkill;
        SkillDTO skill = characterSkill?.Skill;

        if (skill == null)
        {
            session.SendCancelPacket(CancelType.NotInCombatMode);
            session.SendDebugMessage("[U_S] Skill does not exist");
            return;
        }

        if (skill.ItemVNum != 0 && !session.PlayerEntity.HasItem(skill.ItemVNum))
        {
            session.SendCancelPacket(CancelType.NotInCombatMode);
            IGameItem gameItem = _items.GetItem(skill.ItemVNum);
            if (gameItem == null)
            {
                return;
            }

            string itemName = gameItem.GetItemName(_gameLanguage, session.UserLanguage);
            session.SendInformationChatMessage(_gameLanguage.GetLanguageFormat(GameDialogKey.INVENTORY_SHOUTMESSAGE_NOT_ENOUGH_ITEMS, session.UserLanguage, 1, itemName));
            return;
        }

        SkillInfo skillInfo = session.PlayerEntity.GetUpgradedSkill(skill, _cardsManager, _skillsManager) ?? skill.GetInfo();

        if (skillInfo.AoERange != 0 && skill.CastId != 0 && skill.CastId != 1 && session.PlayerEntity.HasBuff(BuffVnums.EXPLOSIVE_ENCHACMENT))
        {
            (int firstData, int secondData) buff = session.PlayerEntity.BCardComponent.GetAllBCardsInformation(BCardType.FireCannoneerRangeBuff,
                (byte)AdditionalTypes.FireCannoneerRangeBuff.AOEIncreased, session.PlayerEntity.Level);
            skillInfo.AoERange += (byte)buff.firstData;
            skillInfo.HitEffect = session.PlayerEntity.GetCannoneerHitEffect(skill.CastId);
        }

        skillInfo.Range += (byte)session.PlayerEntity.BCardComponent.GetAllBCardsInformation(BCardType.FearSkill, (byte)AdditionalTypes.FearSkill.AttackRangedIncreased, session.PlayerEntity.Level)
            .Item1;

        bool canBeUsed = character.CharacterCanCastOrCancel(characterSkill, _gameLanguage, skillInfo, false);
        bool comboSkill = skill.CastId > 10 && character.UseSp && skill.SpecialCost == 999;

        if (!canBeUsed)
        {
            session.SendCancelPacket(CancelType.NotInCombatMode);
            session.SendDebugMessage("[U_S] !canBeUsed");
            return;
        }

        if (!session.PlayerEntity.CanPerformAttack(skillInfo))
        {
            session.SendCancelPacket(CancelType.NotInCombatMode);
            return;
        }

        if (comboSkill)
        {
            ComboSkillState comboSkillState = session.PlayerEntity.GetComboState();
            if (comboSkillState == null)
            {
                session.SendCancelPacket(CancelType.NotInCombatMode);
                session.SendDebugMessage("[U_S] comboSkills == null");
                return;
            }

            bool canCastComboSkill = comboSkillState.LastSkillByCastId == skillInfo.CastId;
            if (!canCastComboSkill)
            {
                session.SendCancelPacket(CancelType.NotInCombatMode);
                session.SendDebugMessage("[U_S] comboSkill && comboSkill == null");
                return;
            }

            if (session.PlayerEntity.AngelElement.HasValue)
            {
                if (!character.HasBuff(BuffVnums.MAGIC_SPELL))
                {
                    session.SendCancelPacket(CancelType.NotInCombatMode);
                    session.SendDebugMessage("[U_S] Character without MAGIC_SPELL");
                    return;
                }

                ElementType? elementType = character.GetBuffElementType((short)skill.Id);

                if (!elementType.HasValue)
                {
                    session.SendCancelPacket(CancelType.NotInCombatMode);
                    session.SendDebugMessage("[U_S] ElementType == null");
                    return;
                }

                if (session.PlayerEntity.AngelElement.Value != elementType.Value)
                {
                    session.SendCancelPacket(CancelType.NotInCombatMode);
                    session.SendDebugMessage("[U_S] Element != skill.Element");
                    return;
                }
            }
        }

        IBattleEntity target = character.MapInstance.GetBattleEntity(packet.VisualType, packet.MapMonsterId);

        if (await TargetChecks(session, target, skillInfo))
        {
            return;
        }

        if (session.PlayerEntity.RainbowBattleComponent.IsFrozen)
        {
            session.SendCancelPacket(CancelType.NotInCombatMode);
            return;
        }

        if (skillInfo.TargetType == TargetType.Self || skillInfo.TargetType == TargetType.SelfOrTarget && target.Id == character.Id)
        {
            session.SendDebugMessage("[U_S] Target = character");
            target = character;
        }

        if (skillInfo.TargetAffectedEntities == TargetAffectedEntities.BuffForAllies && character.IsEnemyWith(target))
        {
            target = character;
        }

        int cellSizeBonus = target switch
        {
            IPlayerEntity => 7,
            _ => 3
        };

        if (target is INpcMonsterEntity npcMonsterEntity)
        {
            cellSizeBonus += npcMonsterEntity.CellSize;
        }

        if (!character.Position.IsInRange(target.Position, skillInfo.Range + cellSizeBonus) && skillInfo.AttackType != AttackType.Dash)
        {
            session.SendCancelPacket(CancelType.NotInCombatMode);
            session.SendDebugMessage($"[U_S] Out of range {character.Position.GetDistance(target.Position)} - {skill.Range}");
            return;
        }

        if (target is IMonsterEntity mob)
        {
            if (!session.PlayerEntity.CanMonsterBeAttacked(mob) && !mob.IsMateTrainer)
            {
                session.SendCancelPacket(CancelType.NotInCombatMode);
                return;
            }

            if (mob.MonsterRaceType == MonsterRaceType.Fixed)
            {
                session.SendCancelPacket(CancelType.NotInCombatMode);
                return;
            }
        }

        // it should check before taking Mp/Hp
        if (character.IsAllyWith(target) && skillInfo.TargetType != TargetType.Self && skillInfo.TargetType != TargetType.SelfOrTarget && target.Id != character.Id &&
            skillInfo.TargetAffectedEntities != TargetAffectedEntities.BuffForAllies)
        {
            character.CancelCastingSkill();
            character.Session.SendDebugMessage("[U_S] !caster.IsEnemyWith(target) && caster.IsPlayer() && skill.TargetType != TargetType.Self");
            return;
        }

        if (!character.IsEnemyWith(target) && skillInfo.TargetType != TargetType.Self && skillInfo.TargetType != TargetType.SelfOrTarget)
        {
            character.CancelCastingSkill();
            return;
        }

        Position positionAfterDash = default;

        if (packet.MapX.HasValue && packet.MapY.HasValue)
        {
            if (skillInfo.AttackType != AttackType.Dash)
            {
                session.SendDebugMessage("[U_S] Skill.AttackType != Dash");
                session.SendCancelPacket(CancelType.NotInCombatMode);
                return;
            }

            if (character.MapInstance.IsBlockedZone((int)packet.MapX, (int)packet.MapY))
            {
                session.SendCancelPacket(CancelType.NotInCombatMode);
                return;
            }

            var newPosition = new Position((short)packet.MapX, (short)packet.MapY);
            if (!character.Position.IsInRange(newPosition, skillInfo.Range + 2))
            {
                session.SendDebugMessage("[U_S] newPosition !IsInRange");
                session.SendCancelPacket(CancelType.NotInCombatMode);
                return;
            }

            positionAfterDash = newPosition;
        }

        await FinalChecks(session, skill, character, skillInfo, target, comboSkill);

        session.SendDebugMessage($"Hit {skillInfo.HitType} / Target {skillInfo.TargetType} / Attack Type {skillInfo.AttackType} / Affected entities {skillInfo.TargetAffectedEntities}");
        if (target is IPlayerEntity characterTarget)
        {
            session.SendDebugMessage($"Sender: {session.PlayerEntity.Name} -> Target: {characterTarget.Name} ");
        }

        character.SkillComponent.CanBeInterrupted = false;
        character.SkillComponent.IsSkillInterrupted = false;
        character.SkillComponent.CanBeInterrupted = character.CanBeInterrupted(skillInfo);
        character.WeaponLoaded(characterSkill, _gameLanguage, true);
        DateTime castTime = character.GenerateSkillCastTime(skillInfo);
        session.SendDebugMessage("[U_S] IsCasting = true");
        await character.EmitEventAsync(new BattleExecuteSkillEvent(character, target, skillInfo, castTime, positionAfterDash));
    }

    private async Task<bool> TargetChecks(IClientSession session, IBattleEntity target, SkillInfo skillInfo)
    {
        if (target == null)
        {
            session.SendCancelPacket(CancelType.NotInCombatMode);
            session.SendDebugMessage("[U_S] No target");
            return true;
        }

        if (target.MapInstance.IsPvp && session.CurrentMapInstance.PvpZone(target.PositionX, target.PositionY))
        {
            session.SendDebugMessage("[U_S] Target no-PvP zone / !map.IsPvp");
            session.SendCancelPacket(CancelType.NotInCombatMode);
            return true;
        }

        switch (session.CurrentMapInstance.MapInstanceType)
        {
            case MapInstanceType.RainbowBattle:

                if (target is not IPlayerEntity rainbowFrozen)
                {
                    break;
                }

                if (session.PlayerEntity.IsEnemyWith(rainbowFrozen))
                {
                    break;
                }

                if (!rainbowFrozen.RainbowBattleComponent.IsFrozen)
                {
                    break;
                }

                if (rainbowFrozen.RainbowBattleComponent.Team != session.PlayerEntity.RainbowBattleComponent.Team)
                {
                    break;
                }

                if (target.Position.GetDistance(session.PlayerEntity.Position) > 5)
                {
                    break;
                }

                session.SendCancelPacket(CancelType.NotInCombatMode);

                DateTime now = DateTime.UtcNow;
                if (session.PlayerEntity.LastUnfreezedPlayer > now)
                {
                    return true;
                }

                session.PlayerEntity.LastUnfreezedPlayer = now.AddSeconds(5);
                DateTime wait = await _delayManager.RegisterAction(session.PlayerEntity, DelayedActionType.RainbowBattleUnfreeze);
                session.SendDelay((int)(wait - DateTime.UtcNow).TotalMilliseconds, GuriType.Unfreezing, $"guri 505 {rainbowFrozen.Id}");
                return true;
        }

        if (target is INpcEntity && skillInfo.Vnum == (short)SkillsVnums.SACRIFICE)
        {
            session.SendCancelPacket(CancelType.NotInCombatMode);
            session.SendDebugMessage("[U_S] MapNpc && Sacrifice");
            return true;
        }

        if (!target.IsAlive())
        {
            session.SendCancelPacket(CancelType.NotInCombatMode);
            session.SendDebugMessage("[U_S] Target is dead");
            return true;
        }

        if (target is not IMonsterEntity mob || mob.SummonerId == 0)
        {
            return false;
        }

        if (mob.SummonerType is not VisualType.Player)
        {
            return false;
        }

        if (session.PlayerEntity.CanMonsterBeAttacked(mob) && !mob.IsMateTrainer)
        {
            return false;
        }

        session.SendDebugMessage("[U_S] mob.SummonerId != 0");
        session.SendCancelPacket(CancelType.NotInCombatMode);
        return true;
    }

    private async Task FinalChecks(IClientSession session, SkillDTO skill, IPlayerEntity character, SkillInfo skillInfo, IBattleEntity target, bool comboSkill)
    {
        if (!session.PlayerEntity.CheatComponent.HasGodMode)
        {
            skillInfo.ManaCost = session.PlayerEntity.BCardComponent.HasBCard(BCardType.TimeCircleSkills, (byte)AdditionalTypes.TimeCircleSkills.DisableMPConsumption) ? 0 : skillInfo.ManaCost;
            if (session.PlayerEntity.AdditionalMp > 0)
            {
                int removedAdditionalMp;
                if (session.PlayerEntity.AdditionalMp > skillInfo.ManaCost)
                {
                    removedAdditionalMp = skillInfo.ManaCost;
                }
                else
                {
                    removedAdditionalMp = session.PlayerEntity.AdditionalMp;

                    int overflow = Math.Abs(session.PlayerEntity.AdditionalMp - skillInfo.ManaCost);
                    session.PlayerEntity.Mp -= overflow;
                }

                await session.EmitEventAsync(new RemoveAdditionalHpMpEvent
                {
                    Mp = removedAdditionalMp
                });
            }
            else
            {
                session.PlayerEntity.RemoveEntityMp((short)skillInfo.ManaCost, skill);
            }

            (int firstDataNegative, int _) = session.PlayerEntity.BCardComponent.GetAllBCardsInformation(BCardType.HealingBurningAndCasting,
                (byte)AdditionalTypes.HealingBurningAndCasting.HPDecreasedByConsumingMP, session.PlayerEntity.Level);

            (int firstDataPositive, int _) = session.PlayerEntity.BCardComponent.GetAllBCardsInformation(BCardType.HealingBurningAndCasting,
                (byte)AdditionalTypes.HealingBurningAndCasting.HPIncreasedByConsumingMP, session.PlayerEntity.Level);

            int hpRemoved = (int)(firstDataPositive / 100.0 * skillInfo.ManaCost - firstDataNegative / 100.0 * skillInfo.ManaCost);

            if (hpRemoved > 0)
            {
                await session.PlayerEntity.EmitEventAsync(new BattleEntityHealEvent
                {
                    Entity = session.PlayerEntity,
                    HpHeal = hpRemoved
                });
            }
            else
            {
                if (session.PlayerEntity.Hp - hpRemoved <= 0)
                {
                    session.PlayerEntity.BroadcastDamage(session.PlayerEntity.Hp - 1);
                    session.PlayerEntity.Hp = 1;
                }
                else
                {
                    session.PlayerEntity.BroadcastDamage(hpRemoved);
                    session.PlayerEntity.Hp -= hpRemoved;
                }
            }

            session.RefreshStat();
            session.SendDebugMessage($"[U_S] MpCost: {skillInfo.ManaCost}");
        }

        if (skill.Id == (short)SkillsVnums.SPY_OUT)
        {
            session.SendEffectObject(target, true, EffectType.Sp6ArcherTargetFalcon);
        }

        if (skill.ItemVNum != 0)
        {
            await session.RemoveItemFromInventory(skill.ItemVNum);
        }

        if (session.PlayerEntity.RainbowBattleComponent.IsInRainbowBattle)
        {
            session.PlayerEntity.RainbowBattleComponent.ActivityPoints += _rainbowBattleConfiguration.UsingSkillActivityPoints;
        }

        character.LastSkillUse = DateTime.UtcNow;
        character.ClearFoodBuffer();
        ComboSkillState newState = null;
        if (!comboSkill && skill.CastId != 0)
        {
            newState = new ComboSkillState
            {
                State = 0
            };
        }

        if (skill.CastId < 11 && skill.CastId != 0 && newState != null)
        {
            newState.OriginalSkillCastId = (byte)skill.CastId;
        }

        session.RefreshStat();
        if (newState != null && !session.PlayerEntity.AngelElement.HasValue)
        {
            session.PlayerEntity.SaveComboSkill(newState);
        }

        if (skill.CastId == 0)
        {
            return;
        }

        ComboSkillState state = session.PlayerEntity.GetComboState();
        bool sendBuffIconWindow = session.PlayerEntity.AngelElement.HasValue && character.Specialist is { SpLevel: > 19 } && character.HasBuff(BuffVnums.MAGIC_SPELL);

        if (state != null)
        {
            if (state.State >= 10)
            {
                session.PlayerEntity.CleanComboState();
            }
            else
            {
                if (comboSkill)
                {
                    session.PlayerEntity.IncreaseComboState((byte)skill.CastId);
                }
                else if (newState == null)
                {
                    session.PlayerEntity.CleanComboState();
                }
            }

            if (sendBuffIconWindow)
            {
                character.Session.SendMSlotPacket(state.OriginalSkillCastId);
            }
        }

        if (sendBuffIconWindow)
        {
            session.RefreshQuicklist();
            return;
        }

        session.SendMsCPacket(0);
        session.RefreshQuicklist();
    }

    private bool BasicChecks(IClientSession session, UseSkillPacket packet)
    {
        IPlayerEntity character = session.PlayerEntity;

        if (!character.CanFight() || packet == null)
        {
            session.SendCancelPacket(CancelType.NotInCombatMode);
            session.SendDebugMessage("[U_S] !canFight, packet null");
            return false;
        }

        if (!session.PlayerEntity.CanPerformAttack())
        {
            session.SendDebugMessage("[U_S] !CanPerformAttack");
            session.SendCancelPacket(CancelType.NotInCombatMode);
            return false;
        }

        if (session.CurrentMapInstance.IsPvp && session.CurrentMapInstance.PvpZone(session.PlayerEntity.PositionX, session.PlayerEntity.PositionY))
        {
            session.SendCancelPacket(CancelType.NotInCombatMode);
            session.SendDebugMessage("[U_S] Character no-PvP zone / !map.IsPvp");
            return false;
        }

        if (session.IsMuted())
        {
            session.SendMuteMessage();
            session.SendCancelPacket(CancelType.NotInCombatMode);
            return false;
        }

        if (session.PlayerEntity.IsOnVehicle || session.PlayerEntity.CheatComponent.IsInvisible)
        {
            session.SendDebugMessage("[U_S] IsVehicled, InvisibleGm");
            session.SendCancelPacket(CancelType.NotInCombatMode);
            return false;
        }

        if ((DateTime.UtcNow - session.PlayerEntity.LastTransform).TotalSeconds < 3)
        {
            session.SendCancelPacket(CancelType.NotInCombatMode);
            session.SendDebugMessage("[U_S] Under transformation cooldown");
            session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.SPECIALIST_SHOUTMESSAGE_CANT_ATTACK_YET, session.UserLanguage), MsgMessageType.Middle);
            return false;
        }

        if (!character.IsCastingSkill)
        {
            return true;
        }

        session.SendCancelPacket(CancelType.NotInCombatMode);
        session.SendDebugMessage("[U_S] Already using a skill");
        return false;
    }
}