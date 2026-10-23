using Game.Shared;
using Game.Shared.Utility;
using System;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>Describes the immutable physical frame for a simulation region.</summary>
    public readonly struct SimulationRegionDescriptor
    {
        /// <summary>Gets the context-qualified simulation-region identity.</summary>
        public SimulationRegionKey Key { get; }

        /// <summary>Gets the number of environment tiles in the region along the X axis.</summary>
        public int TileSpanX { get; }

        /// <summary>Gets the number of environment tiles in the region along the Z axis.</summary>
        public int TileSpanZ { get; }

        /// <summary>Gets the absolute double-precision origin of the region.</summary>
        public WorldPosition WorldOrigin { get; }

        /// <summary>Gets the local Unity-space anchor of the region.</summary>
        public Vector3 LocalAnchor { get; }

        /// <summary>Gets the region width in metres.</summary>
        public double WidthMetres { get; }

        /// <summary>Gets the region depth in metres.</summary>
        public double DepthMetres { get; }

        /// <summary>
        /// Creates an immutable physical-frame descriptor for a simulation region.
        /// </summary>
        /// <param name="key">The context-qualified simulation-region identity.</param>
        /// <param name="tileSpanX">The number of environment tiles along the X axis.</param>
        /// <param name="tileSpanZ">The number of environment tiles along the Z axis.</param>
        /// <param name="localAnchor">The local Unity-space anchor for the region.</param>
        /// <exception cref="ArgumentException">Thrown when the key context or local anchor is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when a tile span is not positive.</exception>
        /// <exception cref="InvalidOperationException">Thrown when shared configuration is not initialized or valid.</exception>
        public SimulationRegionDescriptor(
            SimulationRegionKey key,
            int tileSpanX,
            int tileSpanZ,
            Vector3 localAnchor)
        {
            if (!key.Context.IsValid)
                throw new ArgumentException("A valid simulation-region context is required.", nameof(key));
            if (tileSpanX <= 0)
                throw new ArgumentOutOfRangeException(nameof(tileSpanX), tileSpanX, "The X tile span must be positive.");
            if (tileSpanZ <= 0)
                throw new ArgumentOutOfRangeException(nameof(tileSpanZ), tileSpanZ, "The Z tile span must be positive.");
            if (!IsFinite(localAnchor))
                throw new ArgumentException("The local anchor must have finite components.", nameof(localAnchor));
            if (!GameConfigurationManager.IsInitialized || GameConfigurationManager.Config == null)
            {
                throw new InvalidOperationException(
                    "Simulation-region descriptors require initialized game configuration.");
            }

            GameConfigurationManager.Config.ValidateWorldGridConfiguration();
            int cellSizeMetres = GameConfigurationManager.Config.ChunkSize;
            double widthMetres = (double)tileSpanX * cellSizeMetres;
            double depthMetres = (double)tileSpanZ * cellSizeMetres;

            Key = key;
            TileSpanX = tileSpanX;
            TileSpanZ = tileSpanZ;
            WorldOrigin = new WorldPosition(
                (double)key.RegionCoordinate.x * widthMetres,
                0d,
                (double)key.RegionCoordinate.y * depthMetres);
            LocalAnchor = localAnchor;
            WidthMetres = widthMetres;
            DepthMetres = depthMetres;
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
                && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }
    }
}
