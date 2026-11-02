using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using TMPro;
using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterUnitWindow</c> implementation.
    /// </summary>
    public sealed class UI_CharacterUnitWindow : UI_Window
    {
        private static UI_CharacterUnitWindow _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_CharacterUnitWindow Instance => _instance;

        [SerializeField] private TMP_Text _levelText;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;

            Game.Shared.Networking.AccountEndpoint.onEnteredWorld += OnEnteredWorld;
            ActorEndpoint.onPlayerActorAssigned += OnPlayerActorAssigned;
            CharacterEndpoint.onClientExperienceUpdated += OnExperienceUpdated;
        }

        private void OnDestroy()
        {
            if (_instance != this)
                return;

            Game.Shared.Networking.AccountEndpoint.onEnteredWorld -= OnEnteredWorld;
            ActorEndpoint.onPlayerActorAssigned -= OnPlayerActorAssigned;
            CharacterEndpoint.onClientExperienceUpdated -= OnExperienceUpdated;
            _instance = null;
        }

        private void OnEnteredWorld()
        {
            ShowImmediate();
        }

        private void OnPlayerActorAssigned(Actor actor)
        {
            _levelText.text = actor == null ? string.Empty : actor.GetLevel().ToString();
        }

        private void OnExperienceUpdated(int level, int experience, int delta)
        {
            _levelText.text = level.ToString();
        }
    }
}
