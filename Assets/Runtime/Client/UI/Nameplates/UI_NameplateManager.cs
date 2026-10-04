using UnityEngine;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Debug = Game.Shared.FormattedDebug;
using System.Collections.Generic;
using System.Linq;

namespace Game.Client.UI
{
    public enum NameplateHealthBarDisplayMode
    {
        Always,
        InCombat,
        OnHover,
        Never
    }

    public enum NameplateNameDisplayMode
    {
        Always,
        OnHover,
        Never
    }

    public enum NameplateCastBarDisplayMode
    {
        Always,
        OnHover,
        Never
    }

    public enum NameplateOverlapMode
    {
        Overlap,
        Stack
    }

    public class UI_NameplateManager : UI_WorldSpaceUIManager
    {
        private static UI_NameplateManager instance;
        public static UI_NameplateManager Instance => instance;

        // The prefab for nameplates.
        public GameObject nameplatePrefab;

        // Visibility settings.
        public NameplateHealthBarDisplayMode healthBarDisplayMode = NameplateHealthBarDisplayMode.Always;
        public NameplateNameDisplayMode nameDisplayMode = NameplateNameDisplayMode.Always;
        public NameplateCastBarDisplayMode castBarDisplayMode = NameplateCastBarDisplayMode.Always;
        public NameplateOverlapMode overlapMode = NameplateOverlapMode.Overlap;
        public bool showOwnNameplate = false;

        public Dictionary<Actor, UI_Nameplate> activeNameplates = new();
        public Actor hoveredActor;
        private Actor targetedActor => ClientAccountManager.PlayerActor?.Target;

        private void Awake()
        {
            if (instance != null) return;
            instance = this;
        }

        private void Start()
        {
            // Subscribe to actor spawn events
            ActorService.onActorSpawned += OnActorSpawned;
            ActorService.onClientHoveredActorChanged += SetHoveredActor;
            ActorService.onClientLateTargetedActorChanged += OnLateTargetChanged;
            ActorService.onSpellCastStarted += OnSpellCastStarted;
            ActorService.onSpellCastInterrupted += OnSpellCastInterrupted;
            ActorService.onSpellCastCompleted += OnSpellCastCompleted;
            StatService.onStatsUpdated += OnStatsUpdated;
            ClientSettingsManager.onSettingChanged += OnSettingChanged;
            ClientSettingsManager.onSettingsLoaded += OnSettingsLoaded;
        }

        private void OnDestroy()
        {
            ActorService.onActorSpawned -= OnActorSpawned;
            ActorService.onClientHoveredActorChanged -= SetHoveredActor;
            ActorService.onClientLateTargetedActorChanged -= OnLateTargetChanged;
            ActorService.onSpellCastStarted -= OnSpellCastStarted;
            ActorService.onSpellCastInterrupted -= OnSpellCastInterrupted;
            ActorService.onSpellCastCompleted -= OnSpellCastCompleted;
            StatService.onStatsUpdated -= OnStatsUpdated;
            ClientSettingsManager.onSettingChanged -= OnSettingChanged;
            ClientSettingsManager.onSettingsLoaded -= OnSettingsLoaded;
        }

        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 camPos = cam.transform.position;

            foreach (var kvp in activeNameplates)
            {
                if (kvp.Key == null || kvp.Value == null) continue;

                float dist = Vector3.SqrMagnitude(kvp.Key.transform.position - camPos);
                kvp.Value.SortingDistance = dist;
            }

            // Sort so further nameplates are drawn first (behind), closer ones drawn last (on top)
            // Targeted actor's nameplate always renders on top.
            var container = UI_WorldSpace.Instance.nameplateContainer;
            int childCount = container.childCount;
            for (int i = 1; i < childCount; i++)
            {
                var child = container.GetChild(i);
                var nameplate = child.GetComponent<UI_Nameplate>();
                if (nameplate == null) continue;

                int j = i;
                while (j > 0)
                {
                    var prev = container.GetChild(j - 1).GetComponent<UI_Nameplate>();
                    if (prev == null || prev.SortingDistance >= nameplate.SortingDistance) break;
                    j--;
                }

                if (j != i) child.SetSiblingIndex(j);
            }

            if (targetedActor != null && activeNameplates.TryGetValue(targetedActor, out var targetNameplate))
            {
                targetNameplate.transform.SetAsLastSibling();
            }

            // Own nameplate always renders closest to camera (on top of everything)
            var playerActor = ClientAccountManager.PlayerActor;
            if (playerActor != null && activeNameplates.TryGetValue(playerActor, out var ownNameplate))
            {
                ownNameplate.transform.SetAsLastSibling();
            }

