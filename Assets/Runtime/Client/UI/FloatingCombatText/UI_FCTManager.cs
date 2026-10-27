using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Client.Performance;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using Game.Shared.Runtime.Spellcasting;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
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

        private readonly HashSet<UI_FloatingCombatText> _activeTransients = new();
        private ObjectPool<UI_FloatingCombatText> _pool;
        private bool _isDestroying;
        private bool _isClearingPool;

        [Header("Prefab")]
        [FormerlySerializedAs("fctPrefab")]
        [SerializeField] private GameObject _fctPrefab;

        [Header("Pooling")]
        [Min(1)]
        [SerializeField] private int _poolCapacity = 64;

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
            if (_instance != null && !ReferenceEquals(_instance, this))
            {
                Debug.Error("UI_FCTManager is already initialized.");
                Destroy(this);
                return;
            }

            if (_poolCapacity <= 0)
                throw new InvalidOperationException($"{nameof(_poolCapacity)} must be greater than zero.");

            _instance = this;
            _pool = new ObjectPool<UI_FloatingCombatText>(
                CreatePooledTransient,
                OnTakeFromPool,
                OnReturnedToPool,
                DestroyPooledTransient,
                collectionCheck: true,
                defaultCapacity: Mathf.Min(_poolCapacity, 16),
                maxSize: _poolCapacity);

            PerformanceTelemetry.SessionStarted += OnTelemetrySessionStarted;
            if (PerformanceTelemetry.IsEnabled)
                OnTelemetrySessionStarted();
        }

        private void Start()
        {
            ActorEndpoint.onCombatLogEntryReceived += OnCombatLogEntryReceived;
            CharacterEndpoint.onClientExperienceUpdated += OnExperienceUpdated;
            ClientWorldManager.Instance.onWorldPresentationEnding += OnWorldPresentationEnding;
        }

        private void LateUpdate()
        {
            if (PerformanceTelemetry.IsEnabled)
            {
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.UI_TRANSIENT_COUNT, _activeTransients.Count);
                PerformanceTelemetry.TryRecord(
                    ClientPerformanceMetrics.FCT_POOL_RETAINED_COUNT,
                    _pool?.CountInactive ?? 0);
            }
        }

        private void OnDestroy()
        {
            if (!ReferenceEquals(_instance, this))
                return;

            _isDestroying = true;
            ActorEndpoint.onCombatLogEntryReceived -= OnCombatLogEntryReceived;
            CharacterEndpoint.onClientExperienceUpdated -= OnExperienceUpdated;
            PerformanceTelemetry.SessionStarted -= OnTelemetrySessionStarted;
            if (ClientWorldManager.Initialized)
                ClientWorldManager.Instance.onWorldPresentationEnding -= OnWorldPresentationEnding;

            ClearOwnedTransients();
            _pool.Dispose();
            _pool = null;

            _instance = null;
        }

        private void OnExperienceUpdated(int level, int experience, int delta)
        {
            string content = $"+{delta} XP";
            Color color = _experienceColor;

            Vector3 spawnPos = ClientAccountManager.PlayerActor.transform.position + Vector3.up * _heightOffset;
            float spreadX = UnityEngine.Random.Range(-_randomSpreadX, _randomSpreadX);
            Vector3 direction = (Vector3.up + Vector3.right * spreadX).normalized;

            ActivateTransient(spawnPos, content, color, direction, 1f, 3f);
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

            ActivateTransient(spawnPos, content, color, direction, sizeMultiplier, durationMultiplier);
        }

        private UI_FloatingCombatText CreatePooledTransient()
        {
            var worldSpace = UI_WorldSpace.Instance;
            if (worldSpace == null || worldSpace.fctContainer == null)
                throw new InvalidOperationException("UI_WorldSpace.fctContainer is required to create a pooled FCT.");
            if (_fctPrefab == null)
                throw new InvalidOperationException("UI_FCTManager requires an FCT prefab.");

            var fctObject = Instantiate(_fctPrefab, worldSpace.fctContainer);
            var transient = fctObject.GetComponent<UI_FloatingCombatText>();
            if (transient == null)
            {
                Destroy(fctObject);
                throw new InvalidOperationException("The FCT prefab is missing UI_FloatingCombatText.");
            }

            fctObject.SetActive(false);
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.FCT_POOL_CREATE_COUNT, 1);
            return transient;
        }

        private void OnTakeFromPool(UI_FloatingCombatText transient)
        {
            if (transient == null || transient.gameObject == null)
                throw new InvalidOperationException("Cannot take a destroyed FCT from the pool.");

            var worldSpace = UI_WorldSpace.Instance;
            if (worldSpace == null || worldSpace.fctContainer == null)
                throw new InvalidOperationException("UI_WorldSpace.fctContainer is required to take a pooled FCT.");

            if (transient.transform.parent != worldSpace.fctContainer)
                transient.transform.SetParent(worldSpace.fctContainer);
            transient.gameObject.SetActive(true);
        }

        private void OnReturnedToPool(UI_FloatingCombatText transient)
        {
            if (transient == null || transient.gameObject == null)
                throw new InvalidOperationException("Cannot return a destroyed FCT to the pool.");

            transient.ResetForPool();
            transient.gameObject.SetActive(false);
        }

        private void DestroyPooledTransient(UI_FloatingCombatText transient)
        {
            if (transient == null || transient.gameObject == null)
                return;

            Destroy(transient.gameObject);
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.FCT_POOL_DESTROY_COUNT, 1);
        }

        private UI_FloatingCombatText AcquireTransient(Vector3 spawnPosition)
        {
            if (_pool == null)
                throw new InvalidOperationException("The FCT pool is not initialized.");

            var worldSpace = UI_WorldSpace.Instance;
            if (worldSpace == null || worldSpace.fctContainer == null)
                throw new InvalidOperationException("UI_WorldSpace.fctContainer is required to acquire a pooled FCT.");

            var reused = _pool.CountInactive > 0;
            var transient = _pool.Get();
            if (reused && PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.FCT_POOL_REUSE_COUNT, 1);

            transient.transform.SetParent(worldSpace.fctContainer);
            transient.transform.position = spawnPosition;
            transient.transform.rotation = Quaternion.identity;
            transient.transform.localScale = Vector3.one;
            RecordTransientGauges();
            return transient;
        }

        private void ActivateTransient(
            Vector3 spawnPosition,
            string content,
            Color color,
            Vector3 direction,
            float sizeMultiplier,
            float durationMultiplier)
        {
            var transient = AcquireTransient(spawnPosition);
            if (!_activeTransients.Add(transient))
                throw new InvalidOperationException("An FCT was acquired more than once.");

            transient.ReleaseRequested += OnTransientReleaseRequested;
            transient.DestroyedUnexpectedly += OnTransientDestroyedUnexpectedly;
            try
            {
                transient.Activate(content, color, direction, sizeMultiplier, durationMultiplier);
            }
            catch
            {
                transient.ReleaseRequested -= OnTransientReleaseRequested;
                transient.DestroyedUnexpectedly -= OnTransientDestroyedUnexpectedly;
                _activeTransients.Remove(transient);
                _pool.Release(transient);
                throw;
            }

            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.UI_TRANSIENT_SPAWN_COUNT, 1);
            RecordTransientGauges();
        }

        private void OnTransientReleaseRequested(UI_FloatingCombatText transient)
        {
            if (!_activeTransients.Remove(transient))
                return;

            if (transient != null)
            {
                transient.ReleaseRequested -= OnTransientReleaseRequested;
                transient.DestroyedUnexpectedly -= OnTransientDestroyedUnexpectedly;
            }

            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.UI_TRANSIENT_RELEASE_COUNT, 1);
            _pool.Release(transient);
            RecordTransientGauges();
        }

        private void OnTransientDestroyedUnexpectedly(UI_FloatingCombatText transient)
        {
            if (!_activeTransients.Remove(transient))
                return;

            if (transient != null)
            {
                transient.ReleaseRequested -= OnTransientReleaseRequested;
                transient.DestroyedUnexpectedly -= OnTransientDestroyedUnexpectedly;
            }

            if (PerformanceTelemetry.IsEnabled)
            {
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.UI_TRANSIENT_RELEASE_COUNT, 1);
                PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.FCT_POOL_DESTROY_COUNT, 1);
            }
            Debug.Error("An active pool-owned FCT was destroyed outside the manager/pool lifecycle.");
            RecordTransientGauges();
        }

        private void OnWorldPresentationEnding()
        {
            ClearOwnedTransients();
        }

        private void ClearOwnedTransients()
        {
            if (_pool == null)
                return;

            _isClearingPool = true;
            try
            {
                var activeTransients = new List<UI_FloatingCombatText>(_activeTransients);
                foreach (var transient in activeTransients)
                {
                    if (transient != null)
                        transient.Cancel();
                }

                _activeTransients.RemoveWhere(transient => transient == null);
                if (_activeTransients.Count != 0)
                    throw new InvalidOperationException("Active FCT ownership did not settle during cleanup.");

                _pool.Clear();
            }
            finally
            {
                _isClearingPool = false;
            }

            RecordTransientGauges();
        }

        private void OnTelemetrySessionStarted()
        {
            RecordTransientGauges();
        }

        private void RecordTransientGauges()
        {
            if (!PerformanceTelemetry.IsEnabled)
                return;

            PerformanceTelemetry.TryRecord(ClientPerformanceMetrics.UI_TRANSIENT_COUNT, _activeTransients.Count);
            PerformanceTelemetry.TryRecord(
                ClientPerformanceMetrics.FCT_POOL_RETAINED_COUNT,
                _pool?.CountInactive ?? 0);
        }

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
