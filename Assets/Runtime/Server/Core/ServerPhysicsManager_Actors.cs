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
        /// Transitions an actor's physical membership to another already active simulation region.
        /// </summary>
        /// <param name="actorID">The nonempty actor identity.</param>
        /// <param name="context">The authoritative context containing the accepted world position.</param>
        /// <param name="worldPosition">The finite accepted absolute world position of the actor.</param>
        /// <param name="previousRegion">Receives the actor's source region descriptor.</param>
        /// <param name="currentRegion">Receives the actor's resulting region descriptor.</param>
        /// <returns>
        /// <see langword="true"/> when the actor's physical region binding changed;
        /// otherwise, <see langword="false"/> when it remained in the same region.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the actor ID, context or world position is invalid.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the existing binding is inconsistent, the context changes, the destination region is inactive,
        /// or the membership transition cannot be completed atomically.
        /// </exception>
        public bool TransitionActorRegion(
            Guid actorID,
            ActorSpatialContext context,
            WorldPosition worldPosition,
            out SimulationRegionDescriptor previousRegion,
            out SimulationRegionDescriptor currentRegion)
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
            if (!_actorRegionBindings.TryGetValue(actorID, out SimulationRegionKey sourceKey))
            {
                throw new InvalidOperationException(
                    $"Actor {actorID} is not bound to a simulation region.");
            }
            if (!_activeRegions.TryGetValue(sourceKey, out ActiveSimulationRegion sourceRegion))
            {
                throw new InvalidOperationException(
                    $"Actor {actorID} references missing source simulation region {sourceKey}.");
            }
            if (!sourceRegion.BoundActors.Contains(actorID))
            {
                throw new InvalidOperationException(
                    $"Source simulation region {sourceKey} does not contain actor {actorID}.");
            }
            if (!sourceKey.Context.IsCompatibleWith(context))
            {
                throw new InvalidOperationException(
                    $"Actor {actorID} cannot transition from context {sourceKey.Context} to {context}.");
            }

            previousRegion = sourceRegion.Descriptor;
            var destinationKey = GetRegionKey(context, worldPosition.Chunk);
            if (destinationKey == sourceKey)
            {
                ToRegionLocal(sourceRegion.Descriptor, worldPosition);
                currentRegion = sourceRegion.Descriptor;
                return false;
            }

            if (!_activeRegions.TryGetValue(destinationKey, out ActiveSimulationRegion destinationRegion))
            {
                throw new InvalidOperationException(
                    $"Cannot transition actor {actorID}: destination simulation region {destinationKey} is not active.");
            }
            if (!destinationKey.Context.IsCompatibleWith(context))
            {
                throw new InvalidOperationException(
                    $"Destination simulation region {destinationKey} has an incompatible context for actor {actorID}.");
            }

            ToRegionLocal(destinationRegion.Descriptor, worldPosition);

            if (!destinationRegion.BoundActors.Add(actorID))
            {
                throw new InvalidOperationException(
                    $"Destination simulation region {destinationKey} already contains actor {actorID}.");
            }

            try
            {
                _actorRegionBindings[actorID] = destinationKey;
                if (!sourceRegion.BoundActors.Remove(actorID))
                {
                    throw new InvalidOperationException(
                        $"Source simulation region {sourceKey} did not contain actor {actorID} during transition.");
                }
            }
            catch
            {
                _actorRegionBindings[actorID] = sourceKey;
                sourceRegion.BoundActors.Add(actorID);
                destinationRegion.BoundActors.Remove(actorID);
                throw;
            }

            currentRegion = destinationRegion.Descriptor;
            if (sourceRegion.BoundActors.Count == 0
                && sourceRegion.BoundEnvironmentTiles.Count == 0)
            {
                RetireActiveRegion(sourceKey);
            }

            return true;
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
