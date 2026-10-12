using System.Collections.Generic;
using Game.Shared.Runtime.Spellcasting;
using UnityEngine.Scripting.APIUpdating;

using Game.Shared.Runtime.Actors;
namespace Game.Shared.Data.AuraTargetDefinitions
{
    [System.Serializable]
    [MovedFrom(true, "Game.Shared.Data", "Game.Shared")]
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
