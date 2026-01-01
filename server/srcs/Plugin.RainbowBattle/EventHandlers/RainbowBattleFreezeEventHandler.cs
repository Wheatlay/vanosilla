using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using WingsAPI.Packets.Enums.Rainbow;
using WingsEmu.Game._enum;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Characters;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Entities.Extensions;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Mates;
using WingsEmu.Game.Networking;
using WingsEmu.Game.RainbowBattle;
using WingsEmu.Game.RainbowBattle.Event;
using WingsEmu.Packets.Enums;
using WingsEmu.Packets.Enums.Chat;

namespace Plugin.RainbowBattle.EventHandlers
{
    public class RainbowBattleFreezeEventHandler : IAsyncEventProcessor<RainbowBattleFreezeEvent>
    {
        private readonly RainbowBattleConfiguration _rainbowBattleConfiguration;

        public RainbowBattleFreezeEventHandler(RainbowBattleConfiguration rainbowBattleConfiguration) => _rainbowBattleConfiguration = rainbowBattleConfiguration;

        public async Task HandleAsync(RainbowBattleFreezeEvent e, CancellationToken cancellation)
        {
            IClientSession session = e.Sender;
            IBattleEntity killer = e.Killer;

            RainbowBattleParty rainbowParty = session.PlayerEntity.RainbowBattleComponent.RainbowBattleParty;
            if (rainbowParty == null)
            {
                return;
            }

            session.PlayerEntity.RainbowBattleComponent.IsFrozen = true;
            session.PlayerEntity.RainbowBattleComponent.FrozenTime = DateTime.UtcNow.AddSeconds(_rainbowBattleConfiguration.SecondsBeingFrozen);
            session.SendCondPacket();
            session.BroadcastEffect(EffectType.Frozen);
            await session.PlayerEntity.RemoveNegativeBuffs(100);

            session.PlayerEntity.Hp = session.PlayerEntity.MaxHp;
            session.PlayerEntity.Mp = session.PlayerEntity.MaxMp;
            session.RefreshStat();

            foreach (IMateEntity mate in session.PlayerEntity.MateComponent.TeamMembers())
            {
                session.PlayerEntity.MapInstance.RemoveMate(mate);
                session.PlayerEntity.MapInstance.Broadcast(mate.GenerateOut());
            }

            IReadOnlyList<IClientSession> members = session.PlayerEntity.RainbowBattleComponent.Team == RainbowBattleTeamType.Red ? rainbowParty.RedTeam : rainbowParty.BlueTeam;
            foreach (IClientSession member in members)
            {
                member.SendMsg(member.GetLanguageFormat(GameDialogKey.RAINBOW_BATTLE_SHOUTMESSAGE_FROZEN, session.PlayerEntity.Name), MsgMessageType.Middle);
            }

            session.PlayerEntity.RainbowBattleComponent.Deaths++;
            session.PlayerEntity.RainbowBattleComponent.ActivityPoints += _rainbowBattleConfiguration.DeathActivityPoints;

            IPlayerEntity playerKiller = killer switch
            {
                IPlayerEntity playerEntity => playerEntity,
                IMateEntity mateEntity => mateEntity.Owner,
                IMonsterEntity monsterEntity => monsterEntity.SummonerType != null && monsterEntity.SummonerId != null && monsterEntity.SummonerType == VisualType.Player
                    ? monsterEntity.MapInstance.GetCharacterById(monsterEntity.SummonerId.Value)
                    : null,
                _ => null
            };

            if (playerKiller?.RainbowBattleComponent.RainbowBattleParty == null)
            {
                return;
            }

            playerKiller.RainbowBattleComponent.Kills++;
            playerKiller.RainbowBattleComponent.ActivityPoints += _rainbowBattleConfiguration.KillActivityPoints;

            IReadOnlyList<IClientSession> playerMembers = playerKiller.RainbowBattleComponent.Team == RainbowBattleTeamType.Red
                ? playerKiller.RainbowBattleComponent.RainbowBattleParty.RedTeam
                : playerKiller.RainbowBattleComponent.RainbowBattleParty.BlueTeam;

            string memberList = playerKiller.RainbowBattleComponent.RainbowBattleParty.GenerateRainbowBattleWidget(playerKiller.RainbowBattleComponent.Team);
            foreach (IClientSession member in playerMembers)
            {
                member.SendPacket(memberList);
            }

            await session.EmitEventAsync(new RainbowBattleFrozenEvent
            {
                Id = playerKiller.RainbowBattleComponent.RainbowBattleParty.Id,
                Killer = new RainbowBattlePlayerDump
                {
                    CharacterId = playerKiller.Id,
                    Level = playerKiller.Level,
                    Class = playerKiller.Class,
                    Specialist = playerKiller.Specialist,
                    TotalFireResistance = playerKiller.FireResistance,
                    TotalWaterResistance = playerKiller.WaterResistance,
                    TotalLightResistance = playerKiller.LightResistance,
                    TotalDarkResistance = playerKiller.DarkResistance,
                    FairyLevel = playerKiller.Fairy?.ElementRate + playerKiller.Fairy?.GameItem.ElementRate,
                    Score = playerKiller.RainbowBattleComponent.ActivityPoints,
                    Team = playerKiller.RainbowBattleComponent.Team.ToString()
                },
                Killed = new RainbowBattlePlayerDump
                {
                    CharacterId = session.PlayerEntity.Id,
                    Level = session.PlayerEntity.Level,
                    Class = session.PlayerEntity.Class,
                    Specialist = session.PlayerEntity.Specialist,
                    TotalFireResistance = session.PlayerEntity.FireResistance,
                    TotalWaterResistance = session.PlayerEntity.WaterResistance,
                    TotalLightResistance = session.PlayerEntity.LightResistance,
                    TotalDarkResistance = session.PlayerEntity.DarkResistance,
                    FairyLevel = session.PlayerEntity.Fairy?.ElementRate + session.PlayerEntity.Fairy?.GameItem.ElementRate,
                    Score = session.PlayerEntity.RainbowBattleComponent.ActivityPoints,
                    Team = session.PlayerEntity.RainbowBattleComponent.Team.ToString()
                }
            });
        }
    }
}