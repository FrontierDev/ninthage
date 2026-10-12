using Game.Shared.Data;
using Game.Shared.Networking;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Data
{
    /// <summary>Executes a damage effect whenever the player hits with an auto attack.</summary>
    [CreateAssetMenu(menuName = "NinthAge/Talent Behaviours/Damage On Hit")]
    public class TalentBehaviour_DamageOnHit : TalentBehaviour
    {
        [SerializeReference, FormerlySerializedAs("damageEffect")] private SpellEffectDefinition _damageEffect;
        [SerializeField, FormerlySerializedAs("damageTicks")] private int _damageTicks = 1;

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
            for (int i = 0; i < _damageTicks; i++)
                _damageEffect.Execute(ctx, target);
        }
    }
}
