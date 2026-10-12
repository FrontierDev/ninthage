using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Data;
using Game.Shared.Persistence;
using TMPro;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterListEntry</c> implementation.
    /// </summary>
    public sealed class UI_CharacterListEntry : UI_ListEntry<CharacterData>
    {
        [FormerlySerializedAs("classIconImage")]
        [SerializeField] private Image _classIconImage;
        [FormerlySerializedAs("characterNameText")]
        [SerializeField] private TMP_Text _characterNameText;
        [FormerlySerializedAs("characterInfoText")]
        [SerializeField] private TMP_Text _characterInfoText;
        [FormerlySerializedAs("factionIcon")]
        [SerializeField] private Image _factionIcon;

        /// <summary>
        /// Stores the <c>characterData</c> value.
        /// </summary>
        public CharacterData characterData;
        private int _index;

        /// <inheritdoc/>
        public override void Initialize(CharacterData _characterData, int _index)
        {
            // Metadata setup.
            this._index = _index;
            characterData = _characterData;

            // UI setup.
            _characterNameText.text = characterData.Name;
            _characterInfoText.text = $"Level {characterData.Level} {characterData.RaceID} {characterData.ClassID}";
            _classIconImage.sprite = ClassDefinitionLibrary.Instance.GetDefinition(characterData.ClassID)?.Icon;

            // NYI factionIcon.sprite = FactionDefinitionLibrary.Instance.GetDefinition(characterData.FactionID)?.Icon

            // Set up the callback for when this entry is clicked.
            UI_CharacterSelectWindow.Instance.onCharacterSelected += OnCharacterSelected;
        }

        private void OnDestroy()
        {
            if (UI_CharacterSelectWindow.Instance != null)
                UI_CharacterSelectWindow.Instance.onCharacterSelected -= OnCharacterSelected;
        }

        /// <summary>
        /// Handles the <c>OnCharacterSelected</c> callback.
        /// </summary>
        public void OnCharacterSelected(CharacterData character)
        {
            if (character == characterData) Focus();
            else Unfocus();
        }

        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData)
        {
            CharacterSelectionManager.Instance.SelectCharacter(characterData);
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
            canvasGroup.alpha = 1f;
            isFocused = true;
        }

        /// <inheritdoc/>
        public override void Unfocus()
        {
            canvasGroup.alpha = 0.8f;
            isFocused = false;
        }
    }
}
