using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Packets.Enums.Shells;
using WingsEmu.DTOs.BCards;
using WingsEmu.DTOs.Maps;
using WingsEmu.Game;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Buffs.Events;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Entities.Event;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Extensions.Mates;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Mates.Events;
using WingsEmu.Game.Triggers;
using WingsEmu.Packets.Enums;

namespace WingsEmu.Plugins.BasicImplementations.Event.Battle;

public class EntityDamageEventHandler : IAsyncEventProcessor<EntityDamageEvent>
{
    private static readonly HashSet<BuffVnums> _meditationBuffs = new() { BuffVnums.SPIRIT_OF_STRENGTH, BuffVnums.SPIRIT_OF_TEMPERANCE, BuffVnums.SPIRIT_OF_ENLIGHTENMENT };
    private readonly IBCardEffectHandlerContainer _bCardEffectHandlerContainer;
    private readonly IBuffFactory _buff;
    private readonly IBuffFactory _buffFactory;
    private readonly IBuffsToRemoveConfig _buffsToRemoveConfig;
    private readonly IGameLanguageService _gameLanguage;
    private readonly GameRevivalConfiguration _gameRevivalConfiguration;
    private readonly IMeditationManager _meditationManager;
    private readonly GameMinMaxConfiguration _minMaxConfiguration;
    private readonly IRandomGenerator _randomGenerator;

    public EntityDamageEventHandler(IBuffFactory buff, IMeditationManager meditationManager, IRandomGenerator randomGenerator, IBuffFactory buffFactory,
        GameRevivalConfiguration gameRevivalConfiguration, IBuffsToRemoveConfig buffsToRemoveConfig, IBCardEffectHandlerContainer bCardEffectHandlerContainer,
        GameMinMaxConfiguration minMaxConfiguration, IGameLanguageService gameLanguage)
    {
        _buff = buff;
        _meditationManager = meditationManager;
        _randomGenerator = randomGenerator;
        _buffFactory = buffFactory;
        _gameRevivalConfiguration = gameRevivalConfiguration;
        _buffsToRemoveConfig = buffsToRemoveConfig;
        _bCardEffectHandlerContainer = bCardEffectHandlerContainer;
        _minMaxConfiguration = minMaxConfiguration;
        _gameLanguage = gameLanguage;
    }

