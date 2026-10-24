using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Server.Core
{
    /// <summary>Coordinates server-authoritative auto-attacks.</summary>
    public sealed class ServerAutoAttackManager : MonoBehaviour
    {
        private static readonly PerformanceMetricDefinition UPDATE_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_AUTO_ATTACK_UPDATE_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "auto_attack",
            "server_schedule");
        private static readonly PerformanceMetricDefinition ATTACKER_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_AUTO_ATTACKER_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "auto_attack",
            "server_schedule");
        private static readonly PerformanceMarker SCHEDULE_MARKER = new(
            UPDATE_DURATION_METRIC,
            "Server.AutoAttack.Scheduled");
        private static ServerAutoAttackManager _instance;
        /// <summary>Gets the active server auto-attack manager.</summary>
        public static ServerAutoAttackManager Instance => _instance;
        private static bool _initialized;
        /// <summary>Gets whether the server auto-attack manager is initialized.</summary>
        public static bool IsInitialized => _initialized;

        internal static void RegisterPerformanceMetrics()
        {
            PerformanceTelemetry.RegisterMetric(UPDATE_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(ATTACKER_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
        }

        private SpellDefinition _autoAttackSpell;

        private struct AutoAttackState
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

        private static void ClearHop(ref AutoAttackState state)
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

        private static bool TryGetReducingNeighbour(
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
                    "The computed auto-attack hop does not reduce target Manhattan distance by one.");
            }

            return true;
        }

        private static bool IsBetterHopSeam(
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

        private void StoreAutoAttackStateIfRegistered(
            Guid actorID,
            AutoAttackState state)
        {
            if (_activeAutoAttackers.TryGetValue(actorID, out var registeredState)
                && ReferenceEquals(registeredState.Actor, state.Actor)
                && ReferenceEquals(registeredState.Agent, state.Agent))
            {
                _activeAutoAttackers[actorID] = state;
            }
        }

        private readonly Dictionary<Guid, AutoAttackState> _activeAutoAttackers = new();
        private readonly List<Guid> _toRemove = new();
        private ServerSimulationRegistrationHandle _autoAttackSchedule;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Error("Multiple instances of ServerAutoAttackManager detected! Destroying duplicate.");
                Destroy(this);
                return;
            }
            _instance = this;

            _autoAttackSpell = GameConfigurationManager.Config.AutoAttackSpell;
            if (_autoAttackSpell == null)
                Debug.Warning("GameConfiguration.AutoAttackSpell is not assigned. Auto-attacks will not function.");

            ActorEndpoint.onServerStartAutoAttack += OnStartAutoAttack;
            ActorEndpoint.onServerStopAutoAttack += OnStopAutoAttack;
            ActorEndpoint.onServerActorDespawned += OnActorDespawned;
            ActorEndpoint.onActorDeath += OnActorDeath;

            _initialized = true;
            EnsureScheduleRegistered();
        }

        private void OnDestroy()
        {
            _autoAttackSchedule.Cancel();
            _autoAttackSchedule = default;

            ActorEndpoint.onServerStartAutoAttack -= OnStartAutoAttack;
            ActorEndpoint.onServerStopAutoAttack -= OnStopAutoAttack;
            ActorEndpoint.onServerActorDespawned -= OnActorDespawned;
            ActorEndpoint.onActorDeath -= OnActorDeath;

            _activeAutoAttackers.Clear();
            _toRemove.Clear();

            _instance = null;
            _initialized = false;
        }

        private void OnStartAutoAttack(Actor actor)
        {
            if (_autoAttackSpell == null || actor == null || actor.isDead) return;
            if (actor.Target == null || actor.Target.isDead) return;

            var state = new AutoAttackState
            {
                Actor = actor,
                Agent = actor.GetComponent<NavMeshAgent>()
            };
            ClearHop(ref state);
            _activeAutoAttackers[actor.ID] = state;
        }

        private void OnStopAutoAttack(Actor actor)
        {
            if (actor == null)
                return;

            _activeAutoAttackers.Remove(actor.ID);
        }

        private void OnActorDespawned(Actor actor)
        {
            RemoveActorAndTarget(actor);
        }

        private void OnActorDeath(Actor actor)
        {
            RemoveActorAndTarget(actor);
        }

        private void RemoveActorAndTarget(Actor actor)
        {
            if (ReferenceEquals(actor, null))
                return;

            _activeAutoAttackers.Remove(actor.ID);

            foreach (var kvp in new List<KeyValuePair<Guid, AutoAttackState>>(_activeAutoAttackers))
            {
                var trackedActor = kvp.Value.Actor;
                if (trackedActor == null)
                    continue;

                if (ReferenceEquals(trackedActor, actor)
                    || ReferenceEquals(trackedActor.Target, actor))
                    _activeAutoAttackers.Remove(kvp.Key);
            }
        }

        private void EnsureScheduleRegistered()
        {
            if (!ServerSimulationClock.IsRunning)
            {
                if (_autoAttackSchedule.IsValid)
                {
                    _autoAttackSchedule.Cancel();
                    _autoAttackSchedule = default;
                }

                return;
            }

            if (!_autoAttackSchedule.IsValid)
                _autoAttackSchedule = ServerSimulationClock.RegisterEachTick(ProcessAuthoritativeTick);
        }

        private void Update()
        {
            EnsureScheduleRegistered();
            if (_autoAttackSpell == null)
                return;

            _toRemove.Clear();

            foreach (var kvp in new List<KeyValuePair<Guid, AutoAttackState>>(_activeAutoAttackers))
            {
                var actorID = kvp.Key;
                var state = kvp.Value;

                if (state.Actor == null || state.Actor.isDead)
                {
                    _toRemove.Add(actorID);
                    continue;
                }

                if (state.Actor.Target == null || state.Actor.Target.isDead)
                {
                    _toRemove.Add(actorID);
                    continue;
                }

                // NPC chase — prepare cross-region route state during the frame-side pass.
                if (state.Agent != null && state.Agent.enabled)
                {
                    ResolveChaseNavigationContext(
                        state,
                        out var actorRegion,
                        out var targetRegion,
                        out var targetWorldPosition);

                    if (targetRegion.Key == actorRegion.Key)
                    {
                        if (state.HopTargetID != Guid.Empty)
                        {
                            ClearHop(ref state);
                            StoreAutoAttackStateIfRegistered(
                                actorID,
                                state);
                        }

                        continue;
                    }

                    if (!TryGetReducingNeighbour(
                            actorRegion.Key,
                            targetRegion.Key,
                            0,
                            out var firstNeighbour,
                            out bool hasAlternateNeighbour))
                    {
                        throw new InvalidOperationException(
                            $"Auto-attack target region {targetRegion.Key} is not different from actor region {actorRegion.Key}.");
                    }

                    bool resetHop = state.HopTargetID != state.Actor.Target.ID
                        || state.HopTargetRegionKey != targetRegion.Key
                        || state.HopSourceKey != actorRegion.Key
                        || state.HopAxisIndex < 0
                        || state.HopAxisIndex > 1;
                    SimulationRegionKey expectedNeighbour = default;
                    if (!resetHop
                        && !TryGetReducingNeighbour(
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
                        ClearHop(ref state);
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
                            ClearHop(ref state);
                            StoreAutoAttackStateIfRegistered(actorID, state);
                            continue;
                        }

                        if (!ServerPhysicsManager.Instance.ValidateRegionSeam(
                                state.Agent,
                                state.HopBestSeam,
                                targetWorldPosition,
                                out var onwardAbsolute))
                        {
                            ClearHop(ref state);
                            StoreAutoAttackStateIfRegistered(actorID, state);
                            continue;
                        }

                        state.HopOnwardAbsolute = onwardAbsolute;
                        StoreAutoAttackStateIfRegistered(actorID, state);
                        continue;
                    }

                    if (state.HopHasBest
                        && !ServerPhysicsManager.Instance.ValidateRegionSeam(
                            state.Agent,
                            state.HopBestSeam,
                            targetWorldPosition,
                            out _))
                    {
                        ClearHop(ref state);
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
                            TryGetReducingNeighbour(
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
                            ClearHop(ref state);
                            state.HopTargetID = state.Actor.Target.ID;
                            state.HopTargetRegionKey = targetRegion.Key;
                            state.HopSourceKey = actorRegion.Key;
                            state.HopDestinationKey = firstNeighbour;
                            state.HopAxisIndex = 0;
                        }

                        StoreAutoAttackStateIfRegistered(actorID, state);
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
                            throw new InvalidOperationException("The seam scanner returned a candidate for an unexpected actor or region pair.");
                        }

                        if (!state.HopHasBest || IsBetterHopSeam(batchBest, state.HopBestSeam))
                        {
                            state.HopBestSeam = batchBest;
                            state.HopHasBest = true;
                        }
                    }

                    if (!exhausted)
                    {
                        StoreAutoAttackStateIfRegistered(actorID, state);
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
                            ClearHop(ref state);
                            StoreAutoAttackStateIfRegistered(actorID, state);
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
                            ClearHop(ref state);
                            StoreAutoAttackStateIfRegistered(actorID, state);
                            continue;
                        }

                        StoreAutoAttackStateIfRegistered(actorID, state);
                        continue;
                    }

                    if (state.HopAxisIndex == 0 && hasAlternateNeighbour)
                    {
                        TryGetReducingNeighbour(
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
                        StoreAutoAttackStateIfRegistered(actorID, state);
                        continue;
                    }

                    if (!state.HopSawUnreadyCandidate)
                    {
                        state.Agent.ResetPath();
                        ActorEndpoint.onServerStopAutoAttack?.Invoke(state.Actor);
                        if (state.Actor.Owner == null
                            && state.Actor.Target != null
                            && state.Actor.Target.ID == state.HopTargetID)
                        {
                            state.Actor.SetTargetFromServer(null);
                        }

                        continue;
                    }

                    ClearHop(ref state);
                    StoreAutoAttackStateIfRegistered(actorID, state);
                }
            }

            RemoveQueuedAutoAttackers();
        }

        private static void ResolveChaseNavigationContext(
            AutoAttackState state,
            out SimulationRegionDescriptor actorRegion,
            out SimulationRegionDescriptor targetRegion,
            out WorldPosition targetWorldPosition)
        {
            if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "Enabled NPC auto-attack movement requires an initialized server physics manager.");
            }

            if (state.Actor == null
                || state.Actor.Target == null
                || state.Agent == null
                || !state.Agent.enabled)
            {
                throw new InvalidOperationException(
                    "NPC auto-attack chase requires a live actor, target, and enabled NavMeshAgent.");
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
                    $"Auto-attack actor {state.Actor.ID} and target {state.Actor.Target.ID} have incompatible spatial contexts.");
            }

            if (!ServerPhysicsManager.Instance.TryGetActorRegionDescriptor(
                state.Actor.Target.ID,
                out targetRegion))
            {
                throw new InvalidOperationException(
                    $"Auto-attack target {state.Actor.Target.ID} is not bound to an active simulation region.");
            }

            if (!ServerActorManager.IsInitialized || ServerActorManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "NPC auto-attack chase requires an initialized server actor manager.");
            }

            if (!ServerActorManager.Instance.TryGetAcceptedWorldPosition(
                state.Actor.Target.ID,
                out targetWorldPosition))
            {
                throw new InvalidOperationException(
                    $"Auto-attack target {state.Actor.Target.ID} has no live canonical ECS position.");
            }

            if (!targetWorldPosition.IsFinite)
            {
                throw new InvalidOperationException(
                    $"Auto-attack target {state.Actor.Target.ID} has a non-finite canonical ECS world position.");
            }
        }

        private void UpdateSameRegionChaseDestination(
            AutoAttackState state,
            SimulationRegionDescriptor actorRegion,
            WorldPosition targetWorldPosition)
        {
            if (state.Agent == null || !state.Agent.enabled)
            {
                throw new InvalidOperationException(
                    "Same-region NPC chase requires an enabled NavMeshAgent.");
            }

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
            state.Agent.stoppingDistance = _autoAttackSpell.BaseRange
                + state.Actor.HitboxRadius + state.Actor.Target.HitboxRadius;

            bool accepted = state.Agent.SetDestination(targetRegionLocalPosition);
            if (!accepted)
            {
                throw new InvalidOperationException(
                    $"NPC {state.Actor.ID} rejected same-region chase destination for target {state.Actor.Target.ID}. "
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

        /// <summary>
        /// Commits one selected, source-side region hop for the pre-ECS movement boundary.
        /// </summary>
        /// <param name="actor">The currently auto-attacking NPC.</param>
        /// <param name="acceptedSourcePosition">The latest accepted source position.</param>
        /// <param name="acceptedLandingAbsolute">Receives the selected absolute landing on success.</param>
        /// <returns><see langword="true"/> when the physics owner completed the selected hop; otherwise <see langword="false"/>.</returns>
        internal bool TryCommitReadyRegionHop(
            Actor actor,
            WorldPosition acceptedSourcePosition,
            out WorldPosition acceptedLandingAbsolute)
        {
            acceptedLandingAbsolute = default;
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));
            if (!acceptedSourcePosition.IsFinite)
                throw new ArgumentException("The accepted source position must be finite.", nameof(acceptedSourcePosition));

            if (!_activeAutoAttackers.TryGetValue(actor.ID, out var state)
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
                ClearHop(ref state);
                StoreAutoAttackStateIfRegistered(actor.ID, state);
                return false;
            }

            if (!actor.SpatialContext.IsCompatibleWith(actor.Target.SpatialContext))
            {
                ClearHop(ref state);
                StoreAutoAttackStateIfRegistered(actor.ID, state);
                return false;
            }
            if (actor.Target.ID != state.HopTargetID)
            {
                ClearHop(ref state);
                StoreAutoAttackStateIfRegistered(actor.ID, state);
                return false;
            }

            if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
                throw new InvalidOperationException("A ready region hop requires an initialized server physics manager.");

            if (!ServerPhysicsManager.Instance.TryGetActorRegionDescriptor(
                    actor.ID,
                    out var sourceRegion)
                || sourceRegion.Key != state.HopSourceKey)
            {
                ClearHop(ref state);
                StoreAutoAttackStateIfRegistered(actor.ID, state);
                return false;
            }
            if (!sourceRegion.Key.Context.IsCompatibleWith(actor.SpatialContext))
            {
                ClearHop(ref state);
                StoreAutoAttackStateIfRegistered(actor.ID, state);
                return false;
            }
            if (ServerPhysicsManager.Instance.GetRegionKey(
                    actor.SpatialContext,
                    acceptedSourcePosition.Chunk) != state.HopSourceKey)
            {
                ClearHop(ref state);
                StoreAutoAttackStateIfRegistered(actor.ID, state);
                return false;
            }

            if (!ServerPhysicsManager.Instance.TryGetActorRegionDescriptor(
                    actor.Target.ID,
                    out var targetRegion)
                || !ServerActorManager.IsInitialized
                || ServerActorManager.Instance == null
                || !ServerActorManager.Instance.TryGetAcceptedWorldPosition(
                    actor.Target.ID,
                    out var targetAbsolute))
            {
                ClearHop(ref state);
                StoreAutoAttackStateIfRegistered(actor.ID, state);
                return false;
            }
            if (!targetAbsolute.IsFinite
                || !targetRegion.Key.Context.IsCompatibleWith(actor.Target.SpatialContext)
                || targetRegion.Key != state.HopTargetRegionKey)
            {
                ClearHop(ref state);
                StoreAutoAttackStateIfRegistered(actor.ID, state);
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
                ClearHop(ref state);
                StoreAutoAttackStateIfRegistered(actor.ID, state);
                return false;
            }
            if (state.HopBestSeam.SourceKey != state.HopSourceKey
                || state.HopBestSeam.DestinationKey != state.HopDestinationKey)
            {
                ClearHop(ref state);
                StoreAutoAttackStateIfRegistered(actor.ID, state);
                return false;
            }

            if (!ServerPhysicsManager.Instance.IsRegionSeamApproachReady(state.Agent))
                return false;

            float directStoppingDistance = _autoAttackSpell.BaseRange
                + actor.HitboxRadius + actor.Target.HitboxRadius;
            if (float.IsNaN(directStoppingDistance)
                || float.IsInfinity(directStoppingDistance)
                || directStoppingDistance < 0f)
            {
                throw new InvalidOperationException("The direct auto-attack stopping distance is invalid.");
            }

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
                    ClearHop(ref state);
                    StoreAutoAttackStateIfRegistered(actor.ID, state);
                }

                return false;
            }

            acceptedLandingAbsolute = state.HopBestSeam.LandingAbsolute;
            ClearHop(ref state);
            StoreAutoAttackStateIfRegistered(actor.ID, state);
            return true;
        }

        private void ProcessAuthoritativeTick(ServerSimulationTick tick)
        {
            using var markerScope = SCHEDULE_MARKER.Begin();
            if (_autoAttackSpell == null)
                return;

            _toRemove.Clear();

            foreach (var kvp in new List<KeyValuePair<Guid, AutoAttackState>>(_activeAutoAttackers))
            {
                var actorID = kvp.Key;
                var state = kvp.Value;

                if (!_activeAutoAttackers.TryGetValue(actorID, out var currentState)
                    || currentState.Actor != state.Actor)
                    continue;

                if (state.Actor == null || state.Actor.isDead)
                {
                    _toRemove.Add(actorID);
                    continue;
                }

                if (state.Actor.Target == null || state.Actor.Target.isDead)
                {
                    _toRemove.Add(actorID);
                    continue;
                }

                if (state.Agent != null
                    && state.Agent.enabled)
                {
                    ResolveChaseNavigationContext(
                        state,
                        out var actorRegion,
                        out var targetRegion,
                        out var targetWorldPosition);

                    if (targetRegion.Key == actorRegion.Key)
                    {
                        UpdateSameRegionChaseDestination(
                            state,
                            actorRegion,
                            targetWorldPosition);
                    }
                }

                // Pause auto-attack while casting an ability.
                if (ServerSpellCastManager.Instance.IsCasting(actorID))
                    continue;

                // Check range — skip swing but don't remove (player may walk back).
                if (!ServerActorManager.IsInitialized
                    || ServerActorManager.Instance == null)
                {
                    throw new InvalidOperationException(
                        "Auto-attack range validation requires initialized server actor manager.");
                }

                double distance = ServerActorManager.Instance
                    .GetAcceptedSurfaceDistance(
                        state.Actor,
                        state.Actor.Target);
                if (distance > _autoAttackSpell.BaseRange)
                    continue;

                // Wait for the auto-attack cooldown to expire before swinging again.
                if (ServerCooldownManager.Instance.IsOnCooldown(actorID, _autoAttackSpell.DefinitionID))
                    continue;

                state.Actor.GetComponent<ActorSpellcaster>()
                    ?.ServerOnly_RequestSpellCast(_autoAttackSpell.DefinitionID);
            }

            RemoveQueuedAutoAttackers();
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(ATTACKER_COUNT_METRIC, _activeAutoAttackers.Count);
        }

        private void RemoveQueuedAutoAttackers()
        {
            for (int i = 0; i < _toRemove.Count; i++)
                _activeAutoAttackers.Remove(_toRemove[i]);
        }
    }
}
