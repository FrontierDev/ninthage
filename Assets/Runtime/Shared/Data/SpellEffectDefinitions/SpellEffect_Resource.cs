using System.Collections.Generic;
using Game.Shared.Networking;
using UnityEngine;

namespace Game.Shared.Data
{
    [System.Serializable]
    public class SpellEffect_Resource : SpellEffectDefinition
    {
        public ActorStatDefinition Resource;
        public float Amount; // Positive to restore, negative to consume

        [SerializeField] private List<SpellActorEvent> casterEvents = new();
        public override List<SpellActorEvent> CasterEvents => casterEvents;

        [SerializeField] private List<SpellActorEvent> targetEvents = new();
        public override List<SpellActorEvent> TargetEvents => targetEvents;

        public override SpellEffectDefinition Clone()
        {
            var clone = (SpellEffect_Resource)MemberwiseClone();
            clone.Resource = this.Resource; // If ActorStatDefinition is mutable, consider cloning it
            clone.Amount = this.Amount;
            clone.casterEvents = new List<SpellActorEvent>(this.casterEvents);
            clone.targetEvents = new List<SpellActorEvent>(this.targetEvents);
            return clone;
        }

        public override void Execute(SpellContext ctx, Actor target)
        {
            var statContainer = target.GetComponent<ActorStatContainer>();
            if (statContainer != null)
            {
                statContainer.AddCurrentValue(Resource.DefinitionId, Amount);
            }
        }
    }
}