using System.Collections.Generic;

namespace Game.Shared.Data
{
    public abstract class SpellTargetDefinition
    {
        internal bool _requiresTarget;
        public bool RequiresTarget => _requiresTarget;

        public abstract SpellTargetDefinition Clone();
        public abstract List<Actor> Evaluate(SpellContext ctx);
    }
}