using Game.Shared.Networking;
using Unity.Entities;

namespace Game.Server.ECS
{
    [DisableAutoCreation]
    public partial struct AuraTickSystem : ISystem
    {
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

                        ActorService.onServerAuraTick?.Invoke(
                            actor.ValueRO.ActorId,
                            entry.AuraDefinitionId.ToString(),
                            entry.InstanceId);
                    }

                    if (entry.RemainingDuration <= 0f)
                    {
                        ActorService.onServerAuraExpired?.Invoke(
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