    public async Task HandleAsync(EntityDamageEvent e, CancellationToken cancellation)
    {
        IBattleEntity defender = e.Damaged;
        IBattleEntity attacker = e.Damager;
        int damage = e.Damage;
        int initialDefenderHp = defender.Hp;
        IMapInstance map = defender.MapInstance;
        SkillInfo skillInfo = e.SkillInfo;

        bool shouldDamageNamaju = defender is IMonsterEntity { DamagedOnlyLastJajamaruSkill: true } && skillInfo.Vnum == (short)SkillsVnums.JAJAMARU_LAST_SKILL;

        if (!defender.IsAlive())
        {
            return;
        }

        if (map == null)
        {
            return;
        }

        /* Sorry, it need to be hardcoded :c */
        await RemoveDamagedHardcodedBuff(defender, skillInfo);
        await RemoveDamagerHardcodedBuff(attacker);
        await RemovePvPHardcodedBuff(attacker, defender);
        await AttackerBuffChance(attacker, defender);
        await HeadShot(attacker, defender, skillInfo);
        TryLoseLoyalty(attacker, defender);

        if (attacker is IPlayerEntity player && damage != 0)
        {
            await player.RemoveInvisibility();

            if (player.TriggerAmbush)
            {
                Buff buff = player.BuffComponent.GetBuff((int)BuffVnums.AMBUSH);
                await player.RemoveBuffAsync(false, buff);
                Buff newBuff = _buff.CreateBuff((int)BuffVnums.AMBUSH_RAID, player);
                await player.AddBuffAsync(newBuff);
                player.TriggerAmbush = false;
            }
        }

        switch (defender)
        {
            case IPlayerEntity c:
                if (c.IsSeal)
                {
                    return;
                }

                if (c.TriggerAmbush)
                {
                    break;
                }

                await c.RemoveInvisibility();
                break;
            case INpcEntity { HasGodMode: true }:
                return;
            case IMonsterEntity monster:
                if (monster.BCardComponent.HasBCard(BCardType.NoDefeatAndNoDamage, (byte)AdditionalTypes.NoDefeatAndNoDamage.DecreaseHPNoDeath))
                {
                    e.CanKill = false;
                }

                if (monster.BCardComponent.HasBCard(BCardType.TimeCircleSkills, (byte)AdditionalTypes.TimeCircleSkills.DisableHPConsumption))
                {
                    return;
                }

                if (monster.DamagedOnlyLastJajamaruSkill && !shouldDamageNamaju)
                {
                    return;
                }

                if (monster.OnFirstDamageReceive && monster.BCards.Any())
                {
                    foreach (BCardDTO bCard in monster.BCards.Where(x => x.TriggerType is BCardNpcMonsterTriggerType.ON_FIRST_ATTACK))
                    {
                        _bCardEffectHandlerContainer.Execute(monster, monster, bCard);
                    }

                    monster.OnFirstDamageReceive = false;
                }

                break;
        }

        if (defender.HasGodMode())
        {
            return;
        }

        switch (attacker)
        {
            case IMonsterEntity act4Monster when act4Monster.BCardComponent.HasBCard(BCardType.NoDefeatAndNoDamage, (byte)AdditionalTypes.NoDefeatAndNoDamage.DecreaseHPNoKill):
                e.CanKill = false;
                break;
        }

        ProcessBuffDamage(defender, damage);

        damage /= map.MapInstanceType switch
        {
            MapInstanceType.IceBreakerInstance => 3,
            MapInstanceType.RainbowBattle => 3,
            MapInstanceType.ArenaInstance => 2,
            _ => 1
        };

        if (shouldDamageNamaju)
        {
            damage = GenerateNamajuDamage(defender as IMonsterEntity);
        }

        // HP is increased by x% of damage given.
        await HealByGivenDamage(attacker, damage);

        // Heal x% of inflicted damage by reducing MP.
        ReduceDamageByMp(defender, ref damage);

        // MP is increased by x% of damage given.
        HealMpByGivenDamage(attacker, damage);

        await HealDefenderByGivenDamage(defender, damage);

        if (defender is IPlayerEntity { AdditionalHp: > 0 } characterDamaged)
        {
            int removedAdditionalHp;
            if (characterDamaged.AdditionalHp > damage)
            {
                removedAdditionalHp = damage;
            }
            else
            {
                removedAdditionalHp = characterDamaged.AdditionalHp;

                int overflow = Math.Abs(characterDamaged.AdditionalHp - damage);

                if (e.CanKill)
                {
                    if (!await attacker.ShouldSaveDefender(defender, overflow, _gameRevivalConfiguration, _buffFactory))
                    {
                        defender.Hp = overflow >= defender.Hp ? 0 : defender.Hp - overflow;
                    }
                }
                else
                {
                    defender.Hp = overflow >= defender.Hp ? 1 : defender.Hp - overflow;
                }
            }

            await characterDamaged.Session.EmitEventAsync(new RemoveAdditionalHpMpEvent
            {
                Hp = removedAdditionalHp
            });
        }
        else
        {
            if (e.CanKill)
            {
                if (!await attacker.ShouldSaveDefender(defender, damage, _gameRevivalConfiguration, _buffFactory))
                {
                    defender.Hp = damage >= defender.Hp ? 0 : defender.Hp - damage;
                }
            }
            else
            {
                defender.Hp = damage >= defender.Hp ? 1 : defender.Hp - damage;
            }
        }

        AddPlayerDamageToMonster(attacker, defender, damage);
        AddMonsterHitsToPlayer(attacker, defender);
        attacker.ApplyAttackBCard(defender, e.SkillInfo, _bCardEffectHandlerContainer);
        defender.ApplyDefenderBCard(attacker, e.SkillInfo, _bCardEffectHandlerContainer);

        BCardDTO bCardOnDeath = skillInfo.BCards.FirstOrDefault(x => x.Type == (short)BCardType.TauntSkill && x.SubType == (byte)AdditionalTypes.TauntSkill.EffectOnKill);
        if (defender.Hp <= 0 && bCardOnDeath != null && attacker.IsAlive())
        {
            Buff buffForWinner = _buff.CreateBuff(bCardOnDeath.SecondData, attacker);
            await attacker.AddBuffAsync(buffForWinner);
        }

        bool onHpExecuted = false;
        float quarterHp = defender.MaxHp * 0.25f;
        if (defender.Hp <= quarterHp && quarterHp <= initialDefenderHp)
        {
            onHpExecuted = true;
            await defender.TriggerEvents(BattleTriggers.OnQuarterHp);
        }

        float halfHp = defender.MaxHp * 0.5f;
        if (!onHpExecuted && defender.Hp <= halfHp && halfHp <= initialDefenderHp)
        {
            onHpExecuted = true;
            await defender.TriggerEvents(BattleTriggers.OnHalfHp);
        }

        float threeFourthsHp = defender.MaxHp * 0.75f;
        if (!onHpExecuted && defender.Hp <= threeFourthsHp && threeFourthsHp <= initialDefenderHp)
        {
            await defender.TriggerEvents(BattleTriggers.OnThreeFourthsHp);
        }

        switch (attacker)
        {
            case IPlayerEntity playerEntity:
                playerEntity.LastAttack = DateTime.UtcNow;
                break;
        }

        switch (defender)
        {
            case IPlayerEntity character:
            {
                character.LastDefence = DateTime.UtcNow;

                character.Session.RefreshStat();

                if (character.IsSitting)
                {
                    await character.Session.RestAsync(force: true);
                }

                break;
            }
            case IMateEntity mate:
            {
                mate.LastDefence = DateTime.UtcNow;

                mate.Owner.Session.SendMateLife(mate);

                if (mate.IsSitting)
                {
                    await mate.Owner.Session.EmitEventAsync(new MateRestEvent
                    {
                        MateEntity = mate,
                        Force = true
                    });
                }

                break;
            }
            case IMonsterEntity { Hp: <= 0, IsStillAlive: false }:
                return;
        }

        if (!defender.IsAlive())
        {
            await defender.EmitEventAsync(new GenerateEntityDeathEvent
            {
                Entity = defender,
                Attacker = attacker,
                IsByMainWeapon = !skillInfo.IsUsingSecondWeapon
            });
        }
    }

