using Game.Shared.Data;
using Game.Shared.Networking;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>Executes a damage effect whenever the player hits with an auto attack.</summary>
    [CreateAssetMenu(menuName = "NinthAge/Talent Behaviours/Damage On Hit")]
    public class TalentBehaviour_DamageOnHit : TalentBehaviour
    {
        [SerializeReference] private SpellEffectDefinition damageEffect;
        [SerializeField] private int damageTicks = 1;

        /// <summary>Activates the auto-attack damage handler.</summary>
        public override void OnActivate(PlayerActor actor)
        {
            actor.GetComponent<ActorEvents>().onServerAutoAttackHit += OnServerAutoAttackHit;
        }

        /// <summary>Deactivates the auto-attack damage handler.</summary>
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
