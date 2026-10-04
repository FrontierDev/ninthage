using Game.Shared.Networking;
using Unity.Entities;

namespace Game.Server.ECS
{
    [DisableAutoCreation]
    public partial struct ActorTickerSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            float dt = UnityEngine.Time.deltaTime;

            foreach (var (actor, entity) in
                SystemAPI.Query<RefRO<ActorComponent>>().WithEntityAccess())
            {
                var buffer = SystemAPI.GetBuffer<TickerElement>(entity);

                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    var entry = buffer[i];
                    entry.TimeSinceLastTick += dt;

                    if (entry.RemainingDuration != float.MaxValue)
                        entry.RemainingDuration -= dt;

                    if (entry.TickInterval > 0f && entry.TimeSinceLastTick >= entry.TickInterval)
                    {
                        entry.TimeSinceLastTick -= entry.TickInterval;

                        ActorService.onServerActorTick?.Invoke(
                            actor.ValueRO.ActorId,
                            entry.TickerId);
                    }

                    if (entry.RemainingDuration != float.MaxValue && entry.RemainingDuration <= 0f)
                    {
                        ActorService.onServerActorTickerExpired?.Invoke(
                            actor.ValueRO.ActorId,
                            entry.TickerId);

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
