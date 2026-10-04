using System.Collections.Generic;

namespace Game.Shared.Data
{
    public abstract class AuraTargetDefinition
    {
        public abstract List<Actor> Evaluate(AuraContext ctx);
    }
}