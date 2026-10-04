using System.Collections.Generic;

namespace Game.Shared.Data
{
    [System.Serializable]
    public class AuraTarget_Caster : AuraTargetDefinition
    {
        public override List<Actor> Evaluate(AuraContext ctx)
        {
            if (ctx.Caster != null)
                return new List<Actor> { ctx.Caster };
            return new List<Actor>();
        }
    }
}