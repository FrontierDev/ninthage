using System.Collections.Generic;
using Game.Shared.Data;
using TMPro;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public sealed class UI_ReputationBodyPanel : UI_Panel, IListPanel
    {
        private static UI_ReputationBodyPanel _instance;
        public static UI_ReputationBodyPanel Instance => _instance;

        [SerializeField] private UI_ProgressBar progressBar;
        [SerializeField] private Image factionIcon;
        [SerializeField] private TMP_Text factionNameText;
        [SerializeField] private TMP_Text reputationValueText;
        [SerializeField] private TMP_Text reputationRankText;
        [SerializeField] private TMP_Text factionDescriptionText;

        private FactionDefinition currentFaction;
        private float currentProgress;

        [SerializeField] private GameObject container;
        [SerializeField] private UI_ReputationRewardEntry slotEntryPrefab;
        [SerializeField] private GameObject markerPrefab;
        GameObject IListPanel.contentContainer => container;
        GameObject IListPanel.entryPrefab => slotEntryPrefab.gameObject;
        public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();

        protected override void Awake()
        {
            _instance = this;
            base.Awake();
        }

        public void SetFaction(FactionDefinition faction, float progress)
        {
            currentFaction = faction;
            currentProgress = progress;

            // Set the progress bar
            progressBar.SetProgress(progress);

            // Set the name, icon and description
            factionNameText.text = currentFaction.DisplayName;
            factionIcon.sprite = currentFaction.Icon;
            factionDescriptionText.text = currentFaction.Description;

            // Calculate reputation value from progress
            int reputationValue;
            if (currentFaction.CanWar)
            {
                // Reputation ranges from -maxPoints to +maxPoints
                reputationValue = Mathf.RoundToInt(-currentFaction.MaxPoints + (progress * 2 * currentFaction.MaxPoints));
            }
            else
            {
                // Reputation ranges from 0 to +maxPoints
                reputationValue = Mathf.RoundToInt(progress * currentFaction.MaxPoints);
            }

            // Display reputation value
            if (reputationValueText != null)
                reputationValueText.text = $"{reputationValue} / {currentFaction.MaxPoints}";

            // Find current rank/tier based on progress
            string currentRank = "Unknown";
            bool isPositiveRank = false;
            var rewards = currentFaction.RewardDescriptions;
            if (rewards.Count > 0)
            {
                // Find the tier that the current progress falls into
                for (int i = 0; i < rewards.Count; i++)
                {
                    if (progress <= rewards[i].threshold)
                    {
                        currentRank = rewards[i].title;
                        isPositiveRank = rewards[i].isPositive;
                        break;
                    }
                }
                // If progress exceeds all thresholds, use the last tier
                if (progress > rewards[rewards.Count - 1].threshold)
                {
                    currentRank = rewards[rewards.Count - 1].title;
                    isPositiveRank = rewards[rewards.Count - 1].isPositive;
                }
            }

            if (reputationRankText != null)
            {
                reputationRankText.text = $"Current Rank: {currentRank}";
                // Color based on positive/negative rank
                reputationRankText.color = isPositiveRank
                    ? new Color(0.22f, 0.725f, 0.525f)  // Green for positive
                    : new Color(0.91f, 0.541f, 0.322f); // Orange for negative
            }

            PopulateList(true);
            Refresh();
        }

        private void ClearEntries()
        {
            foreach (var entry in currentEntries)
            {
                Destroy(entry.gameObject);
            }
            currentEntries.Clear();
        }

        public void PopulateList(bool forceClear = false)
        {
            if (forceClear)
                ClearEntries();

            if (currentFaction == null) return;

            var rewardsList = new List<FactionRewardDescriptionEntry>(currentFaction.RewardDescriptions);

            // Clear any existing markers
            if (markerPrefab != null)
            {
                var progressBarTransform = progressBar.transform;
                foreach (Transform child in progressBarTransform)
                {
                    if (child.gameObject != progressBar.gameObject)
                        Destroy(child.gameObject);
                }
            }

            for (int i = 0; i < rewardsList.Count; i++)
            {
                var reward = rewardsList[i];
                var entryObj = Instantiate(slotEntryPrefab.gameObject, container.transform);
                var entry = entryObj.GetComponent<UI_ReputationRewardEntry>();
                entry.Initialize(reward, i);

                // Calculate threshold range for this tier
                float minThreshold = i == 0 ? 0f : rewardsList[i - 1].threshold;
                float maxThreshold = reward.threshold;

                // Convert normalized thresholds to actual reputation points
                int minRep, maxRep;
                if (currentFaction.CanWar)
                {
                    // Reputation ranges from -maxPoints to +maxPoints
                    minRep = Mathf.RoundToInt(-currentFaction.MaxPoints + (minThreshold * 2 * currentFaction.MaxPoints));
                    maxRep = Mathf.RoundToInt(-currentFaction.MaxPoints + (maxThreshold * 2 * currentFaction.MaxPoints));
                }
                else
                {
                    // Reputation ranges from 0 to +maxPoints
                    minRep = Mathf.RoundToInt(minThreshold * currentFaction.MaxPoints);
                    maxRep = Mathf.RoundToInt(maxThreshold * currentFaction.MaxPoints);
                }

                entry.SetThresholdLevels(minRep, maxRep);
                entry.Register(this);
                currentEntries.Add(entry);

                // Check if this is the current reward tier
                float minThresholdCheck = i == 0 ? 0f : rewardsList[i - 1].threshold;
                bool isCurrentTier = (currentProgress >= minThresholdCheck && currentProgress < maxThreshold) ||
                                     (i == rewardsList.Count - 1 && currentProgress >= maxThreshold);

                if (isCurrentTier)
                {
                    entry.SetBorderColor(reward.isPositive);
                }
                else
                {
                    entry.ClearBorderColor();
                }

                // Instantiate marker on progress bar at threshold position (skip if at max)
                if (markerPrefab != null && maxThreshold < 1.0f)
                {
                    var markerObj = Instantiate(markerPrefab, progressBar.transform);
                    var markerRect = markerObj.GetComponent<RectTransform>();
                    if (markerRect != null)
                    {
                        var progressBarRect = progressBar.GetComponent<RectTransform>();
                        if (progressBarRect != null)
                        {
                            // Position marker based on normalized threshold (0-1)
                            float xPosition = (maxThreshold - 0.5f) * progressBarRect.rect.width;
                            markerRect.anchoredPosition = new Vector2(xPosition, 0);
                        }
                    }

                    // Set threshold text on marker
                    var markerText = markerObj.GetComponentInChildren<TMP_Text>();
                    if (markerText != null)
                    {
                        // Convert threshold to actual reputation value
                        int repValue;
                        if (currentFaction.CanWar)
                        {
                            repValue = Mathf.RoundToInt(-currentFaction.MaxPoints + (maxThreshold * 2 * currentFaction.MaxPoints));
                        }
                        else
                        {
                            repValue = Mathf.RoundToInt(maxThreshold * currentFaction.MaxPoints);
                        }
                        markerText.text = repValue.ToString();
                    }
                }
            }
        }
    }
}