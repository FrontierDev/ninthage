using System.Collections.Generic;

namespace Game.Shared.Data
{
    [System.Serializable]
    /// <summary>Resolves the aura host target.</summary>
    public class AuraTarget_Target : AuraTargetDefinition
    {
        /// <summary>Evaluates the host target.</summary>
        public override List<Actor> Evaluate(AuraContext ctx)
        {
            if (ctx.Target != null)
                return new List<Actor> { ctx.Target };
            return new List<Actor>();
        }
    }
}
