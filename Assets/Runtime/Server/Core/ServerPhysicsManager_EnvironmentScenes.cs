using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Server.Core
{
    public sealed partial class ServerPhysicsManager
    {
        /// <summary>
        /// Registers the roots of a loaded environment scene in the continuous physical frame.
        /// </summary>
        /// <param name="chunkKey">The context-qualified environment tile identity.</param>
        /// <param name="scene">The loaded Unity environment scene to register.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the chunk context is invalid or the scene is not valid and loaded.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the tile is already registered or NavMesh registration fails.
        /// </exception>
        public void BindEnvironmentScene(
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
            GameObject[] roots = null;

            try
            {
                roots = scene.GetRootGameObjects();
                RegisterEnvironmentNavigation(chunkKey, roots);
                Physics.SyncTransforms();
            }
            catch (Exception exception)
            {
                if (_navigationRoots.ContainsKey(chunkKey))
                {
                    try
                    {
                        UnregisterEnvironmentNavigation(chunkKey);
                    }
                    catch (Exception navigationRollbackException)
                    {
                        exception.Data["EnvironmentNavigationRollbackException"] = navigationRollbackException;
                    }
                }
                throw;
            }
        }

        /// <summary>
        /// Removes an environment tile's navigation registration after terminal scene unload.
        /// </summary>
        /// <param name="chunkKey">The context-qualified environment tile identity.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the tile has no navigation registration.
        /// </exception>
        public void UnbindEnvironmentScene(WorldChunkKey chunkKey)
        {
            EnsureInitialized();
            if (!_navigationRoots.ContainsKey(chunkKey))
            {
                throw new InvalidOperationException(
                    $"Environment tile {chunkKey.ChunkCoord} is not registered for navigation.");
            }

            UnregisterEnvironmentNavigation(chunkKey);
        }
    }
}
