using Debug = Game.Shared.Utility.FormattedDebug;
using System.Collections.Generic;
using Game.Shared.Runtime.Spellcasting;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

using Game.Shared.Runtime.Actors;
namespace Game.Shared.Data.SpellTargetDefinitions
{
    [System.Serializable]
    [MovedFrom(true, "Game.Shared.Data", "Game.Shared")]
    /// <summary>Targets a ground location and optionally actors around it.</summary>
    public class SpellTarget_GroundLocation : SpellTargetDefinition
    {
        /// <summary>Gets or sets the query radius.</summary>
        public float Radius = 10f;
        /// <summary>Gets or sets whether actors in the radius are returned.</summary>
        public bool GetActorsInRadius = false;

        /// <summary>Initializes a ground-location target definition.</summary>
        public SpellTarget_GroundLocation()
        {
            _requiresTarget = false;
        }

        /// <summary>Creates a copy of this target definition.</summary>
        public override SpellTargetDefinition Clone()
        {
            return new SpellTarget_GroundLocation
            {
                Radius = this.Radius,
                GetActorsInRadius = this.GetActorsInRadius,
            };
        }

        /// <summary>Evaluates the ground location and optional nearby actors.</summary>
        public override List<Actor> Evaluate(SpellContext ctx)
        {
            if (!GetActorsInRadius)
            {
                // No radius query — return the caster as a sentinel so Execute() is
                // still invoked once (required for effects like PlaceRift that don't
                // care about a target but need to run their server-side logic).
                return new List<Actor> { ctx.Caster };
            }

            var results = new List<Actor>();
            var colliders = Physics.OverlapSphere(ctx.PositionTarget, Radius);
            foreach (var col in colliders)
            {
                var actor = col.GetComponentInParent<Actor>();
                if (actor == null || actor == ctx.Caster) continue;
                results.Add(actor);
            }
            return results;
        }
    }
}