    private void TryLoseLoyalty(IBattleEntity attacker, IBattleEntity defender)
    {
        if (defender is not IMateEntity mateEntity)
        {
            return;
        }

        if (attacker is IMonsterEntity { IsMateTrainer: true })
        {
            return;
        }

        if (_randomGenerator.RandomNumber() > 2)
        {
            return;
        }

        mateEntity.RemoveLoyalty((short)_randomGenerator.RandomNumber(1, 6), _minMaxConfiguration, _gameLanguage);
    }

    private int GenerateNamajuDamage(IMonsterEntity namaju)
    {
        BCardDTO bCard = namaju.BCards.FirstOrDefault(x =>
            x.Type == (short)BCardType.RecoveryAndDamagePercent && x.SubType == (byte)AdditionalTypes.RecoveryAndDamagePercent.DecreaseSelfHP);
        if (bCard == null)
        {
            return default;
        }

        return (int)(namaju.MaxHp * (bCard.FirstData * 0.01));
    }

    private async Task RemovePvPHardcodedBuff(IBattleEntity attacker, IBattleEntity defender)
    {
        if (attacker is not IPlayerEntity playerAttacker)
        {
            return;
        }

        if (defender is not IPlayerEntity playerDefender)
        {
            return;
        }

        if (attacker.IsSameEntity(defender))
        {
            return;
        }

        DateTime now = DateTime.UtcNow;
        playerAttacker.LastPvPAttack = now;
        playerDefender.LastPvPAttack = now;

        short[] pvpBuffs = _buffsToRemoveConfig.GetBuffsToRemove(BuffsToRemoveType.PVP);

        if (playerAttacker.BuffComponent.HasAnyBuff())
        {
            foreach (short buff in pvpBuffs)
            {
                foreach (IMateEntity teamMember in playerAttacker.MateComponent.TeamMembers())
                {
                    Buff toRemoveMate = teamMember.BuffComponent.GetBuff(buff);
                    await teamMember.RemoveBuffAsync(false, toRemoveMate);
                }

                Buff toRemove = playerAttacker.BuffComponent.GetBuff(buff);
                await playerAttacker.RemoveBuffAsync(false, toRemove);
            }

            foreach (Buff buff in playerAttacker.BuffComponent.GetAllBuffs(x => x.IsDisappearOnPvp()))
            {
                await playerAttacker.RemoveBuffAsync(false, buff);
            }
        }

        if (!playerDefender.BuffComponent.HasAnyBuff())
        {
            return;
        }

        foreach (short buff in pvpBuffs)
        {
            foreach (IMateEntity teamMember in playerDefender.MateComponent.TeamMembers())
            {
                Buff toRemoveMate = teamMember.BuffComponent.GetBuff(buff);
                await teamMember.RemoveBuffAsync(false, toRemoveMate);
            }

            Buff toRemove = playerDefender.BuffComponent.GetBuff(buff);
            await playerDefender.RemoveBuffAsync(false, toRemove);
        }

        foreach (Buff buff in playerDefender.BuffComponent.GetAllBuffs(x => x.IsDisappearOnPvp()))
        {
            await playerDefender.RemoveBuffAsync(false, buff);
        }
    }

