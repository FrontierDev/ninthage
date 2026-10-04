using System.Collections.Generic;
using System.Linq;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using UnityEngine;
using UnityEngine.UI;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Client.UI
{
    public sealed class UI_TargetAuraBar : UI_AuraBar
    {
        [SerializeField] private bool interactable = false;

        protected override void Awake()
        {
            ActorService.onClientApplyAura += OnApplyAura;
            ActorService.onClientUpdateAura += OnUpdateAura;
            ActorService.onClientExpireAura += OnExpireAura;
            ActorService.onClientDispelAura += OnExpireAura;

            ActorService.onClientTargetedActorChanged += OnTargetActorChanged;
        }

        private void OnDestroy()
        {
            ActorService.onClientApplyAura -= OnApplyAura;
            ActorService.onClientUpdateAura -= OnUpdateAura;
            ActorService.onClientExpireAura -= OnExpireAura;
            ActorService.onClientDispelAura -= OnExpireAura;

            ActorService.onClientTargetedActorChanged -= OnTargetActorChanged;
        }

        private void Update()
        {
            if (actor == null) return;

            foreach (var entry in currentEntries)
            {
                var auraEntry = entry as UI_AuraEntry;
                var currentDuration = actor.GetRemainingAuraTime(auraEntry.Data.InstancedID);
                auraEntry.UpdateDisplay(currentDuration);
            }
        }

        private void OnTargetActorChanged(Actor newTarget)
        {
            actor = newTarget;
            PopulateList(forceClear: true);
        }

        private bool IsConditionAura(AuraInstance aura)
        {
            return aura.Definition.StackBehavior == AuraStackBehavior.Condition_Stack ||
                   aura.Definition.StackBehavior == AuraStackBehavior.Condition_Extend;
        }

        private void OnApplyAura(AuraInstance aura, Actor target, int instancedID)
        {
            if (actor == null || target != actor) return;
            if (!IsConditionAura(aura)) return;

            bool isDebuff = aura.Definition.IsDebuff;
            if ((showBuffs && isDebuff) || (!showBuffs && !isDebuff)) return;

            var entryObj = Instantiate(auraEntryPrefab, container.transform);
            var entry = entryObj.GetComponent<UI_AuraEntry>();
            entry.Initialize(aura, currentEntries.Count);
            entry.Register(this);
            currentEntries.Add(entry);
        }

        private void OnUpdateAura(AuraInstance aura, Actor target, int instancedID)
        {
            if (actor == null || target != actor) return;
            if (!IsConditionAura(aura)) return;

            PopulateList(forceClear: true);
        }

        private void OnExpireAura(AuraInstance aura, Actor target, int instancedID)
        {
            if (actor == null || target != actor) return;
            if (!IsConditionAura(aura)) return;

            var entry = currentEntries.FirstOrDefault(e =>
            {
                var auraEntry = e as UI_AuraEntry;
                return auraEntry != null && auraEntry.Data.InstancedID == aura.InstancedID;
            });

            if (entry != null)
            {
                PopulateList(forceClear: true);
            }
        }

        private void ClearEntries()
        {
            foreach (var entry in currentEntries)
            {
                Destroy(entry.gameObject);
            }
            currentEntries.Clear();
        }

        public override void PopulateList(bool forceClear = false)
        {
            if (actor == null) return;
            if (forceClear)
                ClearEntries();

            List<AuraInstance> auras = actor.GetActiveAuras();
            int index = 0;
            foreach (var aura in auras)
            {
                if (!IsConditionAura(aura)) continue;
                if ((showBuffs && !aura.Definition.IsDebuff) || (!showBuffs && aura.Definition.IsDebuff))
                {
                    var entryObj = Instantiate(auraEntryPrefab, container.transform);
                    var entry = entryObj.GetComponent<UI_AuraEntry>();
                    entry.Initialize(aura, index);
                    entry.Register(this);
                    currentEntries.Add(entry);
                    index++;
                }
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(UI_TargetWindow.Instance.GetComponent<RectTransform>());
        }
    }
}