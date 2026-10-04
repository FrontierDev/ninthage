using System.Collections;
using Game.Shared;
using Game.Shared.Data;
using TMPro;
using UnityEngine;

namespace Game.Client.UI
{
    public sealed class UI_TargetCastBar : UI_ProgressBar
    {
        [SerializeField] private TMP_Text spellNameText;
        [SerializeField] private TMP_Text spellTimeText;

        private void Awake()
        {
            SetMaterial(graphics.material);
            Game.Shared.Networking.ActorService.onClientLateTargetedActorChanged += OnClientLateTargetedActorChanged;
            Game.Shared.Networking.ActorService.onSpellCastStarted += OnSpellCastStarted;
            Game.Shared.Networking.ActorService.onSpellCastCompleted += OnSpellCastFinished;
            Game.Shared.Networking.ActorService.onSpellCastInterrupted += OnSpellCastFinished;
        }

        private void StopCastBar()
        {
            StopAllCoroutines();
            SetProgress(0f);
            Hide();
            gameObject.SetActive(false);
        }

        private IEnumerator CastBarRoutine(float castTime)
        {
            float elapsed = 0f;
            while (elapsed < castTime)
            {
                SetProgress(elapsed / castTime);
                spellTimeText.text = $"{castTime - elapsed:0.0}s";
                elapsed += Time.deltaTime;
                yield return null;
            }
            SetProgress(1f);
        }

        private void OnSpellCastStarted(Game.Shared.Actor caster, string spellID, float finalCastTime)
        {
            if (ClientAccountManager.PlayerActor.Target == caster)
            {
                gameObject.SetActive(true);
                SetProgress(0f);
                Show();
                spellNameText.text = SpellDefinitionLibrary.Instance.GetDefinition(spellID)?.DisplayName ?? "Unknown Spell";
                spellTimeText.text = $"{finalCastTime:0.0}s";
                StartCoroutine(CastBarRoutine(finalCastTime));
            }
        }

        private void OnSpellCastFinished(Game.Shared.Actor caster)
        {
            if (ClientAccountManager.PlayerActor.Target == caster)
            {
                StopCastBar();
            }
        }

        private void OnClientLateTargetedActorChanged(Game.Shared.Actor oldTarget, Game.Shared.Actor newTarget)
        {
            bool shouldShow = false;
            if (newTarget != null && newTarget.TryGetComponent<ActorSpellcaster>(out var spellcaster))
            {
                shouldShow = spellcaster.IsCasting;
            }
            else
            {
                shouldShow = false;
            }

            if (shouldShow)
            {
                Show();
                gameObject.SetActive(true);
            }
            else
            {
                StopCastBar();
            }
        }
    }
}