    private async Task HealDefenderByGivenDamage(IBattleEntity defender, int damage)
    {
        if (defender is not IPlayerEntity playerEntity)
        {
            return;
        }

        int toHeal = playerEntity.GetMaxArmorShellValue(ShellEffectType.RecoveryHPInDefence);
        if (toHeal == 0)
        {
            return;
        }

        if (!playerEntity.IsAlive())
        {
            return;
        }

        int heal = (int)(damage * (toHeal * 0.01 / 5));
        await playerEntity.EmitEventAsync(new BattleEntityHealEvent
        {
            Entity = playerEntity,
            HpHeal = heal
        });
    }

    private void AddMonsterHitsToPlayer(IBattleEntity attacker, IBattleEntity defender)
    {
        if (attacker is not IMonsterEntity monsterEntity)
        {
            return;
        }

        if (!monsterEntity.DropToInventory && !monsterEntity.MapInstance.HasMapFlag(MapFlags.HAS_DROP_DIRECTLY_IN_INVENTORY_ENABLED) && !monsterEntity.MapInstance.HasMapFlag(MapFlags.ACT_4))
        {
            return;
        }

        IPlayerEntity playerEntity = defender switch
        {
            IMateEntity mateEntity => mateEntity.Owner,
            IPlayerEntity player => player,
            _ => null
        };

        if (playerEntity == null)
        {
            return;
        }

        if (!playerEntity.HitsByMonsters.TryGetValue(monsterEntity.Id, out int hits))
        {
            playerEntity.HitsByMonsters.TryAdd(monsterEntity.Id, 1);
            return;
        }

        hits++;
        playerEntity.HitsByMonsters[monsterEntity.Id] = hits;
    }

    private void AddPlayerDamageToMonster(IBattleEntity attacker, IBattleEntity defender, int damage)
    {
        if (defender is not IMonsterEntity monsterEntity)
        {
            return;
        }

        if (!monsterEntity.DropToInventory && !monsterEntity.MapInstance.HasMapFlag(MapFlags.HAS_DROP_DIRECTLY_IN_INVENTORY_ENABLED) && !monsterEntity.MapInstance.HasMapFlag(MapFlags.ACT_4))
        {
            return;
        }

        IPlayerEntity playerEntity = attacker switch
        {
            IMateEntity mateEntity => mateEntity.Owner,
            IMonsterEntity monster => monster.SummonerType is VisualType.Player && monster.SummonerId.HasValue ? attacker.MapInstance.GetCharacterById(monster.SummonerId.Value) : null,
            IPlayerEntity player => player,
            _ => null
        };

        if (playerEntity == null)
        {
            return;
        }

        if (!monsterEntity.PlayersDamage.TryGetValue(playerEntity.Id, out int playerDamage))
        {
            monsterEntity.PlayersDamage.TryAdd(playerEntity.Id, damage);
            return;
        }

        playerDamage += damage;
        monsterEntity.PlayersDamage[playerEntity.Id] = playerDamage;
    }

