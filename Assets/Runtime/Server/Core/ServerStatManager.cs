using System.Collections.Generic;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;
using Actor = Game.Shared.Actor;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Persistence;

namespace Game.Server
{
    public sealed class ServerStatManager : MonoBehaviour
    {
        private static ServerStatManager _instance;
        public static ServerStatManager Instance => _instance;
        private static bool _initialized;
        public static bool IsInitialized => _initialized;

        private const float RegenTickInterval = 1f;
        private float regenTimer;

        private readonly List<Actor> trackedActors = new();
        private readonly Dictionary<Actor, Dictionary<string, float>> currentEquipmentModifiers = new();

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("ServerStatManager is already initialized!");
                return;
            }
            _instance = this;

            // Subscribe to character service events for stat updates
            Game.Shared.Networking.CharacterService.onUpdateStatsRequest += (container) =>
            {
                Recalculate(container);
                FillAttributesToMax(container);
                SendSnapshot(container.GetComponent<Actor>());
            };

            _initialized = true;
        }

        private void OnDestroy()
        {
            trackedActors.Clear();
            _instance = null;
            _initialized = false;
        }

        private void Update()
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= RegenTickInterval)
            {
                TickRegen(regenTimer);
                regenTimer = 0f;
            }
        }

        public void RegisterActor(Actor actor)
        {
            trackedActors.Add(actor);
        }

        public void UnregisterActor(Actor actor)
        {
            trackedActors.Remove(actor);
        }

        #region Initialization

        public void InitializeNpcStats(Actor actor)
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
                container.SetStat(entry.definition.DefinitionId,
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
            var container = actor.GetComponent<ActorStatContainer>();
            if (container == null) return;

            var playerActor = actor as PlayerActor;
            if (playerActor == null || playerActor.CharacterData == null)
            {
                Debug.Error($"PlayerActor or CharacterData is null for actor {actor.Name}.");
                return;
            }

            var data = playerActor.CharacterData;
            var race = RaceDefinitionLibrary.Instance.GetDefinition(data.RaceID);
            var playerClass = ClassDefinitionLibrary.Instance.GetDefinition(data.ClassID);

            if (race == null || playerClass == null)
            {
                Debug.Error($"Race or Class definition not found for player {actor.Name} (Race: {data.RaceID}, Class: {data.ClassID}).");
                return;
            }

            // Initialize all stats from library with their definition base values
            var library = ActorStatDefinitionLibrary.Instance;
            var allDefs = library.GetAllDefinitions();
            for (int i = 0; i < allDefs.Count; i++)
            {
                var def = allDefs[i];
                float baseValue = def.BaseValueMode == ActorStatBaseValueMode.Fixed ? def.BaseValue : 0f;
                container.SetStat(def.DefinitionId,
                    new StatInstance(baseValue, 0, 0, 0, 0));
            }

            // Apply race base stats
            foreach (var entry in race.BaseStats)
            {
                if (entry?.definition == null) continue;
                if (container.TryGetStat(entry.definition.DefinitionId, out var existing))
                {
                    container.SetStat(entry.definition.DefinitionId,
                        new StatInstance(existing.BaseValue + entry.baseValue, 0, 0, 0, 0));
                }
            }

            // Apply class base stats
            foreach (var entry in playerClass.BaseStats)
            {
                if (entry?.definition == null) continue;
                if (container.TryGetStat(entry.definition.DefinitionId, out var existing))
                {
                    container.SetStat(entry.definition.DefinitionId,
                        new StatInstance(existing.BaseValue + entry.baseValue, 0, 0, 0, 0));
                }
            }

            // Apply class growth per level
            foreach (var growth in playerClass.StatGrowthPerLevel)
            {
                if (growth.stat == null) continue;
                if (container.TryGetStat(growth.stat.DefinitionId, out var existing))
                {
                    float levelBonus = growth.flatPerLevel * data.Level;
                    container.SetStat(growth.stat.DefinitionId,
                        new StatInstance(existing.BaseValue, levelBonus, 0, 0, 0));
                }
            }

            Recalculate(container);
            FillResourcesToMax(container);
            FillAttributesToMax(container);
            RestoreSavedResources(container, data);
        }

        #endregion

        #region Recalculation

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
                if (!container.TryGetStat(def.DefinitionId, out var stat)) continue;
                stat.RecalculateEffectiveMax();
            }

            // Pass 2: derived stats
            for (int i = 0; i < allDefs.Count; i++)
            {
                var def = allDefs[i];
                if (def.BaseValueMode != ActorStatBaseValueMode.Derived) continue;
                if (def.SourceStat == null) continue;
                if (!container.TryGetStat(def.DefinitionId, out var stat)) continue;
                if (!container.TryGetStat(def.SourceStat.DefinitionId, out var source)) continue;

                stat.SetBaseValue(source.EffectiveMaximum * def.Multiplier);
                stat.RecalculateEffectiveMax();
            }

            // Pass 3: rating conversions
            for (int i = 0; i < allDefs.Count; i++)
            {
                var def = allDefs[i];
                if (def.BaseValueMode != ActorStatBaseValueMode.Rating) continue;
                if (def.SourceStat == null) continue;
                if (!container.TryGetStat(def.DefinitionId, out var stat)) continue;
                if (!container.TryGetStat(def.SourceStat.DefinitionId, out var source)) continue;

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
            if (currentEquipmentModifiers.TryGetValue(actor, out var previous))
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
                            if (!newModifiers.ContainsKey(itemStat.Stat.DefinitionId))
                                newModifiers[itemStat.Stat.DefinitionId] = 0f;
                            newModifiers[itemStat.Stat.DefinitionId] += itemStat.Value;
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
            currentEquipmentModifiers[actor] = newModifiers;
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
                    if (container.TryGetStat(itemStat.Stat.DefinitionId, out var stat))
                    {
                        stat.SetFlatModifier(stat.FlatModifier + itemStat.Value);
                    }
                }
            }
        }

        public void AddModifier(Actor actor, string statId, float flat, float percent)
        {
            var container = actor.GetComponent<ActorStatContainer>();
            if (container == null || !container.TryGetStat(statId, out var stat)) return;
            stat.SetFlatModifier(stat.FlatModifier + flat);
            stat.SetPercentModifier(stat.PercentModifier + percent);
            Recalculate(container);
            SendSnapshot(actor);
        }

        public void RemoveModifier(Actor actor, string statId, float flat, float percent)
        {
            var container = actor.GetComponent<ActorStatContainer>();
            if (container == null || !container.TryGetStat(statId, out var stat)) return;
            stat.SetFlatModifier(stat.FlatModifier - flat);
            stat.SetPercentModifier(stat.PercentModifier - percent);
            Recalculate(container);
            SendSnapshot(actor);
        }

        public void SetCurrent(Actor actor, string statId, float value)
        {
            var container = actor.GetComponent<ActorStatContainer>();
            if (container == null || !container.TryGetStat(statId, out var stat)) return;
            container.SetStat(statId, new StatInstance(
                stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                stat.EffectiveMaximum, value));
            SendSnapshot(actor);
        }

        #endregion

        #region Regen

        private void TickRegen(float dt)
        {
            var library = ActorStatDefinitionLibrary.Instance;
            var allDefs = library.GetAllDefinitions();

            for (int a = trackedActors.Count - 1; a >= 0; a--)
            {
                var actor = trackedActors[a];
                if (actor == null) { trackedActors.RemoveAt(a); continue; }

                var container = actor.GetComponent<ActorStatContainer>();
                if (container == null) continue;

                bool dirty = false;
                for (int i = 0; i < allDefs.Count; i++)
                {
                    var def = allDefs[i];
                    if (def.Category != ActorStatCategory.Resource) continue;
                    if (!container.TryGetStat(def.DefinitionId, out var stat)) continue;
                    if (stat.CurrentValue >= stat.EffectiveMaximum) continue;

                    float regenRate = ResolveRegenRate(def, container);
                    if (regenRate == 0f) continue;

                    float newCurrent = Mathf.Clamp(
                        stat.CurrentValue + regenRate * dt,
                        0f, stat.EffectiveMaximum);

                    if (newCurrent != stat.CurrentValue)
                    {
                        container.SetStat(def.DefinitionId, new StatInstance(
                            stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                            stat.EffectiveMaximum, newCurrent));
                        dirty = true;
                    }
                }

                if (dirty)
                    SendSnapshot(actor);
            }
        }

        private float ResolveRegenRate(ActorStatDefinition def, ActorStatContainer container)
        {
            if (def.RegenMode == ActorStatBaseValueMode.Fixed)
                return def.RegenPerSecond;

            if (def.RegenSourceStat != null
                && container.TryGetStat(def.RegenSourceStat.DefinitionId, out var source))
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

        public void SendSnapshot(Actor actor)
        {
            var container = actor.GetComponent<ActorStatContainer>();
            if (container == null) return;

            // Broadcast observer stats (health, mana, etc.) to all observers except owner.
            var observerSnapshot = container.BuildSnapshot(ActorStatReplicationMode.Observers);
            if (observerSnapshot.Stats.Length > 0)
                container.Observers_ReceiveSnapshot(observerSnapshot);

            // Send owner + observer stats to the owner via TargetRpc.
            if (actor.IsPlayer && actor.Owner.HasValue)
            {
                var ownerSnapshot = container.BuildSnapshot(ActorStatReplicationMode.Owner);
                if (observerSnapshot.Stats.Length > 0)
                    container.Client_ReceiveSnapshot(actor.Owner.Value, observerSnapshot);
                if (ownerSnapshot.Stats.Length > 0)
                    container.Client_ReceiveSnapshot(actor.Owner.Value, ownerSnapshot);
            }
        }

        private void FillAttributesToMax(ActorStatContainer container)
        {
            var allDefs = ActorStatDefinitionLibrary.Instance.GetAllDefinitions();
            for (int i = 0; i < allDefs.Count; i++)
            {
                var def = allDefs[i];
                if (def.Category != ActorStatCategory.Attribute) continue;
                if (!container.TryGetStat(def.DefinitionId, out var stat)) continue;
                container.SetStat(def.DefinitionId, new StatInstance(
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
                if (!container.TryGetStat(def.DefinitionId, out var stat)) continue;

                if (startsAtZero)
                {
                    container.SetStat(def.DefinitionId, new StatInstance(
                    stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                    stat.EffectiveMaximum, 0));
                    continue;
                }
                else
                {
                    container.SetStat(def.DefinitionId, new StatInstance(
                    stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                    stat.EffectiveMaximum, stat.EffectiveMaximum));
                }

            }
        }

        private void RestoreSavedResources(ActorStatContainer container, CharacterData data)
        {
            for (int i = 0; i < data.SavedResources.Count; i++)
            {
                var saved = data.SavedResources[i];
                if (!container.TryGetStat(saved.StatId, out var stat)) continue;
                container.SetStat(saved.StatId, new StatInstance(
                    stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                    stat.EffectiveMaximum, saved.Current));
            }
        }

        #endregion
    }
}