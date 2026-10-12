using Debug = Game.Shared.Utility.FormattedDebug;
using Unity.Entities;

namespace Game.Client
{
    /// <summary>
    /// Represents the client-side <c>TestClientSystem</c> value.
    /// </summary>
    [DisableAutoCreation]
    public partial struct TestClientSystem : ISystem
    {
        /// <summary>
        /// Handles the <c>OnCreate</c> callback.
        /// </summary>
        public readonly void OnCreate(ref SystemState state)
        {
            Debug.Log(" - Created Test Client System.");
        }

        /// <summary>
        /// Handles the <c>OnUpdate</c> callback.
        /// </summary>
        public readonly void OnUpdate(ref SystemState state)
        {
            // Main update logic for the system.
        }

        /// <summary>
        /// Handles the <c>OnDestroy</c> callback.
        /// </summary>
        public readonly void OnDestroy(ref SystemState state)
        {
            Debug.Log(" - Destroyed Test Client System.");
        }
    }
}
