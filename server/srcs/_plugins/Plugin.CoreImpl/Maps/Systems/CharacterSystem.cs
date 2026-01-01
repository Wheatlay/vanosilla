// WingsEmu
// 
// Developed by NosWings Team

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PhoenixLib.Events;
using WingsAPI.Data.Account;
using WingsAPI.Game.Extensions.Quicklist;
using WingsAPI.Packets.Enums.Rainbow;
using WingsEmu.Core.Extensions;
using WingsEmu.Game;
using WingsEmu.Game._ECS;
using WingsEmu.Game._ECS.Systems;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Battle;
using WingsEmu.Game.Buffs;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Groups.Events;
using WingsEmu.Game.Helpers.Damages;
using WingsEmu.Game.Items;
using WingsEmu.Game.Managers.StaticData;
using WingsEmu.Game.Maps;
using WingsEmu.Game.Networking.Broadcasting;
using WingsEmu.Game.Raids;
using WingsEmu.Game.Revival;
using WingsEmu.Game.Skills;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Character;
using WingsEmu.Packets.Enums.Chat;

namespace Plugin.CoreImpl.Maps.Systems
{
    public class CharacterSystem : ICharacterSystem, IMapSystem
    {
        private static readonly TimeSpan ProcessSpInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan RefreshRate = TimeSpan.FromMilliseconds(500);
        private readonly IAsyncEventPipeline _asyncEventPipeline;
        private readonly BCardTickSystem _bCardTickSystem;
        private readonly IBuffFactory _buffFactory;
        private readonly List<IPlayerEntity> _characters = new();
        private readonly ConcurrentDictionary<long, IPlayerEntity> _charactersById = new();
        private readonly IGameLanguageService _gameLanguage;

        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);
        private readonly IMapInstance _mapInstance;
        private readonly IMeditationManager _meditationManager;
        private readonly SkillCooldownSystem _skillCooldownSystem;
        private readonly ISkillsManager _skillsManager;
        private readonly SnackFoodSystem _snackFoodSystem;
        private readonly ISpyOutManager _spyOutManager;
        private readonly ConcurrentQueue<IPlayerEntity> _toAddPlayers = new();
        private readonly ConcurrentQueue<IPlayerEntity> _toRemovePlayers = new();

        private DateTime _lastProcess;

        public CharacterSystem(IBCardEffectHandlerContainer bcardHandlers, IBuffFactory buffFactory, IMeditationManager meditationManager, IAsyncEventPipeline asyncEventPipeline,
            IMapInstance mapInstance, ISpyOutManager spyOutManager, IRandomGenerator randomGenerator, ISkillsManager skillsManager,
            GameMinMaxConfiguration gameMinMaxConfiguration, IGameLanguageService gameLanguage)
        {
            _buffFactory = buffFactory;
            _meditationManager = meditationManager;
            _asyncEventPipeline = asyncEventPipeline;
            _mapInstance = mapInstance;
            _spyOutManager = spyOutManager;
            _skillsManager = skillsManager;
            _gameLanguage = gameLanguage;
            _snackFoodSystem = new SnackFoodSystem(gameMinMaxConfiguration);
            _bCardTickSystem = new BCardTickSystem(bcardHandlers, randomGenerator, _buffFactory, _gameLanguage);
            _lastProcess = DateTime.MinValue;
            _skillCooldownSystem = new SkillCooldownSystem();
        }

        public IPlayerEntity GetCharacterById(long id) => _charactersById.GetOrDefault(id);

