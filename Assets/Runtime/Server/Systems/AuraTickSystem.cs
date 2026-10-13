using Game.Shared.Networking;
using Unity.Entities;

namespace Game.Server.ECS
{
    /// <summary>Advances active aura durations and invokes aura callbacks.</summary>
    [DisableAutoCreation]
    public partial struct AuraTickSystem : ISystem
    {
        /// <summary>Processes aura ticks and expirations for server actors.</summary>
        public void OnUpdate(ref SystemState state)
        {
            float dt = UnityEngine.Time.deltaTime;

            foreach (var (_, actor, entity) in
                SystemAPI.Query<DynamicBuffer<AuraElement>, RefRO<ActorComponent>>()
                         .WithEntityAccess())
            {
                var buffer = SystemAPI.GetBuffer<AuraElement>(entity);

                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    var entry = buffer[i];
                    entry.RemainingDuration -= dt;
                    entry.TimeSinceLastTick += dt;

                    if (entry.TickInterval > 0f && entry.TimeSinceLastTick >= entry.TickInterval)
                    {
                        entry.TimeSinceLastTick -= entry.TickInterval;

                        ActorEndpoint.onServerAuraTick?.Invoke(
                            actor.ValueRO.ActorId,
                            entry.AuraDefinitionId.ToString(),
                            entry.InstanceId);
                    }

                    if (entry.RemainingDuration <= 0f)
                    {
                        ActorEndpoint.onServerAuraExpired?.Invoke(
                            actor.ValueRO.ActorId,
                            entry.AuraDefinitionId.ToString(),
                            entry.InstanceId);

                        buffer.RemoveAt(i);
                    }
                    else
                    {
                        buffer[i] = entry;
                    }
                }
            }
        }
    }
}
