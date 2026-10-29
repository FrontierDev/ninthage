using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Data;
using Game.Shared.Networking;
using PurrNet;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Runtime.Actors
{
    /// <summary>Stores one actor stat's base, modifier, and current values.</summary>
    public class StatInstance
    {
        private float _baseValue;
        private float _flatModifier;
        private float _percentModifier;
        private float _effectiveMaximum;
        private float _currentValue;

        public float BaseValue => _baseValue;
        public float FlatModifier => _flatModifier;
        public float PercentModifier => _percentModifier;
        public float EffectiveMaximum => _effectiveMaximum;
        public float CurrentValue => Mathf.Clamp(_currentValue, 0, EffectiveMaximum);

        public StatInstance() { }
        public StatInstance(float baseValue, float flatModifier, float percentModifier, float effectiveMaximum, float currentValue)
        {
            _baseValue = baseValue;
            _flatModifier = flatModifier;
            _percentModifier = percentModifier;
            _effectiveMaximum = effectiveMaximum;
            _currentValue = currentValue;
        }

        public void SetBaseValue(float value) { _baseValue = value; }
        public void SetFlatModifier(float value) { _flatModifier = value; }
        public void SetPercentModifier(float value) { _percentModifier = value; }
        public void RecalculateEffectiveMax()
        {
            _effectiveMaximum = (_baseValue + _flatModifier) * (1f + _percentModifier);
        }
    }

    /// <summary>Identifies the source of an actor stat modifier.</summary>
    public enum ActorStatModifierSource
    {
        Aura,
        Talent
    }

    /// <summary>Defines a flat and percentage modifier for a stat.</summary>
    public sealed class StatModifier
    {
        public ActorStatModifierSource Source;
        public string SourceID;

        public string StatID;
        public float FlatModifier;
        public float PercentModifier;

        public StatModifier(string statID, float flatModifier, float percentModifier, ActorStatModifierSource source = ActorStatModifierSource.Talent, string sourceID = "")
        {
            StatID = statID;
            FlatModifier = flatModifier;
            PercentModifier = percentModifier;

            Source = source;
            SourceID = sourceID;
        }
    }


    [RequireComponent(typeof(Actor))]
    /// <summary>Stores and replicates an actor's stats and modifiers.</summary>
    public sealed class ActorStatContainer : NetworkBehaviour
    {
        private Dictionary<string, StatInstance> _stats = new();
        private List<StatModifier> _modifiers = new();
        private bool _initialStatRestoreActive;

        protected override void OnObserverAdded(PlayerID player)
        {
            base.OnObserverAdded(player);

            if (!isServer) return;
            if (_stats.Count == 0) return;

            var observerSnapshot = BuildSnapshot(ActorStatReplicationMode.Observers);
            if (observerSnapshot.Stats.Length > 0)
                PublishObserverBootstrapSnapshot(player, observerSnapshot);

            if (owner.HasValue && owner.Value == player)
            {
                var ownerSnapshot = BuildSnapshot(ActorStatReplicationMode.Owner);
                if (ownerSnapshot.Stats.Length > 0)
                    PublishOwnerSnapshot(player, ownerSnapshot);
            }
        }

        /// <summary>Publishes an observer-mode stat snapshot to the current observer set.</summary>
        /// <exception cref="InvalidOperationException">Thrown when called outside the server.</exception>
        public void PublishObserverSnapshot(StatSnapshot snapshot)
        {
            if (!NetworkManager.isServerStatic)
                throw new InvalidOperationException("Only the server may publish observer stat snapshots.");

            NetworkingPerformanceMetrics.RecordCombatCriticalObserverPublication(GetComponent<NetworkIdentity>());
            Observers_ReceiveSnapshot(snapshot);
        }

        /// <summary>Publishes a stat snapshot privately to the specified owner.</summary>
        /// <exception cref="InvalidOperationException">Thrown when called outside the server.</exception>
        public void PublishOwnerSnapshot(PlayerID playerID, StatSnapshot snapshot)
        {
            if (!NetworkManager.isServerStatic)
                throw new InvalidOperationException("Only the server may publish owner stat snapshots.");

            NetworkingPerformanceMetrics.RecordOwnerPrivateTargetPublication();
            Client_ReceiveSnapshot(playerID, snapshot);
        }

        private void PublishObserverBootstrapSnapshot(PlayerID playerID, StatSnapshot snapshot)
        {
            NetworkingPerformanceMetrics.RecordCombatCriticalTargetPublication();
            Client_ReceiveSnapshot(playerID, snapshot);
        }

        #region Stat Management
        /// <summary>Begins the server-only initial stat restore window.</summary>
        /// <exception cref="InvalidOperationException">Thrown when called outside the server or more than once.</exception>
        public void BeginInitialStatRestore()
        {
            if (!NetworkManager.isServerStatic)
                throw new InvalidOperationException("Initial stat restoration may only run on the server.");
            if (_initialStatRestoreActive)
                throw new InvalidOperationException("Initial stat restoration is already active.");

            _initialStatRestoreActive = true;
        }

        /// <summary>Ends the server-only initial stat restore window.</summary>
        /// <exception cref="InvalidOperationException">Thrown when called outside a matching restore window.</exception>
        public void EndInitialStatRestore()
        {
            if (!NetworkManager.isServerStatic)
                throw new InvalidOperationException("Initial stat restoration may only run on the server.");
            if (!_initialStatRestoreActive)
                throw new InvalidOperationException("Initial stat restoration is not active.");

            _initialStatRestoreActive = false;
        }

        public StatInstance GetStat(string statID)
        {
            if (_stats.TryGetValue(statID, out var stat))
                return stat;
            Debug.Error($"Stat with ID {statID} not found in ActorStatContainer.");
            return null;
        }

        public IEnumerable<KeyValuePair<string, StatInstance>> All => _stats;

        public bool TryGetStat(string statID, out StatInstance stat)
        {
            return _stats.TryGetValue(statID, out stat);
        }

        public void SetStat(string statID, StatInstance instance)
        {
            _stats[statID] = instance;

            if (!_initialStatRestoreActive && statID == "health" && instance.CurrentValue <= 0)
            {
                Actor actor = GetComponent<Actor>();
                if (!actor.isDead)
                {
                    Debug.Log($"{actor.Name} has died.");
                    actor.isDead = true;
                    ActorEndpoint.onServerStopAutoAttack?.Invoke(actor);
                    ActorEndpoint.onActorDeath?.Invoke(actor);
                }
            }
        }

        public void AddCurrentValue(string statID, float amount)
        {
            if (TryGetStat(statID, out var stat))
            {
                float newCurrent = Mathf.Max(0f, stat.CurrentValue + amount);
                SetStat(statID, new StatInstance(stat.BaseValue, stat.FlatModifier, stat.PercentModifier, stat.EffectiveMaximum, newCurrent));
                var def = ActorStatDefinitionLibrary.Instance.GetDefinition(statID);
                if (def.ReplicationMode == ActorStatReplicationMode.Observers)
                {
                    var snapshot = BuildSnapshot(
                        ActorStatReplicationMode.Observers,
                        new[] { statID });
                    if (snapshot.Stats.Length > 0)
                        PublishObserverSnapshot(snapshot);
                }
                else if (def.ReplicationMode == ActorStatReplicationMode.Owner)
                {
                    var snapshot = BuildSnapshot(ActorStatReplicationMode.Owner, new[] { statID });
                    if (snapshot.Stats.Length > 0)
                        PublishOwnerSnapshot(GetComponent<Actor>().Owner.Value, snapshot);
                }
            }
            else
            {
                Debug.Error($"Attempted to add to non-existent stat {statID} in ActorStatContainer.");
            }
        }
        #endregion

        #region Modifier Management
        public void AddModifier(StatModifier modifier) => _modifiers.Add(modifier);
        public void RemoveModifier(StatModifier modifier) => _modifiers.Remove(modifier);

        public bool TryGetModifiersForStat(string statID, out List<StatModifier> flatModifiers, out List<StatModifier> percentModifiers)
        {
            flatModifiers = new List<StatModifier>();
            percentModifiers = new List<StatModifier>();

            foreach (var mod in _modifiers)
            {
                if (mod.StatID != statID) continue;
                if (mod.FlatModifier != 0) flatModifiers.Add(mod);
                if (mod.PercentModifier != 0) percentModifiers.Add(mod);
            }

            return flatModifiers.Count > 0 || percentModifiers.Count > 0;
        }

        #endregion
        #region  Snapshot RPCs
        [TargetRpc]
        public void Client_ReceiveSnapshot(PlayerID playerID, StatSnapshot snapshot, RPCInfo info = default)
        {
            StatEndpoint.RecordSnapshotHandlerInvocation();
            ApplySnapshot(snapshot);
        }

        [ObserversRpc]
        public void Observers_ReceiveSnapshot(StatSnapshot snapshot, RPCInfo info = default)
        {
            StatEndpoint.RecordSnapshotHandlerInvocation();
            ApplySnapshot(snapshot);
        }

        public void ApplySnapshot(StatSnapshot snapshot)
        {
            foreach (var entry in snapshot.Stats)
            {
                _stats[entry.StatID] = new StatInstance(0, entry.FlatModifier, entry.PercentModifier, entry.EffectiveMax, entry.Current);
            }

            Game.Shared.Networking.StatEndpoint.onStatsUpdated?.Invoke(GetComponent<Actor>(), this);
        }

        public StatSnapshot BuildSnapshot(ActorStatReplicationMode mode)
        {
            var library = ActorStatDefinitionLibrary.Instance;
            var list = new List<StatSnapshotEntry>();
            foreach (var kvp in _stats)
            {
                var def = library.GetDefinition(kvp.Key);
                if (def == null || def.ReplicationMode != mode) continue;
                list.Add(CreateSnapshotEntry(kvp.Key, kvp.Value));
            }
            return new StatSnapshot { Stats = list.ToArray() };
        }

        public StatSnapshot BuildSnapshot(ActorStatReplicationMode mode, IReadOnlyList<string> statIDs)
        {
            if (statIDs == null)
                throw new ArgumentNullException(nameof(statIDs));

            var library = ActorStatDefinitionLibrary.Instance;
            var list = new List<StatSnapshotEntry>(statIDs.Count);
            HashSet<string> seenStatIDs = statIDs.Count > 1 ? new HashSet<string>() : null;
            for (int i = 0; i < statIDs.Count; i++)
            {
                string statID = statIDs[i];
                if (statID == null) continue;
                if (seenStatIDs != null && !seenStatIDs.Add(statID))
                    throw new ArgumentException($"Duplicate stat ID '{statID}' supplied to subset snapshot builder.", nameof(statIDs));
                if (!_stats.TryGetValue(statID, out var stat)) continue;

                var def = library.GetDefinition(statID);
                if (def == null || def.ReplicationMode != mode) continue;
                list.Add(CreateSnapshotEntry(statID, stat));
            }

            return new StatSnapshot { Stats = list.ToArray() };
        }

        private static StatSnapshotEntry CreateSnapshotEntry(string statID, StatInstance stat)
        {
            return new StatSnapshotEntry
            {
                StatID = statID,
                EffectiveMax = stat.EffectiveMaximum,
                FlatModifier = stat.FlatModifier,
                PercentModifier = stat.PercentModifier,
                Current = stat.CurrentValue
            };
        }
    }
    #endregion
}
