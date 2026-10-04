using UnityEngine;

namespace Game.Shared.Data
{
    [CreateAssetMenu(menuName = "NinthAge/Aura Behaviours/Heal Attacker On Hit")]
    public class AuraBehaviour_HealAttackerOnHit : AuraBehaviour
    {
        private const string note = "Heals the attacker when hitting the host of the aura with an auto attack.";
        public override string Description => note;

        [SerializeReference] private SpellEffectDefinition healEffect;

        public override void OnApplied(Actor caster, Actor target, int stacks)
        {
            var events = target.GetComponent<ActorEvents>();
            if (events != null)
                events.onServerAutoAttackTaken += OnHit;
        }

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