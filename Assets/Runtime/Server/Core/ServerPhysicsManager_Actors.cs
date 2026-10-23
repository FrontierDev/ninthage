using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using System;
using System.Collections.Generic;

namespace Game.Server.Core
{
    public sealed partial class ServerPhysicsManager
    {
        private readonly Dictionary<Guid, SimulationRegionKey> _actorRegionBindings = new();

        /// <summary>
        /// Binds an actor's physical representation to an already active simulation region.
        /// </summary>
        /// <param name="actorID">The nonempty actor identity.</param>
        /// <param name="context">The authoritative world, map and runtime instance context.</param>
        /// <param name="worldPosition">The finite authoritative absolute world position of the actor.</param>
        /// <returns>The existing active simulation-region descriptor for the actor.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the actor ID, context or world position is invalid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the actor is already bound, the resolved region is not active,
        /// or the active-region membership state is inconsistent.
        /// </exception>
        public SimulationRegionDescriptor BindActor(
            Guid actorID,
            ActorSpatialContext context,
            WorldPosition worldPosition)
        {
            EnsureInitialized();
            if (actorID == Guid.Empty)
                throw new ArgumentException("A nonempty actor ID is required.", nameof(actorID));
            if (!context.IsValid)
            {
                throw new ArgumentException(
                    "The actor context must be valid.",
                    nameof(context));
            }
            if (!worldPosition.IsFinite)
                throw new ArgumentException("The actor world position must be finite.", nameof(worldPosition));
            if (_actorRegionBindings.ContainsKey(actorID))
            {
                throw new InvalidOperationException(
                    $"Actor {actorID} is already bound to a simulation region.");
            }

            SimulationRegionKey regionKey = GetRegionKey(context, worldPosition.Chunk);
            if (!_activeRegions.TryGetValue(regionKey, out ActiveSimulationRegion activeRegion))
            {
                throw new InvalidOperationException(
                    $"Cannot bind actor {actorID}: simulation region {regionKey} is not active.");
            }

            ToRegionLocal(activeRegion.Descriptor, worldPosition);
            if (!activeRegion.BoundActors.Add(actorID))
            {
                throw new InvalidOperationException(
                    $"Active simulation region {regionKey} already contains actor {actorID}.");
            }

            try
            {
                _actorRegionBindings.Add(actorID, regionKey);
            }
            catch
            {
                activeRegion.BoundActors.Remove(actorID);
                throw;
            }

            return activeRegion.Descriptor;
        }

        /// <summary>
        /// Attempts to get the active simulation-region descriptor for a bound actor without creating a region.
        /// </summary>
        /// <param name="actorID">The nonempty actor identity.</param>
        /// <param name="descriptor">Receives the bound region descriptor when found; otherwise the default value.</param>
        /// <returns><see langword="true"/> when the actor is bound to an active region; otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="actorID"/> is empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the actor binding or active-region membership is inconsistent.</exception>
        public bool TryGetActorRegionDescriptor(
            Guid actorID,
            out SimulationRegionDescriptor descriptor)
        {
            EnsureInitialized();
            if (actorID == Guid.Empty)
                throw new ArgumentException("A nonempty actor ID is required.", nameof(actorID));
            if (!_actorRegionBindings.TryGetValue(actorID, out SimulationRegionKey regionKey))
            {
                descriptor = default;
                return false;
            }
            if (!_activeRegions.TryGetValue(regionKey, out ActiveSimulationRegion activeRegion))
            {
                throw new InvalidOperationException(
                    $"Actor {actorID} references missing active simulation region {regionKey}.");
            }
            if (!activeRegion.BoundActors.Contains(actorID))
            {
                throw new InvalidOperationException(
                    $"Active simulation region {regionKey} does not contain actor {actorID}.");
            }

            descriptor = activeRegion.Descriptor;
            return true;
        }

        /// <summary>
        /// Removes an actor's physical association with its simulation region.
        /// </summary>
        /// <param name="actorID">The nonempty actor identity.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="actorID"/> is empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the actor binding or active-region membership is missing or inconsistent.</exception>
        public void UnbindActor(Guid actorID)
        {
            EnsureInitialized();
            if (actorID == Guid.Empty)
                throw new ArgumentException("A nonempty actor ID is required.", nameof(actorID));
            if (!_actorRegionBindings.TryGetValue(actorID, out SimulationRegionKey regionKey))
            {
                throw new InvalidOperationException(
                    $"Actor {actorID} is not bound to a simulation region.");
            }
            if (!_activeRegions.TryGetValue(regionKey, out ActiveSimulationRegion activeRegion))
            {
                throw new InvalidOperationException(
                    $"Actor {actorID} references missing active simulation region {regionKey}.");
            }
            if (!activeRegion.BoundActors.Contains(actorID))
            {
                throw new InvalidOperationException(
                    $"Active simulation region {regionKey} does not contain actor {actorID}.");
            }

            activeRegion.BoundActors.Remove(actorID);
            if (!_actorRegionBindings.Remove(actorID))
            {
                throw new InvalidOperationException(
                    $"Actor {actorID} binding was not present when removing it from the actor-region registry.");
            }

            if (activeRegion.BoundActors.Count == 0
                && activeRegion.BoundEnvironmentTiles.Count == 0)
            {
                RetireActiveRegion(regionKey);
            }
        }
    }
}
