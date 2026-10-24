using Actor = Game.Shared.Runtime.Actors.Actor;
using Game.Shared;
using Game.Shared.Performance;
using Game.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Server.Core
{
    public sealed partial class ServerActorManager
    {
        private static readonly PerformanceMetricDefinition FULL_ENTITY_SCAN_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_FULL_ENTITY_SCAN_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager",
            "server_tick");
        private static readonly PerformanceMetricDefinition GAMEOBJECT_SYNC_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_ECS_GAMEOBJECT_SYNC_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "actor_manager",
            "server_tick");
        private const int TICK_ENTITY_SCAN_SAMPLE_CAPACITY = PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC;

        // Authoritative movement bridges contribute to one observation for each simulation tick.
        private long _pendingTickID = -1;
        private int _pendingTickEntityScanCount;
        private int _pendingTickGameObjectSyncCount;
        private bool _hasPendingTickSyncContribution;

        private struct NPCNavigationState
        {
            public Actor Actor;
            public NavMeshAgent Agent;
            public Guid HopTargetID;
            public SimulationRegionKey HopTargetRegionKey;
            public SimulationRegionKey HopSourceKey;
            public SimulationRegionKey HopDestinationKey;
            public int HopAxisIndex;
            public int HopNextCandidateIndex;
            public bool HopSelected;
            public SimulationRegionSeam HopBestSeam;
            public bool HopHasBest;
            public bool HopSawUnreadyCandidate;
            public bool HopSearchComplete;
            public WorldPosition HopOnwardAbsolute;
        }

        private readonly Dictionary<Guid, NPCNavigationState> _npcNavigationStates = new();

        private static void ClearNPCNavigationHop(ref NPCNavigationState state)
        {
            state.HopTargetID = Guid.Empty;
            state.HopTargetRegionKey = default;
            state.HopSourceKey = default;
            state.HopDestinationKey = default;
            state.HopAxisIndex = 0;
            state.HopNextCandidateIndex = 0;
            state.HopSelected = false;
            state.HopBestSeam = default;
            state.HopHasBest = false;
            state.HopSawUnreadyCandidate = false;
            state.HopSearchComplete = false;
            state.HopOnwardAbsolute = default;
        }

        private static bool TryGetReducingNavigationNeighbour(
            SimulationRegionKey sourceKey,
            SimulationRegionKey targetKey,
            int axisIndex,
            out SimulationRegionKey neighbour,
            out bool hasAlternate)
        {
            neighbour = default;
            hasAlternate = false;
            long dx = (long)targetKey.RegionCoordinate.x - sourceKey.RegionCoordinate.x;
            long dz = (long)targetKey.RegionCoordinate.y - sourceKey.RegionCoordinate.y;
            long distance = Math.Abs(dx) + Math.Abs(dz);
            if (distance == 0L)
                return false;

            bool dominantX = dx != 0L && (dz == 0L || Math.Abs(dx) >= Math.Abs(dz));
            hasAlternate = dx != 0L && dz != 0L;
            if (axisIndex != 0 && (!hasAlternate || axisIndex != 1))
                throw new ArgumentOutOfRangeException(nameof(axisIndex));

            bool stepAlongX = axisIndex == 0 ? dominantX : !dominantX;
            int nextX = sourceKey.RegionCoordinate.x;
            int nextZ = sourceKey.RegionCoordinate.y;
            if (stepAlongX)
                nextX = checked(nextX + Math.Sign(dx));
            else
                nextZ = checked(nextZ + Math.Sign(dz));

            neighbour = new SimulationRegionKey(
                sourceKey.Context,
                new Vector2Int(nextX, nextZ));
            long remainingDistance = Math.Abs((long)targetKey.RegionCoordinate.x - nextX)
                + Math.Abs((long)targetKey.RegionCoordinate.y - nextZ);
            if (remainingDistance != distance - 1L)
            {
                throw new InvalidOperationException(
                    "The computed NPC navigation hop does not reduce target Manhattan distance by one.");
            }

            return true;
        }

        private static bool IsBetterNPCNavigationSeam(
            SimulationRegionSeam candidate,
            SimulationRegionSeam current)
        {
            if (candidate.TotalPathLength < current.TotalPathLength)
                return true;
            if (candidate.TotalPathLength > current.TotalPathLength)
                return false;

            bool alongX = candidate.SourceKey.RegionCoordinate.x == candidate.DestinationKey.RegionCoordinate.x;
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

        private void StoreNPCNavigationStateIfRegistered(
            Guid actorID,
            NPCNavigationState state)
        {
            if (_npcNavigationStates.TryGetValue(actorID, out var registeredState)
                && ReferenceEquals(registeredState.Actor, state.Actor)
                && ReferenceEquals(registeredState.Agent, state.Agent))
            {
                _npcNavigationStates[actorID] = state;
            }
        }

        private void UpdateNPCNavigationRegistration(Actor actor, bool hasEnabledAgent)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            if (!hasEnabledAgent || actor.Owner != null)
            {
                _npcNavigationStates.Remove(actor.ID);
                return;
            }

            var agent = actor.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.enabled)
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} was classified as NavMesh-driven without an enabled NavMeshAgent.");
            }

            if (_npcNavigationStates.TryGetValue(actor.ID, out var existing)
                && ReferenceEquals(existing.Actor, actor)
                && ReferenceEquals(existing.Agent, agent))
            {
                return;
            }

            var state = new NPCNavigationState
            {
                Actor = actor,
                Agent = agent
            };
            ClearNPCNavigationHop(ref state);
            _npcNavigationStates[actor.ID] = state;
        }

        private void RemoveNPCNavigationRegistration(Actor actor)
        {
            if (actor == null)
                return;

            if (!_npcNavigationStates.TryGetValue(
                    actor.ID,
                    out var state))
            {
                return;
            }

            if (!ReferenceEquals(state.Actor, actor))
                return;

            _npcNavigationStates.Remove(actor.ID);
        }

        private static float GetNPCPursuitStoppingDistance(Actor actor)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
            if (actor.Target == null)
                throw new InvalidOperationException($"NPC {actor.ID} has no pursuit target.");

            var autoAttackSpell = GameConfigurationManager.Config.AutoAttackSpell;
            if (autoAttackSpell == null)
            {
                throw new InvalidOperationException(
                    "NPC melee pursuit requires the configured auto-attack spell range.");
            }

            float stoppingDistance = autoAttackSpell.BaseRange
                + actor.HitboxRadius
                + actor.Target.HitboxRadius;
            if (float.IsNaN(stoppingDistance)
                || float.IsInfinity(stoppingDistance)
                || stoppingDistance < 0f)
            {
                throw new InvalidOperationException("The NPC pursuit stopping distance is invalid.");
            }

            return stoppingDistance;
        }

        private void ResolveNPCNavigationContext(
            NPCNavigationState state,
            out SimulationRegionDescriptor actorRegion,
            out SimulationRegionDescriptor targetRegion,
            out WorldPosition targetWorldPosition)
        {
            if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "Enabled NPC movement requires an initialized server physics manager.");
            }

            if (state.Actor == null
                || state.Actor.Target == null
                || state.Agent == null
                || !state.Agent.enabled)
            {
                throw new InvalidOperationException(
                    "NPC pursuit requires a live actor, target, and enabled NavMeshAgent.");
            }

            if (!ServerPhysicsManager.Instance.TryGetActorNavigationRegion(
                state.Actor.ID,
                out actorRegion,
                out var agentTypeID))
            {
                throw new InvalidOperationException(
                    $"Actor {state.Actor.ID} has an enabled NavMeshAgent but no registered region NavMesh.");
            }

            if (state.Agent.agentTypeID != agentTypeID)
            {
                throw new InvalidOperationException(
                    $"Actor {state.Actor.ID} NavMeshAgent type {state.Agent.agentTypeID} does not match region NavMesh agent type {agentTypeID}.");
            }

            if (!state.Agent.isOnNavMesh)
            {
                throw new InvalidOperationException(
                    $"Actor {state.Actor.ID} has an enabled NavMeshAgent that is not attached to a NavMesh.");
            }

            if (!state.Actor.SpatialContext.IsCompatibleWith(state.Actor.Target.SpatialContext))
            {
                throw new InvalidOperationException(
                    $"NPC {state.Actor.ID} and target {state.Actor.Target.ID} have incompatible spatial contexts.");
            }

            if (!ServerPhysicsManager.Instance.TryGetActorRegionDescriptor(
                state.Actor.Target.ID,
                out targetRegion))
            {
                throw new InvalidOperationException(
                    $"NPC target {state.Actor.Target.ID} is not bound to an active simulation region.");
            }

            if (!TryGetAcceptedWorldPosition(
                state.Actor.Target.ID,
                out targetWorldPosition))
            {
                throw new InvalidOperationException(
                    $"NPC target {state.Actor.Target.ID} has no live canonical ECS position.");
            }

            if (!targetWorldPosition.IsFinite)
            {
                throw new InvalidOperationException(
                    $"NPC target {state.Actor.Target.ID} has a non-finite canonical ECS world position.");
            }
        }

        private void UpdateSameRegionNPCDestination(
            NPCNavigationState state,
            SimulationRegionDescriptor actorRegion,
            WorldPosition targetWorldPosition)
        {
            if (state.Agent == null || !state.Agent.enabled)
                throw new InvalidOperationException("Same-region NPC pursuit requires an enabled NavMeshAgent.");

            if (!state.Agent.isOnNavMesh)
            {
                throw new InvalidOperationException(
                    $"Actor {state.Actor.ID} has an enabled NavMeshAgent that is not attached to a NavMesh.");
            }

            if (state.Agent.pathPending)
                return;

            var targetRegionLocalPosition = ServerPhysicsManager.Instance.ToRegionLocal(
                actorRegion,
                targetWorldPosition);
            state.Agent.stoppingDistance = GetNPCPursuitStoppingDistance(state.Actor);

            bool accepted = state.Agent.SetDestination(targetRegionLocalPosition);
            if (!accepted)
            {
                throw new InvalidOperationException(
                    $"NPC {state.Actor.ID} rejected same-region pursuit destination for target {state.Actor.Target.ID}. "
                    + $"actorRegion={actorRegion.Key}; "
                    + $"agentPosition={state.Agent.transform.position}; "
                    + $"nextPosition={state.Agent.nextPosition}; "
                    + $"targetAbsolute=({targetWorldPosition.x}, {targetWorldPosition.y}, {targetWorldPosition.z}); "
                    + $"requestedRegionLocal=({targetRegionLocalPosition.x}, {targetRegionLocalPosition.y}, {targetRegionLocalPosition.z}); "
                    + $"agentTypeID={state.Agent.agentTypeID}; "
                    + $"areaMask={state.Agent.areaMask}; "
                    + $"isOnNavMesh={state.Agent.isOnNavMesh}; "
                    + $"pathPending={state.Agent.pathPending}.");
            }
        }

        private void UpdateSameRegionNPCNavigationDestinations()
        {
            foreach (var kvp in new List<KeyValuePair<Guid, NPCNavigationState>>(_npcNavigationStates))
            {
                var state = kvp.Value;
                if (state.Actor == null
                    || state.Actor.Owner != null
                    || state.Actor.isDead
                    || state.Actor.Target == null
                    || state.Actor.Target.isDead
                    || state.Agent == null
                    || !state.Agent.enabled)
                {
                    continue;
                }

                ResolveNPCNavigationContext(
                    state,
                    out var actorRegion,
                    out var targetRegion,
                    out var targetWorldPosition);

                if (targetRegion.Key == actorRegion.Key)
                {
                    UpdateSameRegionNPCDestination(
                        state,
                        actorRegion,
                        targetWorldPosition);
                }
            }
        }

        private void UpdateNPCNavigation()
        {
            foreach (var kvp in new List<KeyValuePair<Guid, NPCNavigationState>>(_npcNavigationStates))
            {
                var actorID = kvp.Key;
                var state = kvp.Value;

                if (state.Actor == null
                    || state.Actor.Owner != null
                    || state.Agent == null
                    || !state.Agent.enabled)
                {
                    _npcNavigationStates.Remove(actorID);
                    continue;
                }

                if (state.Actor.isDead)
                {
                    _npcNavigationStates.Remove(actorID);
                    continue;
                }

                if (state.Actor.Target == null || state.Actor.Target.isDead)
                {
                    if (state.HopTargetID != Guid.Empty)
                        ClearNPCNavigationHop(ref state);
                    if (state.Agent.isOnNavMesh && state.Agent.hasPath)
                        state.Agent.ResetPath();
                    StoreNPCNavigationStateIfRegistered(actorID, state);
                    continue;
                }

                ResolveNPCNavigationContext(
                    state,
                    out var actorRegion,
                    out var targetRegion,
                    out var targetWorldPosition);

                if (targetRegion.Key == actorRegion.Key)
                {
                    if (state.HopTargetID != Guid.Empty)
                    {
                        ClearNPCNavigationHop(ref state);
                        StoreNPCNavigationStateIfRegistered(actorID, state);
                    }

                    continue;
                }

                if (!TryGetReducingNavigationNeighbour(
                        actorRegion.Key,
                        targetRegion.Key,
                        0,
                        out var firstNeighbour,
                        out bool hasAlternateNeighbour))
                {
                    throw new InvalidOperationException(
                        $"NPC target region {targetRegion.Key} is not different from actor region {actorRegion.Key}.");
                }

                bool resetHop = state.HopTargetID != state.Actor.Target.ID
                    || state.HopTargetRegionKey != targetRegion.Key
                    || state.HopSourceKey != actorRegion.Key
                    || state.HopAxisIndex < 0
                    || state.HopAxisIndex > 1;
                SimulationRegionKey expectedNeighbour = default;
                if (!resetHop
                    && !TryGetReducingNavigationNeighbour(
                        actorRegion.Key,
                        targetRegion.Key,
                        state.HopAxisIndex,
                        out expectedNeighbour,
                        out hasAlternateNeighbour))
                {
                    resetHop = true;
                }

                if (!resetHop && state.HopDestinationKey != expectedNeighbour)
                    resetHop = true;

                if (resetHop)
                {
                    ClearNPCNavigationHop(ref state);
                    state.HopTargetID = state.Actor.Target.ID;
                    state.HopTargetRegionKey = targetRegion.Key;
                    state.HopSourceKey = actorRegion.Key;
                    state.HopDestinationKey = firstNeighbour;
                    state.HopAxisIndex = 0;
                }

                if (state.HopSelected)
                {
                    if (!state.HopSearchComplete
                        || !state.HopHasBest
                        || state.HopBestSeam.SourceKey != actorRegion.Key
                        || state.HopBestSeam.DestinationKey != state.HopDestinationKey
                        || state.HopBestSeam.AgentTypeID != state.Agent.agentTypeID
                        || state.HopBestSeam.AreaMask != state.Agent.areaMask)
                    {
                        ClearNPCNavigationHop(ref state);
                        StoreNPCNavigationStateIfRegistered(actorID, state);
                        continue;
                    }

                    if (!ServerPhysicsManager.Instance.ValidateRegionSeam(
                            state.Agent,
                            state.HopBestSeam,
                            targetWorldPosition,
                            out var onwardAbsolute))
                    {
                        ClearNPCNavigationHop(ref state);
                        StoreNPCNavigationStateIfRegistered(actorID, state);
                        continue;
                    }

                    state.HopOnwardAbsolute = onwardAbsolute;
                    StoreNPCNavigationStateIfRegistered(actorID, state);
                    continue;
                }

                if (state.HopHasBest
                    && !ServerPhysicsManager.Instance.ValidateRegionSeam(
                        state.Agent,
                        state.HopBestSeam,
                        targetWorldPosition,
                        out _))
                {
                    ClearNPCNavigationHop(ref state);
                    state.HopTargetID = state.Actor.Target.ID;
                    state.HopTargetRegionKey = targetRegion.Key;
                    state.HopSourceKey = actorRegion.Key;
                    state.HopDestinationKey = firstNeighbour;
                    state.HopAxisIndex = 0;
                }

                bool foundInBatch = ServerPhysicsManager.Instance.ScanAdjacentRegionSeams(
                    state.Agent,
                    actorRegion.Key,
                    state.HopDestinationKey,
                    targetWorldPosition,
                    state.HopNextCandidateIndex,
                    4,
                    out var batchBest,
                    out var nextIndex,
                    out var exhausted,
                    out var navigationAvailable,
                    out var hadUnavailableCandidate);
                if (!navigationAvailable)
                {
                    state.HopSawUnreadyCandidate = true;
                    if (state.HopAxisIndex == 0 && hasAlternateNeighbour)
                    {
                        TryGetReducingNavigationNeighbour(
                            actorRegion.Key,
                            targetRegion.Key,
                            1,
                            out var alternateNeighbour,
                            out _);
                        state.HopAxisIndex = 1;
                        state.HopDestinationKey = alternateNeighbour;
                        state.HopNextCandidateIndex = 0;
                        state.HopHasBest = false;
                        state.HopBestSeam = default;
                        state.HopSelected = false;
                        state.HopSearchComplete = false;
                    }
                    else
                    {
                        ClearNPCNavigationHop(ref state);
                        state.HopTargetID = state.Actor.Target.ID;
                        state.HopTargetRegionKey = targetRegion.Key;
                        state.HopSourceKey = actorRegion.Key;
                        state.HopDestinationKey = firstNeighbour;
                        state.HopAxisIndex = 0;
                    }

                    StoreNPCNavigationStateIfRegistered(actorID, state);
                    continue;
                }

                if (hadUnavailableCandidate)
                    state.HopSawUnreadyCandidate = true;
                state.HopNextCandidateIndex = nextIndex;
                if (foundInBatch)
                {
                    if (batchBest.SourceKey != actorRegion.Key
                        || batchBest.DestinationKey != state.HopDestinationKey
                        || batchBest.AgentTypeID != state.Agent.agentTypeID
                        || batchBest.AreaMask != state.Agent.areaMask)
                    {
                        throw new InvalidOperationException(
                            "The seam scanner returned a candidate for an unexpected NPC or region pair.");
                    }

                    if (!state.HopHasBest || IsBetterNPCNavigationSeam(batchBest, state.HopBestSeam))
                    {
                        state.HopBestSeam = batchBest;
                        state.HopHasBest = true;
                    }
                }

                if (!exhausted)
                {
                    StoreNPCNavigationStateIfRegistered(actorID, state);
                    continue;
                }

                if (state.HopHasBest)
                {
                    if (!ServerPhysicsManager.Instance.ValidateRegionSeam(
                            state.Agent,
                            state.HopBestSeam,
                            targetWorldPosition,
                            out var selectedOnwardAbsolute))
                    {
                        ClearNPCNavigationHop(ref state);
                        StoreNPCNavigationStateIfRegistered(actorID, state);
                        continue;
                    }

                    state.HopSelected = true;
                    state.HopSearchComplete = true;
                    state.HopOnwardAbsolute = selectedOnwardAbsolute;
                    state.Agent.stoppingDistance = 0f;
                    var approachLocal = ServerPhysicsManager.Instance.ToRegionLocal(
                        actorRegion,
                        state.HopBestSeam.ApproachAbsolute);
                    if (!state.Agent.SetDestination(approachLocal))
                    {
                        ClearNPCNavigationHop(ref state);
                        StoreNPCNavigationStateIfRegistered(actorID, state);
                        continue;
                    }

                    StoreNPCNavigationStateIfRegistered(actorID, state);
                    continue;
                }

                if (state.HopAxisIndex == 0 && hasAlternateNeighbour)
                {
                    TryGetReducingNavigationNeighbour(
                        actorRegion.Key,
                        targetRegion.Key,
                        1,
                        out var alternateNeighbour,
                        out _);
                    state.HopAxisIndex = 1;
                    state.HopDestinationKey = alternateNeighbour;
                    state.HopNextCandidateIndex = 0;
                    state.HopHasBest = false;
                    state.HopBestSeam = default;
                    state.HopSearchComplete = false;
                    StoreNPCNavigationStateIfRegistered(actorID, state);
                    continue;
                }

                if (!state.HopSawUnreadyCandidate)
                {
                    state.Agent.ResetPath();
                    if (state.Actor.Target != null
                        && state.Actor.Target.ID == state.HopTargetID)
                    {
                        state.Actor.SetTargetFromServer(null);
                    }

                    ClearNPCNavigationHop(ref state);
                    StoreNPCNavigationStateIfRegistered(actorID, state);
                    continue;
                }

                ClearNPCNavigationHop(ref state);
                StoreNPCNavigationStateIfRegistered(actorID, state);
            }
        }

        private bool TryCommitReadyNPCRegionHop(
            Actor actor,
            WorldPosition acceptedSourcePosition,
            out WorldPosition acceptedLandingAbsolute)
        {
            acceptedLandingAbsolute = default;
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
            if (!acceptedSourcePosition.IsFinite)
                throw new ArgumentException("The accepted source position must be finite.", nameof(acceptedSourcePosition));

            if (!_npcNavigationStates.TryGetValue(actor.ID, out var state)
                || !ReferenceEquals(state.Actor, actor)
                || actor.GetComponent<NavMeshAgent>() != state.Agent
                || state.Agent == null
                || !state.Agent.enabled
                || !state.Agent.isOnNavMesh
                || !state.HopSelected
                || !state.HopSearchComplete
                || !state.HopHasBest)
            {
                return false;
            }

            if (actor.Target == null || actor.Target.isDead)
            {
                ClearNPCNavigationHop(ref state);
                StoreNPCNavigationStateIfRegistered(actor.ID, state);
                return false;
            }

            if (!actor.SpatialContext.IsCompatibleWith(actor.Target.SpatialContext))
            {
                ClearNPCNavigationHop(ref state);
                StoreNPCNavigationStateIfRegistered(actor.ID, state);
                return false;
            }

            if (actor.Target.ID != state.HopTargetID)
            {
                ClearNPCNavigationHop(ref state);
                StoreNPCNavigationStateIfRegistered(actor.ID, state);
                return false;
            }

            if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
                throw new InvalidOperationException("A ready NPC region hop requires an initialized server physics manager.");

            if (!ServerPhysicsManager.Instance.TryGetActorRegionDescriptor(
                    actor.ID,
                    out var sourceRegion)
                || sourceRegion.Key != state.HopSourceKey)
            {
                ClearNPCNavigationHop(ref state);
                StoreNPCNavigationStateIfRegistered(actor.ID, state);
                return false;
            }

            if (!sourceRegion.Key.Context.IsCompatibleWith(actor.SpatialContext))
            {
                ClearNPCNavigationHop(ref state);
                StoreNPCNavigationStateIfRegistered(actor.ID, state);
                return false;
            }

            if (ServerPhysicsManager.Instance.GetRegionKey(
                    actor.SpatialContext,
                    acceptedSourcePosition.Chunk) != state.HopSourceKey)
            {
                ClearNPCNavigationHop(ref state);
                StoreNPCNavigationStateIfRegistered(actor.ID, state);
                return false;
            }

            if (!ServerPhysicsManager.Instance.TryGetActorRegionDescriptor(
                    actor.Target.ID,
                    out var targetRegion)
                || !TryGetAcceptedWorldPosition(
                    actor.Target.ID,
                    out var targetAbsolute))
            {
                ClearNPCNavigationHop(ref state);
                StoreNPCNavigationStateIfRegistered(actor.ID, state);
                return false;
            }

            if (!targetAbsolute.IsFinite
                || !targetRegion.Key.Context.IsCompatibleWith(actor.Target.SpatialContext)
                || targetRegion.Key != state.HopTargetRegionKey)
            {
                ClearNPCNavigationHop(ref state);
                StoreNPCNavigationStateIfRegistered(actor.ID, state);
                return false;
            }

            if (!ServerPhysicsManager.Instance.TryGetActorNavigationRegion(
                    actor.ID,
                    out var registeredRegion,
                    out var registeredAgentTypeID)
                || registeredRegion.Key != state.HopSourceKey
                || state.Agent.agentTypeID != registeredAgentTypeID
                || state.Agent.agentTypeID != state.HopBestSeam.AgentTypeID
                || state.Agent.areaMask != state.HopBestSeam.AreaMask)
            {
                ClearNPCNavigationHop(ref state);
                StoreNPCNavigationStateIfRegistered(actor.ID, state);
                return false;
            }

            if (state.HopBestSeam.SourceKey != state.HopSourceKey
                || state.HopBestSeam.DestinationKey != state.HopDestinationKey)
            {
                ClearNPCNavigationHop(ref state);
                StoreNPCNavigationStateIfRegistered(actor.ID, state);
                return false;
            }

            if (!ServerPhysicsManager.Instance.IsRegionSeamApproachReady(state.Agent))
                return false;

            float directStoppingDistance = GetNPCPursuitStoppingDistance(actor);
            bool crossed = ServerPhysicsManager.Instance.TryHandoffEnabledNavMeshActor(
                actor.ID,
                actor.SpatialContext,
                state.Agent,
                acceptedSourcePosition,
                state.HopBestSeam,
                targetAbsolute,
                directStoppingDistance);
            if (!crossed)
            {
                if (!ServerPhysicsManager.Instance.ValidateRegionSeam(
                        state.Agent,
                        state.HopBestSeam,
                        targetAbsolute,
                        out _))
                {
                    ClearNPCNavigationHop(ref state);
                    StoreNPCNavigationStateIfRegistered(actor.ID, state);
                }

                return false;
            }

            acceptedLandingAbsolute = state.HopBestSeam.LandingAbsolute;
            ClearNPCNavigationHop(ref state);
            StoreNPCNavigationStateIfRegistered(actor.ID, state);
            return true;
        }

        private void EnsureNPCMovementComponents(Entity entity, Actor actor)
        {
            // Components remain attached when ownership returns to a player; the owner checks in
            // ActorUpdateSystem and both movement bridges keep player movement out of this path.
            bool hasEnabledAgent = HasEnabledNavMeshAgent(actor);
            UpdateNPCNavigationRegistration(actor, hasEnabledAgent);
            UpdateNPCNavigationRegistration(actor, hasEnabledAgent);

            if (_entityManager.HasComponent<Systems.MotionStateComponent>(entity))
            {
                var motion = _entityManager.GetComponentData<Systems.MotionStateComponent>(entity);
                motion.UsesNavMeshAgent = hasEnabledAgent;
                _entityManager.SetComponentData(entity, motion);
            }
            else
            {
                _entityManager.AddComponentData(entity, new Systems.MotionStateComponent
                {
                    VerticalVelocity = 0f,
                    IsGrounded = true,
                    TimeSinceLastGround = 0f,
                    UsesNavMeshAgent = hasEnabledAgent
                });
            }

            if (!_entityManager.HasComponent<Systems.GroundDetectionComponent>(entity))
            {
                _entityManager.AddComponentData(entity, new Systems.GroundDetectionComponent
                {
                    groundDetectionRadius = GameConfigurationManager.Config.GroundDetectionRadius,
                    lastGroundCheck = new Unity.Mathematics.float3(0, -1, 0)
                });
            }
        }

        private static bool HasEnabledNavMeshAgent(Actor actor)
        {
            var agent = actor.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.enabled)
                return false;

            if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "Enabled NPC NavMesh movement requires an initialized server physics manager.");
            }

            if (!ServerPhysicsManager.Instance.TryGetActorNavigationRegion(
                actor.ID,
                out var region,
                out var agentTypeID))
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} has an enabled NavMeshAgent but no registered region NavMesh.");
            }

            if (!region.Key.Context.IsCompatibleWith(actor.SpatialContext))
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} region context is incompatible with the actor context.");
            }

            if (agent.agentTypeID != agentTypeID)
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} NavMeshAgent type {agent.agentTypeID} does not match region NavMesh agent type {agentTypeID}.");
            }

            if (!agent.isOnNavMesh)
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} has an enabled NavMeshAgent that is not attached to a NavMesh.");
            }

            return true;
        }

        private static SimulationRegionDescriptor GetVerifiedActorRegion(Actor actor)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "NPC movement synchronization requires an initialized server physics manager.");
            }

            if (!ServerPhysicsManager.Instance.TryGetActorRegionDescriptor(actor.ID, out var descriptor))
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} is not bound to an active simulation region.");
            }

            if (!descriptor.Key.Context.IsCompatibleWith(actor.SpatialContext))
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} region context is incompatible with the actor context.");
            }

            return descriptor;
        }

        private void SynchronizeNPCPositionsToECS()
        {
            TraverseNPCMovementEntities(SynchronizeNPCPositionToECS);
        }

        private void SynchronizeNPCTransformsFromECS()
        {
            TraverseNPCMovementEntities(SynchronizeNPCTransformFromECS);
        }

        private void TraverseNPCMovementEntities(
            Func<Entity, Systems.ActorComponent, Actor, bool> synchronize)
        {
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int syncCount = 0;
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            int fullScanCount = entities.Length;
            try
            {
                foreach (var entity in entities)
                {
                    var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                    if (component.Owner != default)
                        continue;

                    if (!_actorObjects.TryGetValue(component.ActorID, out var actorObj) || actorObj == null)
                        continue;

                    if (synchronize(entity, component, actorObj) && telemetryEnabled)
                        syncCount++;
                }
            }
            finally
            {
                entities.Dispose();
            }
            if (telemetryEnabled)
            {
                RecordTickSyncContribution(
                    fullScanCount,
                    syncCount,
                    ServerECSManager.CurrentSimulationTickID);
            }
        }

        private bool SynchronizeNPCPositionToECS(
            Entity entity,
            Systems.ActorComponent component,
            Actor actorObj)
        {
            var motion = _entityManager.GetComponentData<Systems.MotionStateComponent>(entity);
            bool hasEnabledAgent = HasEnabledNavMeshAgent(actorObj);
            UpdateNPCNavigationRegistration(actorObj, hasEnabledAgent);
            UpdateNPCNavigationRegistration(actorObj, hasEnabledAgent);
            if (motion.UsesNavMeshAgent != hasEnabledAgent)
            {
                motion.UsesNavMeshAgent = hasEnabledAgent;
                _entityManager.SetComponentData(entity, motion);
            }

            if (!hasEnabledAgent)
                return false;

            var acceptedSourcePosition = new WorldPosition(
                component.WorldPositionD.x,
                component.WorldPositionD.y,
                component.WorldPositionD.z);
            if (!acceptedSourcePosition.IsFinite)
            {
                throw new InvalidOperationException(
                    $"Actor {actorObj.ID} has an invalid canonical ECS world position.");
            }

            bool crossed = TryCommitReadyNPCRegionHop(
                actorObj,
                acceptedSourcePosition,
                out WorldPosition selectedLandingAbsolute);
            bool crossed = TryCommitReadyNPCRegionHop(
                actorObj,
                acceptedSourcePosition,
                out WorldPosition selectedLandingAbsolute);

            var region = GetVerifiedActorRegion(actorObj);
            WorldPosition worldPosition;
            if (crossed)
            {
                if (!selectedLandingAbsolute.IsFinite
                    || ServerPhysicsManager.Instance.GetRegionKey(
                        actorObj.SpatialContext,
                        selectedLandingAbsolute.Chunk)
                    != region.Key)
                {
                    throw new InvalidOperationException(
                        $"Actor {actorObj.ID} crossed into an unexpected simulation region.");
                }

                worldPosition = selectedLandingAbsolute;
            }
            else
            {
                worldPosition = ServerPhysicsManager.Instance.ToWorld(
                    region,
                    actorObj.transform.position);
            }

            component.WorldPositionD = new Unity.Mathematics.double3(
                worldPosition.x,
                worldPosition.y,
                worldPosition.z);
            _entityManager.SetComponentData(entity, component);
            return true;
        }

        private bool SynchronizeNPCTransformFromECS(
            Entity entity,
            Systems.ActorComponent component,
            Actor actorObj)
        {
            // The pre-tick bridge resolved ownership for this tick. A NavMesh agent remains
            // the sole transform writer; ECS-owned NPCs receive the completed simulation state.
            if (_entityManager.GetComponentData<Systems.MotionStateComponent>(entity).UsesNavMeshAgent)
                return false;

            var worldPosition = new WorldPosition(
                component.WorldPositionD.x,
                component.WorldPositionD.y,
                component.WorldPositionD.z);
            if (!worldPosition.IsFinite)
            {
                throw new InvalidOperationException(
                    $"Actor {actorObj.ID} has an invalid canonical ECS world position.");
            }

            var currentRegion = GetVerifiedActorRegion(actorObj);
            var destinationKey = ServerPhysicsManager.Instance.GetRegionKey(
                actorObj.SpatialContext,
                worldPosition.Chunk);
            if (destinationKey == currentRegion.Key)
            {
                actorObj.transform.position = ServerPhysicsManager.Instance.ToRegionLocal(
                    currentRegion,
                    worldPosition);
                return true;
            }

            var destinationChunk = new WorldChunkKey(actorObj.SpatialContext, worldPosition.Chunk);
            if (!ServerPhysicsManager.Instance.TryGetEnvironmentRegionDescriptor(
                    destinationChunk,
                    out var destinationEnvironmentRegion))
            {
                throw new InvalidOperationException(
                    $"Actor {actorObj.ID} cannot synchronize to unavailable destination environment chunk {destinationChunk.ChunkCoord}.");
            }
            if (destinationEnvironmentRegion.Key != destinationKey)
            {
                throw new InvalidOperationException(
                    $"Actor {actorObj.ID} destination environment chunk {destinationChunk.ChunkCoord} is bound to simulation region {destinationEnvironmentRegion.Key}, expected {destinationKey}.");
            }

            ServerPhysicsManager.Instance.ToRegionLocal(
                destinationEnvironmentRegion,
                worldPosition);
            var transitioned = ServerPhysicsManager.Instance.TransitionActorRegion(
                actorObj.ID,
                actorObj.SpatialContext,
                worldPosition,
                out var previousRegion,
                out var destinationRegion);
            if (!transitioned
                || previousRegion.Key != currentRegion.Key
                || destinationRegion.Key != destinationKey)
            {
                throw new InvalidOperationException(
                    $"Actor {actorObj.ID} did not transition from simulation region {currentRegion.Key} to destination region {destinationKey} as expected.");
            }

            actorObj.transform.position = ServerPhysicsManager.Instance.ToRegionLocal(
                destinationRegion,
                worldPosition);
            return true;
        }

        private void RecordTickSyncContribution(int fullScanCount, int gameObjectSyncCount, long tickID)
        {
            if (tickID <= 0)
                throw new InvalidOperationException("Actor bridge telemetry requires an active authoritative simulation tick.");

            if (_hasPendingTickSyncContribution && _pendingTickID != tickID)
                throw new InvalidOperationException("Actor bridge telemetry contributions crossed authoritative simulation ticks before the previous tick was flushed.");

            if (!_hasPendingTickSyncContribution)
            {
                _pendingTickID = tickID;
                _pendingTickEntityScanCount = 0;
                _pendingTickGameObjectSyncCount = 0;
                _hasPendingTickSyncContribution = true;
            }

            _pendingTickEntityScanCount += fullScanCount;
            _pendingTickGameObjectSyncCount += gameObjectSyncCount;
        }

        private void FlushPendingTickSyncContribution()
        {
            if (!_hasPendingTickSyncContribution)
                return;

            try
            {
                PerformanceTelemetry.TryRecord(
                    FULL_ENTITY_SCAN_COUNT_METRIC,
                    _pendingTickEntityScanCount,
                    _pendingTickID);
                PerformanceTelemetry.TryRecord(
                    GAMEOBJECT_SYNC_COUNT_METRIC,
                    _pendingTickGameObjectSyncCount,
                    _pendingTickID);
            }
            finally
            {
                _pendingTickID = -1;
                _pendingTickEntityScanCount = 0;
                _pendingTickGameObjectSyncCount = 0;
                _hasPendingTickSyncContribution = false;
            }
        }
    }
}
