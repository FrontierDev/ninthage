using Game.Shared;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using System;
using UnityEngine;

namespace Game.Server.Core
{
    public sealed partial class ServerPhysicsManager
    {
        // These dimensions match the project PlayerPrefab CharacterController.
        private const float PLAYER_COLLIDER_HEIGHT = 2f;
        private const float PLAYER_COLLIDER_RADIUS = 0.25f;
        private const float PLAYER_COLLIDER_CENTER_Y = 1f;
        private const float ARRIVAL_SUPPORT_CONTACT_EPSILON = 0.001f;

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
        /// Thrown when Unity returns a non-finite hit position.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="maxDistance"/> is non-finite or negative, or when the query origin cannot
        /// be represented in the continuous Unity frame.
        /// </exception>
        public bool RaycastGround(
            WorldPosition worldOrigin,
            float maxDistance,
            int layerMask,
            out WorldPosition hitWorldPosition,
            out float hitDistance)
        {
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

        /// <summary>
        /// Validates that an entry position is representable, loaded and safe for the project player collider.
        /// </summary>
        /// <param name="context">The context that owns the arrival position.</param>
        /// <param name="position">The candidate absolute world position.</param>
        /// <param name="authoredGroundAnchor">Whether the candidate requires a ground hit.</param>
        /// <param name="failureReason">Receives the explicit rejection reason.</param>
        /// <returns><see langword="true"/> when the position passes all arrival checks.</returns>
        internal bool IsCharacterArrivalSafe(
            ActorSpatialContext context,
            WorldPosition position,
            bool authoredGroundAnchor,
            out string failureReason)
        {
            failureReason = null;
            if (!context.IsValid)
                return FailArrival("the arrival context is invalid", out failureReason);
            if (!position.IsFinite)
                return FailArrival("the arrival position is not finite", out failureReason);
            if (!TryGetArrivalChunk(position, out Vector2Int chunk, out failureReason))
                return false;

            Vector3 physicalPosition;
            try
            {
                physicalPosition = ToPhysicalPosition(position);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                return FailArrival(exception.Message, out failureReason);
            }

            ServerWorldManager worldManager = ServerWorldManager.Instance;
            if (worldManager == null || !worldManager.IsChunkLoaded(context, chunk))
            {
                return FailArrival(
                    $"the arrival chunk {chunk} is not loaded for context {context}",
                    out failureReason);
            }

            GameConfiguration configuration = Game.Shared.GameConfigurationManager.Config;
            if (configuration == null)
            {
                return FailArrival("arrival physics configuration is unavailable", out failureReason);
            }

            int blockingLayerMask = configuration.CharacterArrivalBlockingMask.value;
            if (blockingLayerMask == 0)
            {
                return FailArrival(
                    "character-arrival blocking layer mask is empty",
                    out failureReason);
            }

            int groundLayerMask = 0;
            if (authoredGroundAnchor)
            {
                if (string.IsNullOrWhiteSpace(configuration.GroundDetectionMaskName))
                {
                    return FailArrival(
                        "ground detection configuration is unavailable",
                        out failureReason);
                }

                groundLayerMask = LayerMask.GetMask(configuration.GroundDetectionMaskName);
                if (groundLayerMask == 0)
                {
                    return FailArrival(
                        $"ground detection layer '{configuration.GroundDetectionMaskName}' is unavailable",
                        out failureReason);
                }
            }

            try
            {
                if (authoredGroundAnchor)
                {
                    float groundCheckRadius = configuration.GroundDetectionRadius;
                    if (float.IsNaN(groundCheckRadius)
                        || float.IsInfinity(groundCheckRadius)
                        || groundCheckRadius < 0f)
                    {
                        return FailArrival("ground detection radius is invalid", out failureReason);
                    }

                    WorldPosition rayOrigin = new(
                        position.x,
                        position.y + groundCheckRadius,
                        position.z);
                    if (!RaycastGround(
                        rayOrigin,
                        groundCheckRadius * 2f + 0.1f,
                        groundLayerMask,
                        out _,
                        out _))
                    {
                        return FailArrival("the authored arrival has no ground hit", out failureReason);
                    }
                }

                Vector3 capsuleOffset = Vector3.up * PLAYER_COLLIDER_CENTER_Y;
                float halfCylinder = PLAYER_COLLIDER_HEIGHT * 0.5f - PLAYER_COLLIDER_RADIUS;
                // Lift only the lower endpoint so intended contact with the support floor is not reported as penetration.
                Vector3 capsuleStart = physicalPosition
                    + capsuleOffset
                    + Vector3.down * halfCylinder
                    + Vector3.up * ARRIVAL_SUPPORT_CONTACT_EPSILON;
                Vector3 capsuleEnd = physicalPosition + capsuleOffset + Vector3.up * halfCylinder;
                Collider[] overlaps = Physics.OverlapCapsule(
                    capsuleStart,
                    capsuleEnd,
                    PLAYER_COLLIDER_RADIUS,
                    blockingLayerMask,
                    QueryTriggerInteraction.Ignore);
                if (overlaps.Length != 0)
                {
                    return FailArrival(
                        $"the player collider overlaps {overlaps.Length} blocking collider(s)",
                        out failureReason);
                }
            }
            catch (Exception exception)
            {
                return FailArrival(
                    $"arrival physics validation failed: {exception.Message}",
                    out failureReason);
            }

            return true;
        }

        /// <summary>Validates and resolves the streamable chunk for an absolute arrival position.</summary>
        internal bool TryGetCharacterArrivalChunk(
            WorldPosition position,
            out Vector2Int chunk,
            out string failureReason)
        {
            return TryGetArrivalChunk(position, out chunk, out failureReason);
        }

        private static bool TryGetArrivalChunk(
            WorldPosition position,
            out Vector2Int chunk,
            out string failureReason)
        {
            chunk = default;
            failureReason = null;
            GameConfiguration configuration = Game.Shared.GameConfigurationManager.Config;
            if (configuration == null || configuration.ChunkSize <= 0)
                return FailArrival("world chunk configuration is unavailable", out failureReason);

            double chunkSize = configuration.ChunkSize;
            double halfChunk = chunkSize * 0.5d;
            double chunkX = Math.Floor((position.x + halfChunk) / chunkSize);
            double chunkZ = Math.Floor((position.z + halfChunk) / chunkSize);
            if (double.IsNaN(chunkX)
                || double.IsInfinity(chunkX)
                || double.IsNaN(chunkZ)
                || double.IsInfinity(chunkZ)
                || chunkX < int.MinValue
                || chunkX > int.MaxValue
                || chunkZ < int.MinValue
                || chunkZ > int.MaxValue)
            {
                return FailArrival("the arrival position cannot be mapped to a world chunk", out failureReason);
            }

            chunk = new Vector2Int((int)chunkX, (int)chunkZ);
            return true;
        }

        private static bool FailArrival(string reason, out string failureReason)
        {
            failureReason = reason;
            return false;
        }
    }
}
