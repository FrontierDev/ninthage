using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using UnityEngine;

namespace Game.Client.UI
{
    // Technically a window.
    public sealed class UI_ClientCastBarWindow : UI_Window
    {
        private static UI_ClientCastBarWindow _instance;
        public static UI_ClientCastBarWindow Instance => _instance;

        [SerializeField] private UI_CastBar castBar;

        [SerializeField] private Color castColor;
        [SerializeField] private Color interruptColor;
        [SerializeField] private Color completeColor;

        private void Awake()
        {
            _instance = this;

            ActorService.onSpellCastStarted += OnSpellCastStarted;
            ActorService.onSpellCastInterrupted += OnSpellCastInterrupted;
            ActorService.onSpellCastCompleted += OnSpellCastCompleted;
        }

        private void OnSpellCastStarted(Actor actor, string spellID, float duration)
        {
            if (actor != ClientAccountManager.PlayerActor) return;

            var spellDef = SpellDefinitionLibrary.Instance.GetDefinition(spellID);

            castBar.SetColor(castColor);
            castBar.StartCast(duration, spellDef != null && spellDef.IsChanneled);
        }

        private void OnSpellCastInterrupted(Actor actor)
        {
            if (actor != ClientAccountManager.PlayerActor) return;
            castBar.SetColor(interruptColor);
            castBar.InterruptCast();
        }

        private void OnSpellCastCompleted(Actor actor)
        {
            if (actor != ClientAccountManager.PlayerActor) return;
            castBar.SetColor(completeColor);
            castBar.FinishCast();
        }
    }
}