    private void HealMpByGivenDamage(IBattleEntity attacker, int damage)
    {
        if (!attacker.IsAlive())
        {
            return;
        }

        if (!attacker.BCardComponent.HasBCard(BCardType.Reflection, (byte)AdditionalTypes.Reflection.MPIncreased))
        {
            return;
        }

        int firstData = attacker.BCardComponent.GetAllBCardsInformation(BCardType.Reflection, (byte)AdditionalTypes.Reflection.MPIncreased, attacker.Level).firstData;
        int mpToIncrease = (int)(damage * (firstData * 0.01));
        if (attacker.Mp + mpToIncrease < attacker.MaxMp)
        {
            attacker.Mp += mpToIncrease;
        }
        else
        {
            attacker.Mp = attacker.MaxMp;
        }
    }

    private void ReduceDamageByMp(IBattleEntity defender, ref int damage)
    {
        (int firstData, _) = defender.BCardComponent.GetAllBCardsInformation(BCardType.LightAndShadow, (byte)AdditionalTypes.LightAndShadow.InflictDamageToMP, defender.Level);
        if (firstData == 0)
        {
            return;
        }

        (int firstDataPositive, int _) = defender.BCardComponent.GetAllBCardsInformation(BCardType.HealingBurningAndCasting,
            (byte)AdditionalTypes.HealingBurningAndCasting.HPIncreasedByConsumingMP, defender.Level);

        int defenderMp = defender.Mp;
        int defenderMpToRemove = defender.CalculateManaUsage((int)(damage * (firstData * 0.01)));

        if (defenderMp - defenderMpToRemove <= 0)
        {
            damage -= defenderMp;
            defender.Mp = 0;

            int hpToAdd = (int)(firstDataPositive / 100.0 * defenderMp);
            defender.EmitEvent(new BattleEntityHealEvent
            {
                Entity = defender,
                HpHeal = hpToAdd
            });
        }
        else
        {
            damage -= defenderMpToRemove;
            defender.Mp -= defenderMpToRemove;

            int hpToAdd = (int)(firstDataPositive / 100.0 * defenderMpToRemove);
            defender.EmitEvent(new BattleEntityHealEvent
            {
                Entity = defender,
                HpHeal = hpToAdd
            });
        }
    }

    private async Task HealByGivenDamage(IBattleEntity attacker, int damage)
    {
        if (!attacker.IsAlive())
        {
            return;
        }

        if (!attacker.BCardComponent.HasBCard(BCardType.Reflection, (byte)AdditionalTypes.Reflection.HPIncreased))
        {
            return;
        }

        int firstData = attacker.BCardComponent.GetAllBCardsInformation(BCardType.Reflection, (byte)AdditionalTypes.Reflection.HPIncreased, attacker.Level).firstData;
        int hpToIncrease = (int)(damage * (firstData * 0.01));
        await attacker.EmitEventAsync(new BattleEntityHealEvent
        {
            Entity = attacker,
            HpHeal = hpToIncrease
        });
    }

    private async Task HeadShot(IBattleEntity attacker, IBattleEntity defender, SkillInfo skillInfo)
    {
        if (!attacker.IsPlayer())
        {
            return;
        }

        if (skillInfo.Vnum != (short)SkillsVnums.SNIPER)
        {
            return;
        }

        if (!attacker.BuffComponent.HasBuff((short)BuffVnums.SNIPER_POSITION_1) && !attacker.BuffComponent.HasBuff((short)BuffVnums.SNIPER_POSITION_2))
        {
            return;
        }

        (int firstData, int secondData) = attacker.BCardComponent.GetAllBCardsInformation(BCardType.SniperAttack, (byte)AdditionalTypes.SniperAttack.ChanceCausing, attacker.Level);
        if (_randomGenerator.RandomNumber() > firstData)
        {
            return;
        }

        Buff headShot = _buffFactory.CreateBuff(secondData, attacker);
        await defender.AddBuffAsync(headShot);
    }

