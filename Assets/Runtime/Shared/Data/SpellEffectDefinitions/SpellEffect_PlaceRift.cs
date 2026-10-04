using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Data
{
    [System.Serializable]
    public class SpellEffect_PlaceRift : SpellEffectDefinition
    {
        [SerializeField] public SorcererClassBehaviour ClassBehaviour;
        [SerializeField] public float Radius = 5f;
        [SerializeField] public float Duration = 30f;
        [SerializeField] public string RiftPrefabAddressablePath;

        [SerializeField] private List<SpellActorEvent> casterEvents = new();
        public override List<SpellActorEvent> CasterEvents => casterEvents;

        [SerializeField] private List<SpellActorEvent> targetEvents = new();
        public override List<SpellActorEvent> TargetEvents => targetEvents;

        public override SpellEffectDefinition Clone()
        {
            var clone = (SpellEffect_PlaceRift)MemberwiseClone();
            clone.casterEvents = new List<SpellActorEvent>(casterEvents);
            clone.targetEvents = new List<SpellActorEvent>(targetEvents);
            return clone;
        }

        public override void Execute(SpellContext ctx, Actor target)
        {
            if (ClassBehaviour == null) return;
            if (!string.IsNullOrEmpty(RiftPrefabAddressablePath))
            {
                int effectId = Time.time.GetHashCode(); // Generate a unique ID for this effect instance
                ctx.Caster.GetComponent<ActorVFXController>()
                    ?.Observer_SpawnEffectAtPosition(effectId, RiftPrefabAddressablePath, ctx.PositionTarget, Duration);
                ClassBehaviour.RegisterRift(ctx.Caster, ctx.PositionTarget, Duration, Radius, effectId);
            }
            else
            {
                ClassBehaviour.RegisterRift(ctx.Caster, ctx.PositionTarget, Duration, Radius, 0);
            }
        }
    }
}
