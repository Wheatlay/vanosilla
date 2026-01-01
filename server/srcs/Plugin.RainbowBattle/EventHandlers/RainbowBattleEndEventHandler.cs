using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.DAL.Redis.Locks;
using PhoenixLib.Events;
using Plugin.FamilyImpl.Achievements;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsAPI.Packets.Enums;
using WingsAPI.Packets.Enums.Rainbow;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Families;
using WingsEmu.Game.Items;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Raids;
using WingsEmu.Game.RainbowBattle;
using WingsEmu.Game.RainbowBattle.Event;
using WingsEmu.Packets.Enums.Chat;

namespace Plugin.RainbowBattle.EventHandlers
{
    public class RainbowBattleEndEventHandler : IAsyncEventProcessor<RainbowBattleEndEvent>
    {
        private readonly IExpirableLockService _expirableLockService;
        private readonly IFamilyAchievementManager _familyAchievementManager;
        private readonly IFamilyMissionManager _familyMissionManager;
        private readonly IGameItemInstanceFactory _gameItemInstanceFactory;
        private readonly RainbowBattleConfiguration _rainbowBattleConfiguration;

        public RainbowBattleEndEventHandler(IFamilyMissionManager familyMissionManager, RainbowBattleConfiguration rainbowBattleConfiguration,
            IGameItemInstanceFactory gameItemInstanceFactory, IFamilyAchievementManager familyAchievementManager, IExpirableLockService expirableLockService)
        {
            _familyMissionManager = familyMissionManager;
            _rainbowBattleConfiguration = rainbowBattleConfiguration;
            _gameItemInstanceFactory = gameItemInstanceFactory;
            _familyAchievementManager = familyAchievementManager;
            _expirableLockService = expirableLockService;
        }

        public async Task HandleAsync(RainbowBattleEndEvent e, CancellationToken cancellation)
        {
            RainbowBattleParty rainbowBattleParty = e.RainbowBattleParty;
            rainbowBattleParty.FinishTime = DateTime.UtcNow.AddSeconds(15);

            RainbowBattleTeamType? winnerTeam = null;
            if (rainbowBattleParty.BluePoints > rainbowBattleParty.RedPoints)
            {
                winnerTeam = RainbowBattleTeamType.Blue;
            }
            else if (rainbowBattleParty.RedPoints > rainbowBattleParty.BluePoints)
            {
                winnerTeam = RainbowBattleTeamType.Red;
            }

            switch (winnerTeam)
            {
                case RainbowBattleTeamType.Red:
                    await ProcessWin(rainbowBattleParty, RainbowBattleTeamType.Red);
                    await ProcessLose(rainbowBattleParty, RainbowBattleTeamType.Blue);

                    IClientSession redDummy = rainbowBattleParty.RedTeam.FirstOrDefault();
                    if (redDummy != null)
                    {
                        await redDummy.EmitEventAsync(new RainbowBattleWonEvent
                        {
                            Id = rainbowBattleParty.Id,
                            Players = rainbowBattleParty.RedTeam.Select(x => x.PlayerEntity.Id).ToArray()
                        });
                    }

                    IClientSession blueDummy = rainbowBattleParty.BlueTeam.FirstOrDefault();
                    if (blueDummy != null)
                    {
                        await blueDummy.EmitEventAsync(new RainbowBattleLoseEvent
                        {
                            Id = rainbowBattleParty.Id,
                            Players = rainbowBattleParty.BlueTeam.Select(x => x.PlayerEntity.Id).ToArray()
                        });
                    }

                    return;
                case RainbowBattleTeamType.Blue:
                    await ProcessWin(rainbowBattleParty, RainbowBattleTeamType.Blue);
                    await ProcessLose(rainbowBattleParty, RainbowBattleTeamType.Red);

                    redDummy = rainbowBattleParty.RedTeam.FirstOrDefault();
                    if (redDummy != null)
                    {
                        await redDummy.EmitEventAsync(new RainbowBattleLoseEvent
                        {
                            Players = rainbowBattleParty.RedTeam.Select(x => x.PlayerEntity.Id).ToArray()
                        });
                    }

                    blueDummy = rainbowBattleParty.BlueTeam.FirstOrDefault();
                    if (blueDummy != null)
                    {
                        await blueDummy.EmitEventAsync(new RainbowBattleWonEvent
                        {
                            Players = rainbowBattleParty.BlueTeam.Select(x => x.PlayerEntity.Id).ToArray()
                        });
                    }

                    return;
                case null:
                    await ProcessLose(rainbowBattleParty, RainbowBattleTeamType.Blue);
                    await ProcessLose(rainbowBattleParty, RainbowBattleTeamType.Red);

                    IClientSession dummy = rainbowBattleParty.RedTeam.FirstOrDefault();
                    if (dummy != null)
                    {
                        await dummy.EmitEventAsync(new RainbowBattleTieEvent
                        {
                            RedTeam = rainbowBattleParty.RedTeam.Select(x => x.PlayerEntity.Id).ToArray(),
                            BlueTeam = rainbowBattleParty.BlueTeam.Select(x => x.PlayerEntity.Id).ToArray()
                        });
                    }

                    return;
            }
        }

