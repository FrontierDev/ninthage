using Game.Shared.Utility;
using System;
using UnityEngine;

namespace Game.Server.Core
{
    public sealed partial class ServerPhysicsManager
    {
        /// <summary>
        /// Casts a downward ground ray in the continuous server physical frame.
        /// </summary>
        /// <param name="worldOrigin">The finite absolute world-space origin of the ray.</param>
        /// <param name="maxDistance">The nonnegative maximum ray distance in metres.</param>
        /// <param name="layerMask">The Unity physics layer mask used by the raycast.</param>
        /// <param name="hitWorldPosition">Receives the finite absolute world-space hit position when the ray hits; otherwise the default value.</param>
        /// <param name="hitDistance">Receives the hit distance when the ray hits; otherwise zero.</param>
        /// <returns><see langword="true"/> when the ray hits a collider; otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="worldOrigin"/> is non-finite.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="maxDistance"/> is non-finite or negative, or when the query origin or hit
        /// cannot be represented in the continuous Unity frame.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the physics manager is not initialized.
        /// </exception>
        public bool RaycastGround(
            WorldPosition worldOrigin,
            float maxDistance,
            int layerMask,
            out WorldPosition hitWorldPosition,
            out float hitDistance)
        {
            EnsureInitialized();
            if (!worldOrigin.IsFinite)
                throw new ArgumentException("The ray origin must be finite.", nameof(worldOrigin));
            if (float.IsNaN(maxDistance)
                || float.IsInfinity(maxDistance)
                || maxDistance < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxDistance),
                    "The maximum ray distance must be finite and nonnegative.");
            }

            var physicalOrigin = ToPhysicalPosition(worldOrigin);
            var hitGround = Physics.Raycast(
                physicalOrigin,
                Vector3.down,
                out var hit,
                maxDistance,
                layerMask);
            if (!hitGround)
            {
                hitWorldPosition = default;
                hitDistance = 0f;
                return false;
            }

            hitWorldPosition = ToWorldPosition(hit.point);
            hitDistance = hit.distance;
            return true;
        }
    }
}
