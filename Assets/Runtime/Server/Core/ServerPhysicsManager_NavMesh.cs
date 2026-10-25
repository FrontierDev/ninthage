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
        private readonly Dictionary<WorldChunkKey, GameObject[]> _navigationRoots = new();
        private readonly HashSet<WorldChunkKey> _pendingNavigationUnloads = new();
        private NavMeshData _navigationData;
        private NavMeshDataInstance _navigationInstance;

        private static NavMeshBuildSettings GetDefaultNavMeshBuildSettings()
        {
            var settings = NavMesh.GetSettingsByID(0);
            if (settings.agentTypeID < 0)
            {
                throw new InvalidOperationException(
                    "The default Unity NavMesh agent type is not configured.");
            }

            return settings;
        }

        internal bool TryGetNavigationAgentType(out int agentTypeID)
        {
            if (_navigationData == null || !_navigationInstance.valid)
            {
                agentTypeID = default;
                return false;
            }

            agentTypeID = GetDefaultNavMeshBuildSettings().agentTypeID;
            return true;
        }

        private static List<NavMeshBuildSource> CollectNavMeshSources(
            Dictionary<WorldChunkKey, GameObject[]> navigationRoots,
            HashSet<WorldChunkKey> pendingNavigationUnloads,
            out int minChunkX,
            out int maxChunkX,
            out int minChunkZ,
            out int maxChunkZ)
        {
            var sources = new List<NavMeshBuildSource>();
            var navigationEntries = new List<KeyValuePair<WorldChunkKey, GameObject[]>>(navigationRoots);
            navigationEntries.Sort((left, right) =>
            {
                int xComparison = left.Key.ChunkCoord.x.CompareTo(right.Key.ChunkCoord.x);
                return xComparison != 0
                    ? xComparison
                    : left.Key.ChunkCoord.y.CompareTo(right.Key.ChunkCoord.y);
            });

            minChunkX = int.MaxValue;
            maxChunkX = int.MinValue;
            minChunkZ = int.MaxValue;
            maxChunkZ = int.MinValue;
            var markups = new List<NavMeshBuildMarkup>();
            var rootSources = new List<NavMeshBuildSource>();
            foreach (var entry in navigationEntries)
            {
                WorldChunkKey chunkKey = entry.Key;
                if (pendingNavigationUnloads.Contains(chunkKey))
                    continue;

                minChunkX = Math.Min(minChunkX, chunkKey.ChunkCoord.x);
                maxChunkX = Math.Max(maxChunkX, chunkKey.ChunkCoord.x);
                minChunkZ = Math.Min(minChunkZ, chunkKey.ChunkCoord.y);
                maxChunkZ = Math.Max(maxChunkZ, chunkKey.ChunkCoord.y);

                foreach (var root in entry.Value)
                {
                    if (root == null)
                    {
                        throw new InvalidOperationException(
                            $"Environment tile {chunkKey.ChunkCoord} has a null navigation root.");
                    }

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

        private void UpdateNavMesh()
        {
            bool hadNavigation = _navigationData != null && _navigationInstance.valid;
            var sources = CollectNavMeshSources(
                _navigationRoots,
                _pendingNavigationUnloads,
                out int minChunkX,
                out int maxChunkX,
                out int minChunkZ,
                out int maxChunkZ);
            if (sources.Count == 0)
            {
                if (!hadNavigation)
                    return;

                if (!ServerActorManager.IsInitialized || ServerActorManager.Instance == null)
                {
                    throw new InvalidOperationException(
                        "Removing the final continuous NavMesh requires an initialized server actor manager.");
                }

                ServerActorManager.Instance.SuspendNPCNavigationForUnavailableNavMesh();
                _navigationInstance.Remove();
                _navigationInstance = default;
                Destroy(_navigationData);
                _navigationData = null;
                return;
            }

            int chunkSize = Game.Shared.GameConfigurationManager.Config.ChunkSize;

            double minX = (double)minChunkX * chunkSize - chunkSize * 0.5d;
            double maxX = (double)maxChunkX * chunkSize + chunkSize * 0.5d;
            double minZ = (double)minChunkZ * chunkSize - chunkSize * 0.5d;
            double maxZ = (double)maxChunkZ * chunkSize + chunkSize * 0.5d;
            var bounds = new Bounds(
                new Vector3(
                    (float)((minX + maxX) * 0.5d),
                    0f,
                    (float)((minZ + maxZ) * 0.5d)),
                new Vector3(
                    (float)(maxX - minX),
                    64f,
                    (float)(maxZ - minZ)));

            var settings = GetDefaultNavMeshBuildSettings();
            if (_navigationData == null)
                _navigationData = new NavMeshData(settings.agentTypeID);

            bool updated = NavMeshBuilder.UpdateNavMeshData(
                _navigationData,
                settings,
                sources,
                bounds);
            if (!updated)
            {
                throw new InvalidOperationException(
                    "Unity could not update the continuous server NavMesh.");
            }

            if (!_navigationInstance.valid)
            {
                _navigationInstance = NavMesh.AddNavMeshData(_navigationData);
                if (!_navigationInstance.valid)
                {
                    throw new InvalidOperationException(
                        "Unity rejected continuous server NavMesh data registration.");
                }

                if (!hadNavigation)
                    ServerActorManager.Instance?.ResumeNPCNavigationAfterNavMeshAvailable();
            }
        }

        private void RegisterEnvironmentNavigation(WorldChunkKey chunkKey, GameObject[] roots)
        {
            if (roots == null)
                throw new ArgumentNullException(nameof(roots));
            if (_navigationRoots.ContainsKey(chunkKey))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey.ChunkCoord} is already registered for navigation.");
            }

            foreach (var root in roots)
            {
                if (root == null)
                {
                    throw new InvalidOperationException(
                        $"Environment tile {chunkKey.ChunkCoord} has a null navigation root.");
                }
            }

            _navigationRoots.Add(chunkKey, roots);
            try
            {
                UpdateNavMesh();
            }
            catch (Exception exception)
            {
                _navigationRoots.Remove(chunkKey);
                try
                {
                    UpdateNavMesh();
                }
                catch (Exception rollbackException)
                {
                    exception.Data["EnvironmentNavigationRollbackException"] = rollbackException;
                }

                throw;
            }
        }

        private void UnregisterEnvironmentNavigation(WorldChunkKey chunkKey)
        {
            if (!_navigationRoots.Remove(chunkKey))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey.ChunkCoord} is not registered for navigation.");
            }

            bool wasPreparedForUnload = _pendingNavigationUnloads.Remove(chunkKey);
            if (!wasPreparedForUnload)
                UpdateNavMesh();
        }

        public void PrepareEnvironmentNavigationUnload(WorldChunkKey chunkKey)
        {
            if (!_navigationRoots.ContainsKey(chunkKey))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey.ChunkCoord} is not registered for navigation.");
            }
            if (!_pendingNavigationUnloads.Add(chunkKey))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey.ChunkCoord} already has a pending navigation unload.");
            }

            try
            {
                UpdateNavMesh();
            }
            catch (Exception exception)
            {
                _pendingNavigationUnloads.Remove(chunkKey);
                try
                {
                    UpdateNavMesh();
                }
                catch (Exception rollbackException)
                {
                    exception.Data["EnvironmentNavigationPrepareRollbackException"] = rollbackException;
                }

                throw;
            }
        }

        public void CancelEnvironmentNavigationUnload(WorldChunkKey chunkKey)
        {
            if (!_pendingNavigationUnloads.Remove(chunkKey))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey.ChunkCoord} has no pending navigation unload to cancel.");
            }

            try
            {
                UpdateNavMesh();
            }
            catch
            {
                _pendingNavigationUnloads.Add(chunkKey);
                throw;
            }
        }

        private void DisposeContinuousNavigation()
        {
            if (_navigationInstance.valid)
                _navigationInstance.Remove();
            _navigationInstance = default;
            if (_navigationData != null)
                Destroy(_navigationData);
            _navigationData = null;
            _pendingNavigationUnloads.Clear();
            _navigationRoots.Clear();
        }

    }
}
