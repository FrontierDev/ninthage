using Game.Shared.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_QuestDetailsPanel</c> implementation.
    /// </summary>
    public sealed class UI_QuestDetailsPanel : UI_Panel
    {
        private static UI_QuestDetailsPanel _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_QuestDetailsPanel Instance => _instance;

        [FormerlySerializedAs("questName")]
        [SerializeField] private TMP_Text _questName;
        [FormerlySerializedAs("questSubtext")]
        [SerializeField] private TMP_Text _questSubtext;
        [FormerlySerializedAs("questIcon")]
        [SerializeField] private Image _questIcon;
        [FormerlySerializedAs("questDescription")]
        [SerializeField] private TMP_Text _questDescription;
        [FormerlySerializedAs("objectivesPanel")]
        [SerializeField] private UI_QuestObjectivesPanel _objectivesPanel;
        [FormerlySerializedAs("rewardsPanel")]
        [SerializeField] private UI_QuestRewardsPanel _rewardsPanel;

        private QuestDefinition _def;

        protected override void Awake()
        {
            _instance = this;
            base.Awake();
        }

        /// <summary>
        /// Sets the value managed by <c>SetQuestDefinition</c>.
        /// </summary>
        public void SetQuestDefinition(QuestDefinition def)
        {
            this._def = def;

            _questName.text = def.DisplayName;
            _questSubtext.text = "";
            _questIcon.sprite = def.Icon;
            _questDescription.text = def.Description;

            _objectivesPanel.SetQuestDefinition(def);
            _rewardsPanel.SetQuestDefinition(def);

            UpdateObjectivesProgress();
        }

        /// <summary>
        /// Executes the client-side <c>UpdateObjectivesProgress</c> operation.
        /// </summary>
        public void UpdateObjectivesProgress()
        {
            _objectivesPanel.UpdateEntries(UI_QuestListPanel.Instance.Quests.GetQuestProgress(_def.DefinitionID));
        }
    }
}
