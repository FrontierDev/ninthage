using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    // Technically a window.
    /// <summary>
    /// Provides the client-side <c>UI_ClientCastBarWindow</c> implementation.
    /// </summary>
    public sealed class UI_ClientCastBarWindow : UI_Window
    {
        private static UI_ClientCastBarWindow _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_ClientCastBarWindow Instance => _instance;

        [FormerlySerializedAs("castBar")]
        [SerializeField] private UI_CastBar _castBar;

        [FormerlySerializedAs("castColor")]
        [SerializeField] private Color _castColor;
        [FormerlySerializedAs("interruptColor")]
        [SerializeField] private Color _interruptColor;
        [FormerlySerializedAs("completeColor")]
        [SerializeField] private Color _completeColor;

        private void Awake()
        {
            _instance = this;

            ActorEndpoint.onSpellCastStarted += OnSpellCastStarted;
            ActorEndpoint.onSpellCastInterrupted += OnSpellCastInterrupted;
            ActorEndpoint.onSpellCastCompleted += OnSpellCastCompleted;
        }

        private void OnSpellCastStarted(Actor actor, string spellID, float duration)
        {
            if (actor != ClientAccountManager.PlayerActor) return;

            var spellDef = SpellDefinitionLibrary.Instance.GetDefinition(spellID);

            _castBar.SetColor(_castColor);
            _castBar.StartCast(duration, spellDef != null && spellDef.IsChanneled);
        }

        private void OnSpellCastInterrupted(Actor actor)
        {
            if (actor != ClientAccountManager.PlayerActor) return;
            _castBar.SetColor(_interruptColor);
            _castBar.InterruptCast();
        }

        private void OnSpellCastCompleted(Actor actor)
        {
            if (actor != ClientAccountManager.PlayerActor) return;
            _castBar.SetColor(_completeColor);
            _castBar.FinishCast();
        }
    }
}
