using System.Collections.Generic;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Data
{
    [System.Serializable]
    public class SpellTarget_Cone : SpellTargetDefinition
    {
        public float Range = 10f;
        public float Angle = 30f;

        public SpellTarget_Cone()
        {
            _requiresTarget = false;
        }

        public override SpellTargetDefinition Clone()
        {
            return new SpellTarget_Cone
            {
                Range = this.Range,
                Angle = this.Angle
            };
        }

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