using System.Collections.Generic;

namespace Game.Shared.Data
{
    [System.Serializable]
    /// <summary>Resolves the aura caster as its target.</summary>
    public class AuraTarget_Caster : AuraTargetDefinition
    {
        /// <summary>Evaluates the caster target.</summary>
        public override List<Actor> Evaluate(AuraContext ctx)
        {
            if (ctx.Caster != null)
                return new List<Actor> { ctx.Caster };
            return new List<Actor>();
        }
    }
}
