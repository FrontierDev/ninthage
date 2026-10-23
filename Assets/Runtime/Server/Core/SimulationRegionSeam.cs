using Game.Shared.Utility;
using System;

namespace Game.Server.Core
{
    /// <summary>Describes one validated absolute handoff seam between adjacent simulation regions.</summary>
    public readonly struct SimulationRegionSeam
    {
        /// <summary>Gets the source simulation-region key.</summary>
        public SimulationRegionKey SourceKey { get; }

        /// <summary>Gets the destination simulation-region key.</summary>
        public SimulationRegionKey DestinationKey { get; }

        /// <summary>Gets the source-region approach position in absolute coordinates.</summary>
        public WorldPosition ApproachAbsolute { get; }

        /// <summary>Gets the destination-region landing position in absolute coordinates.</summary>
        public WorldPosition LandingAbsolute { get; }

        /// <summary>Gets the onward position inside the destination region in absolute coordinates.</summary>
        public WorldPosition OnwardAbsolute { get; }

        /// <summary>Gets the source NavMesh revision used for validation.</summary>
        public long SourceNavigationRevision { get; }

        /// <summary>Gets the destination NavMesh revision used for validation.</summary>
        public long DestinationNavigationRevision { get; }

        /// <summary>Gets the registered NavMesh agent type.</summary>
        public int AgentTypeID { get; }

        /// <summary>Gets the NavMesh area mask used for validation.</summary>
        public int AreaMask { get; }

        /// <summary>Gets the combined complete-path length in metres.</summary>
        public double TotalPathLength { get; }

        /// <summary>Gets the deterministic candidate ordinal used to produce the seam.</summary>
        public int CandidateOrdinal { get; }

        /// <summary>Creates a validated immutable simulation-region seam.</summary>
        /// <param name="sourceKey">The source simulation-region key.</param>
        /// <param name="destinationKey">The destination simulation-region key.</param>
        /// <param name="approachAbsolute">The source-region approach position.</param>
        /// <param name="landingAbsolute">The destination-region landing position.</param>
        /// <param name="onwardAbsolute">The destination-region onward position.</param>
        /// <param name="sourceNavigationRevision">The source NavMesh revision.</param>
        /// <param name="destinationNavigationRevision">The destination NavMesh revision.</param>
        /// <param name="agentTypeID">The NavMesh agent type.</param>
        /// <param name="areaMask">The NavMesh area mask.</param>
        /// <param name="totalPathLength">The combined complete-path length.</param>
        /// <param name="candidateOrdinal">The deterministic candidate ordinal.</param>
        /// <exception cref="ArgumentException">Thrown when keys or positions are invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when revisions, agent type, cost or ordinal are invalid.</exception>
        public SimulationRegionSeam(
            SimulationRegionKey sourceKey,
            SimulationRegionKey destinationKey,
            WorldPosition approachAbsolute,
            WorldPosition landingAbsolute,
            WorldPosition onwardAbsolute,
            long sourceNavigationRevision,
            long destinationNavigationRevision,
            int agentTypeID,
            int areaMask,
            double totalPathLength,
            int candidateOrdinal)
        {
            if (!sourceKey.Context.IsValid || !destinationKey.Context.IsValid)
                throw new ArgumentException("Both simulation-region contexts must be valid.");
            if (!sourceKey.Context.IsCompatibleWith(destinationKey.Context))
                throw new ArgumentException("Simulation-region seam contexts must be compatible.");

            long dx = (long)destinationKey.RegionCoordinate.x - sourceKey.RegionCoordinate.x;
            long dz = (long)destinationKey.RegionCoordinate.y - sourceKey.RegionCoordinate.y;
            if (sourceKey == destinationKey || Math.Abs(dx) + Math.Abs(dz) != 1L)
                throw new ArgumentException("Simulation-region seam keys must be distinct cardinal neighbours.");

            if (!approachAbsolute.IsFinite)
                throw new ArgumentException("The seam approach position must be finite.", nameof(approachAbsolute));
            if (!landingAbsolute.IsFinite)
                throw new ArgumentException("The seam landing position must be finite.", nameof(landingAbsolute));
            if (!onwardAbsolute.IsFinite)
                throw new ArgumentException("The seam onward position must be finite.", nameof(onwardAbsolute));
            if (sourceNavigationRevision < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceNavigationRevision));
            if (destinationNavigationRevision < 0)
                throw new ArgumentOutOfRangeException(nameof(destinationNavigationRevision));
            if (agentTypeID < 0)
                throw new ArgumentOutOfRangeException(nameof(agentTypeID));
            if (candidateOrdinal < 0)
                throw new ArgumentOutOfRangeException(nameof(candidateOrdinal));
            if (double.IsNaN(totalPathLength)
                || double.IsInfinity(totalPathLength)
                || totalPathLength < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(totalPathLength));
            }

            SourceKey = sourceKey;
            DestinationKey = destinationKey;
            ApproachAbsolute = approachAbsolute;
            LandingAbsolute = landingAbsolute;
            OnwardAbsolute = onwardAbsolute;
            SourceNavigationRevision = sourceNavigationRevision;
            DestinationNavigationRevision = destinationNavigationRevision;
            AgentTypeID = agentTypeID;
            AreaMask = areaMask;
            TotalPathLength = totalPathLength;
            CandidateOrdinal = candidateOrdinal;
        }
    }
}
