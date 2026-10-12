using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Data;
using TMPro;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CC_ClassListEntry</c> implementation.
    /// </summary>
    public sealed class UI_CC_ClassListEntry : UI_ListEntry<ClassDefinition>
    {
        [FormerlySerializedAs("classIconImage")]
        [SerializeField] private Image _classIconImage;

        /// <summary>
        /// Stores the <c>classDefinition</c> value.
        /// </summary>
        public ClassDefinition classDefinition;
        private int _index;

        /// <inheritdoc/>
        public override void Initialize(ClassDefinition _classDefinition, int _index)
        {
            this._index = _index;
            classDefinition = _classDefinition;
            _classIconImage.sprite = classDefinition.Icon;

            UI_CharacterCreationWindow.Instance.onClassSelected += OnClassSelected;
        }

        /// <summary>
        /// Handles the <c>OnClassSelected</c> callback.
        /// </summary>
        public void OnClassSelected(ClassDefinition def)
        {
            if (def.DefinitionID == classDefinition.DefinitionID) Focus();
            else Unfocus();
        }

        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData)
        {
            CharacterCreationManager.Instance.SelectClass(classDefinition);
            // Passes to creation manager before updating the UI.

            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }

        /// <inheritdoc/>
        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
        }

        /// <inheritdoc/>
        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
        }

        /// <inheritdoc/>
        public override void Focus()
        {
            _classIconImage.color = Color.yellow; // Example visual feedback for focus
            isFocused = true;
        }

        /// <inheritdoc/>
        public override void Unfocus()
        {
            _classIconImage.color = Color.white; // Reset to default color
            isFocused = false;
        }
    }
}
