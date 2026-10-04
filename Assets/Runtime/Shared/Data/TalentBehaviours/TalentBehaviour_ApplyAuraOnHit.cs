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
            actor.GetComponent<ActorEvents>().onServerAutoAttackHit += (ctx, target) =>
                ActorService.onServerApplyAura?.Invoke(aura, actor, target, stacks);
        }

        public override void OnDeactivate(PlayerActor actor)
        {
            actor.GetComponent<ActorEvents>().onServerAutoAttackHit -= (ctx, target) =>
                ActorService.onServerApplyAura?.Invoke(aura, actor, target, stacks);
        }
    }
}

