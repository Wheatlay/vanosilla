using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.DAL.Redis.Locks;
using PhoenixLib.Events;
using WingsAPI.Game.Extensions.Families;
using WingsAPI.Game.Extensions.ItemExtension.Inventory;
using WingsAPI.Packets.Enums;
using WingsEmu.Game;
using WingsEmu.Game._i18n;
using WingsEmu.Game.Characters.Events;
using WingsEmu.Game.Entities;
using WingsEmu.Game.Extensions;
using WingsEmu.Game.Families.Enum;
using WingsEmu.Game.Items;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Raids;
using WingsEmu.Game.Raids.Events;
using WingsEmu.Packets.Enums.Chat;

namespace Plugin.Raids;

public class RaidGiveRewardsEventHandler : IAsyncEventProcessor<RaidGiveRewardsEvent>
{
    private readonly IExpirableLockService _expirableLockService;
    private readonly IGameItemInstanceFactory _gameItemInstance;
    private readonly IRandomGenerator _randomGenerator;

    public RaidGiveRewardsEventHandler(IGameItemInstanceFactory gameItemInstance, IRandomGenerator randomGenerator, IExpirableLockService expirableLockService)
    {
        _gameItemInstance = gameItemInstance;
        _randomGenerator = randomGenerator;
        _expirableLockService = expirableLockService;
    }

    public async Task HandleAsync(RaidGiveRewardsEvent e, CancellationToken cancellation)
    {
        RaidParty raidParty = e.RaidParty;
        IMonsterEntity bossMap = e.MapBoss;
        RaidReward raidReward = e.RaidReward;

        if (bossMap == null)
        {
            return;
        }

        int reputation = 0;
        if (raidReward.DefaultReputation)
        {
            reputation = raidParty.MinimumLevel * 30;
        }
        else
        {
            if (raidReward.FixedReputation.HasValue)
            {
                reputation = raidReward.FixedReputation.Value;
            }
        }

        long leaderId = raidParty.Leader.PlayerEntity.Id;
        var randomBag = new RandomBag<RaidBoxRarity>(_randomGenerator);
        foreach (RaidBoxRarity toAdd in raidReward.RaidBox.RaidBoxRarities)
        {
            randomBag.AddEntry(toAdd, toAdd.Chance);
        }

        foreach (IClientSession member in raidParty.Members.ToList())
        {
            if (member == null)
            {
                continue;
            }

            if (member.CurrentMapInstance?.Id != bossMap.MapInstance?.Id)
            {
                continue;
            }


            RaidBoxRarity box = randomBag.GetRandom();
            byte boxRarity = box.Rarity;

            if (member.PlayerEntity.Id == leaderId && boxRarity < 4)
            {
                boxRarity = 4;
            }

            GameItemInstance rewardBox = _gameItemInstance.CreateItem(raidReward.RaidBox.RewardBox, 1, 0, (sbyte)boxRarity);
            await member.AddNewItemToInventory(rewardBox, true, ChatMessageColorType.Yellow, true);
            await member.EmitEventAsync(new RaidRewardReceivedEvent
            {
                BoxRarity = boxRarity
            });

            await ProcessFamilyExperience(member, raidParty.Type);

            await member.EmitEventAsync(new GenerateReputationEvent
            {
                Amount = reputation,
                SendMessage = true
            });
        }
    }

    private async Task ProcessFamilyExperience(IClientSession member, RaidType raidPartyType)
    {
        if (!member.PlayerEntity.IsInFamily())
        {
            return;
        }

        if (!await _expirableLockService.TryAddTemporaryLockAsync(
                $"game:locks:family:{member.PlayerEntity.Id}:raids:{(short)raidPartyType}:character:{member.PlayerEntity.Id}", DateTime.UtcNow.Date.AddDays(1)))
        {
            return;
        }

        member.SendChatMessage(member.GetLanguageFormat(GameDialogKey.FAMILY_CHATMESSAGE_XP_GAINED, 200), ChatMessageColorType.Yellow);
        await member.FamilyAddExperience(200, FamXpObtainedFromType.Raid);
    }
}