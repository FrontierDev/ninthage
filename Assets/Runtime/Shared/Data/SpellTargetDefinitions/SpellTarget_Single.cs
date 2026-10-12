using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Networking;
using System.Collections.Generic;
using Game.Shared.Runtime.Spellcasting;
using UnityEngine;
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

        /// <summary>Evaluates the selected actor and validates range when required.</summary>
        public override List<Actor> Evaluate(SpellContext ctx)
        {
            // If the cast phase is a channelling tick or has ended, we need to check that the target
            // is still in range. 
            if (ctx.Phase == SpellCastPhase.OnChannelTick || ctx.Phase == SpellCastPhase.OnCastEnd)
            {
                var distance = Vector3.Distance(ctx.Caster.transform.position, ctx.ActorTarget.transform.position);
                var maxRange = (ctx.Phase == SpellCastPhase.OnChannelTick ?
                    GameConfigurationManager.Config.SpellCastRangeTickBuffer :
                    GameConfigurationManager.Config.SpellCastRangeEndBuffer)
                    + ctx.Spell.Range;


                if (distance > maxRange)
                {
                    ActorEndpoint.onServerSpellCastInterrupted?.Invoke(ctx.Caster.ID, ctx.Spell.BaseDefinition.DefinitionID);
                    return new List<Actor>();
                }
            }

            return ctx.ActorTarget != null ? new List<Actor> { ctx.ActorTarget } : new List<Actor>();
        }
    }
}
