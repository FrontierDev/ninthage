using Game.Shared.Utility;
using System;
using UnityEngine;

namespace Game.Server.Core
{
    public sealed partial class ServerPhysicsManager
    {
        /// <summary>Converts an absolute world position into the region-local Unity frame.</summary>
        /// <param name="region">The immutable physical-frame descriptor.</param>
        /// <param name="worldPosition">The finite absolute world position.</param>
        /// <returns>The region-local Unity position.</returns>
        /// <exception cref="ArgumentException">Thrown when the world position is non-finite.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the position is outside the region or cannot fit in finite floats.</exception>
        public Vector3 ToRegionLocal(
            SimulationRegionDescriptor region,
            WorldPosition worldPosition)
        {
            if (!worldPosition.IsFinite)
                throw new ArgumentException("The world position must be finite.", nameof(worldPosition));
            if (!ContainsWorldPosition(region, worldPosition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(worldPosition),
                    "The world position must be inside the region's half-open world footprint.");
            }

            double localX = (worldPosition.x - region.WorldOrigin.x) + region.LocalAnchor.x;
            double localY = (worldPosition.y - region.WorldOrigin.y) + region.LocalAnchor.y;
            double localZ = (worldPosition.z - region.WorldOrigin.z) + region.LocalAnchor.z;
            if (!FitsFiniteFloat(localX) || !FitsFiniteFloat(localY) || !FitsFiniteFloat(localZ))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(worldPosition),
                    "The region-local position must fit in finite Unity floats.");
            }

            return new Vector3((float)localX, (float)localY, (float)localZ);
        }

        /// <summary>Converts a region-local Unity position into an absolute world position.</summary>
        /// <param name="region">The immutable physical-frame descriptor.</param>
        /// <param name="regionLocalPosition">The finite region-local Unity position.</param>
        /// <returns>The reconstructed finite absolute world position.</returns>
        /// <exception cref="ArgumentException">Thrown when the local position is non-finite.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the position is outside the region's half-open local footprint or reconstruction is non-finite.</exception>
        public WorldPosition ToWorld(
            SimulationRegionDescriptor region,
            Vector3 regionLocalPosition)
        {
            if (!IsFinite(regionLocalPosition))
                throw new ArgumentException("The region-local position must be finite.", nameof(regionLocalPosition));
            if (!ContainsRegionLocalPosition(region, regionLocalPosition))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(regionLocalPosition),
                    "The region-local position must be inside the region's half-open local footprint.");
            }

            double worldX = region.WorldOrigin.x + ((double)regionLocalPosition.x - region.LocalAnchor.x);
            double worldY = region.WorldOrigin.y + ((double)regionLocalPosition.y - region.LocalAnchor.y);
            double worldZ = region.WorldOrigin.z + ((double)regionLocalPosition.z - region.LocalAnchor.z);
            WorldPosition worldPosition = new WorldPosition(worldX, worldY, worldZ);
            if (!worldPosition.IsFinite)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(regionLocalPosition),
                    "The reconstructed world position must be finite.");
            }

            return worldPosition;
        }

        private static bool ContainsWorldPosition(
            SimulationRegionDescriptor region,
            WorldPosition worldPosition)
        {
            double maxX = region.WorldOrigin.x + region.WidthMetres;
            double maxZ = region.WorldOrigin.z + region.DepthMetres;
            return worldPosition.x >= region.WorldOrigin.x
                && worldPosition.x < maxX
                && worldPosition.z >= region.WorldOrigin.z
                && worldPosition.z < maxZ;
        }

        private static bool ContainsRegionLocalPosition(
            SimulationRegionDescriptor region,
            Vector3 regionLocalPosition)
        {
            double maxX = (double)region.LocalAnchor.x + region.WidthMetres;
            double maxZ = (double)region.LocalAnchor.z + region.DepthMetres;
            return regionLocalPosition.x >= region.LocalAnchor.x
                && regionLocalPosition.x < maxX
                && regionLocalPosition.z >= region.LocalAnchor.z
                && regionLocalPosition.z < maxZ;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
                && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }

        private static bool FitsFiniteFloat(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value)
                && value >= -float.MaxValue && value <= float.MaxValue;
        }
    }
}
