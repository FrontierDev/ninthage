using Game.Shared.Networking;
using Unity.Entities;

namespace Game.Server.ECS
{
    /// <summary>Advances active spell casts and invokes spell callbacks.</summary>
    [DisableAutoCreation]
    public partial struct SpellCastSystem : ISystem
    {
        /// <summary>Updates active spell casts for the current server tick.</summary>
        public void OnUpdate(ref SystemState state)
        {
            float dt = UnityEngine.Time.deltaTime;

            foreach (var (spellCast, actor, entity) in
                SystemAPI.Query<RefRW<SpellCastComponent>, RefRO<ActorComponent>>()
                         .WithEntityAccess())
            {
                spellCast.ValueRW.RemainingCastTime -= dt;

                // Channel tick logic
                if (spellCast.ValueRO.TotalTicks > 0f)
                {
                    float tickInterval = spellCast.ValueRO.TotalCastTime / spellCast.ValueRO.TotalTicks;
                    spellCast.ValueRW.TimeSinceLastTick += dt;

                    if (spellCast.ValueRO.TimeSinceLastTick >= tickInterval)
                    {
                        spellCast.ValueRW.TimeSinceLastTick -= tickInterval;

                        ActorEndpoint.onServerSpellCastTick?.Invoke(
                            actor.ValueRO.ActorID,
                            spellCast.ValueRO.SpellID.ToString());
                    }
                }

                if (spellCast.ValueRO.RemainingCastTime <= 0f)
                {
                    // Disable the component (cast finished)
                    state.EntityManager.SetComponentEnabled<SpellCastComponent>(entity, false);

                    // Fire an event so the MonoBehaviour layer can resolve the spell
                    ActorEndpoint.onServerSpellCastCompleted?.Invoke(
                        actor.ValueRO.ActorID,
                        spellCast.ValueRO.SpellID.ToString());
                }
            }
        }
    }
}