        public IReadOnlyList<IPlayerEntity> GetCharacters()
        {
            _lock.EnterReadLock();
            try
            {
                return _characters.ToArray();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public IReadOnlyList<IPlayerEntity> GetCharacters(Func<IPlayerEntity, bool> predicate)
        {
            _lock.EnterReadLock();
            try
            {
                return _characters.FindAll(s => s != null && (predicate == null || predicate(s)));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }


        public IReadOnlyList<IPlayerEntity> GetCharactersInRange(Position position, short range, Func<IPlayerEntity, bool> predicate)
        {
            _lock.EnterReadLock();
            try
            {
                return _characters.FindAll(s => s != null && position.IsInAoeZone(s.Position, range) && (predicate == null || predicate(s)));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public IReadOnlyList<IPlayerEntity> GetCharactersInRange(Position pos, short distance) => GetCharactersInRange(pos, distance, null);

        public IReadOnlyList<IPlayerEntity> GetClosestCharactersInRange(Position pos, short distance)
        {
            _lock.EnterReadLock();
            try
            {
                List<IPlayerEntity> toReturn = _characters.FindAll(s => s != null && s.IsAlive() && pos.IsInAoeZone(s.Position, distance));
                toReturn.Sort((prev, next) => prev.Position.GetDistance(pos) - next.Position.GetDistance(pos));

                return toReturn;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public IReadOnlyList<IPlayerEntity> GetAliveCharacters() => GetCharacters(null);

        public IReadOnlyList<IPlayerEntity> GetAliveCharacters(Func<IPlayerEntity, bool> predicate)
        {
            _lock.EnterReadLock();
            try
            {
                return _characters.FindAll(s => s != null && s.IsAlive() && (predicate == null || predicate(s)));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }


        public IReadOnlyList<IPlayerEntity> GetAliveCharactersInRange(Position position, short range, Func<IPlayerEntity, bool> predicate)
        {
            _lock.EnterReadLock();
            try
            {
                return _characters.FindAll(s => s != null && s.IsAlive() && position.IsInAoeZone(s.Position, range) && (predicate == null || predicate(s)));
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public IReadOnlyList<IPlayerEntity> GetAliveCharactersInRange(Position pos, short distance) => GetCharactersInRange(pos, distance, null);

        public void AddCharacter(IPlayerEntity character)
        {
            _toAddPlayers.Enqueue(character);
        }

        public void RemoveCharacter(IPlayerEntity entity)
        {
            _toRemovePlayers.Enqueue(entity);
        }

        public string Name => nameof(CharacterSystem);

        public void ProcessTick(DateTime date, bool isTickRefresh = false)
        {
            if (_lastProcess + RefreshRate > date)
            {
                return;
            }

            _lastProcess = date;
            Update(date);
        }

        public void PutIdleState()
        {
            _bCardTickSystem.Clear();
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _charactersById.Clear();
                _characters.Clear();
                _toAddPlayers.Clear();
                _toRemovePlayers.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private void Update(DateTime date)
        {
            _lock.EnterWriteLock();
            try
            {
                while (_toRemovePlayers.TryDequeue(out IPlayerEntity player))
                {
                    RemovePrivateCharacter(player);
                }

                while (_toAddPlayers.TryDequeue(out IPlayerEntity player))
                {
                    AddPrivateCharacter(player);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            foreach (IPlayerEntity character in _characters)
            {
                Update(date, character);
            }
        }

        private void ProcessDayAndNight(in DateTime date, in IPlayerEntity character, in IReadOnlyList<string> packets)
        {
            if (character.LastDayNight.AddSeconds(50) >= date)
            {
                return;
            }

            character.LastDayNight = date;
            character.Session.SendPackets(packets);
        }

        private void Update(in DateTime date, in IPlayerEntity character)
        {
            ProcessRevivalEvents(date, character);
            RemoveManaOnDeath(date, character);
            HealthHeal(date, character);
            ProcessSpecialist(date, character);
            ProcessArchmageTeleport(date, character);
            _bCardTickSystem.ProcessUpdate(character, date);
            _skillCooldownSystem.Update(character, date);
            _snackFoodSystem.ProcessUpdate(character, date);
            ProcessSkillReset(date, character);
            ProcessSpSkillReset(date, character);
            ProcessWeedingBuffCheck(character, date);
            ProcessMinigameEffect(date, character);
            ProcessRandomTeleport(date, character);
            ProcessMuteMessage(date, character);
            ProcessArenaImmunity(date, character);
            ProcessBlockedZone(character);

            RefreshRaidStat(date, character);
            ProcessCharacterEffects(date, character);
            ProcessBubble(date, character);
            ProcessItemsToRemove(date, character);
            ProcessExpiredStaticBonus(date, character);

            ProcessMeditation(date, character);
            ProcessComboSkills(date, character);
            ProcessSpyOut(date, character);

            if (!character.UseSp || character.Specialist == null)
            {
                return;
            }

            ProcessSpPointsRemoving(date, character);
        }

        private void ProcessBlockedZone(in IPlayerEntity character)
        {
            if (character.Session.IsGameMaster())
            {
                return;
            }

            if (character.MapInstance == null)
            {
                return;
            }

            if (!character.MapInstance.IsBlockedZone(character.Position.X, character.Position.Y))
            {
                return;
            }

            Position getRandomPosition = character.MapInstance.GetRandomPosition();
            character.ChangePosition(getRandomPosition);
            character.Session.SendCondPacket();
            character.Session.BroadcastTeleportPacket();
        }

        private void ProcessArenaImmunity(in DateTime date, in IPlayerEntity character)
        {
            if (character.MapInstance?.MapInstanceType != MapInstanceType.ArenaInstance)
            {
                return;
            }

            if (!character.ArenaImmunity.HasValue)
            {
                return;
            }

            if (character.ArenaImmunity.Value.AddSeconds(5) > date)
            {
                return;
            }

            character.ArenaImmunity = null;
            character.Session.SendChatMessage(character.Session.GetLanguage(GameDialogKey.ARENA_CHATMESSAGE_PVP_ACTIVE), ChatMessageColorType.Yellow);
        }

        private void ProcessRandomTeleport(in DateTime date, in IPlayerEntity character)
        {
            if (!character.RandomMapTeleport.HasValue)
            {
                return;
            }

            if (character.RandomMapTeleport.Value.AddSeconds(1.5) > date)
            {
                return;
            }

            Position randomPosition = character.MapInstance.GetRandomPosition();
            character.TeleportOnMap(randomPosition.X, randomPosition.Y);
            character.RandomMapTeleport = null;
            character.BroadcastEffectGround(EffectType.VehicleTeleportation, character.PositionX, character.PositionY, false);
        }

        private void ProcessMuteMessage(in DateTime date, in IPlayerEntity character)
        {
            if (!character.MuteRemainingTime.HasValue)
            {
                return;
            }

            if (character.GameStartDate.AddSeconds(1) > date)
            {
                return;
            }

            character.MuteRemainingTime -= date - character.LastMuteTick;
            character.LastMuteTick = date;
            if (character.MuteRemainingTime.Value.TotalMilliseconds <= 0)
            {
                character.MuteRemainingTime = null;
                character.LastChatMuteMessage = null;

                AccountPenaltyDto penalty = character.Session.Account.Logs.FirstOrDefault(x => x.RemainingTime.HasValue && x.PenaltyType == PenaltyType.Muted);
                if (penalty == null)
                {
                    return;
                }

                penalty.RemainingTime = null;

                return;
            }

            character.LastChatMuteMessage ??= DateTime.MinValue;
            if (character.LastChatMuteMessage.Value.AddMinutes(1) > date)
            {
                return;
            }

            character.LastChatMuteMessage = date;
            string timeLeft = character.MuteRemainingTime.Value.ToString(@"hh\:mm\:ss");
            character.Session.SendChatMessage(character.Session.GetLanguageFormat(GameDialogKey.MUTE_CHATMESSAGE_TIME_LEFT, timeLeft), ChatMessageColorType.Green);
        }

        private void ProcessExpiredStaticBonus(in DateTime date, in IPlayerEntity character)
        {
            if (character.Bonus == null)
            {
                return;
            }

            if (!character.Bonus.Any())
            {
                return;
            }

            if (character.BonusesToRemove.AddMinutes(1) > date)
            {
                return;
            }

            character.BonusesToRemove = date;
            character.Session.EmitEvent(new CharacterBonusExpiredEvent());
        }

        private void ProcessWeedingBuffCheck(in IPlayerEntity character, in DateTime date)
        {
            if (!character.IsInGroup())
            {
                return;
            }

            if (character.CheckWeedingBuff == null)
            {
                return;
            }

            if (character.CheckWeedingBuff.Value.AddSeconds(2) > date)
            {
                return;
            }

            character.CheckWeedingBuff = null;
            character.Session.EmitEvent(new GroupWeedingEvent());
        }

        private void ProcessSpSkillReset(in DateTime date, in IPlayerEntity character)
        {
            if (!character.SkillComponent.ResetSpSkillCooldowns.HasValue)
            {
                return;
            }

            if (character.SkillComponent.ResetSpSkillCooldowns.Value.AddMilliseconds(500) > date)
            {
                return;
            }

            if (!character.UseSp || character.Specialist == null)
            {
                return;
            }

            character.Session.LearnSpSkill(_skillsManager, _gameLanguage);
            character.SkillComponent.ResetSpSkillCooldowns = null;
        }

        private void ProcessSkillReset(in DateTime date, in IPlayerEntity character)
        {
            if (!character.SkillComponent.ResetSkillCooldowns.HasValue)
            {
                return;
            }

            if (character.SkillComponent.ResetSkillCooldowns.Value.AddMilliseconds(500) > date)
            {
                return;
            }

            character.Session.LearnAdventurerSkill(_skillsManager, _gameLanguage);
            character.SkillComponent.ResetSkillCooldowns = null;
        }

        private void ProcessArchmageTeleport(in DateTime date, in IPlayerEntity character)
        {
            if (character.SkillComponent.SendTeleportPacket == null)
            {
                return;
            }

            if (character.SkillComponent.SendTeleportPacket.Value.AddMilliseconds(500) > date)
            {
                return;
            }

            SkillInfo fakeTeleport = character.GetFakeTeleportSkill();
            character.SkillComponent.SendTeleportPacket = null;
            character.Session.SendSkillCooldownResetAfter(fakeTeleport.CastId, (short)character.ApplyCooldownReduction(fakeTeleport));
        }

        private void ProcessSpecialist(in DateTime date, in IPlayerEntity character)
        {
            if (!character.SpCooldownEnd.HasValue)
            {
                return;
            }

            if (character.SpCooldownEnd.Value > date)
            {
                return;
            }

            character.SpCooldownEnd = null;
            character.Session.SendChatMessage(_gameLanguage.GetLanguage(GameDialogKey.SPECIALIST_CHATMESSAGE_TRANSFORM_DISAPPEAR, character.Session.UserLanguage), ChatMessageColorType.Yellow);
            character.Session.ResetSpCooldownUi();
        }

        private void ProcessItemsToRemove(in DateTime date, in IPlayerEntity character)
        {
            if (character.ItemsToRemove.AddSeconds(10) > date)
            {
                return;
            }

            character.ItemsToRemove = date;
            character.Session.EmitEvent(new InventoryExpiredItemsEvent());
        }

        private void ProcessSpyOut(DateTime time, IPlayerEntity character)
        {
            if (!_spyOutManager.ContainsSpyOut(character.Id))
            {
                return;
            }

            if (character.SpyOutStart.AddMinutes(2) > time)
            {
                return;
            }

            character.Session.SendObArPacket();
            _spyOutManager.RemoveSpyOutSkill(character.Id);
        }

        private static void ProcessRevivalEvents(in DateTime date, IPlayerEntity character)
        {
            if (character.RevivalDateTimeForExecution <= date)
            {
                character.DisableRevival();
                character.Session.EmitEvent(new RevivalReviveEvent(character.RevivalType, character.ForcedType));
                return;
            }

            if (character.IsAlive())
            {
                return;
            }

            if (character.AskRevivalDateTimeForExecution > date)
            {
                return;
            }

            character.DisableAskRevival();
            character.Session.EmitEvent(new RevivalAskEvent(character.AskRevivalType));
        }

        private void ProcessBubble(in DateTime date, IPlayerEntity character)
        {
            if (!character.IsUsingBubble())
            {
                return;
            }

            if (character.Bubble.AddMinutes(30) > date)
            {
                return;
            }

            character.RemoveBubble();
        }

        private void RemoveManaOnDeath(in DateTime date, IPlayerEntity character)
        {
            if (character.LastHealth.AddSeconds(1) > date)
            {
                return;
            }

            if (character.Hp != 0)
            {
                return;
            }

            character.Mp = 0;
            character.Session.RefreshStat();
            character.LastHealth = date;
        }

        private void HealthHeal(in DateTime date, IPlayerEntity character)
        {
            if (!character.IsAlive())
            {
                return;
            }

            if (character.LastHealth.AddSeconds(2) > date && (!character.IsSitting || character.LastHealth.AddSeconds(1.5) > date))
            {
                return;
            }

            character.LastHealth = date;
            if (character.LastDefence.AddSeconds(4) > date || character.LastSkillUse.AddSeconds(2) > date)
            {
                return;
            }

            character.Hp += character.Hp + character.HealthHpLoad() < character.MaxHp ? character.HealthHpLoad() : character.MaxHp - character.Hp;
            character.Mp += character.Mp + character.HealthMpLoad() < character.MaxMp ? character.HealthMpLoad() : character.MaxMp - character.Mp;
            character.Session.RefreshStat();
        }

        private void ProcessMinigameEffect(in DateTime date, IPlayerEntity character)
        {
            if (character.LastEffectMinigame.AddSeconds(3) > date)
            {
                return;
            }

            if (character.CurrentMinigame == 0)
            {
                return;
            }

            character.Session.BroadcastEffectInRange(character.CurrentMinigame);
            character.LastEffectMinigame = date;
        }

        private void RefreshRaidStat(in DateTime date, IPlayerEntity character)
        {
            if (!character.IsInRaidParty)
            {
                return;
            }

            if (character.Session.CurrentMapInstance?.MapInstanceType != MapInstanceType.RaidInstance)
            {
                return;
            }

            character.Session.SendRaidPacket(RaidPacketType.REFRESH_MEMBERS_HP_MP);
        }

        private void ProcessCharacterEffects(in DateTime date, IPlayerEntity character)
        {
            if (character.IsInvisible())
            {
                return;
            }

            if (character.RainbowBattleComponent.IsInRainbowBattle)
            {
                if ((date - character.LastRainbowEffects).TotalSeconds >= 1)
                {
                    character.LastRainbowEffects = date;
                    RainbowBattleTeamType team = character.RainbowBattleComponent.Team;
                    EffectType effectType = team switch
                    {
                        RainbowBattleTeamType.Red => EffectType.RedTeam,
                        RainbowBattleTeamType.Blue => EffectType.BlueTeam
                    };

                    character.Session.BroadcastEffect(effectType);

                    if (character.RainbowBattleComponent.IsFrozen)
                    {
                        character.Session.BroadcastEffect(EffectType.Frozen);
                    }
                }
            }

            if (character.LastEffect.AddSeconds(5) > date)
            {
                return;
            }

            character.LastEffect = date;

            if (character.HasBuff(BuffVnums.WEDDING))
            {
                character.Session.BroadcastEffect(EffectType.MediumHearth);
            }

            if (character.IsInRaidParty)
            {
                if (character.IsRaidLeader(character.Id))
                {
                    character.Session.BroadcastEffect(EffectType.OtherRaidLeader, new ExceptRaidBroadcast(character.Raid.Id));
                    character.Session.BroadcastEffect(EffectType.OwnRaidLeader, new InRaidBroadcast(character.Raid));
                }
                else
                {
                    if (character.MapInstance.MapInstanceType == MapInstanceType.RaidInstance)
                    {
                        return;
                    }

                    character.Session.BroadcastEffect(EffectType.OtherRaidMember, new ExceptRaidBroadcast(character.Raid.Id));
                    character.Session.BroadcastEffect(EffectType.OwnRaidMember, new InRaidBroadcast(character.Raid));
                }
            }

            if (character.Specialist is { ItemVNum: (short)ItemVnums.JAJAMARU_SP } && character.UseSp)
            {
                if (character.MateComponent.GetTeamMember(x => x.MateType == MateType.Partner && x.MonsterVNum == (short)MonsterVnum.SAKURA) != null)
                {
                    character.BroadcastEffectInRange(EffectType.Heart);
                }
            }

            GameItemInstance amulet = character.Amulet;
            if (amulet == null)
            {
                return;
            }

            if (character.Invisible || character.CheatComponent.IsInvisible)
            {
                return;
            }

            if (amulet.GameItem.EffectValue == -1)
            {
                return;
            }

            if (amulet.ItemVNum is (int)ItemVnums.DRACO_AMULET or (int)ItemVnums.GLACERUS_AMULET)
            {
                character.Session.BroadcastEffectInRange(amulet.GameItem.EffectValue + (character.Class == ClassType.Adventurer ? 0 : (byte)character.Class - 1));
            }
            else
            {
                character.Session.BroadcastEffectInRange(amulet.GameItem.EffectValue);
            }
        }

        private void ProcessMeditation(in DateTime date, IPlayerEntity character)
        {
            if (!_meditationManager.HasMeditation(character))
            {
                return;
            }

            // Get all the meditations from the character
            List<(short, DateTime)> meditations = _meditationManager.GetAllMeditations(character);
            foreach ((short meditationId, DateTime meditationStart) in meditations.ToList())
            {
                // If that meditation is not ready to start, go next
                if (meditationStart >= date)
                {
                    continue;
                }

                character.Session.BroadcastEffectInRange(meditationId == (short)BuffVnums.SPIRIT_OF_STRENGTH
                    ? EffectType.MeditationFinalStage
                    : EffectType.MeditationFirstStage);

                // Removes one buff or another depending of the current meditation state
                Buff firstBuff, secondBuff;
                switch (meditationId)
                {
                    case (short)BuffVnums.SPIRIT_OF_ENLIGHTENMENT:
                        firstBuff = character.BuffComponent.GetBuff((short)BuffVnums.SPIRIT_OF_TEMPERANCE);
                        secondBuff = character.BuffComponent.GetBuff((short)BuffVnums.SPIRIT_OF_STRENGTH);
                        character.Session.SendSound(SoundType.MEDITATION_FIRST);
                        break;
                    case (short)BuffVnums.SPIRIT_OF_TEMPERANCE:
                        firstBuff = character.BuffComponent.GetBuff((short)BuffVnums.SPIRIT_OF_ENLIGHTENMENT);
                        secondBuff = character.BuffComponent.GetBuff((short)BuffVnums.SPIRIT_OF_STRENGTH);
                        character.Session.SendSound(SoundType.MEDITATION_SECOND);
                        break;
                    case (short)BuffVnums.SPIRIT_OF_STRENGTH:
                        firstBuff = character.BuffComponent.GetBuff((short)BuffVnums.SPIRIT_OF_ENLIGHTENMENT);
                        secondBuff = character.BuffComponent.GetBuff((short)BuffVnums.SPIRIT_OF_TEMPERANCE);
                        character.Session.SendSound(SoundType.MEDITATION_THIRD);
                        break;
                    default:
                        firstBuff = null;
                        secondBuff = null;
                        break;
                }

                character.RemoveBuffAsync(false, firstBuff).ConfigureAwait(false).GetAwaiter().GetResult();
                character.RemoveBuffAsync(false, secondBuff).ConfigureAwait(false).GetAwaiter().GetResult();

                Buff actualBuff = _buffFactory.CreateBuff(meditationId, character);
                character.AddBuffAsync(actualBuff).GetAwaiter().GetResult();
                _meditationManager.RemoveMeditation(character, meditationId);
            }
        }

        private void ProcessComboSkills(in DateTime date, IPlayerEntity character)
        {
            ComboSkillState comboSkillState = character.GetComboState();
            if (comboSkillState == null)
            {
                return;
            }

            if (character.AngelElement.HasValue)
            {
                return;
            }

            if (!character.LastSkillCombo.HasValue)
            {
                return;
            }

            if (character.LastSkillCombo.Value.AddSeconds(5) > date)
            {
                return;
            }

            character.LastSkillCombo = null;
            character.Session.SendMsCPacket(0);
            character.Session.RefreshQuicklist();
            character.CleanComboState();
        }

        private void ProcessSpPointsRemoving(in DateTime date, IPlayerEntity character)
        {
            if (character.HasBuff(BuffVnums.RAINBOW_ENERGY))
            {
                return;
            }

            if (!character.IsAlive())
            {
                return;
            }

            if (character.Skills.All(s => s.LastUse <= DateTime.UtcNow))
            {
                return;
            }

            if (character.LastSkillUse.AddSeconds(15) <= date)
            {
                if (!character.IsRemovingSpecialistPoints)
                {
                    return;
                }

                character.IsRemovingSpecialistPoints = false;
                character.Session.SendScpPacket(0);
                character.Session.RefreshSpPoint();
                character.InitialScpPacketSent = false;
                return;
            }

            if (character.LastSpRemovingProcess.AddSeconds(1) <= date)
            {
                character.LastSpRemovingProcess = date;
                character.IsRemovingSpecialistPoints = true;
                RemoveSpecialistPoints(character);
            }

            if (character.LastSpPacketSent + ProcessSpInterval > date)
            {
                return;
            }

            character.Session.RefreshSpPoint();
            character.LastSpPacketSent = date;
        }

        private void RemoveSpecialistPoints(IPlayerEntity character)
        {
            if (!character.InitialScpPacketSent)
            {
                character.Session.SendScpPacket(1);
                character.InitialScpPacketSent = true;
            }

            byte spPoints = character.Specialist.GameItem.SpPointsUsage;

            if (character.SpPointsBasic == 0 && character.SpPointsBonus == 0)
            {
                if (character.IsOnVehicle)
                {
                    character.Session.EmitEvent(new RemoveVehicleEvent(true));
                }

                character.Session.EmitEvent(new SpUntransformEvent());
                character.Session.SendMsg(_gameLanguage.GetLanguage(GameDialogKey.SPECIALIST_SHOUTMESSAGE_NO_POINTS, character.Session.UserLanguage), MsgMessageType.Middle);
                return;
            }

            character.SpPointsBasic = character.SpPointsBasic - spPoints < 0 ? 0 : character.SpPointsBasic - spPoints;
        }

        private void AddPrivateCharacter(IPlayerEntity character)
        {
            if (_characters.Contains(character))
            {
                return;
            }

            _charactersById.TryAdd(character.Id, character);
            _characters.Add(character);
        }

        private void RemovePrivateCharacter(IPlayerEntity character)
        {
            _charactersById.TryRemove(character.Id, out _);
            _characters.Remove(character);
        }
    }
}