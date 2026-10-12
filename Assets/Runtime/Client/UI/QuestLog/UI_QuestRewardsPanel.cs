using Game.Shared.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_QuestRewardsPanel</c> implementation.
    /// </summary>
    public sealed class UI_QuestRewardsPanel : UI_Panel, IListPanel
    {
        [SerializeField] private TMP_Text choiceText;

        [SerializeField] private GameObject goldReward;
        [SerializeField] private TMP_Text goldAmountText;

        [SerializeField] private GameObject xpReward;
        [SerializeField] private TMP_Text xpAmountText;

        [SerializeField] private GameObject factionReward;
        [SerializeField] private Image factionIcon;
        [SerializeField] private TMP_Text factionReputationText;

        [SerializeField] private GameObject container;
        [SerializeField] private UI_QuestRewardsEntry slotEntryPrefab;
        GameObject IListPanel.contentContainer => container;
        GameObject IListPanel.entryPrefab => slotEntryPrefab.gameObject;
        /// <summary>
        /// Gets the current list entries.
        /// </summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();

        private QuestDefinition def;

        /// <summary>
        /// Sets the value managed by <c>SetQuestDefinition</c>.
        /// </summary>
        public void SetQuestDefinition(Shared.Data.QuestDefinition def)
        {
            this.def = def;

            goldReward.SetActive(def.GoldReward > 0);
            goldAmountText.text = $"{def.GoldReward}g";

            xpReward.SetActive(def.ExperienceReward > 0);
            xpAmountText.text = $"{def.ExperienceReward} XP";

            factionReward.SetActive(def.ReputationReward != null && def.ReputationReward.ReputationPoints > 0 && def.ReputationReward.Faction != null);
            if (factionReward.activeInHierarchy)
            {
                factionIcon.sprite = def.ReputationReward.Faction.Icon;
                factionReputationText.text = $"{def.ReputationReward.ReputationPoints}";
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
            if (forceClear) ClearEntries();

            if (def.GiveAllItems)
                choiceText.text = "all items:";
            else
                choiceText.text = "choice of items:";

            int index = 0;
            foreach (QuestItemReward reward in def.ItemRewards)
            {
                var entryObj = Instantiate(slotEntryPrefab.gameObject, container.transform);
                var entry = entryObj.GetComponent<UI_QuestRewardsEntry>();
                entry.Initialize(reward, index);
                entry.Register(this);
                index++;
            }
        }
    }
}
