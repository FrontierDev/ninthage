using System.Collections.Generic;

namespace Game.Shared.Data
{
    [System.Serializable]
    public class AuraTarget_Target : AuraTargetDefinition
    {
        public override List<Actor> Evaluate(AuraContext ctx)
        {
            if (ctx.Target != null)
                return new List<Actor> { ctx.Target };
            return new List<Actor>();
        }
    }
}