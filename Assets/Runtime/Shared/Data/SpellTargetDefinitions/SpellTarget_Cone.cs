using Debug = Game.Shared.FormattedDebug;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Data
{
    [System.Serializable]
    /// <summary>Targets actors within a cone in front of the caster.</summary>
    public class SpellTarget_Cone : SpellTargetDefinition
    {
        /// <summary>Gets or sets the cone range.</summary>
        public float Range = 10f;
        /// <summary>Gets or sets the cone angle.</summary>
        public float Angle = 30f;

        /// <summary>Initializes a cone target definition.</summary>
        public SpellTarget_Cone()
        {
            _requiresTarget = false;
        }

        /// <summary>Creates a copy of this target definition.</summary>
        public override SpellTargetDefinition Clone()
        {
            return new SpellTarget_Cone
            {
                Range = this.Range,
                Angle = this.Angle
            };
        }

        /// <summary>Evaluates actors inside the cone.</summary>
        public override List<Actor> Evaluate(SpellContext ctx)
        {
            var results = new List<Actor>();
            var caster = ctx.Caster;
            var origin = caster.transform.position;
            var forward = caster.transform.forward;
            float halfAngle = Angle * 0.5f;

            var colliders = Physics.OverlapSphere(origin, Range);
            foreach (var col in colliders)
            {
                var actor = col.GetComponentInParent<Actor>();
                if (actor == null || actor == caster) continue;

                var dirToTarget = (actor.transform.position - origin).normalized;
                float angle = Vector3.Angle(forward, dirToTarget);

                if (angle <= halfAngle)
                    results.Add(actor);
            }

            return results;
        }
    }
}
