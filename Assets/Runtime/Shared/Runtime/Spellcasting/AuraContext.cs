using Game.Shared.Data;
using Game.Shared.Runtime.Actors;
using System;
using System.Collections.Generic;

namespace Game.Shared.Runtime.Spellcasting
{
    /// <summary>Contains mutable state for an aura execution.</summary>
    public sealed class AuraContext
    {
        /// <summary>Gets or sets the current aura phase.</summary>
        public AuraPhase Phase = AuraPhase.OnApply;

        /// <summary>Gets or sets the original aura definition.</summary>
        public AuraDefinition Definition;
        /// <summary>Gets or sets the actor that applied the aura.</summary>
        public Actor Caster;
        /// <summary>Gets the stable identifier of the actor that applied the aura.</summary>
        public Guid CasterID;
        /// <summary>Gets or sets the actor affected by the aura.</summary>
        public Actor Target;
        /// <summary>Gets the stable identifier of the actor affected by the aura.</summary>
        public Guid TargetID;
        /// <summary>Gets the stable persistence identifier of this aura instance.</summary>
        public Guid PersistentAuraID { get; private set; }

        // Mutable copies from AuraDefinition
        /// <summary>Gets or sets the current duration.</summary>
        public float Duration;
        /// <summary>Gets or sets the current tick interval.</summary>
        public float TickInterval;
        /// <summary>Gets or sets the current stack count.</summary>
        public int Stacks;

        /// <summary>
        /// Absolute expiry ticks for independent-duration stacks. This list is null for
        /// refresh-duration auras and is managed only by the authoritative server aura owner.
        /// </summary>
        [NonSerialized]
        public List<long> StackExpiryTickIDs;

        /// <summary>Initializes an empty aura context.</summary>
        public AuraContext() { }
        /// <summary>Initializes an aura context from its definition and actors.</summary>
        public AuraContext(
            AuraDefinition definition,
            Actor caster,
            Actor target,
            Guid persistentAuraID = default)
        {
            Definition = definition;
            Caster = caster;
            CasterID = caster != null ? caster.ID : Guid.Empty;
            Target = target;
            TargetID = target != null ? target.ID : Guid.Empty;
            PersistentAuraID = persistentAuraID;
            Duration = definition.BaseDuration;
            TickInterval = definition.BaseTickInterval;
            Stacks = 1;
        }

        // Per-component overrides (keyed by AuraComponent.GUID)
        /// <summary>Gets component-specific runtime overrides.</summary>
        public Dictionary<string, SpellComponentOverrides> ComponentOverrides = new();

        /// <summary>Gets or creates overrides for a component.</summary>
        public SpellComponentOverrides GetOrCreateOverrides(string componentGUID)
        {
            if (!ComponentOverrides.TryGetValue(componentGUID, out var overrides))
            {
                overrides = new SpellComponentOverrides();
                ComponentOverrides[componentGUID] = overrides;
            }
            return overrides;
        }
    }
}
