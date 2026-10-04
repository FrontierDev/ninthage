using UnityEngine;
using Game.Shared.Data;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Debug = Game.Shared.FormattedDebug;
using Unity.Entities.UniversalDelegates;

namespace Game.Client.UI
{
    public sealed class UI_CC_RaceListEntry : UI_ListEntry<RaceDefinition>
    {
        [SerializeField] private Image raceIconImage;

        public RaceDefinition raceDefinition;
        private int index;

        public override void Initialize(RaceDefinition _raceDefinition, int _index)
        {
            index = _index;
            raceDefinition = _raceDefinition;
            raceIconImage.sprite = raceDefinition.Icon;

            UI_CharacterCreationWindow.Instance.onRaceSelected += OnRaceSelected;
        }

        public void OnRaceSelected(RaceDefinition def)
        {
            // Focus the selected entry and unfocus others.
            if (def.DefinitionId == raceDefinition.DefinitionId) Focus();
            else Unfocus();
        }

        public override void OnClick(PointerEventData eventData)
        {
            CharacterCreationManager.Instance.SelectRace(raceDefinition);
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
            raceIconImage.color = Color.yellow; // Example visual feedback for focus
            isFocused = true;
        }

        public override void Unfocus()
        {
            raceIconImage.color = Color.white; // Reset to default color
            isFocused = false;
        }
    }
}