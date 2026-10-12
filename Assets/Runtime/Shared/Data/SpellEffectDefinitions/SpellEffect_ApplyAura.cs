using Game.Shared.Networking;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Data
{
    [System.Serializable]
    /// <summary>Applies an aura to the effect target.</summary>
    public class SpellEffect_ApplyAura : SpellEffectDefinition
    {
        /// <summary>Gets or sets the aura to apply.</summary>
        public AuraDefinition Aura;
        /// <summary>Gets or sets the number of stacks to apply.</summary>
        public int Stacks = 1;
        /// <summary>Gets or sets the applied duration.</summary>
        public float Duration = 12.0f;

        [SerializeField, FormerlySerializedAs("casterEvents")] private List<SpellActorEvent> _casterEvents = new();
        /// <summary>Gets the caster events raised by this effect.</summary>
        public override List<SpellActorEvent> CasterEvents => _casterEvents;

        [SerializeField, FormerlySerializedAs("targetEvents")] private List<SpellActorEvent> _targetEvents = new();
        /// <summary>Gets the target events raised by this effect.</summary>
        public override List<SpellActorEvent> TargetEvents => _targetEvents;

        /// <summary>Creates a copy of this effect.</summary>
        public override SpellEffectDefinition Clone()
        {
            var clone = (SpellEffect_ApplyAura)MemberwiseClone();
            clone.Aura = this.Aura; // If AuraDefinition is mutable, consider cloning it
            clone.Stacks = this.Stacks;
            clone.Duration = this.Duration;
            clone._casterEvents = new(this._casterEvents);
            clone._targetEvents = new(this._targetEvents);
            return clone;
        }

        /// <summary>Executes the aura application.</summary>
        public override void Execute(SpellContext ctx, Actor target)
        {
            ActorEndpoint.onServerApplyAura?.Invoke(Aura, ctx.Caster, target, Stacks);
        }
    }
}