            // Stacking: offset nameplates so they don't overlap on screen
            if (overlapMode == NameplateOverlapMode.Stack)
                ResolveStacking(cam);
            else
            {
                foreach (var kvp in activeNameplates)
                {
                    if (kvp.Value != null)
                        kvp.Value.StackOffset = 0f;
                }
            }
        }

        private readonly List<UI_Nameplate> sortedForStacking = new();

        private void ResolveStacking(Camera cam)
        {
            sortedForStacking.Clear();

            foreach (var kvp in activeNameplates)
            {
                if (kvp.Key == null || kvp.Value == null) continue;
                if (kvp.Key == ClientAccountManager.PlayerActor) continue;
                kvp.Value.StackOffset = 0f;
                sortedForStacking.Add(kvp.Value);
            }

            if (sortedForStacking.Count < 2) return;

            // Priority: targeted first, then closer actors
            sortedForStacking.Sort((a, b) =>
            {
                bool aTargeted = a.Actor == targetedActor;
                bool bTargeted = b.Actor == targetedActor;
                if (aTargeted != bTargeted) return aTargeted ? -1 : 1;
                return a.SortingDistance.CompareTo(b.SortingDistance);
            });

            // Compute base screen positions (actor pos + heightOffset, no stack offset)
            // and screen heights for each nameplate
            int count = sortedForStacking.Count;
            var screenYPositions = new float[count];
            var screenHeights = new float[count];
            var screenXPositions = new float[count];
            var behind = new bool[count];

            for (int i = 0; i < count; i++)
            {
                var np = sortedForStacking[i];
                Vector3 baseWorldPos = np.Actor.transform.position + Vector3.up * np.HeightOffset;
                Vector3 screenPos = cam.WorldToScreenPoint(baseWorldPos);
                behind[i] = screenPos.z <= 0f;
                screenXPositions[i] = screenPos.x;
                screenYPositions[i] = screenPos.y;
                screenHeights[i] = GetScreenHeight(np, cam);
            }

            // Walk through sorted list; for each nameplate, push it above any higher-priority one it overlaps
            for (int i = 1; i < count; i++)
            {
                if (behind[i]) continue;

                float currentHeight = screenHeights[i];
                float currentWidth = currentHeight * 3f; // approximate width as 3x height

                for (int j = 0; j < i; j++)
                {
                    if (behind[j]) continue;

                    float otherHeight = screenHeights[j];
                    float otherWidth = otherHeight * 3f;

                    float minSepX = (currentWidth + otherWidth) * 0.5f;
                    float minSepY = (currentHeight + otherHeight) * 0.5f;

                    float dx = Mathf.Abs(screenXPositions[i] - screenXPositions[j]);
                    if (dx >= minSepX) continue;

                    float dy = screenYPositions[i] - screenYPositions[j];
                    if (dy >= minSepY) continue; // already above and clear

                    // Push current nameplate above the other
                    screenYPositions[i] = screenYPositions[j] + minSepY;
                }
            }

            // Convert screen Y offsets back to world-space StackOffset
            for (int i = 0; i < count; i++)
            {
                if (behind[i]) continue;

                var np = sortedForStacking[i];
                Vector3 baseWorldPos = np.Actor.transform.position + Vector3.up * np.HeightOffset;
                Vector3 baseScreenPos = cam.WorldToScreenPoint(baseWorldPos);
                float screenDelta = screenYPositions[i] - baseScreenPos.y;

                if (Mathf.Abs(screenDelta) < 0.5f) continue;

                float dist = Vector3.Distance(cam.transform.position, baseWorldPos);
                float frustumHeight = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                np.StackOffset = (screenDelta / Screen.height) * frustumHeight;
            }
        }

        private void OnSettingsLoaded()
        {
            // Apply settings on load
            OnSettingChanged("nameplate_health_bar_display_mode", ClientSettingsManager.GetSetting("nameplate_health_bar_display_mode", healthBarDisplayMode));
            OnSettingChanged("nameplate_name_display_mode", ClientSettingsManager.GetSetting("nameplate_name_display_mode", nameDisplayMode));
            OnSettingChanged("nameplate_cast_bar_display_mode", ClientSettingsManager.GetSetting("nameplate_cast_bar_display_mode", castBarDisplayMode));
            OnSettingChanged("nameplate_show_own", ClientSettingsManager.GetSetting("nameplate_show_own", showOwnNameplate));
            OnSettingChanged("nameplate_overlap_mode", ClientSettingsManager.GetSetting("nameplate_overlap_mode", overlapMode));
        }

