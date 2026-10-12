using Game.Shared.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_QuestRewardsPanel</c> implementation.
    /// </summary>
    public sealed class UI_QuestRewardsPanel : UI_Panel, IListPanel
    {
        [FormerlySerializedAs("choiceText")]
        [SerializeField] private TMP_Text _choiceText;

        [FormerlySerializedAs("goldReward")]
        [SerializeField] private GameObject _goldReward;
        [FormerlySerializedAs("goldAmountText")]
        [SerializeField] private TMP_Text _goldAmountText;

        [FormerlySerializedAs("xpReward")]
        [SerializeField] private GameObject _xpReward;
        [FormerlySerializedAs("xpAmountText")]
        [SerializeField] private TMP_Text _xpAmountText;

        [FormerlySerializedAs("factionReward")]
        [SerializeField] private GameObject _factionReward;
        [FormerlySerializedAs("factionIcon")]
        [SerializeField] private Image _factionIcon;
        [FormerlySerializedAs("factionReputationText")]
        [SerializeField] private TMP_Text _factionReputationText;

        [FormerlySerializedAs("container")]
        [SerializeField] private GameObject _container;
        [FormerlySerializedAs("slotEntryPrefab")]
        [SerializeField] private UI_QuestRewardsEntry _slotEntryPrefab;
        GameObject IListPanel.contentContainer => _container;
        GameObject IListPanel.entryPrefab => _slotEntryPrefab.gameObject;
        /// <summary>
        /// Gets the current list entries.
        /// </summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();

        private QuestDefinition _def;

        /// <summary>
        /// Sets the value managed by <c>SetQuestDefinition</c>.
        /// </summary>
        public void SetQuestDefinition(Shared.Data.QuestDefinition def)
        {
            this._def = def;

            _goldReward.SetActive(def.GoldReward > 0);
            _goldAmountText.text = $"{def.GoldReward}g";

            _xpReward.SetActive(def.ExperienceReward > 0);
            _xpAmountText.text = $"{def.ExperienceReward} XP";

            _factionReward.SetActive(def.ReputationReward != null && def.ReputationReward.ReputationPoints > 0 && def.ReputationReward.Faction != null);
            if (_factionReward.activeInHierarchy)
            {
                _factionIcon.sprite = def.ReputationReward.Faction.Icon;
                _factionReputationText.text = $"{def.ReputationReward.ReputationPoints}";
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

            if (_def.GiveAllItems)
                _choiceText.text = "all items:";
            else
                _choiceText.text = "choice of items:";

            int index = 0;
            foreach (QuestItemReward reward in _def.ItemRewards)
            {
                var entryObj = Instantiate(_slotEntryPrefab.gameObject, _container.transform);
                var entry = entryObj.GetComponent<UI_QuestRewardsEntry>();
                entry.Initialize(reward, index);
                entry.Register(this);
                index++;
            }
        }
    }
}
