using System.Collections.Generic;
using Game.Shared.Data;

using Game.Shared.Runtime.Actors;
namespace Game.Shared.Runtime.Spellcasting
{
    /// <summary>
    /// Tracks an individual stack within a Condition aura, including which caster applied it.
    /// For Condition behavior, each stack has its own remaining duration.
    /// For Condition_Extend, Duration is shared across all stacks (refreshed on new application).
    /// </summary>
    public sealed class ConditionStack
    {
        /// <summary>Gets or sets the caster that applied the stack.</summary>
        public Actor Caster;
        /// <summary>Gets or sets the remaining stack duration.</summary>
        public float Duration; // Only used by Condition (independent durations). Ignored by Condition_Extend.

        /// <summary>Initializes a condition stack.</summary>
        public ConditionStack(Actor caster, float duration)
        {
            Caster = caster;
            Duration = duration;
        }
    }

    /// <summary>Contains mutable state for an aura execution.</summary>
    public sealed class AuraContext
    {
        /// <summary>Gets or sets the current aura phase.</summary>
        public AuraPhase Phase = AuraPhase.OnApply;

        /// <summary>Gets or sets the original aura definition.</summary>
        public AuraDefinition Definition;
        /// <summary>Gets or sets the actor that applied the aura.</summary>
        public Actor Caster;
        /// <summary>Gets or sets the actor affected by the aura.</summary>
        public Actor Target;

        // Mutable copies from AuraDefinition
        /// <summary>Gets or sets the current duration.</summary>
        public float Duration;
        /// <summary>Gets or sets the current tick interval.</summary>
        public float TickInterval;
        /// <summary>Gets or sets the current stack count.</summary>
        public int Stacks;

        /// <summary>
        /// Per-caster stack tracking for Condition and Condition_Extend behaviors.
        /// Null for non-condition auras.
        /// </summary>
        public List<ConditionStack> ConditionStacks;

        /// <summary>Initializes an empty aura context.</summary>
        public AuraContext() { }
        /// <summary>Initializes an aura context from its definition and actors.</summary>
        public AuraContext(AuraDefinition definition, Actor caster, Actor target)
        {
            Definition = definition;
            Caster = caster;
            Target = target;
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