    private async Task AttackerBuffChance(IBattleEntity attacker, IBattleEntity defender)
    {
        if (!defender.BuffComponent.HasAnyBuff())
        {
            return;
        }

        if (!defender.BCardComponent.HasBCard(BCardType.SecondSPCard, (byte)AdditionalTypes.SecondSPCard.HitAttacker))
        {
            return;
        }

        (int firstData, int secondData) = defender.BCardComponent.GetAllBCardsInformation(BCardType.SecondSPCard, (byte)AdditionalTypes.SecondSPCard.HitAttacker, defender.Level);
        if (_randomGenerator.RandomNumber() > firstData)
        {
            return;
        }

        Buff newBuff = _buffFactory.CreateBuff(secondData, defender);
        await attacker.AddBuffAsync(newBuff);
    }

    private void ProcessBuffDamage(IBattleEntity defender, int damage)
    {
        if (!defender.BuffComponent.HasAnyBuff())
        {
            return;
        }

        if (!defender.EndBuffDamages.Any())
        {
            return;
        }

        var listToRemove = new ConcurrentQueue<short>();

        foreach (short buffVnum in defender.EndBuffDamages.Keys)
        {
            if (!defender.BuffComponent.HasBuff(buffVnum))
            {
                defender.RemoveEndBuffDamage(buffVnum);
                continue;
            }

            int damageAfter = defender.DecreaseDamageEndBuff(buffVnum, damage);
            if (damageAfter > 0)
            {
                continue;
            }

            Buff buffToRemove = defender.BuffComponent.GetBuff(buffVnum);
            defender.RemoveBuffAsync(false, buffToRemove).ConfigureAwait(false).GetAwaiter().GetResult();
            listToRemove.Enqueue(buffVnum);
        }

        while (listToRemove.TryDequeue(out short toRemoveBuff))
        {
            defender.RemoveEndBuffDamage(toRemoveBuff);
        }
    }

    private async Task RemoveDamagerHardcodedBuff(IBattleEntity damager)
    {
        var listToRemove = new List<Buff>();
        foreach (short buffVnum in _buffsToRemoveConfig.GetBuffsToRemove(BuffsToRemoveType.ATTACKER))
        {
            if (!damager.BuffComponent.HasBuff(buffVnum))
            {
                continue;
            }

            listToRemove.Add(damager.BuffComponent.GetBuff(buffVnum));
        }

        await damager.EmitEventAsync(new BuffRemoveEvent
        {
            Entity = damager,
            Buffs = listToRemove.AsReadOnly(),
            RemovePermanentBuff = false
        });
    }

    private async Task RemoveDamagedHardcodedBuff(IBattleEntity damaged, SkillInfo skillInfo)
    {
        if (damaged.BuffComponent.HasBuff((short)BuffVnums.MAGICAL_FETTERS) && damaged.IsPlayer())
        {
            var characterMagical = (IPlayerEntity)damaged;
            await characterMagical.Session.EmitEventAsync(new AngelSpecialistElementalBuffEvent
            {
                Skill = skillInfo
            });
        }

        var listToRemove = new List<Buff>();
        foreach (short buffVnum in _buffsToRemoveConfig.GetBuffsToRemove(BuffsToRemoveType.DEFENDER))
        {
            if (!damaged.BuffComponent.HasBuff(buffVnum))
            {
                continue;
            }

            listToRemove.Add(damaged.BuffComponent.GetBuff(buffVnum));
        }

        await damaged.EmitEventAsync(new BuffRemoveEvent
        {
            Entity = damaged,
            Buffs = listToRemove.AsReadOnly(),
            RemovePermanentBuff = false
        });

        if (damaged is IPlayerEntity character)
        {
            if (!_meditationManager.HasMeditation(character))
            {
                return;
            }

            _meditationManager.RemoveAllMeditation(character);
            foreach (BuffVnums buffVnum in _meditationBuffs)
            {
                Buff buff = character.BuffComponent.GetBuff((short)buffVnum);
                await damaged.RemoveBuffAsync(false, buff);
            }

            await damaged.AddBuffAsync(_buff.CreateBuff((short)BuffVnums.KUNDALINI_SYNDROME, damaged));
        }
    }
}