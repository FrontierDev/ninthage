using System.Collections.Generic;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Data
{
    [System.Serializable]
    public class SpellTarget_GroundLocation : SpellTargetDefinition
    {
        public float Radius = 10f;
        public bool GetActorsInRadius = false;

        public SpellTarget_GroundLocation()
        {
            _requiresTarget = false;
        }

        public override SpellTargetDefinition Clone()
        {
            return new SpellTarget_GroundLocation
            {
                Radius = this.Radius,
                GetActorsInRadius = this.GetActorsInRadius,
            };
        }

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