using UnityEngine;
using Game.Shared.Data;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Debug = Game.Shared.FormattedDebug;
using Unity.Entities.UniversalDelegates;
using Game.Shared.Persistence;

namespace Game.Client.UI
{
    public sealed class UI_CharacterListEntry : UI_ListEntry<CharacterData>
    {
        [SerializeField] private Image classIconImage;
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text characterInfoText;
        [SerializeField] private Image factionIcon;

        public CharacterData characterData;
        private int index;

        public override void Initialize(CharacterData _characterData, int _index)
        {
            // Metadata setup.
            index = _index;
            characterData = _characterData;

            // UI setup.
            characterNameText.text = characterData.Name;
            characterInfoText.text = $"Level {characterData.Level} {characterData.RaceID} {characterData.ClassID}";
            classIconImage.sprite = ClassDefinitionLibrary.Instance.GetDefinition(characterData.ClassID)?.Icon;

            // NYI factionIcon.sprite = FactionDefinitionLibrary.Instance.GetDefinition(characterData.FactionID)?.Icon

            // Set up the callback for when this entry is clicked.
            UI_CharacterSelectWindow.Instance.onCharacterSelected += OnCharacterSelected;
        }

        private void OnDestroy()
        {
            if (UI_CharacterSelectWindow.Instance != null)
                UI_CharacterSelectWindow.Instance.onCharacterSelected -= OnCharacterSelected;
        }

        public void OnCharacterSelected(CharacterData character)
        {
            if (character == characterData) Focus();
            else Unfocus();
        }

        public override void OnClick(PointerEventData eventData)
        {
            CharacterSelectionManager.Instance.SelectCharacter(characterData);
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
            canvasGroup.alpha = 1f;
            isFocused = true;
        }

        public override void Unfocus()
        {
            canvasGroup.alpha = 0.8f;
            isFocused = false;
        }
    }
}