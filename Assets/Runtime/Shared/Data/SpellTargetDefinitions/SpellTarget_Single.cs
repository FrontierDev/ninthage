using System.Collections.Generic;
using Game.Shared.Runtime.Spellcasting;
using UnityEngine.Scripting.APIUpdating;

using Game.Shared.Runtime.Actors;
namespace Game.Shared.Data.SpellTargetDefinitions
{
    [System.Serializable]
    [MovedFrom(true, "Game.Shared.Data", "Game.Shared")]
    /// <summary>Targets the actor selected by the spell context.</summary>
    public class SpellTarget_Single : SpellTargetDefinition
    {
        /// <summary>Initializes a single-target definition.</summary>
        public SpellTarget_Single()
        {
            _requiresTarget = true;
        }

        /// <summary>Creates a copy of this target definition.</summary>
        public override SpellTargetDefinition Clone()
        {
            return new SpellTarget_Single();
        }

        /// <summary>Evaluates the selected actor.</summary>
        public override List<Actor> Evaluate(SpellContext ctx)
        {
            return ctx.ActorTarget != null ? new List<Actor> { ctx.ActorTarget } : new List<Actor>();
        }
    }
}
