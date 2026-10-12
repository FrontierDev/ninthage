using Game.Shared.Data;
using Game.Shared.Data.Dialogue;
using Game.Shared.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_DialogueOptionEntry</c> implementation.
    /// </summary>
    public sealed class UI_DialogueOptionEntry : UI_ListEntry<DialogueChoice>
    {
        /// <summary>
        /// Stores the <c>data</c> value.
        /// </summary>
        public DialogueChoice data;
        /// <summary>
        /// Stores the <c>index</c> value.
        /// </summary>
        public int index;

        [FormerlySerializedAs("choiceText")]
        [SerializeField] private TMP_Text _choiceText;
        [FormerlySerializedAs("choiceIcon")]
        [SerializeField] private Image _choiceIcon;

        /// <inheritdoc/>
        public override void Initialize(DialogueChoice data, int index)
        {
            this.data = data;
            this.index = index;
        }

        [ToDo("Set the choice icon sprite.")]
        private void UpdateEntry()
        {
            _choiceText.text = data.ChoiceText;
            // choiceIcon.sprite = data.Icon;
        }

        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData = null)
        {
            UI_DialogueBodyPanel.Instance.GoToNode(data.NextNodeID);
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}
