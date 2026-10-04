using UnityEngine;
using Game.Shared.Data;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Debug = Game.Shared.FormattedDebug;
using Unity.Entities.UniversalDelegates;

namespace Game.Client.UI
{
    public sealed class UI_CC_ClassListEntry : UI_ListEntry<ClassDefinition>
    {
        [SerializeField] private Image classIconImage;

        public ClassDefinition classDefinition;
        private int index;

        public override void Initialize(ClassDefinition _classDefinition, int _index)
        {
            index = _index;
            classDefinition = _classDefinition;
            classIconImage.sprite = classDefinition.Icon;

            UI_CharacterCreationWindow.Instance.onClassSelected += OnClassSelected;
        }

        public void OnClassSelected(ClassDefinition def)
        {
            if (def.DefinitionId == classDefinition.DefinitionId) Focus();
            else Unfocus();
        }

        public override void OnClick(PointerEventData eventData)
        {
            CharacterCreationManager.Instance.SelectClass(classDefinition);
            // Passes to creation manager before updating the UI.

            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
        }

        public override void Focus()
        {
            classIconImage.color = Color.yellow; // Example visual feedback for focus
            isFocused = true;
        }

        public override void Unfocus()
        {
            classIconImage.color = Color.white; // Reset to default color
            isFocused = false;
        }
    }
}