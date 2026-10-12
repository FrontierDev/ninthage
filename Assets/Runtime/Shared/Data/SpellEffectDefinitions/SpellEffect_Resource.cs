using Game.Shared.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Data
{
    [System.Serializable]
    /// <summary>Changes an actor resource by a configured amount.</summary>
    public class SpellEffect_Resource : SpellEffectDefinition
    {
        /// <summary>Gets or sets the resource definition.</summary>
        public ActorStatDefinition Resource;
        /// <summary>Gets or sets the amount to restore or consume.</summary>
        public float Amount; // Positive to restore, negative to consume

        [SerializeField] private List<SpellActorEvent> casterEvents = new();
        /// <summary>Gets the caster events raised by this effect.</summary>
        public override List<SpellActorEvent> CasterEvents => casterEvents;

        [SerializeField] private List<SpellActorEvent> targetEvents = new();
        /// <summary>Gets the target events raised by this effect.</summary>
        public override List<SpellActorEvent> TargetEvents => targetEvents;

        /// <summary>Creates a copy of this effect.</summary>
        public override SpellEffectDefinition Clone()
        {
            var clone = (SpellEffect_Resource)MemberwiseClone();
            clone.Resource = this.Resource; // If ActorStatDefinition is mutable, consider cloning it
            clone.Amount = this.Amount;
            clone.casterEvents = new(this.casterEvents);
            clone.targetEvents = new(this.targetEvents);
            return clone;
        }

        /// <summary>Executes the resource change.</summary>
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
