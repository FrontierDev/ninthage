using Game.Shared.Utility;
using System;
using UnityEngine;

namespace Game.Server.Core
{
    public sealed partial class ServerPhysicsManager
    {
        /// <summary>
        /// Casts a downward ground ray for an actor using the actor's verified simulation-region frame.
        /// </summary>
        /// <param name="actorID">The nonempty identity of the actor performing the query.</param>
        /// <param name="worldOrigin">The finite absolute world-space origin of the ray.</param>
        /// <param name="maxDistance">The nonnegative maximum ray distance in metres.</param>
        /// <param name="layerMask">The Unity physics layer mask used by the raycast.</param>
        /// <param name="hitWorldPosition">Receives the finite absolute world-space hit position when the ray hits; otherwise the default value.</param>
        /// <param name="hitDistance">Receives the hit distance when the ray hits; otherwise zero.</param>
        /// <returns><see langword="true"/> when the ray hits a collider; otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="actorID"/> is empty or <paramref name="worldOrigin"/> is non-finite.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="maxDistance"/> is non-finite or negative, or when the query origin or hit
        /// cannot be represented inside the actor's simulation region.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the physics manager is not initialized, or the actor's physical-region binding is missing
        /// or stale.
        /// </exception>
        public bool RaycastActorGround(
            Guid actorID,
            WorldPosition worldOrigin,
            float maxDistance,
            int layerMask,
            out WorldPosition hitWorldPosition,
            out float hitDistance)
        {
            EnsureInitialized();
            if (actorID == Guid.Empty)
                throw new ArgumentException("A nonempty actor ID is required.", nameof(actorID));
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

            if (!TryGetActorRegionDescriptor(actorID, out var region))
            {
                throw new InvalidOperationException(
                    $"Actor {actorID} is not bound to an active simulation region.");
            }

            var localOrigin = ToRegionLocal(region, worldOrigin);
            var hitGround = Physics.Raycast(
                localOrigin,
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

            hitWorldPosition = ToWorld(region, hit.point);
            hitDistance = hit.distance;
            return true;
        }
    }
}
