using Game.Shared.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public sealed class UI_QuestListEntry : UI_ListEntry<QuestDefinition>
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtext;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Image icon;

        [SerializeField] private QuestDefinition data;
        public QuestDefinition Data => data;
        private int index;

        public override void Initialize(QuestDefinition data, int index)
        {
            this.data = data;
            this.index = index;

            titleText.text = data.DisplayName;
            levelText.text = $"Lv. {data.Level}";
            subtext.text = "";
            icon.sprite = data.Icon;
        }

        public override void OnClick(PointerEventData eventData = null)
        {
            UI_QuestDetailsPanel.Instance.SetQuestDefinition(data);
            UI_QuestDetailsPanel.Instance.Show();
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}