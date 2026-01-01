using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using PhoenixLib.Logging;
using Plugin.Act4.Const;
using Plugin.Act4.Extension;
using WingsAPI.Game.Extensions.Families;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsEmu.Game;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Act4;
using WingsEmu.Game.Act4.Configuration;
using WingsEmu.Game.Act4.Entities;
using WingsEmu.Game.Act4.Event;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Families.Enum;
using WingsEmu.Game.Items;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Raids;
using WingsEmu.Packets.Enums.Chat;
using WingsEmu.Packets.Enums.Families;

namespace Plugin.Act4.Event;

public class Act4DungeonRewardEventHandler : IAsyncEventProcessor<Act4DungeonRewardEvent>
{
    private readonly Act4DungeonsConfiguration _act4DungeonsConfiguration;
    private readonly IAsyncEventPipeline _asyncEventPipeline;
    private readonly IGameItemInstanceFactory _gameItemInstance;
    private readonly IGameLanguageService _languageService;
    private readonly IRandomGenerator _randomGenerator;

    public Act4DungeonRewardEventHandler(IAsyncEventPipeline asyncEventPipeline, IGameItemInstanceFactory gameItemInstance, IRandomGenerator randomGenerator,
        Act4DungeonsConfiguration act4DungeonsConfiguration, IGameLanguageService languageService)
    {
        _asyncEventPipeline = asyncEventPipeline;
        _gameItemInstance = gameItemInstance;
        _randomGenerator = randomGenerator;
        _act4DungeonsConfiguration = act4DungeonsConfiguration;
        _languageService = languageService;
    }

    public async Task HandleAsync(Act4DungeonRewardEvent e, CancellationToken cancellation)
    {
        DungeonInstance dungeonInstance = e.DungeonInstanceWrapper.DungeonInstance;
        DungeonSubInstance bossMap = dungeonInstance.DungeonSubInstances.Values.FirstOrDefault(x => 0 < x.Bosses.Count);
        if (bossMap == null)
        {
            Log.Warn($"[ACT4_DUNGEON_SYSTEM] Can't give the Dungeon's Reward due to the impossibility of finding the bossMap. DungeonType: '{dungeonInstance.DungeonType.ToString()}'");
            await _asyncEventPipeline.ProcessEventAsync(new Act4DungeonStopEvent
            {
                DungeonInstance = dungeonInstance
            }, cancellation);
            return;
        }

        RaidReward raidReward = dungeonInstance.DungeonReward;

        dungeonInstance.FinishSlowMoDate = DateTime.UtcNow + _act4DungeonsConfiguration.DungeonSlowMoDelay;

        dungeonInstance.CleanUpBossMapDate = dungeonInstance.FinishSlowMoDate + _act4DungeonsConfiguration.DungeonBossMapClosureAfterReward;
        bossMap.AddEvent(DungeonConstEventKeys.RaidSubInstanceCleanUp, new Act4DungeonBossMapCleanUpEvent
        {
            DungeonInstance = dungeonInstance,
            BossMap = bossMap
        });

        bossMap.LoopWaves.Clear();
        bossMap.LinearWaves.Clear();

        if (dungeonInstance.DungeonType == DungeonType.Hatus)
        {
            bossMap.HatusHeads.HeadsState = HatusDragonHeadState.HIDE_HEAD;
            bossMap.MapInstance.Broadcast(Act4DungeonExtension.HatusHeadStatePacket(7, bossMap.HatusHeads));
        }

        bool createRaidFinishLog = true;

        var members = bossMap.MapInstance.Sessions.ToList();

        var randomBag = new RandomBag<RaidBoxRarity>(_randomGenerator);
        foreach (RaidBoxRarity toAdd in raidReward.RaidBox.RaidBoxRarities)
        {
            randomBag.AddEntry(toAdd, toAdd.Chance);
        }

        foreach (IClientSession member in members)
        {
            if (member == null)
            {
                continue;
            }

            if (createRaidFinishLog)
            {
                createRaidFinishLog = false;
                await member.FamilyAddLogAsync(FamilyLogType.RaidWon, ((short)dungeonInstance.DungeonType).ToString());
                await member.FamilyAddExperience(10000 / bossMap.MapInstance.Sessions.Count, FamXpObtainedFromType.Raid);
            }

            byte boxRarity = randomBag.GetRandom().Rarity;

            GameItemInstance rewardBox = _gameItemInstance.CreateItem(raidReward.RaidBox.RewardBox, 1, 0, (sbyte)boxRarity);
            await member.AddNewItemToInventory(rewardBox, true, ChatMessageColorType.Yellow, true);

            member.SendMsg(_languageService.GetLanguage(GameDialogKey.ACT4_DUNGEON_SHOUTMESSAGE_BOSS_COMPLETED, member.UserLanguage), MsgMessageType.Middle);
            member.SendEmptyRaidBoss();
        }

        await _asyncEventPipeline.ProcessEventAsync(new Act4DungeonWonEvent
        {
            DungeonInstance = dungeonInstance,
            DungeonLeader = members[0],
            Members = members
        }, cancellation);

        await _asyncEventPipeline.ProcessEventAsync(new Act4DungeonBroadcastPacketEvent
        {
            DungeonInstance = dungeonInstance
        }, cancellation);
    }
}