using System.Collections.Generic;
using Game.Shared.Data.ClassBehaviours;
using Game.Shared.Runtime.Spellcasting;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

using Game.Shared.Runtime.Actors;
namespace Game.Shared.Data.SpellEffectDefinitions
{
    [System.Serializable]
    [MovedFrom(true, "Game.Shared.Data", "Game.Shared")]
    /// <summary>Places a Sorcerer rift at the spell target position.</summary>
    public class SpellEffect_PlaceRift : SpellEffectDefinition
    {
        /// <summary>Gets or sets the Sorcerer class behaviour.</summary>
        [SerializeField] public SorcererClassBehaviour ClassBehaviour;
        /// <summary>Gets or sets the rift radius.</summary>
        [SerializeField] public float Radius = 5f;
        /// <summary>Gets or sets the rift duration.</summary>
        [SerializeField] public float Duration = 30f;
        /// <summary>Gets or sets the rift prefab addressable path.</summary>
        [SerializeField] public string RiftPrefabAddressablePath;

        [SerializeField, FormerlySerializedAs("casterEvents")] private List<SpellActorEvent> _casterEvents = new();
        /// <summary>Gets the caster events raised by this effect.</summary>
        public override List<SpellActorEvent> CasterEvents => _casterEvents;

        [SerializeField, FormerlySerializedAs("targetEvents")] private List<SpellActorEvent> _targetEvents = new();
        /// <summary>Gets the target events raised by this effect.</summary>
        public override List<SpellActorEvent> TargetEvents => _targetEvents;

        /// <summary>Creates a copy of this effect.</summary>
        public override SpellEffectDefinition Clone()
        {
            var clone = (SpellEffect_PlaceRift)MemberwiseClone();
            clone._casterEvents = new List<SpellActorEvent>(_casterEvents);
            clone._targetEvents = new List<SpellActorEvent>(_targetEvents);
            return clone;
        }

        /// <summary>Places the rift for the spell caster.</summary>
        public override void Execute(SpellContext ctx, Actor target)
        {
            if (ClassBehaviour == null) return;
            if (!string.IsNullOrEmpty(RiftPrefabAddressablePath))
            {
                int effectId = Time.time.GetHashCode(); // Generate a unique ID for this effect instance
                ctx.Caster.GetComponent<ActorVFXController>()
                    ?.Observers_SpawnEffectAtPosition(effectId, RiftPrefabAddressablePath, ctx.PositionTarget, Duration);
                ClassBehaviour.RegisterRift(ctx.Caster, ctx.PositionTarget, Duration, Radius, effectId);
            }
            else
            {
                ClassBehaviour.RegisterRift(ctx.Caster, ctx.PositionTarget, Duration, Radius, 0);
            }
        }
    }
}
