using System.Collections.Generic;

namespace Game.Shared.Data
{
    public enum SpellActorEvent
    {
        onAutoAttackHit,
        onAutoAttackTaken,
    }

    public enum SpellActorEventTarget
    {
        Caster,
        Target
    }

    public abstract class SpellEffectDefinition
    {
        public abstract List<SpellActorEvent> CasterEvents { get; }
        public abstract List<SpellActorEvent> TargetEvents { get; }
        public abstract SpellEffectDefinition Clone();
        public abstract void Execute(SpellContext ctx, Actor target);
    }
}