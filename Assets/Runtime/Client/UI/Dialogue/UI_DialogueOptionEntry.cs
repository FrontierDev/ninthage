using Game.Shared.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public sealed class UI_DialogueOptionEntry : UI_ListEntry<DialogueChoice>
    {
        public DialogueChoice data;
        public int index;

        [SerializeField] private TMP_Text choiceText;
        [SerializeField] private Image choiceIcon;

        public override void Initialize(DialogueChoice data, int index)
        {
            this.data = data;
            this.index = index;
        }

        [ToDo("Set the choice icon sprite.")]
        private void UpdateEntry()
        {
            choiceText.text = data.ChoiceText;
            // choiceIcon.sprite = data.Icon;
        }

        public override void OnClick(PointerEventData eventData = null)
        {
            UI_DialogueBodyPanel.Instance.GoToNode(data.NextNodeID);
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}