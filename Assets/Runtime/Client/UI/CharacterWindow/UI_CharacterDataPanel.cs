using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Runtime.Actors;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterDataPanel</c> implementation.
    /// </summary>
    public sealed class UI_CharacterDataPanel : UI_Panel
    {
        private static UI_CharacterDataPanel _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_CharacterDataPanel Instance => _instance;
        private static bool _initialized;
        /// <summary>
        /// Gets whether the client-side <c>IsInitialized</c> condition is true.
        /// </summary>
        public static bool IsInitialized => _initialized;

        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private TMP_Text characterTitleText;
        [SerializeField] private TMP_Text characterLevelText;
        [SerializeField] private TMP_Text characterClassText;
        [SerializeField] private TMP_Text characterRaceText;
        [SerializeField] private Image characterClassIcon;
        [SerializeField] private Image characterRaceIcon;
        [SerializeField] private Image characterClassBackground;

        protected override void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;

            Game.Shared.Networking.ActorEndpoint.onPlayerActorAssigned += OnPlayerActorAssigned;

            _initialized = true;
            base.Awake();
        }

        private void OnPlayerActorAssigned(Actor actor)
        {
            Debug.Log($"Received player actor assignment for {actor.Name} (ID: {actor.ID}). Updating character data panel.");
            // UI_Manager.Instance.ShowWindow("CharacterWindow");

            var characterData = ClientAccountManager.ActiveCharacter;
            if (characterData != null)
            {
                characterNameText.text = characterData.Name;
                characterTitleText.text = characterData.Title;
                characterLevelText.text = $"Level {actor.GetLevel()}";
                characterClassText.text = characterData.ClassID;
                characterRaceText.text = characterData.RaceID;

                var classDef = Shared.Data.ClassDefinitionLibrary.Instance.GetDefinition(characterData.ClassID);
                var raceDef = Shared.Data.RaceDefinitionLibrary.Instance.GetDefinition(characterData.RaceID);

                if (classDef != null && classDef.Icon != null)
                {
                    characterClassIcon.sprite = classDef.Icon;
                    characterClassIcon.enabled = true;
                    characterClassBackground.sprite = classDef.Icon;
                }

                if (raceDef != null && raceDef.Icon != null)
                {
                    characterRaceIcon.sprite = raceDef.Icon;
                    characterRaceIcon.enabled = true;
                }
            }
            else
            {
                Debug.Warning("No active character data found for the assigned player actor.");
            }
        }
    }
}
