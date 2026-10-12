using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Data;
using TMPro;
using Unity.Entities.UniversalDelegates;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CC_RaceListEntry</c> implementation.
    /// </summary>
    public sealed class UI_CC_RaceListEntry : UI_ListEntry<RaceDefinition>
    {
        [SerializeField] private Image raceIconImage;

        /// <summary>
        /// Stores the <c>raceDefinition</c> value.
        /// </summary>
        public RaceDefinition raceDefinition;
        private int index;

        /// <inheritdoc/>
        public override void Initialize(RaceDefinition _raceDefinition, int _index)
        {
            index = _index;
            raceDefinition = _raceDefinition;
            raceIconImage.sprite = raceDefinition.Icon;

            UI_CharacterCreationWindow.Instance.onRaceSelected += OnRaceSelected;
        }

        /// <summary>
        /// Handles the <c>OnRaceSelected</c> callback.
        /// </summary>
        public void OnRaceSelected(RaceDefinition def)
        {
            // Focus the selected entry and unfocus others.
            if (def.DefinitionID == raceDefinition.DefinitionID) Focus();
            else Unfocus();
        }

        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData)
        {
            CharacterCreationManager.Instance.SelectRace(raceDefinition);
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
            raceIconImage.color = Color.yellow; // Example visual feedback for focus
            isFocused = true;
        }

        /// <inheritdoc/>
        public override void Unfocus()
        {
            raceIconImage.color = Color.white; // Reset to default color
            isFocused = false;
        }
    }
}
