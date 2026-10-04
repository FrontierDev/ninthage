using System;
using System.Collections.Generic;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Persistence;
using PurrNet;
using UnityEngine;

namespace Game.Shared
{
    public sealed class PlayerQuestProgress
    {
        public QuestDefinition Quest;
        public int[] Progress;
        public bool IsCompleted;
    }

    public sealed class PlayerQuests : NetworkBehaviour
    {
        [SerializeField] private List<PlayerQuestProgress> activeQuests = new();
        public IReadOnlyList<PlayerQuestProgress> ActiveQuests => activeQuests;

        public Action onUpdate;

        void Awake()
        {
            CharacterService.onClientQuestUpdated += OnClientQuestUpdated;
        }

        public void OnClientQuestUpdated(string questID, int[] progress, bool isCompleted = false)
        {
            var questDef = QuestDefinitionLibrary.Instance.GetDefinition(questID);
            if (questDef == null)
            {
                Debug.LogWarning($"Received quest update for unknown quest ID: {questID}");
                return;
            }

            // Validate progress array length matches number of objectives
            if (progress.Length != questDef.Objectives.Count)
            {
                Debug.LogError($"Quest {questID} has {questDef.Objectives.Count} objectives but progress array has {progress.Length} elements");
                return;
            }

            var existingProgress = activeQuests.Find(q => q.Quest.DefinitionId == questID);
            if (existingProgress != null)
            {
                existingProgress.Progress = progress;
                existingProgress.IsCompleted = isCompleted;
            }
            else
            {
                activeQuests.Add(new PlayerQuestProgress
                {
                    Quest = questDef,
                    Progress = progress,
                    IsCompleted = isCompleted
                });
            }

            onUpdate?.Invoke();
        }

        /// <summary>
        /// Loads quest data from the given character data.
        /// </summary>
        /// <param name="data"></param>
        public void LoadQuests(CharacterData data)
        {
            activeQuests.Clear();

            foreach (var entry in data.SavedQuests)
            {
                var questDef = QuestDefinitionLibrary.Instance.GetDefinition(entry.QuestID);
                if (questDef == null)
                {
                    Debug.LogWarning($"Loaded quest data for unknown quest ID: {entry.QuestID}");
                    continue;
                }

                // Validate progress array length matches number of objectives
                if (entry.Progress.Length != questDef.Objectives.Count)
                {
                    Debug.LogError($"Saved quest {entry.QuestID} has {questDef.Objectives.Count} objectives but progress array has {entry.Progress.Length} elements");
                    continue;
                }

                activeQuests.Add(new PlayerQuestProgress
                {
                    Quest = questDef,
                    Progress = entry.Progress,
                    IsCompleted = entry.IsCompleted
                });

                Debug.Log($"Loaded quest {entry.QuestID} with progress [{string.Join(", ", entry.Progress)}] and completion status {entry.IsCompleted}");
            }

            onUpdate?.Invoke();
        }

        /// <summary>
        /// Marks a quest as completed and removes it from active quests.
        /// </summary>
        public void CompleteQuest(string questID)
        {
            var questProgress = activeQuests.Find(q => q.Quest.DefinitionId == questID);
            if (questProgress == null)
            {
                Debug.LogWarning($"Attempted to complete quest {questID} but it is not in active quests");
                return;
            }

            questProgress.IsCompleted = true;
            onUpdate?.Invoke();
        }

        /// <summary>
        /// Updates progress for a specific objective in a quest.
        /// </summary>
        public void UpdateObjectiveProgress(string questID, int objectiveIndex, int value)
        {
            var questProgress = activeQuests.Find(q => q.Quest.DefinitionId == questID);
            if (questProgress == null)
            {
                Debug.LogWarning($"Attempted to update progress for quest {questID} but it is not in active quests");
                return;
            }

            if (objectiveIndex < 0 || objectiveIndex >= questProgress.Progress.Length)
            {
                Debug.LogError($"Objective index {objectiveIndex} is out of bounds for quest {questID} with {questProgress.Progress.Length} objectives");
                return;
            }

            questProgress.Progress[objectiveIndex] = value;
            onUpdate?.Invoke();
        }

        public int[] GetQuestProgress(string questID)
        {
            var questProgress = activeQuests.Find(q => q.Quest.DefinitionId == questID);
            if (questProgress == null)
            {
                Debug.LogWarning($"Attempted to get progress for quest {questID} but it is not in active quests");
                return null;
            }

            return questProgress.Progress;
        }
    }
}