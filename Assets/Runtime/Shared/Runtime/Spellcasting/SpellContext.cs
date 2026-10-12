using Game.Shared.Data;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Game.Shared.Runtime.Actors;
namespace Game.Shared.Runtime.Spellcasting
{
    /// <summary>Contains runtime state for one spell cast.</summary>
    public sealed class SpellContext
    {
        /// <summary>Gets or sets the current cast phase.</summary>
        public SpellCastPhase Phase = SpellCastPhase.OnCastStart;  // Current phase of the spell cast (Start, Channeling, End)

        /// <summary>Gets or sets the effective spell.</summary>
        public SpellOverride Spell;
        /// <summary>Gets or sets the casting actor.</summary>
        public Actor Caster;
        /// <summary>Gets or sets the actor target.</summary>
        public Actor ActorTarget;
        /// <summary>Gets or sets the ground target position.</summary>
        public Vector3 PositionTarget;
        /// <summary>Gets the selected actor targets.</summary>
        public List<Actor> Targets = new();

        /// <summary>Initializes a spell context.</summary>
        public SpellContext(SpellOverride spell, Actor caster, Actor actorTarget = null)
        {
            Spell = spell;
            Caster = caster;
            ActorTarget = actorTarget;

            if (actorTarget != null)
                Targets.Add(actorTarget);
        }

        /// <summary>Gets whether this spell has no cast time.</summary>
        public bool IsInstantCast()
        {
            return Spell.CastTime <= 0f;
        }
    }
}
