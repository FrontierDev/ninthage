using System.Collections.Generic;
using Game.Shared.Data;
using Game.Shared.Networking;
using PurrNet;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared
{
    public class StatInstance
    {
        private float baseValue;
        private float flatModifier;
        private float percentModifier;
        private float effectiveMaximum;
        private float currentValue;

        public float BaseValue => baseValue;
        public float FlatModifier => flatModifier;
        public float PercentModifier => percentModifier;
        public float EffectiveMaximum => effectiveMaximum;
        public float CurrentValue => Mathf.Clamp(currentValue, 0, EffectiveMaximum);

        public StatInstance() { }
        public StatInstance(float baseValue, float flatModifier, float percentModifier, float effectiveMaximum, float currentValue)
        {
            this.baseValue = baseValue;
            this.flatModifier = flatModifier;
            this.percentModifier = percentModifier;
            this.effectiveMaximum = effectiveMaximum;
            this.currentValue = currentValue;
        }

        public void SetBaseValue(float value) { baseValue = value; }
        public void SetFlatModifier(float value) { flatModifier = value; }
        public void SetPercentModifier(float value) { percentModifier = value; }
        public void RecalculateEffectiveMax()
        {
            effectiveMaximum = (baseValue + flatModifier) * (1f + percentModifier);
        }
    }

    public enum ActorStatModifierSource
    {
        Aura,
        Talent
    }

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
    public sealed class ActorStatContainer : NetworkBehaviour
    {
        private Dictionary<string, StatInstance> stats = new();
        private List<StatModifier> modifiers = new();

        protected override void OnObserverAdded(PlayerID player)
        {
            base.OnObserverAdded(player);

            if (!isServer) return;
            if (stats.Count == 0) return;

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
            if (stats.TryGetValue(statId, out var stat))
                return stat;
            Debug.Error($"Stat with ID {statId} not found in ActorStatContainer.");
            return null;
        }

        public IEnumerable<KeyValuePair<string, StatInstance>> All => stats;

        public bool TryGetStat(string statId, out StatInstance stat)
        {
            return stats.TryGetValue(statId, out stat);
        }

        public void SetStat(string statId, StatInstance instance)
        {
            stats[statId] = instance;

            if (statId == "health" && instance.CurrentValue <= 0 && !GetComponent<Actor>().isDead)
            {
                Debug.Log($"{GetComponent<Actor>().Name} has died.");
                ActorService.onActorDeath?.Invoke(GetComponent<Actor>());
                GetComponent<Actor>().isDead = true;
                ActorService.onServerStopAutoAttack?.Invoke(GetComponent<Actor>());
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
        public void AddModifier(StatModifier modifier) => modifiers.Add(modifier);
        public void RemoveModifier(StatModifier modifier) => modifiers.Remove(modifier);

        public bool TryGetModifiersForStat(string statId, out List<StatModifier> flatModifiers, out List<StatModifier> percentModifiers)
        {
            flatModifiers = new List<StatModifier>();
            percentModifiers = new List<StatModifier>();

            foreach (var mod in modifiers)
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
                stats[entry.StatId] = new StatInstance(0, entry.FlatModifier, entry.PercentModifier, entry.EffectiveMax, entry.Current);
            }

            Game.Shared.Networking.StatService.onStatsUpdated?.Invoke(GetComponent<Actor>(), this);
        }

        public StatSnapshot BuildSnapshot(ActorStatReplicationMode mode)
        {
            var library = ActorStatDefinitionLibrary.Instance;
            var list = new List<StatSnapshotEntry>();
            foreach (var kvp in stats)
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