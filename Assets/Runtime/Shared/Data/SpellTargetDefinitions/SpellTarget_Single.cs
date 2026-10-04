using System.Collections.Generic;
using Game.Shared.Networking;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Data
{
    [System.Serializable]
    public class SpellTarget_Single : SpellTargetDefinition
    {
        public SpellTarget_Single()
        {
            _requiresTarget = true;
        }

        public override SpellTargetDefinition Clone()
        {
            return new SpellTarget_Single();
        }

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
                    ActorService.onServerSpellCastInterrupted?.Invoke(ctx.Caster.Id, ctx.Spell.BaseDefinition.DefinitionId);
                    return new List<Actor>();
                }
            }

            return ctx.ActorTarget != null ? new List<Actor> { ctx.ActorTarget } : new List<Actor>();
        }
    }
}