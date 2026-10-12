using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>Heals an attacker when it hits an aura host with an auto attack.</summary>
    [CreateAssetMenu(menuName = "NinthAge/Aura Behaviours/Heal Attacker On Hit")]
    public class AuraBehaviour_HealAttackerOnHit : AuraBehaviour
    {
        private const string note = "Heals the attacker when hitting the host of the aura with an auto attack.";
        /// <summary>Gets the behavior description.</summary>
        public override string Description => note;

        [SerializeReference] private SpellEffectDefinition healEffect;

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
            healEffect?.Execute(ctx, ctx.Caster);
    }
}