        private async Task ProcessWin(RainbowBattleParty rainbowBattleParty, RainbowBattleTeamType team)
        {
            IReadOnlyList<IClientSession> members = team == RainbowBattleTeamType.Red ? rainbowBattleParty.RedTeam : rainbowBattleParty.BlueTeam;
            string removeClock = RainbowBattleExtensions.GenerateRainbowTime(RainbowTimeType.End);

            short neededPoints = _rainbowBattleConfiguration.NeededActivityPoints;

            foreach (IClientSession member in members)
            {
                member.SendMsg(member.GetLanguage(GameDialogKey.RAINBOW_BATTLE_MESSAGE_YOU_WON), MsgMessageType.Middle);
                member.SendChatMessage(member.GetLanguage(GameDialogKey.RAINBOW_BATTLE_MESSAGE_YOU_WON), ChatMessageColorType.Yellow);
                member.SendEmptyRaidBoss();
                member.SendPacket(removeClock);

                if (member.PlayerEntity.RainbowBattleComponent.ActivityPoints < neededPoints)
                {
                    member.SendChatMessage(member.GetLanguage(GameDialogKey.RAINBOW_BATTLE_CHATMESSAGE_NOT_ENOUGH_ACTIVITY_POINTS), ChatMessageColorType.Red);
                    continue;
                }

                if (member.PlayerEntity.RainbowBattleLeaverBusterDto is { RewardPenalty: > 0 })
                {
                    member.PlayerEntity.RainbowBattleLeaverBusterDto.RewardPenalty -= 1;

                    if (member.PlayerEntity.RainbowBattleLeaverBusterDto.RewardPenalty == 0)
                    {
                        member.SendChatMessageNoPlayer(member.GetLanguage(GameDialogKey.RAINBOW_BATTLE_CHATMESSAGE_PENALTY_NEXT_REWARD), ChatMessageColorType.IntenseRed);
                    }
                    else
                    {
                        member.SendChatMessageNoPlayer(member.GetLanguageFormat(GameDialogKey.RAINBOW_BATTLE_CHATMESSAGE_PENALTY_LEFT,
                            member.PlayerEntity.RainbowBattleLeaverBusterDto.RewardPenalty), ChatMessageColorType.IntenseRed);
                    }

                    continue;
                }

                await ProcessCoins(member, true);
                await member.EmitEventAsync(new GenerateReputationEvent
                {
                    Amount = _rainbowBattleConfiguration.ReputationMultiplier * member.PlayerEntity.Level,
                    SendMessage = true
                });

                ProcessFamilyWinMission(member);
                await ProcessFamilyAchievement(member, true);
                ProcessFamilyMission(member);
            }
        }

