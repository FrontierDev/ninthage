using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Persistence;
using PurrNet;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared
{
    /// <summary>Stores progress for one active quest.</summary>
    public sealed class PlayerQuestProgress
    {
        /// <summary>Gets or sets the quest definition.</summary>
        public QuestDefinition Quest;
        /// <summary>Gets or sets objective progress.</summary>
        public int[] Progress;
        /// <summary>Gets or sets whether the quest is complete.</summary>
        public bool IsCompleted;
    }

    /// <summary>Manages a player's active quest progress.</summary>
    public sealed class PlayerQuests : NetworkBehaviour
    {
        [SerializeField] private List<PlayerQuestProgress> activeQuests = new();
        /// <summary>Gets the active quests.</summary>
        public IReadOnlyList<PlayerQuestProgress> ActiveQuests => activeQuests;

        /// <summary>Raised when quest state changes.</summary>
        public Action onUpdate;

        void Awake()
        {
            CharacterEndpoint.onClientQuestUpdated += OnClientQuestUpdated;
        }

        /// <summary>Applies a quest update received by the client.</summary>
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

        /// <summary>Gets progress for an active quest.</summary>
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
