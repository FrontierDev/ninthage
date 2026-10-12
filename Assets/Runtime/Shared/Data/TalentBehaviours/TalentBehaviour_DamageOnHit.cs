using UnityEngine;
using Game.Shared.Networking;
using Game.Shared.Data;

namespace Game.Shared.Data
{
    [CreateAssetMenu(menuName = "NinthAge/Talent Behaviours/Damage On Hit")]
    public class TalentBehaviour_DamageOnHit : TalentBehaviour
    {
        [SerializeReference] private SpellEffectDefinition damageEffect;
        [SerializeField] private int damageTicks = 1;

        public override void OnActivate(PlayerActor actor)
        {
            actor.GetComponent<ActorEvents>().onServerAutoAttackHit += OnServerAutoAttackHit;
        }

        public override void OnDeactivate(PlayerActor actor)
        {
            actor.GetComponent<ActorEvents>().onServerAutoAttackHit -= OnServerAutoAttackHit;
        }

        private void OnServerAutoAttackHit(SpellContext ctx, Actor target)
        {
            for (int i = 0; i < damageTicks; i++)
                damageEffect.Execute(ctx, target);
        }
    }
}
