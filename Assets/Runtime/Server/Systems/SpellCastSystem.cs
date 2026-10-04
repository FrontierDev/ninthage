using Game.Shared.Networking;
using Unity.Entities;

namespace Game.Server.ECS
{
    [DisableAutoCreation]
    public partial struct SpellCastSystem : ISystem
    {
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

                        ActorService.onServerSpellCastTick?.Invoke(
                            actor.ValueRO.ActorId,
                            spellCast.ValueRO.SpellID.ToString());
                    }
                }

                if (spellCast.ValueRO.RemainingCastTime <= 0f)
                {
                    // Disable the component (cast finished)
                    state.EntityManager.SetComponentEnabled<SpellCastComponent>(entity, false);

                    // Fire an event so the MonoBehaviour layer can resolve the spell
                    ActorService.onServerSpellCastCompleted?.Invoke(
                        actor.ValueRO.ActorId,
                        spellCast.ValueRO.SpellID.ToString());
                }
            }
        }
    }
}