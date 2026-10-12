using Game.Shared.Networking;
using UnityEngine;

namespace Game.Shared.Data
{
    [CreateAssetMenu(menuName = "NinthAge/Talent Behaviours/Apply Aura On Hit")]
    public class TalentBehaviour_ApplyAuraOnHit : TalentBehaviour
    {
        [SerializeField] private AuraDefinition aura;
        [SerializeField] private int stacks = 1;

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
            ActorEndpoint.onServerApplyAura?.Invoke(aura, ctx.Caster, target, stacks);
        }
    }
}

