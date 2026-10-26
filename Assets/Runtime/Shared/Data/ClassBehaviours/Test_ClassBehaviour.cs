using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Runtime.Spellcasting;
using UnityEngine;
using UnityEngine.Serialization;

using Game.Shared.Runtime.Actors;
namespace Game.Shared.Data.ClassBehaviours
{
    // Test_ClassBehaviour.cs — one asset handles all test passives
    /// <summary>Provides the test class passive behaviours.</summary>
    [CreateAssetMenu(menuName = "NinthAge/Class Behaviours/Test")]
    public class Test_ClassBehaviour : ClassBehaviour
    {
        [SerializeField, FormerlySerializedAs("manaPerHit")] private float _manaPerHit = 10f;
        [SerializeField, FormerlySerializedAs("manaOnDamageTaken")] private float _manaOnDamageTaken = 5f;

        [SerializeField, FormerlySerializedAs("resourcePrefab")] private GameObject _resourcePrefab;
        /// <summary>Gets the test class resource prefab.</summary>
        public override GameObject ResourcePrefab => _resourcePrefab;

        /// <summary>Activates the test class event handlers.</summary>
        public override void OnActivate(PlayerActor actor)
        {
            actor.GetComponent<ActorEvents>().onServerAutoAttackHit += OnServerAutoAttackHit;
        }

        /// <summary>Deactivates the test class event handlers.</summary>
        public override void OnDeactivate(PlayerActor actor)
        {
            actor.GetComponent<ActorEvents>().onServerAutoAttackHit -= OnServerAutoAttackHit;
        }

        private void OnServerAutoAttackHit(SpellContext ctx, Actor target)
        {
            if (ctx.Caster is PlayerActor player)
                GenerateMana(player, _manaPerHit);
        }

        private void GenerateMana(PlayerActor actor, float amount)
        {
            Debug.Log($"Generating {amount} mana for {actor.name}");

            var statContainer = actor.GetComponent<ActorStatContainer>();
            var stat = statContainer.GetStat("mana");
            var def = ActorStatDefinitionLibrary.Instance.GetDefinition("mana");
            float newCurrent = Mathf.Max(0f, stat.CurrentValue + amount);
            statContainer.SetStat("mana", new StatInstance(
                stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                stat.EffectiveMaximum, newCurrent));

            if (def.ReplicationMode == ActorStatReplicationMode.Observers)
                statContainer.PublishObserverSnapshot(statContainer.BuildSnapshot(ActorStatReplicationMode.Observers));
            else if (def.ReplicationMode == ActorStatReplicationMode.Owner)
                statContainer.PublishOwnerSnapshot(actor.Owner.Value, statContainer.BuildSnapshot(ActorStatReplicationMode.Owner));

        }
    }
}
