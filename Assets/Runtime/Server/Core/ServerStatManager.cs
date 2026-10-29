using Actor = Game.Shared.Runtime.Actors.Actor;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Persistence;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>Maintains server-authoritative actor statistics.</summary>
    public sealed class ServerStatManager : MonoBehaviour
    {
        private static ServerStatManager _instance;
        /// <summary>Gets the active server stat manager.</summary>
        public static ServerStatManager Instance => _instance;
        private static bool _initialized;
        /// <summary>Gets whether the stat manager is initialized.</summary>
        public static bool IsInitialized => _initialized;

        private const float REGEN_TICK_INTERVAL = 1f;
        private const string STAT_HEALTH = "health";
        private float _regenTimer;

        private readonly List<Actor> _trackedActors = new();
        private readonly Dictionary<Actor, Dictionary<string, float>> _currentEquipmentModifiers = new();
        private readonly List<string> _changedStatIDs = new();

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("ServerStatManager is already initialized!");
                return;
            }
            _instance = this;

            // Subscribe to character service events for stat updates
            Game.Shared.Networking.CharacterEndpoint.onUpdateStatsRequest += OnUpdateStatsRequest;

            _initialized = true;
        }

        private void OnDestroy()
        {
            Game.Shared.Networking.CharacterEndpoint.onUpdateStatsRequest -= OnUpdateStatsRequest;

            _trackedActors.Clear();
            _changedStatIDs.Clear();
            _instance = null;
            _initialized = false;
        }

        private void OnUpdateStatsRequest(ActorStatContainer container)
        {
            Recalculate(container);
            FillAttributesToMax(container);
            SendSnapshot(container.GetComponent<Actor>());
        }

        private void Update()
        {
            _regenTimer += Time.deltaTime;
            if (_regenTimer >= REGEN_TICK_INTERVAL)
            {
                TickRegen(_regenTimer);
                _regenTimer = 0f;
            }
        }

        /// <summary>Registers an actor for server stat updates.</summary>
        /// <param name="actor">The actor to register.</param>
        public void RegisterActor(Actor actor)
        {
            _trackedActors.Add(actor);
        }

        /// <summary>Unregisters an actor from server stat updates.</summary>
        /// <param name="actor">The actor to unregister.</param>
        public void UnregisterActor(Actor actor)
        {
            _trackedActors.Remove(actor);
        }

        #region Initialization

        /// <summary>Initializes statistics for an NPC actor.</summary>
        /// <param name="actor">The NPC actor to initialize.</param>
        public void InitializeNPCStats(Actor actor)
        {
            var container = actor.GetComponent<ActorStatContainer>();
            if (container == null) return;

            var npcProfile = actor.GetComponent<NPCStatProfile>();
            if (npcProfile == null)
            {
                Debug.Warning($"NPCStatProfile not found on actor {actor.Name}. No stats initialized.");
                return;
            }

            foreach (var entry in npcProfile.BaseStats)
            {
                container.SetStat(entry.definition.DefinitionID,
                    new StatInstance(entry.baseValue, 0, 0, entry.baseValue, entry.baseValue));
            }

            Recalculate(container);
            FillResourcesToMax(container);
            FillAttributesToMax(container);
        }

        /// <summary>
        /// Initializes player stats based on their race, class, and level. 
        /// This should be called when a player first spawns in the world or when their character data is loaded. 
        /// It sets up the base stats from the libraries, applies race and class modifiers, 
        /// and calculates level-based growth.
        /// </summary>
        /// <param name="actor"></param>
        public void InitializePlayerStats(Actor actor)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            var container = actor.GetComponent<ActorStatContainer>();
            if (container == null)
                throw new InvalidOperationException($"Actor {actor.Name} has no ActorStatContainer.");

            var playerActor = actor as PlayerActor;
            if (playerActor == null || playerActor.CharacterData == null)
                throw new InvalidOperationException($"PlayerActor or CharacterData is null for actor {actor.Name}.");

            var data = playerActor.CharacterData;
            var race = RaceDefinitionLibrary.Instance.GetDefinition(data.RaceID);
            var playerClass = ClassDefinitionLibrary.Instance.GetDefinition(data.ClassID);

            if (race == null || playerClass == null)
                throw new InvalidOperationException(
                    $"Race or Class definition not found for player {actor.Name} "
                    + $"(Race: {data.RaceID}, Class: {data.ClassID}).");

            container.BeginInitialStatRestore();
            try
            {
                // Initialize all stats from library with their definition base values.
                var library = ActorStatDefinitionLibrary.Instance;
                var allDefs = library.GetAllDefinitions();
                for (int i = 0; i < allDefs.Count; i++)
                {
                    var def = allDefs[i];
                    float baseValue = def.BaseValueMode == ActorStatBaseValueMode.Fixed ? def.BaseValue : 0f;
                    container.SetStat(def.DefinitionID,
                        new StatInstance(baseValue, 0, 0, 0, 0));
                }

                if (!container.TryGetStat(STAT_HEALTH, out _))
                    throw new InvalidOperationException($"Actor stat definition '{STAT_HEALTH}' is missing for player {actor.Name}.");

                // Apply race base stats.
                foreach (var entry in race.BaseStats)
                {
                    if (entry?.definition == null) continue;
                    if (container.TryGetStat(entry.definition.DefinitionID, out var existing))
                    {
                        container.SetStat(entry.definition.DefinitionID,
                            new StatInstance(existing.BaseValue + entry.baseValue, 0, 0, 0, 0));
                    }
                }

                // Apply class base stats.
                foreach (var entry in playerClass.BaseStats)
                {
                    if (entry?.definition == null) continue;
                    if (container.TryGetStat(entry.definition.DefinitionID, out var existing))
                    {
                        container.SetStat(entry.definition.DefinitionID,
                            new StatInstance(existing.BaseValue + entry.baseValue, 0, 0, 0, 0));
                    }
                }

                // Apply class growth per level.
                foreach (var growth in playerClass.StatGrowthPerLevel)
                {
                    if (growth.stat == null) continue;
                    if (container.TryGetStat(growth.stat.DefinitionID, out var existing))
                    {
                        float levelBonus = growth.flatPerLevel * data.Level;
                        container.SetStat(growth.stat.DefinitionID,
                            new StatInstance(existing.BaseValue, levelBonus, 0, 0, 0));
                    }
                }

                Recalculate(container);
                FillResourcesToMax(container);
                FillAttributesToMax(container);
                RestoreSavedResources(container, data);
            }
            finally
            {
                container.EndInitialStatRestore();
            }
        }

        #endregion

        #region Recalculation

        /// <summary>Recalculates the effective statistics in a container.</summary>
        /// <param name="container">The stat container to recalculate.</param>
        public void Recalculate(ActorStatContainer container)
        {
            UpdateEquipmentModifiers(container);

            var library = ActorStatDefinitionLibrary.Instance;
            var allDefs = library.GetAllDefinitions();

            // Pass 1: fixed stats
            for (int i = 0; i < allDefs.Count; i++)
            {
                var def = allDefs[i];
                if (def.BaseValueMode != ActorStatBaseValueMode.Fixed) continue;
                if (!container.TryGetStat(def.DefinitionID, out var stat)) continue;
                stat.RecalculateEffectiveMax();
            }

            // Pass 2: derived stats
            for (int i = 0; i < allDefs.Count; i++)
            {
                var def = allDefs[i];
                if (def.BaseValueMode != ActorStatBaseValueMode.Derived) continue;
                if (def.SourceStat == null) continue;
                if (!container.TryGetStat(def.DefinitionID, out var stat)) continue;
                if (!container.TryGetStat(def.SourceStat.DefinitionID, out var source)) continue;

                stat.SetBaseValue(source.EffectiveMaximum * def.Multiplier);
                stat.RecalculateEffectiveMax();
            }

            // Pass 3: rating conversions
            for (int i = 0; i < allDefs.Count; i++)
            {
                var def = allDefs[i];
                if (def.BaseValueMode != ActorStatBaseValueMode.Rating) continue;
                if (def.SourceStat == null) continue;
                if (!container.TryGetStat(def.DefinitionID, out var stat)) continue;
                if (!container.TryGetStat(def.SourceStat.DefinitionID, out var source)) continue;

                int level = GetActorLevel(container); // resolve from PlayerActor or NPCStatProfile
                float percent = CombatRatingHelper.RatingToPercent(source.EffectiveMaximum, level, def.Multiplier);
                stat.SetBaseValue(percent);
                stat.RecalculateEffectiveMax();
            }
        }

        private void UpdateEquipmentModifiers(ActorStatContainer container)
        {
            var actor = container.GetComponent<Actor>();
            if (actor == null || actor is not PlayerActor) return;

            var equipment = actor.GetComponent<PlayerEquipment>();
            if (equipment == null) return;

            // Remove previous equipment modifiers
            if (_currentEquipmentModifiers.TryGetValue(actor, out var previous))
            {
                foreach (var kvp in previous)
                {
                    if (container.TryGetStat(kvp.Key, out var stat))
                        stat.SetFlatModifier(stat.FlatModifier - kvp.Value);
                }
            }

            // Calculate new equipment modifiers from all equipped items
            var newModifiers = new Dictionary<string, float>();
            foreach (var equippedItem in equipment.Equipment.Values)
            {
                if (equippedItem?.BaseItem?.Stats != null)
                {
                    foreach (var itemStat in equippedItem.BaseItem.Stats)
                    {
                        if (itemStat?.Stat != null)
                        {
                            if (!newModifiers.ContainsKey(itemStat.Stat.DefinitionID))
                                newModifiers[itemStat.Stat.DefinitionID] = 0f;
                            newModifiers[itemStat.Stat.DefinitionID] += itemStat.Value;
                        }
                    }
                }
            }

            // Apply new equipment modifiers
            foreach (var kvp in newModifiers)
            {
                if (container.TryGetStat(kvp.Key, out var stat))
                    stat.SetFlatModifier(stat.FlatModifier + kvp.Value);
            }

            // Cache for next update
            _currentEquipmentModifiers[actor] = newModifiers;
        }

        #endregion

        #region Modifiers

        private void ApplyEquipmentModifiers(ActorStatContainer container)
        {
            var actor = container.GetComponent<Actor>();
            if (actor == null) return;

            // Only players have equipment
            if (actor is not PlayerActor playerActor) return;

            var equipment = actor.GetComponent<PlayerEquipment>();
            if (equipment == null) return;

            // Iterate through all equipped items and collect their stat bonuses
            foreach (var kvp in equipment.Equipment)
            {
                var itemInstance = kvp.Value;
                if (itemInstance == null) continue;

                var itemDef = itemInstance.BaseItem;
                if (itemDef == null) continue;

                // Extract all stats from the item definition
                foreach (var itemStat in itemDef.Stats)
                {
                    if (itemStat?.Stat == null) continue;

                    // Apply the stat bonus as a flat modifier
                    if (container.TryGetStat(itemStat.Stat.DefinitionID, out var stat))
                    {
                        stat.SetFlatModifier(stat.FlatModifier + itemStat.Value);
                    }
                }
            }
        }

        /// <summary>Adds a flat and percentage modifier to an actor statistic.</summary>
        /// <param name="actor">The actor whose statistic changes.</param>
        /// <param name="statID">The statistic identifier.</param>
        /// <param name="flat">The flat modifier.</param>
        /// <param name="percent">The percentage modifier.</param>
        public void AddModifier(Actor actor, string statID, float flat, float percent)
        {
            var container = actor.GetComponent<ActorStatContainer>();
            if (container == null || !container.TryGetStat(statID, out var stat)) return;
            stat.SetFlatModifier(stat.FlatModifier + flat);
            stat.SetPercentModifier(stat.PercentModifier + percent);
            Recalculate(container);
            SendSnapshot(actor);
        }

        /// <summary>Removes a flat and percentage modifier from an actor statistic.</summary>
        /// <param name="actor">The actor whose statistic changes.</param>
        /// <param name="statID">The statistic identifier.</param>
        /// <param name="flat">The flat modifier.</param>
        /// <param name="percent">The percentage modifier.</param>
        public void RemoveModifier(Actor actor, string statID, float flat, float percent)
        {
            var container = actor.GetComponent<ActorStatContainer>();
            if (container == null || !container.TryGetStat(statID, out var stat)) return;
            stat.SetFlatModifier(stat.FlatModifier - flat);
            stat.SetPercentModifier(stat.PercentModifier - percent);
            Recalculate(container);
            SendSnapshot(actor);
        }

        /// <summary>Sets the current value of an actor statistic.</summary>
        /// <param name="actor">The actor whose statistic changes.</param>
        /// <param name="statID">The statistic identifier.</param>
        /// <param name="value">The new current value.</param>
        public void SetCurrent(Actor actor, string statID, float value)
        {
            var container = actor.GetComponent<ActorStatContainer>();
            if (container == null || !container.TryGetStat(statID, out var stat)) return;
            container.SetStat(statID, new StatInstance(
                stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                stat.EffectiveMaximum, value));
            SendSnapshot(actor, new[] { statID });
        }

        /// <summary>Resets an actor's Resource-category stats to their authored reset baseline.</summary>
        /// <param name="actor">The actor whose resource stats should be reset.</param>
        /// <returns><see langword="true"/> when the actor has a stat container and was reset.</returns>
        public bool ResetResources(Actor actor)
        {
            if (actor == null)
                return false;

            var container = actor.GetComponent<ActorStatContainer>();
            if (container == null)
                return false;

            FillResourcesToMax(container);
            SendSnapshot(actor);
            return true;
        }

        #endregion

        #region Regen

        private void TickRegen(float dt)
        {
            var library = ActorStatDefinitionLibrary.Instance;
            var allDefs = library.GetAllDefinitions();

            for (int a = _trackedActors.Count - 1; a >= 0; a--)
            {
                _changedStatIDs.Clear();
                var actor = _trackedActors[a];
                if (actor == null) { _trackedActors.RemoveAt(a); continue; }

                var container = actor.GetComponent<ActorStatContainer>();
                if (container == null) continue;

                for (int i = 0; i < allDefs.Count; i++)
                {
                    var def = allDefs[i];
                    if (def.Category != ActorStatCategory.Resource) continue;
                    if (!container.TryGetStat(def.DefinitionID, out var stat)) continue;
                    if (stat.CurrentValue >= stat.EffectiveMaximum) continue;

                    float regenRate = ResolveRegenRate(def, container);
                    if (regenRate == 0f) continue;

                    float newCurrent = Mathf.Clamp(
                        stat.CurrentValue + regenRate * dt,
                        0f, stat.EffectiveMaximum);

                    if (newCurrent != stat.CurrentValue)
                    {
                        container.SetStat(def.DefinitionID, new StatInstance(
                            stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                            stat.EffectiveMaximum, newCurrent));
                        _changedStatIDs.Add(def.DefinitionID);
                    }
                }

                if (_changedStatIDs.Count > 0)
                    SendSnapshot(actor, _changedStatIDs);

                _changedStatIDs.Clear();
            }
        }

        private float ResolveRegenRate(ActorStatDefinition def, ActorStatContainer container)
        {
            if (def.RegenMode == ActorStatBaseValueMode.Fixed)
                return def.RegenPerSecond;

            if (def.RegenSourceStat != null
                && container.TryGetStat(def.RegenSourceStat.DefinitionID, out var source))
                return source.EffectiveMaximum * def.RegenMultiplier;

            return 0f;
        }

        #endregion

        #region Helpers
        private int GetActorLevel(ActorStatContainer container)
        {
            // Try to resolve level from PlayerActor or NPCStatProfile. Default to 1 if not found.
            var actor = container.GetComponent<Actor>();
            if (actor == null) return 1;

            if (actor is PlayerActor player && player.CharacterData != null)
                return player.GetLevel();
            else if (actor is PlayerActor playerActor && playerActor.CharacterData == null)
                Debug.Warning($"CharacterData found on PlayerActor {actor.Name} but it was null. Defaulting level to 1.");

            var npcActor = actor.GetComponent<NPCActor>();
            if (npcActor != null)
                return npcActor.GetLevel();

            return 1;
        }

        /// <summary>Sends the actor's current stat snapshot to its client.</summary>
        /// <param name="actor">The actor whose stats are sent.</param>
        public void SendSnapshot(Actor actor)
        {
            var container = actor.GetComponent<ActorStatContainer>();
            if (container == null) return;

            // Broadcast observer stats (health, mana, etc.) to all observers except owner.
            var observerSnapshot = container.BuildSnapshot(ActorStatReplicationMode.Observers);
            if (observerSnapshot.Stats.Length > 0)
                container.PublishObserverSnapshot(observerSnapshot);

            // Send owner + observer stats to the owner via TargetRpc.
            if (actor.IsPlayer && actor.Owner.HasValue)
            {
                var ownerSnapshot = container.BuildSnapshot(ActorStatReplicationMode.Owner);
                if (observerSnapshot.Stats.Length > 0)
                    container.PublishOwnerSnapshot(actor.Owner.Value, observerSnapshot);
                if (ownerSnapshot.Stats.Length > 0)
                    container.PublishOwnerSnapshot(actor.Owner.Value, ownerSnapshot);
            }
        }

        private void SendSnapshot(Actor actor, IReadOnlyList<string> changedStatIDs)
        {
            var container = actor.GetComponent<ActorStatContainer>();
            if (container == null) return;

            var observerSnapshot = container.BuildSnapshot(
                ActorStatReplicationMode.Observers,
                changedStatIDs);
            if (observerSnapshot.Stats.Length > 0)
                container.PublishObserverSnapshot(observerSnapshot);

            if (actor.IsPlayer && actor.Owner.HasValue)
            {
                if (observerSnapshot.Stats.Length > 0)
                    container.PublishOwnerSnapshot(actor.Owner.Value, observerSnapshot);

                var ownerSnapshot = container.BuildSnapshot(
                    ActorStatReplicationMode.Owner,
                    changedStatIDs);
                if (ownerSnapshot.Stats.Length > 0)
                    container.PublishOwnerSnapshot(actor.Owner.Value, ownerSnapshot);
            }
        }

        private void FillAttributesToMax(ActorStatContainer container)
        {
            var allDefs = ActorStatDefinitionLibrary.Instance.GetAllDefinitions();
            for (int i = 0; i < allDefs.Count; i++)
            {
                var def = allDefs[i];
                if (def.Category != ActorStatCategory.Attribute) continue;
                if (!container.TryGetStat(def.DefinitionID, out var stat)) continue;
                container.SetStat(def.DefinitionID, new StatInstance(
                    stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                    stat.EffectiveMaximum, stat.EffectiveMaximum));
            }
        }

        private void FillResourcesToMax(ActorStatContainer container)
        {
            var allDefs = ActorStatDefinitionLibrary.Instance.GetAllDefinitions();
            for (int i = 0; i < allDefs.Count; i++)
            {
                var def = allDefs[i];
                bool startsAtZero = def.StartsAtZero;

                if (def.Category != ActorStatCategory.Resource) continue;
                if (!container.TryGetStat(def.DefinitionID, out var stat)) continue;

                if (startsAtZero)
                {
                    container.SetStat(def.DefinitionID, new StatInstance(
                    stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                    stat.EffectiveMaximum, 0));
                    continue;
                }
                else
                {
                    container.SetStat(def.DefinitionID, new StatInstance(
                    stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                    stat.EffectiveMaximum, stat.EffectiveMaximum));
                }

            }
        }

        private void RestoreSavedResources(ActorStatContainer container, CharacterData data)
        {
            var library = ActorStatDefinitionLibrary.Instance;
            HashSet<string> restoredStatIDs = new(StringComparer.Ordinal);
            for (int i = 0; i < data.SavedResources.Count; i++)
            {
                var saved = data.SavedResources[i];
                if (string.IsNullOrWhiteSpace(saved.StatID)
                    || !restoredStatIDs.Add(saved.StatID))
                {
                    throw new InvalidOperationException(
                        $"Character data contains a duplicate or blank resource ID '{saved.StatID}'.");
                }
                if (float.IsNaN(saved.Current) || float.IsInfinity(saved.Current) || saved.Current < 0f)
                {
                    throw new InvalidOperationException(
                        $"Character data contains invalid current value {saved.Current} for resource '{saved.StatID}'.");
                }

                var definition = library?.GetDefinition(saved.StatID);
                if (definition == null || definition.Category != ActorStatCategory.Resource)
                {
                    throw new InvalidOperationException(
                        $"Character data references unknown or non-resource definition '{saved.StatID}'.");
                }
                if (!container.TryGetStat(saved.StatID, out var stat))
                    throw new InvalidOperationException($"Resource stat '{saved.StatID}' was not initialized.");

                container.SetStat(saved.StatID, new StatInstance(
                    stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                    stat.EffectiveMaximum,
                    Mathf.Clamp(saved.Current, 0f, stat.EffectiveMaximum)));
            }
        }

        #endregion
    }
}
