using System.Collections.Generic;
using Game.Shared.Runtime.Spellcasting;
using UnityEngine.Scripting.APIUpdating;

using Game.Shared.Runtime.Actors;
namespace Game.Shared.Data.AuraTargetDefinitions
{
    [System.Serializable]
    [MovedFrom(true, "Game.Shared.Data", "Game.Shared")]
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
