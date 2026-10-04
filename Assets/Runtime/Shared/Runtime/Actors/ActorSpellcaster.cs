using System.Collections.Generic;
using System.Linq;
using Game.Shared.Data;
using Game.Shared.Networking;
using PurrNet;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared
{
    public sealed class SpellOverride
    {
        public SpellDefinition BaseDefinition;

        public float Range;
        public float Cooldown;
        public float CastTime;
        public float TotalTicks;
        public Vector3 PositionTarget;

        public List<SpellComponent> Components = new();

        public SpellOverride(SpellDefinition baseDefinition)
        {
            BaseDefinition = baseDefinition;
            Range = baseDefinition.BaseRange;
            Cooldown = baseDefinition.BaseCooldown;
            CastTime = baseDefinition.BaseCastTime;
            TotalTicks = baseDefinition.BaseTotalTicks;

            // Create initial components from the base definition.
            Components.Clear();
            foreach (var compDef in baseDefinition.BaseComponents)
            {
                var comp = new SpellComponent
                {
                    CastPhase = compDef.CastPhase,
                    TargetDefinition = compDef.TargetDefinition?.Clone(), // Clone the target definition to allow for modifications by modifiers without affecting the base definition.
                    EffectDefinition = compDef.EffectDefinition?.Clone()
                };
                Components.Add(comp);
            }
        }
    }

    [RequireComponent(typeof(Actor))]
    public sealed class ActorSpellcaster : NetworkBehaviour
    {
        /// <summary>
        /// The base spell IDs known by the actor.
        /// </summary>
        [SerializeField] private List<string> knownSpellIDs = new();
        public IReadOnlyList<string> KnownSpellIDs => knownSpellIDs;

        [SerializeField] private Dictionary<string, SpellOverride> spells = new();
        public IReadOnlyDictionary<string, SpellOverride> Spells => spells;

        private readonly Dictionary<string, List<ISpellModifier>> modifiersBySource = new();
        public IReadOnlyList<ISpellModifier> SpellModifiers => modifiersBySource.Values.SelectMany(l => l).ToList();

        private bool isCasting;
        public bool IsCasting => isCasting;

        private void Awake()
        {
            // Update spell overrides whenever equipment, talents or stats change.
            CharacterService.onClientEquipmentUpdated += RecalculateOverrides;
            CharacterService.onClientTalentsUpdated += RecalculateOverrides;
        }

        #region Spell Management
        public void LearnSpell(string spellID)
        {
            var spellDef = SpellDefinitionLibrary.Instance.GetDefinition(spellID);
            if (spellDef == null)
            {
                Debug.Warning($"Attempted to learn unknown spell ID '{spellID}'.");
                return;
            }

            if (!knownSpellIDs.Contains(spellID))
                knownSpellIDs.Add(spellID);

            if (!spells.ContainsKey(spellID))
                spells.Add(spellID, new SpellOverride(spellDef));
        }

        public void LoadKnownSpells(List<string> spellIDs)
        {
            knownSpellIDs.AddRange(spellIDs);
            foreach (var id in spellIDs)
            {
                var spellDef = SpellDefinitionLibrary.Instance.GetDefinition(id);
                if (spellDef == null)
                {
                    Debug.Warning($"Attempted to load unknown spell ID '{id}'.");
                    continue;
                }

                RegisterSpellOverride(id, spellDef);
            }

            // Load modifiers.
            AddModifier("WeaponSwingTimerCooldown", new WeaponSwingTimerCooldown());
            AddModifier("CooldownHasteScaling", new CooldownHasteScaling());
            AddModifier("CastTimeHasteScaling", new CastTimeHasteScaling());
        }

        private void RegisterSpellOverride(string spellID, SpellDefinition spellDef)
        {
            // Create the initial override.
            spells.Add(spellID, new SpellOverride(spellDef));
        }
        #endregion

        #region Spell Modifiers
        public void AddModifier(string source, ISpellModifier modifier, int rank = 1)
        {
            if (string.IsNullOrEmpty(source) || modifier == null) return;

            // Clone incoming modifier so we don't mutate shared assets
            var clone = CloneModifier(modifier, source);

            if (!modifiersBySource.TryGetValue(source, out var list))
            {
                list = new List<ISpellModifier>();
                modifiersBySource[source] = list;
            }

            // If the modifier has ranks (e.g., for talents), then we should multiply the effects
            // by rank before adding it.
            if (clone is IScalableModifier scalable)
            {
                (clone as IScalableModifier).Scale(rank);
            }

            // If a modifier of the same concrete type already exists for this source, replace it.
            var existing = list.FirstOrDefault(m => m.GetType() == clone.GetType());
            if (existing != null)
            {
                var idx = list.IndexOf(existing);
                list[idx] = clone;
            }
            else
            {
                list.Add(clone);
            }

            RecalculateOverrides();
        }

        public void RemoveModifier(string source, ISpellModifier modifier)
        {
            if (string.IsNullOrEmpty(source) || modifier == null) return;

            if (!modifiersBySource.TryGetValue(source, out var list)) return;

            // Remove by concrete type so callers can pass asset instances
            list.RemoveAll(m => m.GetType() == modifier.GetType());
            if (list.Count == 0)
                modifiersBySource.Remove(source);

            RecalculateOverrides();
        }

        private static ISpellModifier CloneModifier(ISpellModifier modifier, string source)
        {
            if (modifier == null) return null;
            try
            {
                var json = JsonUtility.ToJson(modifier);
                var clone = (ISpellModifier)JsonUtility.FromJson(json, modifier.GetType());
                // Try to set internal backing field/property named 'source' so modifier.Source reflects origin
                try
                {
                    var t = clone.GetType();
                    var field = t.GetField("source");
                    if (field != null && field.FieldType == typeof(string)) field.SetValue(clone, source);
                    else
                    {
                        var prop = t.GetProperty("Source");
                        if (prop != null && prop.CanWrite && prop.PropertyType == typeof(string)) prop.SetValue(clone, source);
                    }
                }
                catch { }

                return clone;
            }
            catch
            {
                // Fallback: if JsonUtility fails, try shallow copy via Activator (may lose field values)
                try { return (ISpellModifier)System.Activator.CreateInstance(modifier.GetType()); } catch { return modifier; }
            }
        }
        public void RecalculateOverrides()
        {
            var actor = GetComponent<Actor>();
            var sorted = modifiersBySource.Values.SelectMany(l => l).OrderBy(m => m.Priority);

            foreach (var spell in spells.Values)
            {
                // Reset to base
                spell.Range = spell.BaseDefinition.BaseRange;
                spell.Cooldown = spell.BaseDefinition.BaseCooldown;
                spell.CastTime = spell.BaseDefinition.BaseCastTime;
                spell.TotalTicks = spell.BaseDefinition.BaseTotalTicks;

                spell.Components.Clear();
                foreach (var compDef in spell.BaseDefinition.BaseComponents)
                {
                    var comp = new SpellComponent
                    {
                        CastPhase = compDef.CastPhase,
                        TargetDefinition = compDef.TargetDefinition?.Clone(),
                        EffectDefinition = compDef.EffectDefinition?.Clone()
                    };
                    spell.Components.Add(comp);
                }

                // Apply modifiers
                foreach (var mod in sorted)
                {
                    if (mod.AppliesTo(spell))
                        mod.Apply(spell, actor);

                }
            }
        }
        #endregion

        #region Spell Casting
        [ServerRpc(requireOwnership: true)]
        public void Server_RequestSpellCast(string spellID, RPCInfo rpcInfo = default)
        {
            if (!CanCastSpell(spellID, out var spellDef)) return;

            Debug.Log($"Actor {name} is casting spell '{spellID}'");
            ActorService.onServerSpellCastStarted?.Invoke(GetComponent<Actor>(), spellDef);
        }

        [ServerRpc(requireOwnership: true)]
        public void Server_RequestGroundSpellCast(string spellID, Vector3 groundPosition, RPCInfo rpcInfo = default)
        {
            if (!CanCastSpell(spellID, out var spellDef)) return;

            Debug.Log($"Actor {name} is casting ground spell '{spellID}' at {groundPosition}");
            spellDef.PositionTarget = groundPosition;
            ActorService.onServerSpellCastStarted?.Invoke(GetComponent<Actor>(), spellDef);
            spellDef.PositionTarget = default;
        }

        /// <summary>
        /// Server-authoritative spell cast for NPCs and server-driven systems (no ownership required).
        /// </summary>
        public void ServerOnly_RequestSpellCast(string spellID)
        {
            if (!CanCastSpell(spellID, out var spellDef)) return;

            Debug.Log($"Actor {name} is casting spell '{spellID}'");
            ActorService.onServerSpellCastStarted?.Invoke(GetComponent<Actor>(), spellDef);
        }

        [ServerRpc(requireOwnership: true)]
        public void Server_CancelSpellCast(RPCInfo rpcInfo = default)
        {
            var actor = GetComponent<Actor>();
            ActorService.onServerRequestSpellCastCancel?.Invoke(actor);
        }

        [ServerOnly]
        private bool CanCastSpell(string spellID, out SpellOverride spell)
        {
            spell = null;

            // ❓ Does the caster know this spell?
            if (!knownSpellIDs.Contains(spellID))
            {
                Debug.Warning($"Actor {name} attempted to cast a spell that they do not know '{spellID}'.");
                return false;
            }

            // ❓ Is the spell in the override cache?
            if (!spells.TryGetValue(spellID, out var spellOverride))
            {
                Debug.Warning($"Spell '{spellID}' is known but not in override cache for actor {name}.");
                return false;
            }

            // Get the base definition for validation checks
            var spellDef = spellOverride.BaseDefinition;
            if (spellDef == null)
            {
                Debug.Warning($"Spell '{spellID}' has no BaseDefinition for actor {name}.");
                return false;
            }

            // Check the spell definition to see if any of its components require a target, 
            // and if so, ensure we have one. If it requires a target, check that the target
            // is in range.
            if (spellDef.BaseComponents.Any(c => c.TargetDefinition.RequiresTarget))
            {
                // ❓ Target check
                if (GetComponent<Actor>().Target == null)
                {
                    Debug.Warning($"Actor {name} attempted to cast '{spellID}' which requires a target, but no target was selected.");
                    return false;
                }

                // ❓ Range check
                // Strict range checking on start. 
                float maxRange = spellDef.BaseRange;
                var target = GetComponent<Actor>().Target;
                float distance = GetComponent<Actor>().DistanceTo(target);
                if (distance > maxRange)
                {
                    Debug.Warning($"Actor {name} attempted to cast '{spellDef.DefinitionId}' on target {target.name} which is out of range. Distance: {distance}, Max Range: {maxRange}");
                    return false;
                }

            }

            // ❓ Resource check
            if (spellDef.BaseResourceCosts.Count > 0)
            {
                var statContainer = GetComponent<ActorStatContainer>();
                foreach (var cost in spellDef.BaseResourceCosts)
                {
                    if (!statContainer.TryGetStat(cost.ResourceType.DefinitionId, out var stat))
                    {
                        Debug.Warning($"Actor {name} attempted to cast '{spellID}' which costs {cost.ResourceType.DefinitionId}, but they do not have that stat.");
                        return false;
                    }

                    if (stat.CurrentValue < cost.Amount)
                    {
                        Debug.Warning($"Actor {name} attempted to cast '{spellID}' which costs {cost.Amount} {cost.ResourceType.DefinitionId}, but they only have {stat.CurrentValue}.");
                        return false;
                    }
                }
            }

            spell = spellOverride;
            return true;
        }

        /// <summary>
        /// This is called on clients to trigger the visual/audio effects of a spell cast. 
        /// It is invoked by the server on all clients when a spell cast starts, and can be used to update UI elements, play animations, spawn particle effects, etc. The finalCastTime parameter indicates when the spell will actually go off, allowing clients to synchronize their effects with the server's timing. Note that this is purely for client-side visuals and does not contain any game logic or validation - all game logic should be handled on the server in Server_RequestSpellCast and related methods.
        /// </summary>
        /// <param name="spellID"></param>
        /// <param name="finalCastTime"></param>
        /// <param name="rpcInfo"></param>
        [ObserversRpc]
        public void Observer_StartSpellCast(string spellID, float finalCastTime, RPCInfo rpcInfo = default)
        {
            isCasting = true;
            ActorService.onSpellCastStarted?.Invoke(GetComponent<Actor>(), spellID, finalCastTime);
        }



        [ObserversRpc]
        public void Observer_TickSpellCast(string spellID)
        {
            // TO DO...
        }

        [ObserversRpc]
        public void Observer_InterruptSpellCast(string spellID)
        {
            isCasting = false;
            ActorService.onSpellCastInterrupted?.Invoke(GetComponent<Actor>());
        }

        [ObserversRpc]
        public void Observer_FinishSpellCast(string spellID)
        {
            isCasting = false;
            ActorService.onSpellCastCompleted?.Invoke(GetComponent<Actor>());
        }

        [ObserversRpc]
        public void Observers_ReceiveCombatLog(Actor target, int damageAmount, List<DamageSchoolDefinition> damageTypes)
        {
            // This method is called on the caster's client to show a damage popup when a spell hits a target. 
            // The damage amount and types are passed in so that the client can display the appropriate visuals (e.g. different colors or icons for different damage types). 
            // The actual implementation of the popup is up to the client - this could involve instantiating a prefab, updating a UI element, etc.
        }

        [TargetRpc]
        public void Client_StartGlobalCooldown(PlayerID playerId, float duration, RPCInfo info = default)
        {
            ActorService.onStartGlobalCooldown?.Invoke();
        }

        [TargetRpc]
        public void Client_StartSpellCooldown(PlayerID playerId, string spellID, float duration, RPCInfo info = default)
        {
            ActorService.onStartCooldown?.Invoke(spellID, duration);
        }
        #endregion
    }
}