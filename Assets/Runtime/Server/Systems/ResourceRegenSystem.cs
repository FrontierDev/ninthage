using Debug = Game.Shared.Utility.FormattedDebug;
using Unity.Entities;

namespace Game.Server.ECS
{
    /// <summary>Processes server-side resource regeneration.</summary>
    [DisableAutoCreation]
    public partial struct ResourceRegenSystem : ISystem
    {
        /// <summary>Initializes resource-regeneration system state.</summary>
        public readonly void OnCreate(ref SystemState state)
        {
            Debug.Log(" - Created ResourceRegenSystem.");
        }

        /// <summary>Updates resource regeneration for the current server tick.</summary>
        public readonly void OnUpdate(ref SystemState state)
        {
            // Main update logic for the system.
        }

        /// <summary>Releases resource-regeneration system state.</summary>
        public readonly void OnDestroy(ref SystemState state)
        {
            Debug.Log(" - Destroyed ResourceRegenSystem.");
        }
    }
}
