using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Client.Performance;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using Game.Shared.Runtime.Spellcasting;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_FCTManager</c> implementation.
    /// </summary>
    public class UI_FCTManager : MonoBehaviour
    {
        private static UI_FCTManager _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_FCTManager Instance => _instance;

        private readonly HashSet<UI_FloatingCombatText> _trackedTransients = new();

        [Header("Prefab")]
        [FormerlySerializedAs("fctPrefab")]
        [SerializeField] private GameObject _fctPrefab;

        [Header("Colors")]
        [FormerlySerializedAs("autoHitColor")]
        [SerializeField] private Color _autoHitColor = Color.white;
        [FormerlySerializedAs("abilityHitColor")]
        [SerializeField] private Color _abilityHitColor = Color.lightGoldenRodYellow;
        [FormerlySerializedAs("petHitColor")]
        [SerializeField] private Color _petHitColor = Color.darkOrange;
        [FormerlySerializedAs("healColor")]
        [SerializeField] private Color _healColor = Color.green;
        [FormerlySerializedAs("buffColor")]
        [SerializeField] private Color _buffColor = Color.cyan;
        [FormerlySerializedAs("debuffColor")]
        [SerializeField] private Color _debuffColor = Color.yellow;
        [FormerlySerializedAs("experienceColor")]
        [SerializeField] private Color _experienceColor = Color.magenta;

        [Header("Spawn")]
        [FormerlySerializedAs("heightOffset")]
        [SerializeField] private float _heightOffset = 2f;
        [FormerlySerializedAs("randomSpreadX")]
        [SerializeField] private float _randomSpreadX = 0.3f;

        private void Awake()
        {
            if (_instance != null) return;
            _instance = this;

            PerformanceTelemetry.SessionStarted += OnTelemetrySessionStarted;
            if (PerformanceTelemetry.IsEnabled)
                ReconcileTransientTracking();
        }

        private void Start()
        {
            ActorEndpoint.onCombatLogEntryReceived += OnCombatLogEntryReceived;
            CharacterEndpoint.onClientExperienceUpdated += OnExperienceUpdated;
        }

        private void LateUpdate()
        {
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.UI_TRANSIENT_COUNT, _activeTransientCount);
        }

        private void OnDestroy()
        {
            ActorEndpoint.onCombatLogEntryReceived -= OnCombatLogEntryReceived;
            CharacterEndpoint.onClientExperienceUpdated -= OnExperienceUpdated;
            PerformanceTelemetry.SessionStarted -= OnTelemetrySessionStarted;

            foreach (UI_FloatingCombatText transient in _trackedTransients)
            {
                if (transient != null)
                    transient.Released -= OnTransientReleased;
            }
            _trackedTransients.Clear();

            if (_instance == this)
                _instance = null;
        }

        private void OnExperienceUpdated(int level, int experience, int delta)
        {
            string content = $"+{delta} XP";
            Color color = _experienceColor;

            Vector3 spawnPos = ClientAccountManager.PlayerActor.transform.position + Vector3.up * _heightOffset;
            float spreadX = UnityEngine.Random.Range(-_randomSpreadX, _randomSpreadX);
            Vector3 direction = (Vector3.up + Vector3.right * spreadX).normalized;

            GameObject fctObj = Instantiate(_fctPrefab, spawnPos, Quaternion.identity, UI_WorldSpace.Instance.fctContainer);
            var fct = fctObj.GetComponent<UI_FloatingCombatText>();
            fct.Initialize(content, color, direction, 1, 3);
            TrackTransient(fct);
        }

        private void OnCombatLogEntryReceived(CombatLogEntry entry)
        {
            if (entry.Source != ClientAccountManager.PlayerActor && entry.Target != ClientAccountManager.PlayerActor) return;
            if (entry.Target == null) return;

            string content;
            Color color;
            float sizeMultiplier = 1f;
            float durationMultiplier = 1f;

            switch (entry.EntryType)
            {
                case CombatHistoryEntryType.Damage:
                    color = GetHitTypeColor(entry.HitType);

                    switch (entry.ResultType)
                    {
                        case CombatLogResultType.Miss:
                            content = "Miss";
                            break;
                        case CombatLogResultType.Dodged:
                            content = "Dodge";
                            break;
                        case CombatLogResultType.Parried:
                            content = "Parry";
                            break;
                        case CombatLogResultType.Blocked:
                            content = "Block";
                            break;
                        case CombatLogResultType.Resisted:
                            content = "Resist";
                            break;
                        case CombatLogResultType.Absorbed:
                            content = "Absorb";
                            break;
                        case CombatLogResultType.Reflected:
                            content = "Reflect";
                            break;
                        case CombatLogResultType.CriticalHit:
                            content = entry.Amount.ToString();
                            sizeMultiplier = 1.5f;
                            durationMultiplier = 1.5f;
                            break;
                        default:
                            content = entry.Amount.ToString();
                            break;
                    }
                    break;
                case CombatHistoryEntryType.Heal:
                    content = $"+{entry.Amount}";
                    color = _healColor;
                    if (entry.ResultType == CombatLogResultType.CriticalHeal)
                    {
                        sizeMultiplier = 1.5f;
                        durationMultiplier = 1.5f;
                    }
                    break;
                case CombatHistoryEntryType.BuffApplied:
                case CombatHistoryEntryType.BuffRemoved:
                    color = _buffColor;
                    content = entry.Amount.ToString();
                    break;
                case CombatHistoryEntryType.DebuffApplied:
                case CombatHistoryEntryType.DebuffRemoved:
                    color = _debuffColor;
                    content = entry.Amount.ToString();
                    break;
                default:
                    return;
            }

            Vector3 spawnPos = entry.Target.transform.position + Vector3.up * _heightOffset;
            float spreadX = UnityEngine.Random.Range(-_randomSpreadX, _randomSpreadX);
            Vector3 direction = ((entry.Target != ClientAccountManager.PlayerActor ? Vector3.up : Vector3.down) + Vector3.right * spreadX).normalized;

            GameObject fctObj = Instantiate(_fctPrefab, spawnPos, Quaternion.identity, UI_WorldSpace.Instance.fctContainer);
            var fct = fctObj.GetComponent<UI_FloatingCombatText>();
            fct.Initialize(content, color, direction, sizeMultiplier, durationMultiplier);
            TrackTransient(fct);
        }

        private void TrackTransient(UI_FloatingCombatText transient)
        {
            if (!PerformanceTelemetry.IsEnabled || transient == null)
                return;

            if (!_trackedTransients.Add(transient))
                return;

            transient.Released += OnTransientReleased;
            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.UI_TRANSIENT_SPAWN_COUNT, 1);
            RecordTransientCount(1);
        }

        private void OnTransientReleased(UI_FloatingCombatText transient)
        {
            if (!_trackedTransients.Remove(transient))
                return;

            if (transient != null)
                transient.Released -= OnTransientReleased;
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.UI_TRANSIENT_RELEASE_COUNT, 1);
            RecordTransientCount(-1);
        }

        private void RecordTransientCount(int delta)
        {
            _activeTransientCount = Mathf.Max(0, _activeTransientCount + delta);
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.UI_TRANSIENT_COUNT, _activeTransientCount);
        }

        private void OnTelemetrySessionStarted()
        {
            ReconcileTransientTracking();
        }

        private void ReconcileTransientTracking()
        {
            _trackedTransients.RemoveWhere(transient => transient == null);

            Transform container = UI_WorldSpace.Instance != null
                ? UI_WorldSpace.Instance.fctContainer
                : null;
            if (container != null)
            {
                UI_FloatingCombatText[] existingTransients = container.GetComponentsInChildren<UI_FloatingCombatText>(true);
                for (int i = 0; i < existingTransients.Length; i++)
                {
                    UI_FloatingCombatText transient = existingTransients[i];
                    if (transient == null || !_trackedTransients.Add(transient))
                        continue;

                    transient.Released += OnTransientReleased;
                }
            }

            _activeTransientCount = 0;
            foreach (UI_FloatingCombatText transient in _trackedTransients)
            {
                if (transient != null)
                    _activeTransientCount++;
            }
        }

        private int _activeTransientCount;

        private Color GetHitTypeColor(SpellHitType hitType)
        {
            return hitType switch
            {
                SpellHitType.Auto => _autoHitColor,
                SpellHitType.Ability => _abilityHitColor,
                SpellHitType.Pet => _petHitColor,
                _ => _autoHitColor
            };
        }
    }
}
