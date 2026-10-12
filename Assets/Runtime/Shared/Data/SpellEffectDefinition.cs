using System.Collections.Generic;
using Game.Shared.Runtime.Spellcasting;

using Game.Shared.Runtime.Actors;
namespace Game.Shared.Data
{
    /// <summary>Identifies an actor event that can trigger a spell effect.</summary>
    public enum SpellActorEvent
    {
        onAutoAttackHit,
        onAutoAttackTaken,
    }

    /// <summary>Identifies whether an actor event targets the caster or target.</summary>
    public enum SpellActorEventTarget
    {
        Caster,
        Target
    }

    /// <summary>Base definition for an effect executed by a spell.</summary>
    public abstract class SpellEffectDefinition
    {
        /// <summary>Gets the events on which the caster can trigger this effect.</summary>
        public abstract List<SpellActorEvent> CasterEvents { get; }
        /// <summary>Gets the events on which the target can trigger this effect.</summary>
        public abstract List<SpellActorEvent> TargetEvents { get; }
        /// <summary>Creates a copy of this effect definition.</summary>
        public abstract SpellEffectDefinition Clone();
        /// <summary>Executes the effect for the supplied spell context and target.</summary>
        public abstract void Execute(SpellContext ctx, Actor target);
    }
}
