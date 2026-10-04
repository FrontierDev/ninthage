using UnityEngine;

namespace Game.Shared
{
    /// <summary>
    /// Deterministic movement math shared by client (prediction) and server (replay).
    /// No transform dependencies — uses yaw degrees to derive forward/right.
    /// </summary>
    public static class MovementSimulation
    {
        /// <summary>
        /// Computes the total move delta to pass to CharacterController.Move().
        /// </summary>
        public static Vector3 ComputeMoveDelta(
            Vector2 moveInput,
            float yawDegrees,
            float moveSpeed,
            float verticalVelocity,
            float deltaTime,
            bool isGrounded,
            out Vector3 horizontalVelocity)
        {
            var yawRad = yawDegrees * Mathf.Deg2Rad;
            var forward = new Vector3(Mathf.Sin(yawRad), 0f, Mathf.Cos(yawRad));
            var right = new Vector3(forward.z, 0f, -forward.x);

            var inputVec = right * moveInput.x + forward * moveInput.y;
            if (inputVec.sqrMagnitude > 1f)
                inputVec.Normalize();

            horizontalVelocity = isGrounded ? inputVec * moveSpeed : Vector3.zero;

            // When airborne, horizontalVelocity is zero here, but the caller
            // preserves the existing motor velocity. We return zero horizontal
            // contribution so the caller can decide.
            var horizontalDelta = isGrounded ? horizontalVelocity * deltaTime : Vector3.zero;
            var verticalDelta = new Vector3(0f, verticalVelocity * deltaTime, 0f);
            return horizontalDelta + verticalDelta;
        }

        /// <summary>
        /// Updates vertical velocity with gravity. Returns the new vertical velocity.
        /// </summary>
        public static float ApplyGravity(float verticalVelocity, float gravity, float maxFallSpeed, float deltaTime, bool isGrounded)
        {
            if (!isGrounded)
            {
                verticalVelocity += gravity * deltaTime;
                if (verticalVelocity < -maxFallSpeed)
                    verticalVelocity = -maxFallSpeed;
            }
            return verticalVelocity;
        }
    }
}
