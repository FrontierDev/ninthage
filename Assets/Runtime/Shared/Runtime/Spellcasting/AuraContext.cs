using System.Collections.Generic;
using Game.Shared.Data;

namespace Game.Shared
{
    /// <summary>
    /// Tracks an individual stack within a Condition aura, including which caster applied it.
    /// For Condition behavior, each stack has its own remaining duration.
    /// For Condition_Extend, Duration is shared across all stacks (refreshed on new application).
    /// </summary>
    public sealed class ConditionStack
    {
        public Actor Caster;
        public float Duration; // Only used by Condition (independent durations). Ignored by Condition_Extend.

        public ConditionStack(Actor caster, float duration)
        {
            Caster = caster;
            Duration = duration;
        }
    }

    public sealed class AuraContext
    {
        public AuraPhase Phase = AuraPhase.OnApply;

        public AuraDefinition Definition;    // original asset reference
        public Actor Caster;                 // who applied the aura (or the first applicator for conditions)
        public Actor Target;                 // who the aura is applied to (one target per instance)

        // Mutable copies from AuraDefinition
        public float Duration;
        public float TickInterval;
        public int Stacks;

        /// <summary>
        /// Per-caster stack tracking for Condition and Condition_Extend behaviors.
        /// Null for non-condition auras.
        /// </summary>
        public List<ConditionStack> ConditionStacks;

        public AuraContext() { }
        public AuraContext(AuraDefinition definition, Actor caster, Actor target)
        {
            Definition = definition;
            Caster = caster;
            Target = target;
            Duration = definition.BaseDuration;
            TickInterval = definition.BaseTickInterval;
            Stacks = 1;
        }

        // Per-component overrides (keyed by AuraComponent.Guid)
        public Dictionary<string, SpellComponentOverrides> ComponentOverrides = new();

        public SpellComponentOverrides GetOrCreateOverrides(string componentGuid)
        {
            if (!ComponentOverrides.TryGetValue(componentGuid, out var overrides))
            {
                overrides = new SpellComponentOverrides();
                ComponentOverrides[componentGuid] = overrides;
            }
            return overrides;
        }
    }
}