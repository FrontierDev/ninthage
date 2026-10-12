using Debug = Game.Shared.FormattedDebug;
using UnityEngine;

namespace Game.Shared
{
    /// <summary>Tracks shared actor movement state.</summary>
    [RequireComponent(typeof(Actor))]
    public sealed class ActorMotor : MonoBehaviour
    {
        // Vertical velocity and grounded state are tracked separately from the 
        // main transform to allow for more precise control over character movement
        // and physics interactions, especially in a networked environment where the 
        // authoritative position is managed by the server.
        /// <summary>Gets or sets the actor's vertical velocity.</summary>
        public float verticalVelocity = 0;
        /// <summary>Gets or sets whether the actor is grounded.</summary>
        public bool isGrounded = true;
        /// <summary>Gets or sets the actor's horizontal velocity.</summary>
        public Vector3 horizontalVelocity = Vector3.zero;

        private Actor actor;

        private void Awake()
        {
            actor = GetComponent<Actor>();
        }
    }
}
