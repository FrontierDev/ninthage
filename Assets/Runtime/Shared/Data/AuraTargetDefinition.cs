using System.Collections.Generic;
using Game.Shared.Runtime.Spellcasting;

using Game.Shared.Runtime.Actors;
namespace Game.Shared.Data
{
    /// <summary>Defines how an aura resolves its affected actors.</summary>
    public abstract class AuraTargetDefinition
    {
        /// <summary>Evaluates the target actors for an aura context.</summary>
        public abstract List<Actor> Evaluate(AuraContext ctx);
    }
}
