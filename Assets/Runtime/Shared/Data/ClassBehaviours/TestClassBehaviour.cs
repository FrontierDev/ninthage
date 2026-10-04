using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Data
{
    // TestClassBehaviour.cs — one asset handles all test passives
    [CreateAssetMenu(menuName = "NinthAge/Class Behaviours/Test")]
    public class TestClassBehaviour : ClassBehaviour
    {
        [SerializeField] private float manaPerHit = 10f;
        [SerializeField] private float manaOnDamageTaken = 5f;

        [SerializeField] private GameObject resourcePrefab;
        public override GameObject ResourcePrefab => resourcePrefab;

        public override void OnActivate(PlayerActor actor)
        {
            actor.GetComponent<ActorEvents>().onServerAutoAttackHit += (ctx, target) => GenerateMana(actor, manaPerHit);
        }

        public override void OnDeactivate(PlayerActor actor)
        {
            actor.GetComponent<ActorEvents>().onServerAutoAttackHit -= (ctx, target) => GenerateMana(actor, manaPerHit);
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
                statContainer.Observers_ReceiveSnapshot(statContainer.BuildSnapshot(ActorStatReplicationMode.Observers));
            else if (def.ReplicationMode == ActorStatReplicationMode.Owner)
                statContainer.Client_ReceiveSnapshot(actor.Owner.Value, statContainer.BuildSnapshot(ActorStatReplicationMode.Owner));

        }
    }
}