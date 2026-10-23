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
        /// Performs one synchronous, reversible handoff of an enabled NavMesh actor across an accepted region seam.
        /// </summary>
        /// <param name="actorID">The nonempty actor identity.</param>
        /// <param name="context">The actor's compatible logical spatial context.</param>
        /// <param name="agent">The enabled NavMesh agent being handed off.</param>
        /// <param name="acceptedSourceAbsolute">The accepted source position for this handoff.</param>
        /// <param name="seam">The previously validated adjacent-region seam.</param>
        /// <param name="latestTargetAbsolute">The latest absolute target position.</param>
        /// <param name="directChaseStoppingDistance">The stopping distance when the target is in the destination region.</param>
        /// <returns>
        /// <see langword="true"/> when the same agent and actor binding are attached to the destination region;
        /// otherwise <see langword="false"/> when the handoff is not currently eligible.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when an identity, context or position is invalid.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the authoritative tick, physical state or a post-mutation rollback is invalid.
        /// </exception>
        public bool TryHandoffEnabledNavMeshActor(
            Guid actorID,
            ActorSpatialContext context,
            NavMeshAgent agent,
            WorldPosition acceptedSourceAbsolute,
            SimulationRegionSeam seam,
            WorldPosition latestTargetAbsolute,
            float directChaseStoppingDistance)
        {
            EnsureInitialized();
            if (ServerECSManager.CurrentSimulationTickID <= 0)
                throw new InvalidOperationException("NavMesh actor handoff requires an active authoritative ECS tick.");
            if (actorID == Guid.Empty)
                throw new ArgumentException("A nonempty actor ID is required.", nameof(actorID));
            if (!context.IsValid)
                throw new ArgumentException("The actor context must be valid.", nameof(context));
            if (!acceptedSourceAbsolute.IsFinite)
                throw new ArgumentException("The accepted source position must be finite.", nameof(acceptedSourceAbsolute));
            if (!latestTargetAbsolute.IsFinite)
                throw new ArgumentException("The latest target position must be finite.", nameof(latestTargetAbsolute));
            if (agent == null)
                throw new ArgumentNullException(nameof(agent));
            if (!agent.enabled || !agent.gameObject.activeInHierarchy)
                throw new InvalidOperationException("NavMesh actor handoff requires an enabled active agent.");
            if (float.IsNaN(directChaseStoppingDistance)
                || float.IsInfinity(directChaseStoppingDistance)
                || directChaseStoppingDistance < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(directChaseStoppingDistance),
                    directChaseStoppingDistance,
                    "The direct-chase stopping distance must be finite and nonnegative.");
            }

            if (!TryGetActorRegionDescriptor(actorID, out var sourceRegion))
                return false;
            if (sourceRegion.Key != seam.SourceKey)
            {
                throw new InvalidOperationException(
                    $"Actor {actorID} is bound to source region {sourceRegion.Key}, but the seam starts at {seam.SourceKey}.");
            }
            if (!sourceRegion.Key.Context.IsCompatibleWith(context))
            {
                throw new InvalidOperationException(
                    $"Actor {actorID} source region context {sourceRegion.Key.Context} is incompatible with {context}.");
            }
            if (GetRegionKey(context, acceptedSourceAbsolute.Chunk) != seam.SourceKey)
            {
                throw new InvalidOperationException(
                    $"The accepted source position does not resolve to seam source region {seam.SourceKey}.");
            }

            if (!TryGetActorNavigationRegion(actorID, out var registeredRegion, out var registeredAgentTypeID))
                return false;
            if (registeredRegion.Key != seam.SourceKey
                || agent.agentTypeID != registeredAgentTypeID
                || agent.agentTypeID != seam.AgentTypeID)
            {
                throw new InvalidOperationException(
                    $"Actor {actorID} has an incompatible source NavMesh agent type or region registration.");
            }
            if (agent.areaMask != seam.AreaMask)
            {
                throw new InvalidOperationException(
                    $"Actor {actorID} NavMesh area mask {agent.areaMask} does not match seam mask {seam.AreaMask}.");
            }
            if (!agent.isOnNavMesh)
                return false;

            if (!IsRegionSeamApproachReady(agent))
                return false;

            ResolveNavigationSeamGeometry(
                agent,
                out _,
                out _,
                out _,
                out _,
                out _,
                out var sampleRadius,
                out _);

            Vector3 originalSourceLocalPose = agent.transform.position;
            WorldPosition originalSourceAbsolutePose = ToWorld(sourceRegion, originalSourceLocalPose);
            WorldPosition originalSourceApproach = ToWorld(sourceRegion, agent.destination);

            WorldPosition landingAbsolute = seam.LandingAbsolute;
            SimulationRegionKey landingKey = GetRegionKey(context, landingAbsolute.Chunk);
            if (landingKey != seam.DestinationKey)
            {
                throw new InvalidOperationException(
                    $"The seam landing resolves to region {landingKey}, expected {seam.DestinationKey}.");
            }

            if (!TryGetAdjacentNavigationAdmission(
                    seam.SourceKey,
                    landingAbsolute,
                    agent.agentTypeID,
                    out var admittedSourceRegion,
                    out var admittedDestinationRegion,
                    out long sourceRevision,
                    out long destinationRevision))
            {
                return false;
            }
            if (admittedSourceRegion.Key != seam.SourceKey
                || admittedDestinationRegion.Key != seam.DestinationKey
                || sourceRevision != seam.SourceNavigationRevision
                || destinationRevision != seam.DestinationNavigationRevision
                || agent.agentTypeID != seam.AgentTypeID)
            {
                return false;
            }

            if (!_activeRegions.TryGetValue(seam.SourceKey, out var sourceActiveRegion)
                || !_activeRegions.TryGetValue(seam.DestinationKey, out var destinationActiveRegion))
            {
                return false;
            }
            if (sourceActiveRegion.BoundEnvironmentTiles.Count == 0
                || destinationActiveRegion.BoundEnvironmentTiles.Count == 0
                || sourceActiveRegion.NavigationData == null
                || !sourceActiveRegion.NavigationInstance.valid
                || destinationActiveRegion.NavigationData == null
                || !destinationActiveRegion.NavigationInstance.valid)
            {
                return false;
            }

            if (!ValidateRegionSeam(agent, seam, latestTargetAbsolute, out var onwardAbsolute))
                return false;
            SimulationRegionKey targetKey = GetRegionKey(context, latestTargetAbsolute.Chunk);
            Vector3 destinationLocal = ToRegionLocal(admittedDestinationRegion, landingAbsolute);
            Vector3 onwardLocal = ToRegionLocal(admittedDestinationRegion, onwardAbsolute);
            float stoppingDistance = targetKey == seam.DestinationKey
                ? directChaseStoppingDistance
                : 0f;

            bool wasStopped = agent.isStopped;
            bool enabled = agent.enabled;
            float oldStoppingDistance = agent.stoppingDistance;
            Vector3 oldVelocityIntent = agent.velocity;
            _ = oldVelocityIntent;
            bool bindingChanged = false;

            try
            {
                agent.isStopped = true;
                agent.ResetPath();

                bool warped = agent.Warp(destinationLocal);
                if (!warped
                    || agent.enabled != enabled
                    || !agent.isOnNavMesh
                    || agent.agentTypeID != seam.AgentTypeID
                    || agent.areaMask != seam.AreaMask)
                {
                    throw new InvalidOperationException("The NavMesh agent could not attach to the destination seam landing.");
                }

                WorldPosition warpedAbsolute = ToWorld(admittedDestinationRegion, agent.transform.position);
                double warpedDX = warpedAbsolute.x - landingAbsolute.x;
                double warpedDY = warpedAbsolute.y - landingAbsolute.y;
                double warpedDZ = warpedAbsolute.z - landingAbsolute.z;
                if (warpedDX * warpedDX + warpedDY * warpedDY + warpedDZ * warpedDZ
                    > (double)sampleRadius * sampleRadius)
                {
                    throw new InvalidOperationException("The warped NavMesh pose exceeded the seam landing tolerance.");
                }

                bool transitioned = TransitionActorRegion(
                    actorID,
                    context,
                    seam.LandingAbsolute,
                    out var previousRegion,
                    out var currentRegion);
                bindingChanged = transitioned;
                if (!transitioned
                    || previousRegion.Key != seam.SourceKey
                    || currentRegion.Key != seam.DestinationKey)
                {
                    throw new InvalidOperationException("The actor physical-region membership did not transition across the accepted seam.");
                }

                agent.stoppingDistance = stoppingDistance;
                if (!agent.SetDestination(onwardLocal))
                    throw new InvalidOperationException("The destination-region onward path request was rejected.");
                if (!agent.enabled
                    || !agent.isOnNavMesh
                    || agent.agentTypeID != seam.AgentTypeID
                    || agent.areaMask != seam.AreaMask)
                {
                    throw new InvalidOperationException("The destination NavMesh agent changed state during the onward path request.");
                }

                agent.isStopped = wasStopped;
                return true;
            }
            catch (Exception originalFailure)
            {
                var restorationFailures = new List<Exception>();
                bool sourceBindingVerified = false;
                if (bindingChanged)
                {
                    try
                    {
                        bool reversed = TransitionActorRegion(
                            actorID,
                            context,
                            originalSourceAbsolutePose,
                            out var reversedPreviousRegion,
                            out var reversedCurrentRegion);
                        if (!reversed
                            || reversedPreviousRegion.Key != seam.DestinationKey
                            || reversedCurrentRegion.Key != seam.SourceKey)
                        {
                            throw new InvalidOperationException("The actor physical-region membership did not reverse to the source region.");
                        }

                        sourceBindingVerified = true;
                    }
                    catch (Exception restorationFailure)
                    {
                        restorationFailures.Add(restorationFailure);
                    }
                }

                if (!sourceBindingVerified)
                {
                    try
                    {
                        sourceBindingVerified = TryGetActorRegionDescriptor(
                            actorID,
                            out var restoredRegion)
                            && restoredRegion.Key == seam.SourceKey;
                        if (!sourceBindingVerified)
                        {
                            throw new InvalidOperationException("The actor binding was not restored to the source region.");
                        }
                    }
                    catch (Exception restorationFailure)
                    {
                        restorationFailures.Add(restorationFailure);
                    }
                }

                try
                {
                    agent.ResetPath();
                }
                catch (Exception restorationFailure)
                {
                    restorationFailures.Add(restorationFailure);
                }

                if (sourceBindingVerified)
                {
                    try
                    {
                        if (agent.enabled != enabled
                            || !agent.Warp(originalSourceLocalPose)
                            || !agent.isOnNavMesh
                            || agent.agentTypeID != seam.AgentTypeID
                            || agent.areaMask != seam.AreaMask)
                        {
                            throw new InvalidOperationException("The source NavMesh agent could not be restored after the failed handoff.");
                        }

                        WorldPosition restoredAbsolute = ToWorld(sourceRegion, agent.transform.position);
                        if (restoredAbsolute.x != originalSourceAbsolutePose.x
                            || restoredAbsolute.y != originalSourceAbsolutePose.y
                            || restoredAbsolute.z != originalSourceAbsolutePose.z)
                        {
                            throw new InvalidOperationException("The restored source NavMesh pose does not match the original pose.");
                        }
                    }
                    catch (Exception restorationFailure)
                    {
                        restorationFailures.Add(restorationFailure);
                        sourceBindingVerified = false;
                    }
                }

                try
                {
                    agent.stoppingDistance = oldStoppingDistance;
                    if (sourceBindingVerified)
                    {
                        if (!wasStopped)
                        {
                            agent.isStopped = false;
                            if (!agent.SetDestination(ToRegionLocal(sourceRegion, originalSourceApproach)))
                                throw new InvalidOperationException("The source NavMesh path could not be restored after the failed handoff.");
                        }

                        agent.isStopped = wasStopped;
                    }
                    else
                    {
                        agent.isStopped = true;
                    }
                }
                catch (Exception restorationFailure)
                {
                    restorationFailures.Add(restorationFailure);
                }

                try
                {
                    if (!TryGetActorRegionDescriptor(actorID, out var restoredRegion)
                        || restoredRegion.Key != seam.SourceKey)
                    {
                        throw new InvalidOperationException("The actor binding verification did not resolve to the source region.");
                    }
                }
                catch (Exception restorationFailure)
                {
                    restorationFailures.Add(restorationFailure);
                }

                if (restorationFailures.Count == 0)
                {
                    throw new InvalidOperationException(
                        "The NavMesh actor handoff failed and was rolled back.",
                        originalFailure);
                }

                var failures = new List<Exception>(restorationFailures.Count + 1)
                {
                    originalFailure
                };
                failures.AddRange(restorationFailures);
                throw new InvalidOperationException(
                    "The NavMesh actor handoff failed and physical rollback also failed.",
                    new AggregateException(failures));
            }
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
