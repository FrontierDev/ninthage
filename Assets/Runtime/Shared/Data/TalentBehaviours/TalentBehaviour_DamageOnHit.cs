using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Spellcasting;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

using Game.Shared.Runtime.Actors;
namespace Game.Shared.Data.TalentBehaviours
{
    /// <summary>Executes a damage effect whenever the player hits with an auto attack.</summary>
    [CreateAssetMenu(menuName = "NinthAge/Talent Behaviours/Damage On Hit")]
    [MovedFrom(true, "Game.Shared.Data", "Game.Shared")]
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
