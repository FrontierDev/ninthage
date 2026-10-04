using Game.Shared.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public sealed class UI_QuestDetailsPanel : UI_Panel
    {
        private static UI_QuestDetailsPanel instance;
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

        public void UpdateObjectivesProgress()
        {
            objectivesPanel.UpdateEntries(UI_QuestListPanel.Instance.Quests.GetQuestProgress(def.DefinitionId));
        }
    }
}