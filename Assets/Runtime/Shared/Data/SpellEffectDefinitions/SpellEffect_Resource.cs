using Game.Shared.Networking;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

        [SerializeField, FormerlySerializedAs("casterEvents")] private List<SpellActorEvent> _casterEvents = new();
        /// <summary>Gets the caster events raised by this effect.</summary>
        public override List<SpellActorEvent> CasterEvents => _casterEvents;

        [SerializeField, FormerlySerializedAs("targetEvents")] private List<SpellActorEvent> _targetEvents = new();
        /// <summary>Gets the target events raised by this effect.</summary>
        public override List<SpellActorEvent> TargetEvents => _targetEvents;

        /// <summary>Creates a copy of this effect.</summary>
        public override SpellEffectDefinition Clone()
        {
            var clone = (SpellEffect_Resource)MemberwiseClone();
            clone.Resource = this.Resource; // If ActorStatDefinition is mutable, consider cloning it
            clone.Amount = this.Amount;
            clone._casterEvents = new(this._casterEvents);
            clone._targetEvents = new(this._targetEvents);
            return clone;
        }

        /// <summary>Executes the resource change.</summary>
        public override void Execute(SpellContext ctx, Actor target)
        {
            var statContainer = target.GetComponent<ActorStatContainer>();
            if (statContainer != null)
            {
                statContainer.AddCurrentValue(Resource.DefinitionID, Amount);
            }
        }
    }
}
