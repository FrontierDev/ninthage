using Game.Shared.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_QuestListEntry</c> implementation.
    /// </summary>
    public sealed class UI_QuestListEntry : UI_ListEntry<QuestDefinition>
    {
        [FormerlySerializedAs("titleText")]
        [SerializeField] private TMP_Text _titleText;
        [FormerlySerializedAs("subtext")]
        [SerializeField] private TMP_Text _subtext;
        [FormerlySerializedAs("levelText")]
        [SerializeField] private TMP_Text _levelText;
        [FormerlySerializedAs("icon")]
        [SerializeField] private Image _icon;

        [FormerlySerializedAs("data")]
        [SerializeField] private QuestDefinition _data;
        /// <summary>
        /// Gets the <c>Data</c> value.
        /// </summary>
        public QuestDefinition Data => _data;
        private int _index;

        /// <inheritdoc/>
        public override void Initialize(QuestDefinition _data, int index)
        {
            this._data = _data;
            this._index = index;

            _titleText.text = _data.DisplayName;
            _levelText.text = $"Lv. {_data.Level}";
            _subtext.text = "";
            _icon.sprite = _data.Icon;
        }

        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData = null)
        {
            UI_QuestDetailsPanel.Instance.SetQuestDefinition(_data);
            UI_QuestDetailsPanel.Instance.Show();
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}
