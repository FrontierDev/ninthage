using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Server.Core
{
    public sealed partial class ServerPhysicsManager
    {
        private static NavMeshBuildSettings GetRegionNavMeshBuildSettings()
        {
            var settings = NavMesh.GetSettingsByID(0);
            if (settings.agentTypeID < 0)
            {
                throw new InvalidOperationException(
                    "The default Unity NavMesh agent type is not configured.");
            }

            return settings;
        }

        private static List<NavMeshBuildSource> CollectRegionNavMeshSources(
            ActiveSimulationRegion activeRegion)
        {
            if (activeRegion == null)
                throw new ArgumentNullException(nameof(activeRegion));

            var sources = new List<NavMeshBuildSource>();
            var chunkKeys = new List<WorldChunkKey>(activeRegion.NavigationRoots.Keys);
            chunkKeys.Sort((left, right) =>
            {
                int xComparison = left.ChunkCoord.x.CompareTo(right.ChunkCoord.x);
                return xComparison != 0
                    ? xComparison
                    : left.ChunkCoord.y.CompareTo(right.ChunkCoord.y);
            });

            foreach (var chunkKey in chunkKeys)
            {
                if (activeRegion.PendingNavigationUnloads.Contains(chunkKey))
                    continue;

                if (!activeRegion.NavigationRoots.TryGetValue(chunkKey, out var roots)
                    || roots == null)
                {
                    throw new InvalidOperationException(
                        $"Simulation region {activeRegion.Descriptor.Key} has no navigation roots for environment tile {chunkKey.ChunkCoord}.");
                }

                foreach (var root in roots)
                {
                    if (root == null)
                    {
                        throw new InvalidOperationException(
                            $"Environment tile {chunkKey.ChunkCoord} has a null navigation root.");
                    }

                    var markups = new List<NavMeshBuildMarkup>();
                    var rootSources = new List<NavMeshBuildSource>();
                    NavMeshBuilder.CollectSources(
                        root.transform,
                        ~0,
                        NavMeshCollectGeometry.PhysicsColliders,
                        0,
                        markups,
                        rootSources);
                    sources.AddRange(rootSources);
                }
            }

            return sources;
        }

        private void UpdateRegionNavMesh(ActiveSimulationRegion activeRegion)
        {
            if (activeRegion == null)
                throw new ArgumentNullException(nameof(activeRegion));

            var sources = CollectRegionNavMeshSources(activeRegion);
            if (sources.Count == 0)
            {
                bool hadNavigation = activeRegion.NavigationData != null
                    || activeRegion.NavigationInstance.valid;
                if (activeRegion.NavigationInstance.valid)
                    activeRegion.NavigationInstance.Remove();
                activeRegion.NavigationInstance = default;
                if (activeRegion.NavigationData != null)
                    Destroy(activeRegion.NavigationData);
                activeRegion.NavigationData = null;
                if (hadNavigation)
                {
                    checked
                    {
                        activeRegion.NavigationRevision++;
                    }
                }
                return;
            }

            var settings = GetRegionNavMeshBuildSettings();
            if (activeRegion.NavigationData == null)
            {
                activeRegion.NavigationData = new NavMeshData(settings.agentTypeID);
            }

            bool updated = NavMeshBuilder.UpdateNavMeshData(
                activeRegion.NavigationData,
                settings,
                sources,
                default);
            if (!updated)
            {
                throw new InvalidOperationException(
                    $"Unity could not update the NavMesh data for simulation region {activeRegion.Descriptor.Key}.");
            }

            if (!activeRegion.NavigationInstance.valid)
            {
                activeRegion.NavigationInstance = NavMesh.AddNavMeshData(activeRegion.NavigationData);
                if (!activeRegion.NavigationInstance.valid)
                {
                    throw new InvalidOperationException(
                        $"Unity rejected NavMesh data registration for simulation region {activeRegion.Descriptor.Key}.");
                }
            }

            checked
            {
                activeRegion.NavigationRevision++;
            }
        }

        /// <summary>
        /// Attempts to get the registered NavMesh representation for an actor's existing simulation region.
        /// </summary>
        /// <param name="actorID">The nonempty actor identity.</param>
        /// <param name="descriptor">Receives the actor's active simulation-region descriptor when available.</param>
        /// <param name="agentTypeID">Receives the registered region NavMesh agent type when available.</param>
        /// <returns>
        /// <see langword="true"/> when the actor is physically bound to an active region with registered NavMesh data;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="actorID"/> is empty.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the actor binding references a missing active simulation region.
        /// </exception>
        public bool TryGetActorNavigationRegion(
            Guid actorID,
            out SimulationRegionDescriptor descriptor,
            out int agentTypeID)
        {
            EnsureInitialized();

            if (actorID == Guid.Empty)
                throw new ArgumentException("A nonempty actor ID is required.", nameof(actorID));

            if (!TryGetActorRegionDescriptor(actorID, out descriptor))
            {
                descriptor = default;
                agentTypeID = default;
                return false;
            }

            if (!_activeRegions.TryGetValue(descriptor.Key, out ActiveSimulationRegion activeRegion))
            {
                throw new InvalidOperationException(
                    $"Actor {actorID} references missing active simulation region {descriptor.Key}.");
            }

            if (activeRegion.NavigationData == null
                || !activeRegion.NavigationInstance.valid)
            {
                agentTypeID = default;
                return false;
            }

            NavMeshBuildSettings settings = GetRegionNavMeshBuildSettings();
            agentTypeID = settings.agentTypeID;
            return true;
        }

        /// <summary>
        /// Checks whether a candidate landing position has a usable NavMesh admission from one
        /// active region to an adjacent active region without creating or mutating physical state.
        /// </summary>
        /// <param name="sourceKey">The context-qualified source simulation-region key.</param>
        /// <param name="landingAbsolute">The candidate absolute landing position.</param>
        /// <param name="agentTypeID">The NavMesh agent type required by the caller.</param>
        /// <param name="source">Receives the existing source-region descriptor on success.</param>
        /// <param name="destination">Receives the existing destination-region descriptor on success.</param>
        /// <param name="sourceRevision">Receives the source region's NavMesh revision on success.</param>
        /// <param name="destinationRevision">Receives the destination region's NavMesh revision on success.</param>
        /// <returns>
        /// <see langword="true"/> when the adjacent regions and exact landing tile are ready;
        /// otherwise <see langword="false"/> when a required physical resource is unavailable.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the source context is invalid, the landing position is non-finite or the
        /// destination region is not cardinally adjacent.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="agentTypeID"/> is negative.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the environment binding registries disagree about the landing tile.
        /// </exception>
        public bool TryGetAdjacentNavigationAdmission(
            SimulationRegionKey sourceKey,
            WorldPosition landingAbsolute,
            int agentTypeID,
            out SimulationRegionDescriptor source,
            out SimulationRegionDescriptor destination,
            out long sourceRevision,
            out long destinationRevision)
        {
            EnsureInitialized();
            source = default;
            destination = default;
            sourceRevision = default;
            destinationRevision = default;

            if (!sourceKey.Context.IsValid)
                throw new ArgumentException("The source simulation-region context must be valid.", nameof(sourceKey));
            if (!landingAbsolute.IsFinite)
                throw new ArgumentException("The landing absolute position must be finite.", nameof(landingAbsolute));
            if (agentTypeID < 0)
                throw new ArgumentOutOfRangeException(nameof(agentTypeID), agentTypeID, "The NavMesh agent type must not be negative.");

            SimulationRegionKey destinationKey = GetRegionKey(
                sourceKey.Context,
                landingAbsolute.Chunk);
            long dx = (long)destinationKey.RegionCoordinate.x
                - sourceKey.RegionCoordinate.x;
            long dz = (long)destinationKey.RegionCoordinate.y
                - sourceKey.RegionCoordinate.y;
            if (Math.Abs(dx) + Math.Abs(dz) != 1L)
            {
                throw new ArgumentException(
                    $"The landing position resolves to non-adjacent simulation region {destinationKey} from source region {sourceKey}.",
                    nameof(landingAbsolute));
            }

            if (!_activeRegions.TryGetValue(sourceKey, out var sourceRegion)
                || !_activeRegions.TryGetValue(destinationKey, out var destinationRegion))
            {
                return false;
            }

            if (sourceRegion.NavigationData == null
                || !sourceRegion.NavigationInstance.valid
                || destinationRegion.NavigationData == null
                || !destinationRegion.NavigationInstance.valid)
            {
                return false;
            }

            var settings = GetRegionNavMeshBuildSettings();
            if (agentTypeID != settings.agentTypeID)
                return false;

            var landingTile = new WorldChunkKey(sourceKey.Context, landingAbsolute.Chunk);
            if (!_environmentRegionBindings.TryGetValue(landingTile, out var boundKey))
                return false;
            if (boundKey != destinationKey)
            {
                throw new InvalidOperationException(
                    $"Environment tile {landingTile.ChunkCoord} is bound to simulation region {boundKey}, expected {destinationKey}.");
            }

            if (!destinationRegion.BoundEnvironmentTiles.Contains(landingTile)
                || !destinationRegion.NavigationRoots.ContainsKey(landingTile))
            {
                throw new InvalidOperationException(
                    $"Environment tile {landingTile.ChunkCoord} is registered in the binding registry but missing destination-region membership or navigation roots.");
            }
            if (destinationRegion.PendingNavigationUnloads.Contains(landingTile))
                return false;

            ToRegionLocal(destinationRegion.Descriptor, landingAbsolute);
            source = sourceRegion.Descriptor;
            destination = destinationRegion.Descriptor;
            sourceRevision = sourceRegion.NavigationRevision;
            destinationRevision = destinationRegion.NavigationRevision;
            return true;
        }

        /// <summary>
        /// Scans a bounded deterministic batch of candidate seams between adjacent active regions.
        /// </summary>
        /// <param name="sourceAgent">The enabled source-region NavMesh agent.</param>
        /// <param name="sourceKey">The source simulation-region key.</param>
        /// <param name="destinationKey">The cardinally adjacent destination key.</param>
        /// <param name="targetAbsolute">The latest absolute target position.</param>
        /// <param name="nextCandidateIndex">The first candidate ordinal to examine.</param>
        /// <param name="candidateBudget">The maximum number of ordinals to examine.</param>
        /// <param name="bestCandidate">Receives the lowest-cost valid candidate in this batch.</param>
        /// <param name="nextIndex">Receives the next candidate ordinal.</param>
        /// <param name="exhausted">Receives whether every permitted ordinal was examined.</param>
        /// <param name="navigationAvailable">Receives whether both region NavMeshes were available.</param>
        /// <param name="hadUnavailableCandidate">Receives whether an exact landing tile was unavailable.</param>
        /// <returns><see langword="true"/> when this batch contains a valid candidate.</returns>
        public bool ScanAdjacentRegionSeams(
            NavMeshAgent sourceAgent,
            SimulationRegionKey sourceKey,
            SimulationRegionKey destinationKey,
            WorldPosition targetAbsolute,
            int nextCandidateIndex,
            int candidateBudget,
            out SimulationRegionSeam bestCandidate,
            out int nextIndex,
            out bool exhausted,
            out bool navigationAvailable,
            out bool hadUnavailableCandidate)
        {
            EnsureInitialized();
            bestCandidate = default;
            nextIndex = nextCandidateIndex;
            exhausted = false;
            navigationAvailable = false;
            hadUnavailableCandidate = false;

            ValidateAdjacentRegionRequest(sourceKey, destinationKey);
            if (!targetAbsolute.IsFinite)
                throw new ArgumentException("The target absolute position must be finite.", nameof(targetAbsolute));
            if (nextCandidateIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(nextCandidateIndex));
            if (candidateBudget <= 0)
                throw new ArgumentOutOfRangeException(nameof(candidateBudget));

            NavMeshBuildSettings build;
            float voxel;
            float radius;
            float inset;
            float spacing;
            float sampleRadius;
            float arrivalTolerance;
            ResolveNavigationSeamGeometry(
                sourceAgent,
                out build,
                out voxel,
                out radius,
                out inset,
                out spacing,
                out sampleRadius,
                out arrivalTolerance);

            if (!_activeRegions.TryGetValue(sourceKey, out var sourceRegion)
                || !_activeRegions.TryGetValue(destinationKey, out var destinationRegion))
            {
                return false;
            }
            if (sourceRegion.Descriptor.Key != sourceKey
                || destinationRegion.Descriptor.Key != destinationKey)
            {
                throw new InvalidOperationException(
                    "The active simulation-region registry contains a descriptor with a mismatched key.");
            }
            if (sourceRegion.NavigationData == null
                || !sourceRegion.NavigationInstance.valid
                || destinationRegion.NavigationData == null
                || !destinationRegion.NavigationInstance.valid)
            {
                return false;
            }

            navigationAvailable = true;
            GetAdjacentRegionBoundary(
                sourceRegion.Descriptor,
                destinationRegion.Descriptor,
                out bool alongX,
                out int direction,
                out double boundary,
                out double borderMin,
                out double borderMax);

            var sourceAbsolute = ToWorld(sourceRegion.Descriptor, sourceAgent.nextPosition);
            double projectedAlong = ProjectAlongBorder(
                sourceAbsolute,
                targetAbsolute,
                alongX,
                boundary);
            double permittedMin = borderMin + inset;
            double permittedMax = borderMax - inset;
            if (!IsFinite(permittedMin)
                || !IsFinite(permittedMax)
                || permittedMin > permittedMax)
            {
                exhausted = true;
                return false;
            }

            projectedAlong = Math.Max(permittedMin, Math.Min(permittedMax, projectedAlong));
            int lastCandidateOrdinal = GetLastCandidateOrdinal(
                projectedAlong,
                permittedMin,
                permittedMax,
                spacing);
            if (nextCandidateIndex > lastCandidateOrdinal)
            {
                exhausted = true;
                return false;
            }

            bool foundCandidate = false;
            long ordinal = nextCandidateIndex;
            long endExclusive = Math.Min(
                (long)lastCandidateOrdinal + 1L,
                (long)nextCandidateIndex + candidateBudget);
            for (; ordinal < endExclusive; ordinal++)
            {
                int candidateOrdinal = checked((int)ordinal);
                if (!TryGetCandidateAlongCoordinate(
                        candidateOrdinal,
                        projectedAlong,
                        permittedMin,
                        permittedMax,
                        spacing,
                        out double alongCoordinate))
                {
                    continue;
                }

                if (!TryEvaluateRegionSeamCandidate(
                        sourceAgent,
                        sourceKey,
                        destinationKey,
                        sourceRegion.Descriptor,
                        destinationRegion.Descriptor,
                        destinationRegion,
                        targetAbsolute,
                        sourceAbsolute,
                        candidateOrdinal,
                        alongCoordinate,
                        alongX,
                        direction,
                        build,
                        voxel,
                        radius,
                        inset,
                        sampleRadius,
                        arrivalTolerance,
                        out var candidate,
                        out bool unavailableCandidate))
                {
                    hadUnavailableCandidate |= unavailableCandidate;
                    continue;
                }

                if (!foundCandidate || IsBetterSeamCandidate(candidate, bestCandidate, alongX))
                {
                    bestCandidate = candidate;
                    foundCandidate = true;
                }
            }

            nextIndex = checked((int)ordinal);
            exhausted = nextIndex > lastCandidateOrdinal;
            return foundCandidate;
        }

        /// <summary>Revalidates one stored seam against current region NavMesh and target state.</summary>
        /// <param name="sourceAgent">The enabled source-region NavMesh agent.</param>
        /// <param name="seam">The previously evaluated seam.</param>
        /// <param name="latestTargetAbsolute">The latest absolute target position.</param>
        /// <param name="currentOnwardAbsolute">Receives the current destination onward position.</param>
        /// <returns><see langword="true"/> when the stored seam remains valid.</returns>
        public bool ValidateRegionSeam(
            NavMeshAgent sourceAgent,
            SimulationRegionSeam seam,
            WorldPosition latestTargetAbsolute,
            out WorldPosition currentOnwardAbsolute)
        {
            EnsureInitialized();
            currentOnwardAbsolute = default;
            ValidateSeamValue(seam);
            if (!latestTargetAbsolute.IsFinite)
                throw new ArgumentException("The latest target absolute position must be finite.", nameof(latestTargetAbsolute));

            NavMeshBuildSettings build;
            float voxel;
            float radius;
            float inset;
            float sampleRadius;
            float arrivalTolerance;
            ResolveNavigationSeamGeometry(
                sourceAgent,
                out build,
                out voxel,
                out radius,
                out inset,
                out _,
                out sampleRadius,
                out arrivalTolerance);
            if (sourceAgent.agentTypeID != seam.AgentTypeID
                || sourceAgent.areaMask != seam.AreaMask)
            {
                return false;
            }

            if (!_activeRegions.TryGetValue(seam.SourceKey, out var sourceRegion)
                || !_activeRegions.TryGetValue(seam.DestinationKey, out var destinationRegion))
            {
                return false;
            }
            if (sourceRegion.Descriptor.Key != seam.SourceKey
                || destinationRegion.Descriptor.Key != seam.DestinationKey
                || sourceRegion.NavigationData == null
                || !sourceRegion.NavigationInstance.valid
                || destinationRegion.NavigationData == null
                || !destinationRegion.NavigationInstance.valid)
            {
                return false;
            }
            if (sourceRegion.NavigationRevision != seam.SourceNavigationRevision
                || destinationRegion.NavigationRevision != seam.DestinationNavigationRevision)
            {
                return false;
            }

            GetAdjacentRegionBoundary(
                sourceRegion.Descriptor,
                destinationRegion.Descriptor,
                out bool alongX,
                out int direction,
                out double boundary,
                out double borderMin,
                out double borderMax);
            double alongCoordinate = alongX
                ? seam.ApproachAbsolute.z
                : seam.ApproachAbsolute.x;
            double permittedMin = borderMin + inset;
            double permittedMax = borderMax - inset;
            if (!IsFinite(alongCoordinate)
                || alongCoordinate < permittedMin
                || alongCoordinate > permittedMax
                || !IsStoredSeamGeometry(
                    seam,
                    alongX,
                    direction,
                    boundary,
                    inset))
            {
                throw new InvalidOperationException("The stored simulation-region seam geometry is internally inconsistent.");
            }

            var sourceAbsolute = ToWorld(sourceRegion.Descriptor, sourceAgent.nextPosition);
            if (!TryEvaluateRegionSeamCandidate(
                    sourceAgent,
                    seam.SourceKey,
                    seam.DestinationKey,
                    sourceRegion.Descriptor,
                    destinationRegion.Descriptor,
                    destinationRegion,
                    latestTargetAbsolute,
                    sourceAbsolute,
                    seam.CandidateOrdinal,
                    alongCoordinate,
                    alongX,
                    direction,
                    build,
                    voxel,
                    radius,
                    inset,
                    sampleRadius,
                    arrivalTolerance,
                    out var evaluatedSeam,
                    out _))
            {
                return false;
            }

            if (evaluatedSeam.SourceNavigationRevision != seam.SourceNavigationRevision
                || evaluatedSeam.DestinationNavigationRevision != seam.DestinationNavigationRevision)
            {
                return false;
            }
            if (evaluatedSeam.ApproachAbsolute.x != seam.ApproachAbsolute.x
                || evaluatedSeam.ApproachAbsolute.z != seam.ApproachAbsolute.z
                || evaluatedSeam.LandingAbsolute.x != seam.LandingAbsolute.x
                || evaluatedSeam.LandingAbsolute.z != seam.LandingAbsolute.z)
            {
                throw new InvalidOperationException("The stored simulation-region seam no longer matches its candidate ordinal.");
            }

            currentOnwardAbsolute = evaluatedSeam.OnwardAbsolute;
            return true;
        }

        private static void ValidateAdjacentRegionRequest(
            SimulationRegionKey sourceKey,
            SimulationRegionKey destinationKey)
        {
            if (!sourceKey.Context.IsValid || !destinationKey.Context.IsValid)
                throw new ArgumentException("Both simulation-region contexts must be valid.");
            if (!sourceKey.Context.IsCompatibleWith(destinationKey.Context))
                throw new ArgumentException("Simulation-region seam contexts must be compatible.");

            long dx = (long)destinationKey.RegionCoordinate.x - sourceKey.RegionCoordinate.x;
            long dz = (long)destinationKey.RegionCoordinate.y - sourceKey.RegionCoordinate.y;
            if (sourceKey == destinationKey || Math.Abs(dx) + Math.Abs(dz) != 1L)
                throw new ArgumentException("Simulation-region seam keys must be distinct cardinal neighbours.");
        }

        private static void ValidateSeamValue(SimulationRegionSeam seam)
        {
            ValidateAdjacentRegionRequest(seam.SourceKey, seam.DestinationKey);
            if (!seam.ApproachAbsolute.IsFinite
                || !seam.LandingAbsolute.IsFinite
                || !seam.OnwardAbsolute.IsFinite)
            {
                throw new ArgumentException("The stored seam positions must be finite.", nameof(seam));
            }
            if (seam.SourceNavigationRevision < 0
                || seam.DestinationNavigationRevision < 0
                || seam.AgentTypeID < 0
                || seam.CandidateOrdinal < 0
                || double.IsNaN(seam.TotalPathLength)
                || double.IsInfinity(seam.TotalPathLength)
                || seam.TotalPathLength < 0d)
            {
                throw new ArgumentException("The stored seam contains invalid revision, type, ordinal or cost data.", nameof(seam));
            }
        }

        private static void ResolveNavigationSeamGeometry(
            NavMeshAgent sourceAgent,
            out NavMeshBuildSettings build,
            out float voxel,
            out float radius,
            out float inset,
            out float spacing,
            out float sampleRadius,
            out float arrivalTolerance)
        {
            if (sourceAgent == null)
                throw new ArgumentNullException(nameof(sourceAgent));
            if (!sourceAgent.enabled || !sourceAgent.gameObject.activeInHierarchy)
                throw new InvalidOperationException("Region seam evaluation requires an enabled active NavMeshAgent.");
            if (!sourceAgent.isOnNavMesh)
                throw new InvalidOperationException("Region seam evaluation requires a NavMeshAgent attached to a NavMesh.");

            build = GetRegionNavMeshBuildSettings();
            if (sourceAgent.agentTypeID != build.agentTypeID)
            {
                throw new ArgumentException(
                    $"The source NavMeshAgent type {sourceAgent.agentTypeID} does not match the registered type {build.agentTypeID}.",
                    nameof(sourceAgent));
            }

            voxel = build.overrideVoxelSize
                ? build.voxelSize
                : build.agentRadius / 3f;
            radius = Mathf.Max(sourceAgent.radius, build.agentRadius);
            inset = radius + 2f * voxel;
            spacing = Mathf.Max(2f * radius, 4f * voxel);
            sampleRadius = 2f * voxel;
            arrivalTolerance = Mathf.Max(radius * 0.5f, 2f * voxel);
            if (!IsPositiveFinite(voxel)
                || !IsPositiveFinite(radius)
                || !IsPositiveFinite(inset)
                || !IsPositiveFinite(spacing)
                || !IsPositiveFinite(sampleRadius)
                || !IsPositiveFinite(arrivalTolerance))
            {
                throw new InvalidOperationException("Region seam navigation resolution produced a non-positive or non-finite value.");
            }
        }

        private bool TryEvaluateRegionSeamCandidate(
            NavMeshAgent sourceAgent,
            SimulationRegionKey sourceKey,
            SimulationRegionKey destinationKey,
            SimulationRegionDescriptor sourceRegion,
            SimulationRegionDescriptor destinationRegion,
            ActiveSimulationRegion destinationActiveRegion,
            WorldPosition targetAbsolute,
            WorldPosition sourceAbsolute,
            int candidateOrdinal,
            double alongCoordinate,
            bool alongX,
            int direction,
            NavMeshBuildSettings build,
            float voxel,
            float radius,
            float inset,
            float sampleRadius,
            float arrivalTolerance,
            out SimulationRegionSeam candidate,
            out bool unavailableCandidate)
        {
            candidate = default;
            unavailableCandidate = false;

            GetAdjacentRegionBoundary(
                sourceRegion,
                destinationRegion,
                out _,
                out _,
                out double boundary,
                out _,
                out _);
            WorldPosition sourceNominal = CreateSeamPosition(
                boundary,
                alongCoordinate,
                sourceAbsolute.y,
                alongX,
                direction,
                inset,
                false);
            WorldPosition destinationNominal = CreateSeamPosition(
                boundary,
                alongCoordinate,
                sourceAbsolute.y,
                alongX,
                direction,
                inset,
                true);
            Vector3 sourceNominalLocal = ToRegionLocal(sourceRegion, sourceNominal);
            Vector3 destinationNominalLocal = ToRegionLocal(destinationRegion, destinationNominal);
            var filter = new NavMeshQueryFilter
            {
                agentTypeID = sourceAgent.agentTypeID,
                areaMask = sourceAgent.areaMask
            };

            if (!TrySampleNavigationPoint(
                    sourceRegion,
                    sourceNominal,
                    sourceNominalLocal,
                    filter,
                    sampleRadius,
                    alongX,
                    alongCoordinate,
                    out var sourceApproach,
                    out var sourceApproachLocal)
                || !TrySampleNavigationPoint(
                    destinationRegion,
                    destinationNominal,
                    destinationNominalLocal,
                    filter,
                    sampleRadius,
                    alongX,
                    alongCoordinate,
                    out var destinationLanding,
                    out var destinationLandingLocal))
            {
                return false;
            }

            WorldChunkKey landingTile = new(sourceKey.Context, destinationLanding.Chunk);
            bool landingTileUnavailable = !_environmentRegionBindings.ContainsKey(landingTile)
                || destinationActiveRegion.PendingNavigationUnloads.Contains(landingTile);
            if (!TryGetAdjacentNavigationAdmission(
                    sourceKey,
                    destinationLanding,
                    sourceAgent.agentTypeID,
                    out var admittedSource,
                    out var admittedDestination,
                    out long sourceRevision,
                    out long destinationRevision))
            {
                unavailableCandidate = landingTileUnavailable;
                return false;
            }
            if (admittedSource.Key != sourceKey || admittedDestination.Key != destinationKey)
            {
                throw new InvalidOperationException("Adjacent navigation admission returned an unexpected region pair.");
            }

            if (!TryResolveOnwardPoint(
                    destinationActiveRegion,
                    destinationKey,
                    destinationLanding,
                    targetAbsolute,
                    filter,
                    sampleRadius,
                    radius,
                    out var onwardAbsolute,
                    out var onwardLocal))
            {
                return false;
            }

            var sourcePath = new NavMeshPath();
            var destinationPath = new NavMeshPath();
            bool sourcePathFound = NavMesh.CalculatePath(
                sourceAgent.nextPosition,
                sourceApproachLocal,
                filter,
                sourcePath);
            bool destinationPathFound = NavMesh.CalculatePath(
                destinationLandingLocal,
                onwardLocal,
                filter,
                destinationPath);
            if (!sourcePathFound
                || !destinationPathFound
                || sourcePath.status != NavMeshPathStatus.PathComplete
                || destinationPath.status != NavMeshPathStatus.PathComplete
                || !IsPathEndpointWithinTolerance(sourcePath, sourceApproachLocal, arrivalTolerance)
                || !IsPathEndpointWithinTolerance(destinationPath, onwardLocal, arrivalTolerance))
            {
                return false;
            }

            double totalPathLength = GetPathLength(sourcePath) + GetPathLength(destinationPath);
            if (!IsFinite(totalPathLength)
                || totalPathLength < 0d
                || !ValidateSeamGeometry(
                    sourceAgent,
                    sourceRegion,
                    destinationRegion,
                    sourceApproach,
                    destinationLanding,
                    sourceApproachLocal,
                    destinationLandingLocal,
                    alongX,
                    direction,
                    build,
                    voxel,
                    radius,
                    inset))
            {
                return false;
            }

            candidate = new SimulationRegionSeam(
                sourceKey,
                destinationKey,
                sourceApproach,
                destinationLanding,
                onwardAbsolute,
                sourceRevision,
                destinationRevision,
                sourceAgent.agentTypeID,
                sourceAgent.areaMask,
                totalPathLength,
                candidateOrdinal);
            return true;
        }

        private bool TrySampleNavigationPoint(
            SimulationRegionDescriptor region,
            WorldPosition nominalAbsolute,
            Vector3 nominalLocal,
            NavMeshQueryFilter filter,
            float sampleRadius,
            bool alongX,
            double expectedAlong,
            out WorldPosition sampledAbsolute,
            out Vector3 sampledLocal)
        {
            sampledAbsolute = default;
            sampledLocal = default;
            if (!NavMesh.SamplePosition(
                    nominalLocal,
                    out NavMeshHit hit,
                    sampleRadius,
                    filter))
            {
                return false;
            }

            float horizontalX = hit.position.x - nominalLocal.x;
            float horizontalZ = hit.position.z - nominalLocal.z;
            if (horizontalX * horizontalX + horizontalZ * horizontalZ > sampleRadius * sampleRadius)
                return false;

            try
            {
                sampledAbsolute = ToWorld(region, hit.position);
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }

            double sampledAlong = alongX
                ? sampledAbsolute.z
                : sampledAbsolute.x;
            if (!IsFinite(sampledAlong)
                || Math.Abs(sampledAlong - expectedAlong) > sampleRadius)
            {
                return false;
            }

            sampledAbsolute = new WorldPosition(
                nominalAbsolute.x,
                sampledAbsolute.y,
                nominalAbsolute.z);
            sampledLocal = hit.position;
            return true;
        }

        private bool TryResolveOnwardPoint(
            ActiveSimulationRegion destinationRegion,
            SimulationRegionKey destinationKey,
            WorldPosition landingAbsolute,
            WorldPosition targetAbsolute,
            NavMeshQueryFilter filter,
            float sampleRadius,
            float radius,
            out WorldPosition onwardAbsolute,
            out Vector3 onwardLocal)
        {
            onwardAbsolute = default;
            onwardLocal = default;
            SimulationRegionKey targetKey = GetRegionKey(
                destinationKey.Context,
                targetAbsolute.Chunk);
            WorldPosition onwardNominal = targetAbsolute;
            if (targetKey != destinationKey)
            {
                long dx = (long)targetKey.RegionCoordinate.x - destinationKey.RegionCoordinate.x;
                long dz = (long)targetKey.RegionCoordinate.y - destinationKey.RegionCoordinate.y;
                bool moveAlongX = Math.Abs(dx) >= Math.Abs(dz);
                if (moveAlongX && dx == 0)
                    moveAlongX = false;
                double onwardOffset = 4d * radius;
                double x = landingAbsolute.x;
                double z = landingAbsolute.z;
                if (moveAlongX)
                    x += Math.Sign(dx) * onwardOffset;
                else
                    z += Math.Sign(dz) * onwardOffset;

                x = ClampToHalfOpen(x, destinationRegion.Descriptor.WorldOrigin.x, destinationRegion.Descriptor.WorldOrigin.x + destinationRegion.Descriptor.WidthMetres);
                z = ClampToHalfOpen(z, destinationRegion.Descriptor.WorldOrigin.z, destinationRegion.Descriptor.WorldOrigin.z + destinationRegion.Descriptor.DepthMetres);
                onwardNominal = new WorldPosition(x, landingAbsolute.y, z);
            }

            if (!TryGetBoundNavigationTile(
                    destinationRegion,
                    destinationKey,
                    onwardNominal,
                    out _))
            {
                return false;
            }

            Vector3 onwardNominalLocal = ToRegionLocal(
                destinationRegion.Descriptor,
                onwardNominal);
            if (!NavMesh.SamplePosition(
                    onwardNominalLocal,
                    out NavMeshHit hit,
                    sampleRadius,
                    filter))
            {
                return false;
            }

            float horizontalX = hit.position.x - onwardNominalLocal.x;
            float horizontalZ = hit.position.z - onwardNominalLocal.z;
            if (horizontalX * horizontalX + horizontalZ * horizontalZ > sampleRadius * sampleRadius)
                return false;

            try
            {
                onwardAbsolute = ToWorld(destinationRegion.Descriptor, hit.position);
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
            if (!TryGetBoundNavigationTile(
                    destinationRegion,
                    destinationKey,
                    onwardAbsolute,
                    out _))
            {
                return false;
            }

            onwardLocal = hit.position;
            return true;
        }

        private bool TryGetBoundNavigationTile(
            ActiveSimulationRegion region,
            SimulationRegionKey regionKey,
            WorldPosition absolutePosition,
            out WorldChunkKey tile)
        {
            tile = new WorldChunkKey(regionKey.Context, absolutePosition.Chunk);
            if (!_environmentRegionBindings.TryGetValue(tile, out var boundKey))
                return false;
            if (boundKey != regionKey)
            {
                throw new InvalidOperationException(
                    $"Environment tile {tile.ChunkCoord} is bound to simulation region {boundKey}, expected {regionKey}.");
            }
            if (!region.BoundEnvironmentTiles.Contains(tile)
                || !region.NavigationRoots.ContainsKey(tile))
            {
                throw new InvalidOperationException(
                    $"Environment tile {tile.ChunkCoord} is registered in the binding registry but missing region membership or navigation roots.");
            }
            return !region.PendingNavigationUnloads.Contains(tile);
        }

        private static void GetAdjacentRegionBoundary(
            SimulationRegionDescriptor source,
            SimulationRegionDescriptor destination,
            out bool alongX,
            out int direction,
            out double boundary,
            out double borderMin,
            out double borderMax)
        {
            ValidateAdjacentRegionRequest(source.Key, destination.Key);
            if (source.Key.RegionCoordinate.x != destination.Key.RegionCoordinate.x)
            {
                alongX = false;
                direction = destination.Key.RegionCoordinate.x > source.Key.RegionCoordinate.x ? 1 : -1;
                double sourceBoundary = direction > 0
                    ? source.WorldOrigin.x + source.WidthMetres
                    : source.WorldOrigin.x;
                double destinationBoundary = direction > 0
                    ? destination.WorldOrigin.x
                    : destination.WorldOrigin.x + destination.WidthMetres;
                if (sourceBoundary != destinationBoundary
                    || source.WorldOrigin.z != destination.WorldOrigin.z
                    || source.DepthMetres != destination.DepthMetres)
                {
                    throw new InvalidOperationException("Adjacent simulation-region descriptors do not share a matching X boundary.");
                }

                boundary = sourceBoundary;
                borderMin = source.WorldOrigin.z;
                borderMax = source.WorldOrigin.z + source.DepthMetres;
                return;
            }

            alongX = true;
            direction = destination.Key.RegionCoordinate.y > source.Key.RegionCoordinate.y ? 1 : -1;
            double sourceBoundaryZ = direction > 0
                ? source.WorldOrigin.z + source.DepthMetres
                : source.WorldOrigin.z;
            double destinationBoundaryZ = direction > 0
                ? destination.WorldOrigin.z
                : destination.WorldOrigin.z + destination.DepthMetres;
            if (sourceBoundaryZ != destinationBoundaryZ
                || source.WorldOrigin.x != destination.WorldOrigin.x
                || source.WidthMetres != destination.WidthMetres)
            {
                throw new InvalidOperationException("Adjacent simulation-region descriptors do not share a matching Z boundary.");
            }

            boundary = sourceBoundaryZ;
            borderMin = source.WorldOrigin.x;
            borderMax = source.WorldOrigin.x + source.WidthMetres;
        }

        private static double ProjectAlongBorder(
            WorldPosition sourceAbsolute,
            WorldPosition targetAbsolute,
            bool alongX,
            double boundary)
        {
            double sourceNormal = alongX ? sourceAbsolute.z : sourceAbsolute.x;
            double targetNormal = alongX ? targetAbsolute.z : targetAbsolute.x;
            double sourceAlong = alongX ? sourceAbsolute.x : sourceAbsolute.z;
            double targetAlong = alongX ? targetAbsolute.x : targetAbsolute.z;
            double normalDelta = targetNormal - sourceNormal;
            if (normalDelta == 0d)
                return sourceAlong;

            double fraction = (boundary - sourceNormal) / normalDelta;
            double projected = sourceAlong + (targetAlong - sourceAlong) * fraction;
            if (!IsFinite(projected))
                throw new InvalidOperationException("The target projection onto the region boundary is not finite.");
            return projected;
        }

        private static int GetLastCandidateOrdinal(
            double projectedAlong,
            double permittedMin,
            double permittedMax,
            float spacing)
        {
            long positiveSteps = checked((long)Math.Floor((permittedMax - projectedAlong) / spacing));
            long negativeSteps = checked((long)Math.Floor((projectedAlong - permittedMin) / spacing));
            long lastOrdinal = Math.Max(
                2L * negativeSteps,
                2L * positiveSteps - 1L);
            if (lastOrdinal < 0)
                return 0;
            if (lastOrdinal >= int.MaxValue)
            {
                throw new InvalidOperationException(
                    "The region seam candidate ordinal range exceeds the supported integer cursor range.");
            }

            return (int)lastOrdinal;
        }

        private static bool TryGetCandidateAlongCoordinate(
            int candidateOrdinal,
            double projectedAlong,
            double permittedMin,
            double permittedMax,
            float spacing,
            out double alongCoordinate)
        {
            if (candidateOrdinal == 0)
            {
                alongCoordinate = projectedAlong;
                return true;
            }

            long step = ((long)candidateOrdinal + 1L) / 2L;
            double offset = step * spacing;
            alongCoordinate = candidateOrdinal % 2 == 1
                ? projectedAlong + offset
                : projectedAlong - offset;
            return IsFinite(alongCoordinate)
                && alongCoordinate >= permittedMin
                && alongCoordinate <= permittedMax;
        }

        private static WorldPosition CreateSeamPosition(
            double boundary,
            double alongCoordinate,
            double y,
            bool alongX,
            int direction,
            float inset,
            bool destination)
        {
            double normal = boundary + (destination ? direction : -direction) * inset;
            return alongX
                ? new WorldPosition(alongCoordinate, y, normal)
                : new WorldPosition(normal, y, alongCoordinate);
        }

        private static bool IsBetterSeamCandidate(
            SimulationRegionSeam candidate,
            SimulationRegionSeam current,
            bool alongX)
        {
            if (candidate.TotalPathLength < current.TotalPathLength)
                return true;
            if (candidate.TotalPathLength > current.TotalPathLength)
                return false;

            double candidateAlong = alongX
                ? candidate.ApproachAbsolute.x
                : candidate.ApproachAbsolute.z;
            double currentAlong = alongX
                ? current.ApproachAbsolute.x
                : current.ApproachAbsolute.z;
            if (candidateAlong < currentAlong)
                return true;
            if (candidateAlong > currentAlong)
                return false;
            return candidate.CandidateOrdinal < current.CandidateOrdinal;
        }

        private static bool IsStoredSeamGeometry(
            SimulationRegionSeam seam,
            bool alongX,
            int direction,
            double boundary,
            float inset)
        {
            WorldPosition expectedApproach = CreateSeamPosition(
                boundary,
                alongX ? seam.ApproachAbsolute.x : seam.ApproachAbsolute.z,
                seam.ApproachAbsolute.y,
                alongX,
                direction,
                inset,
                false);
            WorldPosition expectedLanding = CreateSeamPosition(
                boundary,
                alongX ? seam.LandingAbsolute.x : seam.LandingAbsolute.z,
                seam.LandingAbsolute.y,
                alongX,
                direction,
                inset,
                true);
            return seam.ApproachAbsolute.x == expectedApproach.x
                && seam.ApproachAbsolute.z == expectedApproach.z
                && seam.LandingAbsolute.x == expectedLanding.x
                && seam.LandingAbsolute.z == expectedLanding.z;
        }

        private bool ValidateSeamGeometry(
            NavMeshAgent sourceAgent,
            SimulationRegionDescriptor sourceRegion,
            SimulationRegionDescriptor destinationRegion,
            WorldPosition sourceApproach,
            WorldPosition destinationLanding,
            Vector3 sourceApproachLocal,
            Vector3 destinationLandingLocal,
            bool alongX,
            int direction,
            NavMeshBuildSettings build,
            float voxel,
            float radius,
            float inset)
        {
            double heightDifference = Math.Abs(destinationLanding.y - sourceApproach.y);
            if (!IsFinite(heightDifference)
                || heightDifference > build.agentClimb
                || Mathf.Atan2((float)heightDifference, 2f * inset) * Mathf.Rad2Deg > build.agentSlope)
            {
                return false;
            }

            GetAdjacentRegionBoundary(
                sourceRegion,
                destinationRegion,
                out _,
                out direction,
                out _,
                out _,
                out _);
            return TryValidateSeamSide(
                    sourceAgent,
                    sourceRegion,
                    sourceApproach,
                    sourceApproachLocal,
                    alongX,
                    direction,
                    build,
                    voxel,
                    radius,
                    inset)
                && TryValidateSeamSide(
                    sourceAgent,
                    destinationRegion,
                    destinationLanding,
                    destinationLandingLocal,
                    alongX,
                    -direction,
                    build,
                    voxel,
                    radius,
                    inset);
        }

        private bool TryValidateSeamSide(
            NavMeshAgent sourceAgent,
            SimulationRegionDescriptor region,
            WorldPosition seamPoint,
            Vector3 sampledLocal,
            bool alongX,
            int sideToBoundaryDirection,
            NavMeshBuildSettings build,
            float voxel,
            float radius,
            float inset)
        {
            double normal = alongX ? seamPoint.x : seamPoint.z;
            double probeNormal = normal - sideToBoundaryDirection * voxel;
            WorldPosition probeAbsolute = alongX
                ? new WorldPosition(seamPoint.x, seamPoint.y, probeNormal)
                : new WorldPosition(probeNormal, seamPoint.y, seamPoint.z);
            Vector3 probeLocal = ToRegionLocal(region, probeAbsolute);
            float slopeCosine = Mathf.Cos(build.agentSlope * Mathf.Deg2Rad);
            float rayDistance = 2f * sourceAgent.height + 3f * build.agentClimb;
            if (!IsPositiveFinite(rayDistance))
                return false;

            if (!HasSupportingCollider(
                    sourceAgent,
                    sampledLocal,
                    sourceAgent.height,
                    build.agentClimb,
                    voxel,
                    slopeCosine,
                    rayDistance)
                || !HasSupportingCollider(
                    sourceAgent,
                    probeLocal,
                    sourceAgent.height,
                    build.agentClimb,
                    voxel,
                    slopeCosine,
                    rayDistance))
            {
                return false;
            }

            Vector3 castDirection = alongX
                ? new Vector3(sideToBoundaryDirection, 0f, 0f)
                : new Vector3(0f, 0f, sideToBoundaryDirection);
            return !HasBlockingCollider(
                sourceAgent,
                sampledLocal,
                castDirection,
                sourceAgent.height,
                radius,
                inset,
                slopeCosine);
        }

        private static bool HasSupportingCollider(
            NavMeshAgent sourceAgent,
            Vector3 floorPoint,
            float agentHeight,
            float agentClimb,
            float voxel,
            float slopeCosine,
            float rayDistance)
        {
            Vector3 origin = floorPoint + Vector3.up * (agentHeight + agentClimb);
            var hits = Physics.RaycastAll(
                origin,
                Vector3.down,
                rayDistance,
                ~0,
                QueryTriggerInteraction.Ignore);
            float contactTolerance = agentClimb + voxel;
            foreach (var hit in hits)
            {
                if (hit.collider == null || IsOwnCollider(sourceAgent, hit.collider))
                    continue;
                if (hit.normal.y >= slopeCosine
                    && Mathf.Abs(hit.point.y - floorPoint.y) <= contactTolerance)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasBlockingCollider(
            NavMeshAgent sourceAgent,
            Vector3 floorPoint,
            Vector3 direction,
            float agentHeight,
            float radius,
            float distance,
            float slopeCosine)
        {
            float capsuleHeight = Mathf.Max(agentHeight, 2f * radius);
            Vector3 bottom = floorPoint + Vector3.up * radius;
            Vector3 top = floorPoint + Vector3.up * (capsuleHeight - radius);
            var hits = Physics.CapsuleCastAll(
                bottom,
                top,
                radius,
                direction,
                distance,
                ~0,
                QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit.collider == null || IsOwnCollider(sourceAgent, hit.collider))
                    continue;
                if (hit.normal.y >= slopeCosine)
                    continue;
                return true;
            }

            return false;
        }

        private static bool IsOwnCollider(
            NavMeshAgent sourceAgent,
            Collider collider)
        {
            Transform ownTransform = sourceAgent.transform;
            return collider.transform == ownTransform
                || collider.transform.IsChildOf(ownTransform);
        }

        private static double GetPathLength(NavMeshPath path)
        {
            Vector3[] corners = path.corners;
            double length = 0d;
            for (int i = 1; i < corners.Length; i++)
            {
                double dx = corners[i].x - corners[i - 1].x;
                double dy = corners[i].y - corners[i - 1].y;
                double dz = corners[i].z - corners[i - 1].z;
                length += Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }

            return length;
        }

        private static bool IsPathEndpointWithinTolerance(
            NavMeshPath path,
            Vector3 expected,
            float tolerance)
        {
            Vector3[] corners = path.corners;
            if (corners == null || corners.Length == 0)
                return false;

            Vector3 endpoint = corners[corners.Length - 1];
            float dx = endpoint.x - expected.x;
            float dy = endpoint.y - expected.y;
            float dz = endpoint.z - expected.z;
            return dx * dx + dy * dy + dz * dz <= tolerance * tolerance;
        }

        private static double ClampToHalfOpen(double value, double minimum, double maximumExclusive)
        {
            if (value < minimum)
                return minimum;
            if (value >= maximumExclusive)
                return PreviousRepresentable(maximumExclusive);
            return value;
        }

        private static double PreviousRepresentable(double value)
        {
            if (value == 0d)
                return -double.Epsilon;

            long bits = BitConverter.DoubleToInt64Bits(value);
            return value > 0d
                ? BitConverter.Int64BitsToDouble(bits - 1L)
                : BitConverter.Int64BitsToDouble(bits + 1L);
        }

        private static bool IsPositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private void BindEnvironmentNavigation(
            ActiveSimulationRegion activeRegion,
            WorldChunkKey chunkKey,
            GameObject[] roots)
        {
            if (activeRegion == null)
                throw new ArgumentNullException(nameof(activeRegion));
            if (roots == null)
                throw new ArgumentNullException(nameof(roots));
            if (activeRegion.NavigationRoots.ContainsKey(chunkKey))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey.ChunkCoord} is already registered for region navigation.");
            }

            var storedRoots = (GameObject[])roots.Clone();
            foreach (var root in storedRoots)
            {
                if (root == null)
                {
                    throw new InvalidOperationException(
                        $"Environment tile {chunkKey.ChunkCoord} has a null navigation root.");
                }
            }

            activeRegion.NavigationRoots.Add(chunkKey, storedRoots);
            try
            {
                UpdateRegionNavMesh(activeRegion);
            }
            catch (Exception exception)
            {
                activeRegion.NavigationRoots.Remove(chunkKey);
                try
                {
                    UpdateRegionNavMesh(activeRegion);
                }
                catch (Exception restorationException)
                {
                    exception.Data["EnvironmentNavigationRollbackException"] = restorationException;
                }

                throw;
            }
        }

        /// <summary>
        /// Excludes an environment tile from its region NavMesh before its owned scene unload begins.
        /// </summary>
        /// <param name="chunkKey">The environment tile whose navigation contribution should be excluded.</param>
        /// <exception cref="ArgumentException">Thrown when the chunk key has no valid world context.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the tile is not bound or is already pending unload.</exception>
        public void PrepareEnvironmentNavigationUnload(WorldChunkKey chunkKey)
        {
            EnsureInitialized();

            if (!chunkKey.Context.IsValid)
            {
                throw new ArgumentException(
                    $"Environment navigation unload preparation requires a valid world context for {chunkKey}.",
                    nameof(chunkKey));
            }

            if (!_environmentRegionBindings.TryGetValue(chunkKey, out SimulationRegionKey regionKey))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey} is not bound to a simulation region.");
            }

            if (!_activeRegions.TryGetValue(regionKey, out ActiveSimulationRegion activeRegion))
            {
                throw new InvalidOperationException(
                    $"Simulation region {regionKey} for environment tile {chunkKey} is not active.");
            }

            if (!activeRegion.BoundEnvironmentTiles.Contains(chunkKey))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey} is not bound in simulation region {regionKey}.");
            }

            if (!activeRegion.NavigationRoots.ContainsKey(chunkKey))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey} has no registered navigation roots in simulation region {regionKey}.");
            }

            if (activeRegion.PendingNavigationUnloads.Contains(chunkKey))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey} already has a pending navigation unload.");
            }

            activeRegion.PendingNavigationUnloads.Add(chunkKey);

            try
            {
                UpdateRegionNavMesh(activeRegion);
            }
            catch (Exception exception)
            {
                activeRegion.PendingNavigationUnloads.Remove(chunkKey);

                try
                {
                    UpdateRegionNavMesh(activeRegion);
                }
                catch (Exception restorationException)
                {
                    exception.Data["EnvironmentNavigationPrepareRollbackException"] = restorationException;
                }

                throw;
            }
        }

        /// <summary>
        /// Restores an environment tile's region NavMesh contribution after its owned scene unload is rejected.
        /// </summary>
        /// <param name="chunkKey">The environment tile whose navigation contribution should be restored.</param>
        /// <exception cref="ArgumentException">Thrown when the chunk key has no valid world context.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the tile is not bound or has no pending navigation unload.</exception>
        public void CancelEnvironmentNavigationUnload(WorldChunkKey chunkKey)
        {
            EnsureInitialized();

            if (!chunkKey.Context.IsValid)
            {
                throw new ArgumentException(
                    $"Environment navigation unload cancellation requires a valid world context for {chunkKey}.",
                    nameof(chunkKey));
            }

            if (!_environmentRegionBindings.TryGetValue(chunkKey, out SimulationRegionKey regionKey))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey} is not bound to a simulation region.");
            }

            if (!_activeRegions.TryGetValue(regionKey, out ActiveSimulationRegion activeRegion))
            {
                throw new InvalidOperationException(
                    $"Simulation region {regionKey} for environment tile {chunkKey} is not active.");
            }

            if (!activeRegion.BoundEnvironmentTiles.Contains(chunkKey))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey} is not bound in simulation region {regionKey}.");
            }

            if (!activeRegion.NavigationRoots.ContainsKey(chunkKey))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey} has no registered navigation roots in simulation region {regionKey}.");
            }

            if (!activeRegion.PendingNavigationUnloads.Contains(chunkKey))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey} has no pending navigation unload to cancel.");
            }

            activeRegion.PendingNavigationUnloads.Remove(chunkKey);

            try
            {
                UpdateRegionNavMesh(activeRegion);
            }
            catch
            {
                activeRegion.PendingNavigationUnloads.Add(chunkKey);
                throw;
            }
        }

        private void UnbindEnvironmentNavigation(
            ActiveSimulationRegion activeRegion,
            WorldChunkKey chunkKey)
        {
            if (!activeRegion.NavigationRoots.ContainsKey(chunkKey))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey.ChunkCoord} is not registered for region navigation.");
            }

            bool wasPrepared = activeRegion.PendingNavigationUnloads.Remove(chunkKey);
            activeRegion.NavigationRoots.Remove(chunkKey);

            if (wasPrepared)
                return;

            UpdateRegionNavMesh(activeRegion);
        }

        private static void DisposeRegionNavMesh(
            ActiveSimulationRegion activeRegion)
        {
            if (activeRegion == null)
                throw new ArgumentNullException(nameof(activeRegion));

            if (activeRegion.NavigationInstance.valid)
                activeRegion.NavigationInstance.Remove();
            activeRegion.NavigationInstance = default;
            activeRegion.PendingNavigationUnloads.Clear();
            if (activeRegion.NavigationData != null)
                UnityEngine.Object.Destroy(activeRegion.NavigationData);
            activeRegion.NavigationData = null;
            activeRegion.NavigationRoots.Clear();
        }
    }
}
