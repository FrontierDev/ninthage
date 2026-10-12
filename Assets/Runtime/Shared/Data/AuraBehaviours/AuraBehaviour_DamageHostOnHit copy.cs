using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Data
{
    /// <summary>Damages an aura host when it is hit by an auto attack.</summary>
    [CreateAssetMenu(menuName = "NinthAge/Aura Behaviours/Damage Host On Hit")]
    public class AuraBehaviour_DamageHostOnHit : AuraBehaviour
    {
        private const string NOTE = "Damages the host of the aura when hit by an auto attack.";
        /// <summary>Gets the behavior description.</summary>
        public override string Description => NOTE;

        [SerializeReference, FormerlySerializedAs("damageEffect")] private SpellEffectDefinition _damageEffect;

        /// <summary>Subscribes the behavior to the host's auto-attack events.</summary>
        public override void OnApplied(Actor caster, Actor target, int stacks)
        {
            var events = target.GetComponent<ActorEvents>();
            if (events != null)
                events.onServerAutoAttackTaken += OnHit;
        }

        /// <summary>Removes the behavior's event subscription from the host.</summary>
        public override void OnRemoved(Actor caster, Actor target)
        {
            var events = target.GetComponent<ActorEvents>();
            if (events != null)
                events.onServerAutoAttackTaken -= OnHit;
        }

        private void OnHit(SpellContext ctx, Actor target) =>
            _damageEffect?.Execute(ctx, target);
    }
}
