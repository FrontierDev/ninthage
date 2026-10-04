using System.Collections.Generic;
using Game.Shared.Networking;
using UnityEngine;

namespace Game.Shared.Data
{
    [System.Serializable]
    public class SpellEffect_ApplyAura : SpellEffectDefinition
    {
        public AuraDefinition Aura;
        public int Stacks = 1;
        public float Duration = 12.0f;

        [SerializeField] private List<SpellActorEvent> casterEvents = new();
        public override List<SpellActorEvent> CasterEvents => casterEvents;

        [SerializeField] private List<SpellActorEvent> targetEvents = new();
        public override List<SpellActorEvent> TargetEvents => targetEvents;

        public override SpellEffectDefinition Clone()
        {
            var clone = (SpellEffect_ApplyAura)MemberwiseClone();
            clone.Aura = this.Aura; // If AuraDefinition is mutable, consider cloning it
            clone.Stacks = this.Stacks;
            clone.Duration = this.Duration;
            clone.casterEvents = new List<SpellActorEvent>(this.casterEvents);
            clone.targetEvents = new List<SpellActorEvent>(this.targetEvents);
            return clone;
        }

        public override void Execute(SpellContext ctx, Actor target)
        {
            ActorService.onServerApplyAura?.Invoke(Aura, ctx.Caster, target, Stacks);
        }
    }
}