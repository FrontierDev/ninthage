using Game.Shared.Data;
using System.Collections.Generic;
using TMPro;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_ReputationBodyPanel</c> implementation.
    /// </summary>
    public sealed class UI_ReputationBodyPanel : UI_Panel, IListPanel
    {
        private static UI_ReputationBodyPanel _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_ReputationBodyPanel Instance => _instance;

        [FormerlySerializedAs("progressBar")]
        [SerializeField] private UI_ProgressBar _progressBar;
        [FormerlySerializedAs("factionIcon")]
        [SerializeField] private Image _factionIcon;
        [FormerlySerializedAs("factionNameText")]
        [SerializeField] private TMP_Text _factionNameText;
        [FormerlySerializedAs("reputationValueText")]
        [SerializeField] private TMP_Text _reputationValueText;
        [FormerlySerializedAs("reputationRankText")]
        [SerializeField] private TMP_Text _reputationRankText;
        [FormerlySerializedAs("factionDescriptionText")]
        [SerializeField] private TMP_Text _factionDescriptionText;

        private FactionDefinition _currentFaction;
        private float _currentProgress;

        [FormerlySerializedAs("container")]
        [SerializeField] private GameObject _container;
        [FormerlySerializedAs("slotEntryPrefab")]
        [SerializeField] private UI_ReputationRewardEntry _slotEntryPrefab;
        [FormerlySerializedAs("markerPrefab")]
        [SerializeField] private GameObject _markerPrefab;
        GameObject IListPanel.contentContainer => _container;
        GameObject IListPanel.entryPrefab => _slotEntryPrefab.gameObject;
        /// <summary>
        /// Gets the current list entries.
        /// </summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();

        protected override void Awake()
        {
            _instance = this;
            base.Awake();
        }

        /// <summary>
        /// Sets the value managed by <c>SetFaction</c>.
        /// </summary>
        public void SetFaction(FactionDefinition faction, float progress)
        {
            _currentFaction = faction;
            _currentProgress = progress;

            // Set the progress bar
            _progressBar.SetProgress(progress);

            // Set the name, icon and description
            _factionNameText.text = _currentFaction.DisplayName;
            _factionIcon.sprite = _currentFaction.Icon;
            _factionDescriptionText.text = _currentFaction.Description;

            // Calculate reputation value from progress
            int reputationValue;
            if (_currentFaction.CanWar)
            {
                // Reputation ranges from -maxPoints to +maxPoints
                reputationValue = Mathf.RoundToInt(-_currentFaction.MaxPoints + (progress * 2 * _currentFaction.MaxPoints));
            }
            else
            {
                // Reputation ranges from 0 to +maxPoints
                reputationValue = Mathf.RoundToInt(progress * _currentFaction.MaxPoints);
            }

            // Display reputation value
            if (_reputationValueText != null)
                _reputationValueText.text = $"{reputationValue} / {_currentFaction.MaxPoints}";

            // Find current rank/tier based on progress
            string currentRank = "Unknown";
            bool isPositiveRank = false;
            var rewards = _currentFaction.RewardDescriptions;
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

            if (_reputationRankText != null)
            {
                _reputationRankText.text = $"Current Rank: {currentRank}";
                // Color based on positive/negative rank
                _reputationRankText.color = isPositiveRank
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

        /// <summary>
        /// Executes the client-side <c>PopulateList</c> operation.
        /// </summary>
        public void PopulateList(bool forceClear = false)
        {
            if (forceClear)
                ClearEntries();

            if (_currentFaction == null) return;

            var rewardsList = new List<FactionRewardDescriptionEntry>(_currentFaction.RewardDescriptions);

            // Clear any existing markers
            if (_markerPrefab != null)
            {
                var progressBarTransform = _progressBar.transform;
                foreach (Transform child in progressBarTransform)
                {
                    if (child.gameObject != _progressBar.gameObject)
                        Destroy(child.gameObject);
                }
            }

            for (int i = 0; i < rewardsList.Count; i++)
            {
                var reward = rewardsList[i];
                var entryObj = Instantiate(_slotEntryPrefab.gameObject, _container.transform);
                var entry = entryObj.GetComponent<UI_ReputationRewardEntry>();
                entry.Initialize(reward, i);

                // Calculate threshold range for this tier
                float minThreshold = i == 0 ? 0f : rewardsList[i - 1].threshold;
                float maxThreshold = reward.threshold;

                // Convert normalized thresholds to actual reputation points
                int minRep, maxRep;
                if (_currentFaction.CanWar)
                {
                    // Reputation ranges from -maxPoints to +maxPoints
                    minRep = Mathf.RoundToInt(-_currentFaction.MaxPoints + (minThreshold * 2 * _currentFaction.MaxPoints));
                    maxRep = Mathf.RoundToInt(-_currentFaction.MaxPoints + (maxThreshold * 2 * _currentFaction.MaxPoints));
                }
                else
                {
                    // Reputation ranges from 0 to +maxPoints
                    minRep = Mathf.RoundToInt(minThreshold * _currentFaction.MaxPoints);
                    maxRep = Mathf.RoundToInt(maxThreshold * _currentFaction.MaxPoints);
                }

                entry.SetThresholdLevels(minRep, maxRep);
                entry.Register(this);
                currentEntries.Add(entry);

                // Check if this is the current reward tier
                float minThresholdCheck = i == 0 ? 0f : rewardsList[i - 1].threshold;
                bool isCurrentTier = (_currentProgress >= minThresholdCheck && _currentProgress < maxThreshold) ||
                                     (i == rewardsList.Count - 1 && _currentProgress >= maxThreshold);

                if (isCurrentTier)
                {
                    entry.SetBorderColor(reward.isPositive);
                }
                else
                {
                    entry.ClearBorderColor();
                }

                // Instantiate marker on progress bar at threshold position (skip if at max)
                if (_markerPrefab != null && maxThreshold < 1.0f)
                {
                    var markerObj = Instantiate(_markerPrefab, _progressBar.transform);
                    var markerRect = markerObj.GetComponent<RectTransform>();
                    if (markerRect != null)
                    {
                        var progressBarRect = _progressBar.GetComponent<RectTransform>();
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
                        if (_currentFaction.CanWar)
                        {
                            repValue = Mathf.RoundToInt(-_currentFaction.MaxPoints + (maxThreshold * 2 * _currentFaction.MaxPoints));
                        }
                        else
                        {
                            repValue = Mathf.RoundToInt(maxThreshold * _currentFaction.MaxPoints);
                        }
                        markerText.text = repValue.ToString();
                    }
                }
            }
        }
    }
}
