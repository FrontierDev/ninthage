using Game.Shared.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_QuestObjectivesEntry</c> implementation.
    /// </summary>
    public sealed class UI_QuestObjectivesEntry : UI_ListEntry<QuestObjective>
    {
        private QuestObjective _data;
        private int _index;

        [FormerlySerializedAs("objectiveText")]
        [SerializeField] private TMP_Text _objectiveText;
        // [SerialzieField] private UI_Checkbox checkbox;

        /// <inheritdoc/>
        public override void Initialize(QuestObjective data, int index)
        {
            this._data = data;
            this._index = index;
        }

        /// <summary>
        /// Executes the client-side <c>UpdateEntry</c> operation.
        /// </summary>
        public void UpdateEntry(int progress)
        {
            var text = _data.Description + " (" + progress + "/" + _data.RequiredAmount + ")";
            _objectiveText.text = text;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_objectiveText.rectTransform);
        }

        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData = null)
        {
            // No click behavior for quest objectives in this implementation.
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}
