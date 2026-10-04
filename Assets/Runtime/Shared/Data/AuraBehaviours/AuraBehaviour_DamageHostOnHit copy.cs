using UnityEngine;

namespace Game.Shared.Data
{
    [CreateAssetMenu(menuName = "NinthAge/Aura Behaviours/Damage Host On Hit")]
    public class AuraBehaviour_DamageHostOnHit : AuraBehaviour
    {
        private const string note = "Damages the host of the aura when hit by an auto attack.";
        public override string Description => note;

        [SerializeReference] private SpellEffectDefinition damageEffect;

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
            damageEffect?.Execute(ctx, target);
    }
}