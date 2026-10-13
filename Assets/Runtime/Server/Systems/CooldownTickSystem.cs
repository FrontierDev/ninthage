using Unity.Entities;

namespace Game.Server.Systems
{
    /// <summary>Advances and removes server actor cooldown entries.</summary>
    [DisableAutoCreation]
    public partial struct CooldownTickSystem : ISystem
    {
        /// <summary>Updates active cooldown durations for all server actors.</summary>
        public void OnUpdate(ref SystemState state)
        {
            float dt = UnityEngine.Time.deltaTime;

            foreach (var (_, entity) in
                SystemAPI.Query<DynamicBuffer<CooldownElement>>()
                         .WithEntityAccess())
            {
                var buffer = SystemAPI.GetBuffer<CooldownElement>(entity);
                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    var entry = buffer[i];
                    entry.RemainingCooldown -= dt;

                    if (entry.RemainingCooldown <= 0f)
                        buffer.RemoveAt(i);
                    else
                        buffer[i] = entry;
                }
            }
        }
    }
}
