using System.Collections.Generic;
using System.Linq;
using Game.Shared.Data;
using UnityEngine;

namespace Game.Shared
{
    public sealed class SpellContext
    {
        public SpellCastPhase Phase = SpellCastPhase.OnCastStart;  // Current phase of the spell cast (Start, Channeling, End)

        public SpellOverride Spell;
        public Actor Caster;
        public Actor ActorTarget;
        public Vector3 PositionTarget;
        public List<Actor> Targets = new();

        public SpellContext(SpellOverride spell, Actor caster, Actor actorTarget = null)
        {
            Spell = spell;
            Caster = caster;
            ActorTarget = actorTarget;

            if (actorTarget != null)
                Targets.Add(actorTarget);
        }

        public bool IsInstantCast()
        {
            return Spell.CastTime <= 0f;
        }
    }
}