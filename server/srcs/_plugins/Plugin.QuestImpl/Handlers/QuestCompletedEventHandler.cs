using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PhoenixLib.Events;
using PhoenixLib.Logging;
using WingsAPI.Game.Extensions.Quests;
using WingsEmu.DTOs.Quests;
using WingsEmu.Game.Configurations;
using WingsEmu.Game.Networking;
using WingsEmu.Game.Quests;
using WingsEmu.Game.Quests.Event;

namespace Plugin.QuestImpl.Handlers
{
    public class QuestCompletedEventHandler : IAsyncEventProcessor<QuestCompletedEvent>
    {
        private readonly IPeriodicQuestsConfiguration _periodicQuestsConfiguration;
        private readonly IQuestManager _questManager;

        public QuestCompletedEventHandler(IQuestManager questManager, IPeriodicQuestsConfiguration periodicQuestsConfiguration)
        {
            _questManager = questManager;
            _periodicQuestsConfiguration = periodicQuestsConfiguration;
        }

        public async Task HandleAsync(QuestCompletedEvent e, CancellationToken cancellation)
        {
            CharacterQuest characterQuest = e.CharacterQuest;
            IClientSession session = e.Sender;
            bool claimReward = e.ClaimReward;
            bool refreshProgress = e.RefreshProgress;
            bool giveNextQuest = e.GiveNextQuest;

            Log.Debug($"[INFO] Received petition to finish quest: {characterQuest.QuestId}");

            if (!session.PlayerEntity.IsQuestCompleted(characterQuest) && !e.IgnoreNotCompletedQuest)
            {
                Log.Debug($"[INFO] Quest not completed: {characterQuest.QuestId}");
                return;
            }

            await HandleQuest(session, characterQuest, claimReward, refreshProgress, giveNextQuest);
        }

        private async Task HandleQuest(IClientSession session, CharacterQuest characterQuest, bool claimReward, bool refreshProgress, bool giveNextQuest)
        {
            session.DeleteQuestTarget(characterQuest);
            if (refreshProgress)
            {
                session.RefreshQuestProgress(_questManager, characterQuest.QuestId);
            }

            TutorialDto qPayScript = _questManager.GetQuestPayScriptByQuestId(characterQuest.QuestId);
            if (characterQuest.Quest.Prizes.Any())
            {
                if (!claimReward && qPayScript != null)
                {
                    return;
                }
            }

            await session.EmitEventAsync(new QuestRewardEvent
            {
                QuestId = characterQuest.QuestId,
                ClaimReward = claimReward
            });

            if (_periodicQuestsConfiguration.IsDailyQuest(characterQuest.Quest))
            {
                session.PlayerEntity.AddCompletedPeriodicQuest(characterQuest);
            }

            PeriodicQuestSet periodicQuestSet = characterQuest.Quest.GetPeriodicQuestSet(_questManager, _periodicQuestsConfiguration);
            if (periodicQuestSet != null) // If it's a continuation from a daily quest
            {
                if (periodicQuestSet.PerNoswingsAccount is true)
                {
                    await _questManager.TryTakeDailyQuest(session.Account.MasterAccountId, periodicQuestSet.Id);
                }
                else
                {
                    await _questManager.TryTakeDailyQuest(session.PlayerEntity.Id, periodicQuestSet.Id);
                }
            }

            await session.EmitEventAsync(new QuestRemoveEvent(characterQuest, true));

            session.UpdateQuestSqstPacket(_questManager, characterQuest.QuestId);

            if (characterQuest.SlotType != QuestSlotType.MAIN)
            {
                if (characterQuest.Quest.NextQuestId == -1)
                {
                    return;
                }

                QuestDto nextQuest = _questManager.GetQuestById(characterQuest.Quest.NextQuestId);
                if (nextQuest == null)
                {
                    Log.Debug($"The quest with ID: {characterQuest.Quest.NextQuestId} was not found.");
                    return;
                }

                if (!giveNextQuest)
                {
                    return;
                }

                await session.EmitEventAsync(new AddQuestEvent(nextQuest.Id, characterQuest.SlotType));
                return;
            }

            // This should never happen (having no prizes and having a qpayScript at the same time)
            if (!characterQuest.Quest.Prizes.Any() && qPayScript != null)
            {
                session.PlayerEntity.SaveScript(qPayScript.ScriptId, qPayScript.ScriptIndex, qPayScript.Type, DateTime.UtcNow);

                TutorialDto nextScript = _questManager.GetScriptTutorialById(qPayScript.Id + 1);
                session.SendScriptPacket(nextScript.ScriptId, nextScript.ScriptIndex);
                return;
            }

            session.SendNextQuestScriptPacket(characterQuest, _questManager);
        }
    }
}