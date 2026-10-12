using Debug = Game.Shared.FormattedDebug;
using Game.Shared.Data;
using Game.Shared.Networking;
using PurrNet;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared
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

        public string StatId;
        public float FlatModifier;
        public float PercentModifier;

        public StatModifier(string statId, float flatModifier, float percentModifier, ActorStatModifierSource source = ActorStatModifierSource.Talent, string sourceId = "")
        {
            StatId = statId;
            FlatModifier = flatModifier;
            PercentModifier = percentModifier;

            Source = source;
            SourceID = sourceId;
        }
    }


    [RequireComponent(typeof(Actor))]
    /// <summary>Stores and replicates an actor's stats and modifiers.</summary>
    public sealed class ActorStatContainer : NetworkBehaviour
    {
        private Dictionary<string, StatInstance> _stats = new();
        private List<StatModifier> _modifiers = new();

        protected override void OnObserverAdded(PlayerID player)
        {
            base.OnObserverAdded(player);

            if (!isServer) return;
            if (_stats.Count == 0) return;

            var observerSnapshot = BuildSnapshot(ActorStatReplicationMode.Observers);
            if (observerSnapshot.Stats.Length > 0)
                Client_ReceiveSnapshot(player, observerSnapshot);

            if (owner.HasValue && owner.Value == player)
            {
                var ownerSnapshot = BuildSnapshot(ActorStatReplicationMode.Owner);
                if (ownerSnapshot.Stats.Length > 0)
                    Client_ReceiveSnapshot(player, ownerSnapshot);
            }
        }

        #region Stat Management
        public StatInstance GetStat(string statId)
        {
            if (_stats.TryGetValue(statId, out var stat))
                return stat;
            Debug.Error($"Stat with ID {statId} not found in ActorStatContainer.");
            return null;
        }

        public IEnumerable<KeyValuePair<string, StatInstance>> All => _stats;

        public bool TryGetStat(string statId, out StatInstance stat)
        {
            return _stats.TryGetValue(statId, out stat);
        }

        public void SetStat(string statId, StatInstance instance)
        {
            _stats[statId] = instance;

            if (statId == "health" && instance.CurrentValue <= 0 && !GetComponent<Actor>().isDead)
            {
                Debug.Log($"{GetComponent<Actor>().Name} has died.");
                ActorEndpoint.onActorDeath?.Invoke(GetComponent<Actor>());
                GetComponent<Actor>().isDead = true;
                ActorEndpoint.onServerStopAutoAttack?.Invoke(GetComponent<Actor>());
            }
        }

        public void AddCurrentValue(string statId, float amount)
        {
            if (TryGetStat(statId, out var stat))
            {
                float newCurrent = Mathf.Max(0f, stat.CurrentValue + amount);
                SetStat(statId, new StatInstance(stat.BaseValue, stat.FlatModifier, stat.PercentModifier, stat.EffectiveMaximum, newCurrent));
                var def = ActorStatDefinitionLibrary.Instance.GetDefinition(statId);
                if (def.ReplicationMode == ActorStatReplicationMode.Observers)
                    Observers_ReceiveSnapshot(BuildSnapshot(ActorStatReplicationMode.Observers));
                else if (def.ReplicationMode == ActorStatReplicationMode.Owner)
                    Client_ReceiveSnapshot(GetComponent<Actor>().Owner.Value, BuildSnapshot(ActorStatReplicationMode.Owner));
            }
            else
            {
                Debug.Error($"Attempted to add to non-existent stat {statId} in ActorStatContainer.");
            }
        }
        #endregion

        #region Modifier Management
        public void AddModifier(StatModifier modifier) => _modifiers.Add(modifier);
        public void RemoveModifier(StatModifier modifier) => _modifiers.Remove(modifier);

        public bool TryGetModifiersForStat(string statId, out List<StatModifier> flatModifiers, out List<StatModifier> percentModifiers)
        {
            flatModifiers = new List<StatModifier>();
            percentModifiers = new List<StatModifier>();

            foreach (var mod in _modifiers)
            {
                if (mod.StatId != statId) continue;
                if (mod.FlatModifier != 0) flatModifiers.Add(mod);
                if (mod.PercentModifier != 0) percentModifiers.Add(mod);
            }

            return flatModifiers.Count > 0 || percentModifiers.Count > 0;
        }

        #endregion
        #region  Snapshot RPCs
        [TargetRpc]
        public void Client_ReceiveSnapshot(PlayerID playerId, StatSnapshot snapshot, RPCInfo info = default)
        {
            ApplySnapshot(snapshot);
        }

        [ObserversRpc]
        public void Observers_ReceiveSnapshot(StatSnapshot snapshot, RPCInfo info = default)
        {
            ApplySnapshot(snapshot);
        }

        public void ApplySnapshot(StatSnapshot snapshot)
        {
            foreach (var entry in snapshot.Stats)
            {
                _stats[entry.StatId] = new StatInstance(0, entry.FlatModifier, entry.PercentModifier, entry.EffectiveMax, entry.Current);
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
                list.Add(new StatSnapshotEntry
                {
                    StatId = kvp.Key,
                    EffectiveMax = kvp.Value.EffectiveMaximum,
                    FlatModifier = kvp.Value.FlatModifier,
                    PercentModifier = kvp.Value.PercentModifier,
                    Current = kvp.Value.CurrentValue
                });
            }
            return new StatSnapshot { Stats = list.ToArray() };
        }
    }
    #endregion
}