        private void OnActorSpawned(Actor actor)
        {
            if (actor == null) return;

            if (actor == ClientAccountManager.PlayerActor && !showOwnNameplate)
                return;

            // Instantiate nameplate from prefab as child of world canvas
            GameObject nameplate = Instantiate(nameplatePrefab, UI_WorldSpace.Instance.nameplateContainer);

            // Initialize the nameplate
            UI_Nameplate nameplateView = nameplate.GetComponent<UI_Nameplate>();
            if (nameplateView != null)
            {
                nameplateView.Initialize(actor, UI_WorldSpace.Instance.root);
                activeNameplates[actor] = nameplateView;
            }
        }

        private void OnStatsUpdated(Actor actor, ActorStatContainer statContainer)
        {
            if (activeNameplates.TryGetValue(actor, out var nameplate))
            {
                nameplate.OnStatsUpdated(actor, statContainer);
            }
        }

        private void OnSettingChanged(string settingKey, object newValue)
        {
            if (settingKey == "nameplate_health_bar_display_mode" && newValue is NameplateHealthBarDisplayMode mode)
            {
                healthBarDisplayMode = mode;
                foreach (var nameplate in activeNameplates.Values)
                {
                    nameplate.EvaluateHealthBarVisibility();
                }
            }
            else if (settingKey == "nameplate_name_display_mode" && newValue is NameplateNameDisplayMode nameMode)
            {
                nameDisplayMode = nameMode;
                foreach (var nameplate in activeNameplates.Values)
                {
                    nameplate.EvaluateNameVisibility();
                }
            }
            else if (settingKey == "nameplate_cast_bar_display_mode" && newValue is NameplateCastBarDisplayMode castMode)
            {
                castBarDisplayMode = castMode;
                foreach (var nameplate in activeNameplates.Values)
                {
                    nameplate.EvaluateCastBarVisibility();
                }
            }
            else if (settingKey == "nameplate_show_own" && newValue is bool showOwn)
            {
                showOwnNameplate = showOwn;

                if (ClientAccountManager.PlayerActor != null)
                {
                    if (showOwn)
                    {
                        // If enabling own nameplate, spawn it immediately
                        if (!activeNameplates.ContainsKey(ClientAccountManager.PlayerActor))
                        {
                            OnActorSpawned(ClientAccountManager.PlayerActor);
                        }
                    }
                    else
                    {
                        // If disabling own nameplate, remove it if it exists
                        if (activeNameplates.TryGetValue(ClientAccountManager.PlayerActor, out var ownNameplate))
                        {
                            Destroy(ownNameplate.gameObject);
                            activeNameplates.Remove(ClientAccountManager.PlayerActor);
                        }
                    }
                }

                foreach (var nameplate in activeNameplates.Values)
                {
                    nameplate.EvaluateNameVisibility();
                }
            }
            else if (settingKey == "nameplate_overlap_mode" && newValue is NameplateOverlapMode overlap)
            {
                overlapMode = overlap;
                // Overlap mode changes are handled in LateUpdate by sorting nameplates based on distance.
            }
        }

        public void SetHoveredActor(Actor actor)
        {
            if (hoveredActor == actor) return;

            hoveredActor = actor;

            foreach (var kvp in activeNameplates)
            {
                kvp.Value.EvaluateHealthBarVisibility();
                kvp.Value.EvaluateNameVisibility();
                kvp.Value.EvaluateCastBarVisibility();
            }
        }

        public void OnLateTargetChanged(Actor previousActor, Actor actor)
        {
            var prevNameplate = previousActor != null && activeNameplates.TryGetValue(previousActor, out var prev) ? prev : null;

            // Untarget previous actor if it exists
            if (prevNameplate != null)
            {
                prevNameplate.EvaluateHealthBarVisibility();
                prevNameplate.EvaluateNameVisibility();
                prevNameplate.EvaluateCastBarVisibility();
                prevNameplate.SetTargetedIndicatorVisibility(false);
            }

            // Target new actor
            if (actor != null && activeNameplates.TryGetValue(actor, out var newNameplate))
            {
                newNameplate.EvaluateHealthBarVisibility();
                newNameplate.EvaluateNameVisibility();
                newNameplate.EvaluateCastBarVisibility();
                newNameplate.SetTargetedIndicatorVisibility(true);
            }
        }

        private void OnSpellCastStarted(Actor actor, string spellID, float duration)
        {
            if (!activeNameplates.TryGetValue(actor, out var nameplate)) return;

            var spellDef = SpellDefinitionLibrary.Instance.GetDefinition(spellID);
            bool reverse = spellDef != null && spellDef.IsChanneled;
            nameplate.StartCast(duration, reverse);
        }

        private void OnSpellCastInterrupted(Actor actor)
        {
            if (!activeNameplates.TryGetValue(actor, out var nameplate)) return;
            nameplate.InterruptCast();
        }

        private void OnSpellCastCompleted(Actor actor)
        {
            if (!activeNameplates.TryGetValue(actor, out var nameplate)) return;
            nameplate.FinishCast();
        }
    }
}