using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Server.Core
{
    public sealed partial class ServerPhysicsManager
    {
        private readonly Dictionary<WorldChunkKey, SimulationRegionKey> _environmentRegionBindings = new();

        /// <summary>
        /// Binds the roots of a loaded environment scene to the physical frame of its configured simulation region.
        /// </summary>
        /// <param name="chunkKey">The context-qualified environment tile identity.</param>
        /// <param name="scene">The loaded Unity environment scene to place in the region frame.</param>
        /// <returns>The active simulation-region descriptor used for the binding.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the chunk context is invalid or the scene is not valid and loaded.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the tile is already bound, the frame offset cannot fit in finite Unity floats,
        /// or the region cannot be activated.
        /// </exception>
        public SimulationRegionDescriptor BindEnvironmentScene(
            WorldChunkKey chunkKey,
            Scene scene)
        {
            EnsureInitialized();
            if (!chunkKey.Context.IsValid)
            {
                throw new ArgumentException(
                    "The environment tile key must contain a valid context.",
                    nameof(chunkKey));
            }
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new ArgumentException(
                    "The environment scene must be valid and loaded.",
                    nameof(scene));
            }
            if (_environmentRegionBindings.ContainsKey(chunkKey))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey.ChunkCoord} is already bound to a simulation region.");
            }

            SimulationRegionKey regionKey = GetRegionKey(chunkKey.Context, chunkKey.ChunkCoord);
            bool regionWasActive = _activeRegions.ContainsKey(regionKey);
            ActiveSimulationRegion activeRegion = GetOrCreateActiveRegion(regionKey);
            bool rootMovementStarted = false;
            int shiftedRootCount = 0;
            GameObject[] roots = null;
            Vector3 frameOffset = default;

            try
            {
                SimulationRegionDescriptor descriptor = activeRegion.Descriptor;
                double offsetX = (double)descriptor.LocalAnchor.x - descriptor.WorldOrigin.x;
                double offsetY = (double)descriptor.LocalAnchor.y - descriptor.WorldOrigin.y;
                double offsetZ = (double)descriptor.LocalAnchor.z - descriptor.WorldOrigin.z;
                if (!FitsFiniteFloat(offsetX)
                    || !FitsFiniteFloat(offsetY)
                    || !FitsFiniteFloat(offsetZ))
                {
                    throw new InvalidOperationException(
                        $"Simulation-region frame offset for {regionKey} does not fit in finite Unity floats.");
                }

                frameOffset = new Vector3(
                    (float)offsetX,
                    (float)offsetY,
                    (float)offsetZ);
                roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    rootMovementStarted = true;
                    roots[rootIndex].transform.position += frameOffset;
                    shiftedRootCount++;
                }

                Physics.SyncTransforms();
                activeRegion.BoundEnvironmentTiles.Add(chunkKey);
                _environmentRegionBindings.Add(chunkKey, regionKey);
                return descriptor;
            }
            catch (Exception exception)
            {
                Exception rollbackException = null;
                if (rootMovementStarted)
                {
                    for (int rootIndex = shiftedRootCount - 1; rootIndex >= 0; rootIndex--)
                    {
                        try
                        {
                            roots[rootIndex].transform.position -= frameOffset;
                        }
                        catch (Exception rootRollbackException)
                        {
                            rollbackException ??= rootRollbackException;
                        }
                    }

                    try
                    {
                        Physics.SyncTransforms();
                    }
                    catch (Exception syncException)
                    {
                        rollbackException ??= syncException;
                    }
                }

                activeRegion.BoundEnvironmentTiles.Remove(chunkKey);
                _environmentRegionBindings.Remove(chunkKey);
                if (!regionWasActive
                    && activeRegion.BoundEnvironmentTiles.Count == 0
                    && activeRegion.BoundActors.Count == 0)
                    RetireActiveRegion(regionKey);

                if (rollbackException != null)
                    exception.Data["EnvironmentSceneBindRollbackException"] = rollbackException;

                ExceptionDispatchInfo.Capture(exception).Throw();
                throw;
            }
        }

        /// <summary>
        /// Attempts to get the active simulation-region descriptor for a previously bound environment tile.
        /// </summary>
        /// <param name="chunkKey">The context-qualified environment tile identity.</param>
        /// <param name="descriptor">Receives the bound region descriptor when found; otherwise the default value.</param>
        /// <returns><see langword="true"/> when the tile has a valid active-region binding; otherwise <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when a binding references a missing active region.</exception>
        public bool TryGetEnvironmentRegionDescriptor(
            WorldChunkKey chunkKey,
            out SimulationRegionDescriptor descriptor)
        {
            EnsureInitialized();
            if (!_environmentRegionBindings.TryGetValue(chunkKey, out SimulationRegionKey regionKey))
            {
                descriptor = default;
                return false;
            }
            if (!_activeRegions.TryGetValue(regionKey, out ActiveSimulationRegion activeRegion))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey.ChunkCoord} references missing active simulation region {regionKey}.");
            }

            descriptor = activeRegion.Descriptor;
            return true;
        }

        /// <summary>
        /// Removes the physical binding for an environment tile after its Unity scene has reached terminal unload.
        /// </summary>
        /// <param name="chunkKey">The context-qualified environment tile identity.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the tile has no binding, its region is missing, or its region does not contain the tile membership.
        /// </exception>
        public void UnbindEnvironmentScene(WorldChunkKey chunkKey)
        {
            EnsureInitialized();
            if (!_environmentRegionBindings.TryGetValue(chunkKey, out SimulationRegionKey regionKey))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey.ChunkCoord} is not bound to a simulation region.");
            }
            if (!_activeRegions.TryGetValue(regionKey, out ActiveSimulationRegion activeRegion))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey.ChunkCoord} references missing active simulation region {regionKey}.");
            }
            if (!activeRegion.BoundEnvironmentTiles.Contains(chunkKey))
            {
                throw new InvalidOperationException(
                    $"Active simulation region {regionKey} does not contain environment tile {chunkKey.ChunkCoord}.");
            }

            activeRegion.BoundEnvironmentTiles.Remove(chunkKey);
            _environmentRegionBindings.Remove(chunkKey);
            if (activeRegion.BoundEnvironmentTiles.Count == 0
                && activeRegion.BoundActors.Count == 0)
                RetireActiveRegion(regionKey);
        }
    }
}
