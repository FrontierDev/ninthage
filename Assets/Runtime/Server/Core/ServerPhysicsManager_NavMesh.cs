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
                if (activeRegion.NavigationInstance.valid)
                    activeRegion.NavigationInstance.Remove();
                activeRegion.NavigationInstance = default;
                if (activeRegion.NavigationData != null)
                    Destroy(activeRegion.NavigationData);
                activeRegion.NavigationData = null;
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
