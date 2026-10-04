using Game.Shared.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public sealed class UI_QuestObjectivesEntry : UI_ListEntry<QuestObjective>
    {
        private QuestObjective data;
        private int index;

        [SerializeField] private TMP_Text objectiveText;
        // [SerialzieField] private UI_Checkbox checkbox;

        public override void Initialize(QuestObjective data, int index)
        {
            this.data = data;
            this.index = index;
        }

        public void UpdateEntry(int progress)
        {
            var text = data.Description + " (" + progress + "/" + data.RequiredAmount + ")";
            objectiveText.text = text;
            LayoutRebuilder.ForceRebuildLayoutImmediate(objectiveText.rectTransform);
        }

        public override void OnClick(PointerEventData eventData = null)
        {
            // No click behavior for quest objectives in this implementation.
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}