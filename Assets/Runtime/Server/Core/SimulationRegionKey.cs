using Game.Shared.Runtime.Actors;
using System;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>Identifies a coarse server simulation region within an authoritative runtime context.</summary>
    public readonly struct SimulationRegionKey : IEquatable<SimulationRegionKey>
    {
        /// <summary>Gets the authoritative world, map and runtime instance context.</summary>
        public ActorSpatialContext Context { get; }

        /// <summary>Gets the coarse simulation-region coordinate.</summary>
        public Vector2Int RegionCoordinate { get; }

        /// <summary>
        /// Creates a context-qualified simulation-region key.
        /// </summary>
        /// <param name="context">The authoritative runtime context.</param>
        /// <param name="regionCoordinate">The coarse simulation-region coordinate.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="context"/> is invalid.</exception>
        public SimulationRegionKey(ActorSpatialContext context, Vector2Int regionCoordinate)
        {
            if (!context.IsValid)
                throw new ArgumentException("A valid world, map and instance context is required.", nameof(context));

            Context = context;
            RegionCoordinate = regionCoordinate;
        }

        /// <summary>
        /// Creates the simulation-region key containing an environment tile.
        /// </summary>
        /// <param name="context">The authoritative runtime context.</param>
        /// <param name="tileCoordinate">The environment-tile coordinate.</param>
        /// <param name="regionTilesX">The number of environment tiles in a region along the X axis.</param>
        /// <param name="regionTilesZ">The number of environment tiles in a region along the Z axis.</param>
        /// <returns>The context-qualified simulation-region key for the tile.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="context"/> is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when either region tile span is not positive.</exception>
        public static SimulationRegionKey FromEnvironmentTile(
            ActorSpatialContext context,
            Vector2Int tileCoordinate,
            int regionTilesX,
            int regionTilesZ)
        {
            if (!context.IsValid)
                throw new ArgumentException("A valid world, map and instance context is required.", nameof(context));
            if (regionTilesX <= 0)
                throw new ArgumentOutOfRangeException(nameof(regionTilesX), regionTilesX, "The X region tile span must be positive.");
            if (regionTilesZ <= 0)
                throw new ArgumentOutOfRangeException(nameof(regionTilesZ), regionTilesZ, "The Z region tile span must be positive.");

            return new SimulationRegionKey(
                context,
                new Vector2Int(
                    FloorDivide(tileCoordinate.x, regionTilesX),
                    FloorDivide(tileCoordinate.y, regionTilesZ)));
        }

        /// <summary>Determines whether another key identifies the same context and region.</summary>
        public bool Equals(SimulationRegionKey other)
        {
            return Context == other.Context && RegionCoordinate == other.RegionCoordinate;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is SimulationRegionKey other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (Context.GetHashCode() * 397) ^ RegionCoordinate.GetHashCode();
            }
        }

        /// <summary>Determines whether two keys identify the same context and region.</summary>
        public static bool operator ==(SimulationRegionKey left, SimulationRegionKey right)
        {
            return left.Equals(right);
        }

        /// <summary>Determines whether two keys identify different contexts or regions.</summary>
        public static bool operator !=(SimulationRegionKey left, SimulationRegionKey right)
        {
            return !left.Equals(right);
        }

        private static int FloorDivide(int value, int divisor)
        {
            int quotient = value / divisor;
            if (value % divisor < 0)
                quotient--;

            return quotient;
        }
    }
}
