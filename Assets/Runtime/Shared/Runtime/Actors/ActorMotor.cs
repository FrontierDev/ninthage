using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared
{
    [RequireComponent(typeof(Actor))]
    public sealed class ActorMotor : MonoBehaviour
    {
        // Vertical velocity and grounded state are tracked separately from the 
        // main transform to allow for more precise control over character movement
        // and physics interactions, especially in a networked environment where the 
        // authoritative position is managed by the server.
        public float verticalVelocity = 0;
        public bool isGrounded = true;
        public Vector3 horizontalVelocity = Vector3.zero;

        private Actor actor;

        private void Awake()
        {
            actor = GetComponent<Actor>();
        }
    }
}