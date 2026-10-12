using Game.Shared.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_QuestDetailsPanel</c> implementation.
    /// </summary>
    public sealed class UI_QuestDetailsPanel : UI_Panel
    {
        private static UI_QuestDetailsPanel instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_QuestDetailsPanel Instance => instance;

        [SerializeField] private TMP_Text questName;
        [SerializeField] private TMP_Text questSubtext;
        [SerializeField] private Image questIcon;
        [SerializeField] private TMP_Text questDescription;
        [SerializeField] private UI_QuestObjectivesPanel objectivesPanel;
        [SerializeField] private UI_QuestRewardsPanel rewardsPanel;

        private QuestDefinition def;

        protected override void Awake()
        {
            instance = this;
            base.Awake();
        }

        /// <summary>
        /// Sets the value managed by <c>SetQuestDefinition</c>.
        /// </summary>
        public void SetQuestDefinition(QuestDefinition def)
        {
            this.def = def;

            questName.text = def.DisplayName;
            questSubtext.text = "";
            questIcon.sprite = def.Icon;
            questDescription.text = def.Description;

            objectivesPanel.SetQuestDefinition(def);
            rewardsPanel.SetQuestDefinition(def);

            UpdateObjectivesProgress();
        }

        /// <summary>
        /// Executes the client-side <c>UpdateObjectivesProgress</c> operation.
        /// </summary>
        public void UpdateObjectivesProgress()
        {
            objectivesPanel.UpdateEntries(UI_QuestListPanel.Instance.Quests.GetQuestProgress(def.DefinitionID));
        }
    }
}