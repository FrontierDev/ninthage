using Game.Shared.Networking;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Data
{
    /// <summary>Applies an aura whenever the player hits with an auto attack.</summary>
    [CreateAssetMenu(menuName = "NinthAge/Talent Behaviours/Apply Aura On Hit")]
    public class TalentBehaviour_ApplyAuraOnHit : TalentBehaviour
    {
        [SerializeField, FormerlySerializedAs("aura")] private AuraDefinition _aura;
        [SerializeField, FormerlySerializedAs("stacks")] private int _stacks = 1;

        /// <summary>Activates the auto-attack aura handler.</summary>
        public override void OnActivate(PlayerActor actor)
        {
            actor.GetComponent<ActorEvents>().onServerAutoAttackHit += OnServerAutoAttackHit;
        }

        /// <summary>Deactivates the auto-attack aura handler.</summary>
        public override void OnDeactivate(PlayerActor actor)
        {
            actor.GetComponent<ActorEvents>().onServerAutoAttackHit -= OnServerAutoAttackHit;
        }

        private void OnServerAutoAttackHit(SpellContext ctx, Actor target)
        {
            ActorEndpoint.onServerApplyAura?.Invoke(_aura, ctx.Caster, target, _stacks);
        }
    }
}