        private async Task ProcessLose(RainbowBattleParty rainbowBattleParty, RainbowBattleTeamType team)
        {
            IReadOnlyList<IClientSession> members = team == RainbowBattleTeamType.Red ? rainbowBattleParty.RedTeam : rainbowBattleParty.BlueTeam;
            string removeClock = RainbowBattleExtensions.GenerateRainbowTime(RainbowTimeType.End);

            short neededPoints = _rainbowBattleConfiguration.NeededActivityPoints;

            foreach (IClientSession member in members)
            {
                member.SendMsg(member.GetLanguage(GameDialogKey.RAINBOW_BATTLE_MESSAGE_YOU_LOSE), MsgMessageType.Middle);
                member.SendChatMessage(member.GetLanguage(GameDialogKey.RAINBOW_BATTLE_MESSAGE_YOU_LOSE), ChatMessageColorType.Yellow);
                member.SendRaidUiPacket(RaidType.Cuby, RaidWindowType.MISSION_FAIL);
                member.SendPacket(removeClock);

                if (member.PlayerEntity.RainbowBattleComponent.ActivityPoints < neededPoints)
                {
                    member.SendChatMessage(member.GetLanguage(GameDialogKey.RAINBOW_BATTLE_CHATMESSAGE_NOT_ENOUGH_ACTIVITY_POINTS), ChatMessageColorType.Red);
                    continue;
                }

                if (member.PlayerEntity.RainbowBattleLeaverBusterDto is { RewardPenalty: > 0 })
                {
                    member.PlayerEntity.RainbowBattleLeaverBusterDto.RewardPenalty -= 1;

                    if (member.PlayerEntity.RainbowBattleLeaverBusterDto.RewardPenalty == 0)
                    {
                        member.SendChatMessageNoPlayer(member.GetLanguage(GameDialogKey.RAINBOW_BATTLE_CHATMESSAGE_PENALTY_NEXT_REWARD), ChatMessageColorType.IntenseRed);
                    }
                    else
                    {
                        member.SendChatMessageNoPlayer(member.GetLanguageFormat(GameDialogKey.RAINBOW_BATTLE_CHATMESSAGE_PENALTY_LEFT,
                            member.PlayerEntity.RainbowBattleLeaverBusterDto.RewardPenalty), ChatMessageColorType.IntenseRed);
                    }

                    continue;
                }

                await ProcessCoins(member, false);
                await ProcessFamilyAchievement(member, false);
                ProcessFamilyMission(member);
            }
        }

        private async Task ProcessFamilyAchievement(IClientSession session, bool isWin)
        {
            if (!session.PlayerEntity.IsInFamily())
            {
                return;
            }

            IFamily family = session.PlayerEntity.Family;

            if (await _expirableLockService.TryAddTemporaryLockAsync($"game:locks:family:rainbowbattle:character:{session.PlayerEntity.Id}", DateTime.UtcNow.Date.AddDays(1)))
            {
                _familyAchievementManager.IncrementFamilyAchievement(family.Id, (short)FamilyAchievementsVnum.COMPLETE_10_RAINBOW_BATTLE);
            }

            if (!isWin)
            {
                return;
            }

            if (!await _expirableLockService.TryAddTemporaryLockAsync($"game:locks:family:rainbowbattle-win:character:{session.PlayerEntity.Id}", DateTime.UtcNow.Date.AddDays(1)))
            {
                return;
            }

            _familyAchievementManager.IncrementFamilyAchievement(family.Id, (short)FamilyAchievementsVnum.WIN_5_RAINBOW_BATTLE);
        }

        private async Task ProcessCoins(IClientSession session, bool isWinner)
        {
            GameItemInstance coin = _gameItemInstanceFactory.CreateItem((short)ItemVnums.RAINBOW_COIN, isWinner ? 3 : 1);
            await session.AddNewItemToInventory(coin, true, sendGiftIsFull: true);
        }

        private void ProcessFamilyMission(IClientSession session)
        {
            if (!session.PlayerEntity.IsInFamily())
            {
                return;
            }

            _familyMissionManager.IncrementFamilyMission(session.PlayerEntity.Family.Id, session.PlayerEntity.Id, (int)FamilyMissionVnums.DAILY_COMPLETE_10_RAINBOW_BATTLE);
        }

        private void ProcessFamilyWinMission(IClientSession session)
        {
            if (!session.PlayerEntity.IsInFamily())
            {
                return;
            }

            _familyMissionManager.IncrementFamilyMission(session.PlayerEntity.Family.Id, session.PlayerEntity.Id, (int)FamilyMissionVnums.DAILY_WIN_5_RAINBOW_BATTLE);
        }
    }
}