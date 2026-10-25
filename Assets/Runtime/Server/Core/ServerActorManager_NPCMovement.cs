using Actor = Game.Shared.Runtime.Actors.Actor;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using System;
using System.Collections.Generic;
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

        private long _pendingTickID = -1;
        private int _pendingTickEntityScanCount;
        private int _pendingTickGameObjectSyncCount;
        private bool _hasPendingTickSyncContribution;

        private struct NPCNavigationState
        {
            public Actor Actor;
            public NavMeshAgent Agent;
            public bool SuspendedForUnavailableNavigation;
        }

        private readonly Dictionary<Guid, NPCNavigationState> _npcNavigationStates = new();
        private readonly List<KeyValuePair<Guid, NPCNavigationState>> _npcNavigationSnapshot = new();

        private void UpdateNPCNavigationRegistration(
            Actor actor,
            bool hasEnabledAgent,
            NavMeshAgent agent)
        {
            if (actor == null)
                throw new ArgumentNullException(nameof(actor));

            if (!hasEnabledAgent || actor.Owner != null)
            {
                _npcNavigationStates.Remove(actor.ID);
                return;
            }

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

            _npcNavigationStates[actor.ID] = new NPCNavigationState
            {
                Actor = actor,
                Agent = agent
            };
        }

        private void RemoveNPCNavigationRegistration(Actor actor)
        {
            if (actor == null)
                return;

            if (_npcNavigationStates.TryGetValue(actor.ID, out var state)
                && ReferenceEquals(state.Actor, actor))
            {
                _npcNavigationStates.Remove(actor.ID);
            }
        }

        private static float GetNPCPursuitStoppingDistance(Actor actor, Actor target)
        {
            var autoAttackSpell = GameConfigurationManager.Config.AutoAttackSpell;
            if (autoAttackSpell == null)
            {
                throw new InvalidOperationException(
                    "NPC melee pursuit requires the configured auto-attack spell range.");
            }

            float stoppingDistance = autoAttackSpell.BaseRange
                + actor.HitboxRadius
                + target.HitboxRadius;
            if (float.IsNaN(stoppingDistance)
                || float.IsInfinity(stoppingDistance)
                || stoppingDistance < 0f)
            {
                throw new InvalidOperationException("The NPC pursuit stopping distance is invalid.");
            }

            return stoppingDistance;
        }

        internal void DropNPCTarget(Actor npc)
        {
            if (npc.Owner != null)
            {
                throw new InvalidOperationException(
                    $"Actor '{npc.ID}' is player-owned and cannot use NPC target-drop handling.");
            }

            Actor target = npc.Target;
            if (target != null)
                npc.SetTargetFromServer(null);

            ActorEndpoint.onServerStopAutoAttack?.Invoke(npc);
            ServerSpellCastManager.Instance.TryInterruptCast(npc.ID);

            if (npc.TryGetComponent<NPCBehavior>(out var npcBehavior))
                npcBehavior.ClearThreat();

            ServerAuraManager.Instance.RemoveAllAuras(npc.ID);

            if (!ServerTickerManager.Instance.RemoveAllTickers(npc))
            {
                throw new InvalidOperationException(
                    $"Ticker owner could not clear live NPC actor '{npc.ID}'.");
            }

            if (!ServerCooldownManager.Instance.ClearAllCooldowns(npc.ID))
            {
                throw new InvalidOperationException(
                    $"Cooldown owner could not clear live NPC actor '{npc.ID}'.");
            }

            if (npc.TryGetComponent<ActorStatContainer>(out _)
                && !ServerStatManager.Instance.ResetResources(npc))
            {
                throw new InvalidOperationException(
                    $"Stat owner could not reset resources for live NPC actor '{npc.ID}'.");
            }

            if (_npcAggroStates.TryGetValue(npc.ID, out var aggroState))
            {
                if (!ReferenceEquals(aggroState.NPC, npc))
                {
                    throw new InvalidOperationException(
                        $"NPC aggro state for '{npc.ID}' belongs to a different actor instance.");
                }

                ClearPendingAggro(ref aggroState);
                _npcAggroStates[npc.ID] = aggroState;
            }

            if (_npcNavigationStates.TryGetValue(npc.ID, out var navigationState)
                && ReferenceEquals(navigationState.Actor, npc)
                && navigationState.Agent != null
                && navigationState.Agent.enabled
                && navigationState.Agent.isOnNavMesh
                && navigationState.Agent.hasPath)
            {
                navigationState.Agent.ResetPath();
            }

            if (npc is NPCActor && !npc.isDead)
                ReevaluateNPCActivity(npc);
        }

        private void UpdateNPCNavigation()
        {
            _npcNavigationSnapshot.Clear();
            try
            {
                foreach (var entry in _npcNavigationStates)
                    _npcNavigationSnapshot.Add(entry);

                foreach (var kvp in _npcNavigationSnapshot)
                {
                    Guid actorID = kvp.Key;
                    NPCNavigationState state = kvp.Value;
                    if (state.Actor == null)
                    {
                        _npcNavigationStates.Remove(actorID);
                        continue;
                    }

                    if (state.Actor.isDead)
                    {
                        RetireNPCNavigationOnDeath(state.Actor);
                        continue;
                    }

                    if (state.Actor.Owner != null)
                    {
                        _npcNavigationStates.Remove(actorID);
                        continue;
                    }

                    if (state.Agent == null)
                        continue;

                    if (state.SuspendedForUnavailableNavigation)
                        continue;

                    if (!state.Agent.enabled)
                    {
                        continue;
                    }

                    Actor target = state.Actor.Target;
                    if (target == null)
                    {
                        if (state.Agent.isOnNavMesh && state.Agent.hasPath)
                            state.Agent.ResetPath();
                        continue;
                    }

                    if (!state.Actor.SpatialContext.IsCompatibleWith(target.SpatialContext))
                    {
                        DropNPCTarget(state.Actor);
                        continue;
                    }

                    if (target.isDead)
                    {
                        DropNPCTarget(state.Actor);
                        continue;
                    }

                    if (!TryGetAcceptedWorldPosition(target.ID, out WorldPosition targetWorldPosition))
                    {
                        throw new InvalidOperationException(
                            $"NPC target {target.ID} has no live canonical ECS position.");
                    }

                    if (state.Agent.pathPending)
                        continue;
                    if (!state.Agent.isOnNavMesh)
                    {
                        throw new InvalidOperationException(
                            $"NPC {state.Actor.ID} has an enabled NavMeshAgent that is not attached to a NavMesh.");
                    }

                    state.Agent.stoppingDistance = GetNPCPursuitStoppingDistance(state.Actor, target);
                    Vector3 physicalTargetPosition = ServerPhysicsManager.Instance.ToPhysicalPosition(targetWorldPosition);
                    if (!state.Agent.SetDestination(physicalTargetPosition))
                    {
                        throw new InvalidOperationException(
                            $"NPC {state.Actor.ID} rejected pursuit destination for target {target.ID}. "
                            + $"agentPosition={state.Agent.transform.position}; "
                            + $"nextPosition={state.Agent.nextPosition}; "
                            + $"targetWorld=({targetWorldPosition.x}, {targetWorldPosition.y}, {targetWorldPosition.z}); "
                            + $"requestedPhysical=({physicalTargetPosition.x}, {physicalTargetPosition.y}, {physicalTargetPosition.z}); "
                            + $"agentTypeID={state.Agent.agentTypeID}; "
                            + $"areaMask={state.Agent.areaMask}; "
                            + $"isOnNavMesh={state.Agent.isOnNavMesh}; "
                            + $"pathPending={state.Agent.pathPending}.");
                    }
                }
            }
            finally
            {
                _npcNavigationSnapshot.Clear();
            }
        }

        private void EnsureNPCMovementComponents(Entity entity, Actor actor)
        {
            bool hasEnabledAgent = HasEnabledNavMeshAgent(actor, out NavMeshAgent agent);
            UpdateNPCNavigationRegistration(actor, hasEnabledAgent, agent);

            if (!_entityManager.HasComponent<Systems.MotionStateComponent>(entity))
            {
                _entityManager.AddComponentData(entity, new Systems.MotionStateComponent
                {
                    VerticalVelocity = 0f,
                    IsGrounded = true,
                    TimeSinceLastGround = 0f
                });
            }

            _entityManager.SetComponentEnabled<Systems.MotionStateComponent>(
                entity,
                !hasEnabledAgent);

            if (!_entityManager.HasComponent<Systems.GroundDetectionComponent>(entity))
            {
                _entityManager.AddComponentData(entity, new Systems.GroundDetectionComponent
                {
                    groundDetectionRadius = GameConfigurationManager.Config.GroundDetectionRadius,
                    lastGroundCheck = new Unity.Mathematics.float3(0, -1, 0)
                });
            }
        }

        private void RetireNPCNavigationOnDeath(Actor actor)
        {
            if (actor == null)
                return;

            if (actor.Owner.HasValue)
            {
                _npcNavigationStates.Remove(actor.ID);
                return;
            }

            NavMeshAgent agent = actor.GetComponent<NavMeshAgent>();
            if (_npcNavigationStates.TryGetValue(actor.ID, out var state))
            {
                if (!ReferenceEquals(state.Actor, actor))
                {
                    throw new InvalidOperationException(
                        $"NPC navigation state for actor '{actor.ID}' belongs to a different actor instance.");
                }

                if (agent != null && !ReferenceEquals(state.Agent, agent))
                {
                    throw new InvalidOperationException(
                        $"NPC navigation state for actor '{actor.ID}' references a different NavMeshAgent component.");
                }
            }

            if (agent != null && agent.enabled)
            {
                if (agent.isOnNavMesh && agent.hasPath)
                    agent.ResetPath();

                agent.isStopped = true;
                agent.enabled = false;
            }

            _npcNavigationStates.Remove(actor.ID);
        }

        internal void SuspendNPCNavigationForUnavailableNavMesh()
        {
            var states = new List<KeyValuePair<Guid, NPCNavigationState>>(_npcNavigationStates);
            var actorsToSuspend = new List<KeyValuePair<Guid, NPCNavigationState>>();
            foreach (var entry in states)
            {
                NPCNavigationState state = entry.Value;
                if (state.Actor == null
                    || state.Actor.isDead
                    || state.Actor.Owner != null
                    || state.Agent == null)
                {
                    _npcNavigationStates.Remove(entry.Key);
                    continue;
                }

                if (state.SuspendedForUnavailableNavigation)
                    continue;

                if (!state.Agent.enabled || !state.Agent.isOnNavMesh)
                {
                    throw new InvalidOperationException(
                        $"NPC {state.Actor.ID} cannot be suspended because its registered NavMeshAgent is not enabled on the continuous NavMesh.");
                }

                actorsToSuspend.Add(entry);
            }

            foreach (var entry in actorsToSuspend)
            {
                NPCNavigationState state = entry.Value;
                if (state.Agent.hasPath)
                    state.Agent.ResetPath();
                state.Agent.enabled = false;
                state.SuspendedForUnavailableNavigation = true;
                _npcNavigationStates[entry.Key] = state;
            }
        }

        internal void ResumeNPCNavigationAfterNavMeshAvailable()
        {
            if (!ServerPhysicsManager.Instance.TryGetNavigationAgentType(out int agentTypeID))
            {
                throw new InvalidOperationException(
                    "Suspended NPC navigation cannot resume without a registered continuous NavMesh.");
            }

            var suspendedStates = new List<KeyValuePair<Guid, NPCNavigationState>>();
            foreach (var entry in new List<KeyValuePair<Guid, NPCNavigationState>>(_npcNavigationStates))
            {
                NPCNavigationState state = entry.Value;
                if (!state.SuspendedForUnavailableNavigation)
                    continue;

                if (state.Actor == null
                    || state.Actor.isDead
                    || state.Actor.Owner != null
                    || state.Agent == null)
                {
                    _npcNavigationStates.Remove(entry.Key);
                    continue;
                }

                if (state.Agent.agentTypeID != agentTypeID)
                {
                    throw new InvalidOperationException(
                        $"Suspended NPC {state.Actor.ID} NavMeshAgent type {state.Agent.agentTypeID} does not match continuous NavMesh agent type {agentTypeID}.");
                }

                suspendedStates.Add(entry);
            }

            var resumedStates = new List<KeyValuePair<Guid, NPCNavigationState>>();
            try
            {
                foreach (var entry in suspendedStates)
                {
                    NPCNavigationState state = entry.Value;
                    resumedStates.Add(entry);
                    state.Agent.enabled = true;
                    if (!state.Agent.isOnNavMesh)
                    {
                        state.Agent.enabled = false;
                        throw new InvalidOperationException(
                            $"Suspended NPC {state.Actor.ID} could not attach to the restored continuous NavMesh at its existing transform.");
                    }

                    state.SuspendedForUnavailableNavigation = false;
                    _npcNavigationStates[entry.Key] = state;
                }
            }
            catch (Exception exception)
            {
                Exception rollbackException = null;
                foreach (var entry in resumedStates)
                {
                    NPCNavigationState state = entry.Value;
                    try
                    {
                        state.Agent.enabled = false;
                    }
                    catch (Exception agentException)
                    {
                        rollbackException = rollbackException ?? agentException;
                    }

                    state.SuspendedForUnavailableNavigation = true;
                    _npcNavigationStates[entry.Key] = state;
                }

                var resumeException = new InvalidOperationException(
                    "NPC navigation resume failed; all previously suspended agents were returned to the suspended state.",
                    exception);
                if (rollbackException != null)
                    resumeException.Data["NPCNavigationResumeRollbackException"] = rollbackException;

                throw resumeException;
            }
        }

        private static bool HasEnabledNavMeshAgent(Actor actor, out NavMeshAgent agent)
        {
            agent = actor.GetComponent<NavMeshAgent>();
            if (agent == null || !agent.enabled)
                return false;

            if (!ServerPhysicsManager.Instance.TryGetNavigationAgentType(out int agentTypeID))
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} has an enabled NavMeshAgent but no continuous NavMesh is registered.");
            }
            if (agent.agentTypeID != agentTypeID)
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} NavMeshAgent type {agent.agentTypeID} does not match continuous NavMesh agent type {agentTypeID}.");
            }
            if (!agent.isOnNavMesh)
            {
                throw new InvalidOperationException(
                    $"Actor {actor.ID} has an enabled NavMeshAgent that is not attached to a NavMesh.");
            }

            return true;
        }

        private void SynchronizeNPCPositionsToECS()
        {
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            int syncCount = 0;
            _npcNavigationSnapshot.Clear();
            try
            {
                foreach (var entry in _npcNavigationStates)
                    _npcNavigationSnapshot.Add(entry);

                foreach (var entry in _npcNavigationSnapshot)
                {
                    Guid actorID = entry.Key;
                    var navigationState = entry.Value;
                    var actor = navigationState.Actor;
                    if (actor == null)
                    {
                        _npcNavigationStates.Remove(actorID);
                        continue;
                    }

                    if (!TryGetActorEntity(actor.ID, out var entity))
                    {
                        _npcNavigationStates.Remove(actorID);
                        continue;
                    }

                    var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                    if (SynchronizeNPCPositionToECS(entity, component, navigationState) && telemetryEnabled)
                        syncCount++;
                }
            }
            finally
            {
                _npcNavigationSnapshot.Clear();
            }

            if (telemetryEnabled)
            {
                RecordTickSyncContribution(
                    0,
                    syncCount,
                    ServerECSManager.CurrentSimulationTickID);
            }
        }

        private bool SynchronizeNPCPositionToECS(
            Entity entity,
            Systems.ActorComponent component,
            NPCNavigationState navigationState)
        {
            var actor = navigationState.Actor;
            if (component.Owner != default || actor.Owner != null)
            {
                _entityManager.SetComponentEnabled<Systems.MotionStateComponent>(entity, false);
                _npcNavigationStates.Remove(actor.ID);
                return false;
            }

            if (actor.isDead)
                return false;

            if (navigationState.SuspendedForUnavailableNavigation)
            {
                _entityManager.SetComponentEnabled<Systems.MotionStateComponent>(entity, false);
                return false;
            }

            if (navigationState.Agent == null)
            {
                _npcNavigationStates.Remove(actor.ID);
                _entityManager.SetComponentEnabled<Systems.MotionStateComponent>(entity, true);
                return false;
            }

            if (!navigationState.Agent.enabled)
            {
                _npcNavigationStates.Remove(actor.ID);
                _entityManager.SetComponentEnabled<Systems.MotionStateComponent>(entity, true);
                return false;
            }

            if (!navigationState.Agent.isOnNavMesh)
            {
                throw new InvalidOperationException(
                    $"NPC {actor.ID} has an enabled NavMeshAgent that is not attached to a NavMesh.");
            }

            _entityManager.SetComponentEnabled<Systems.MotionStateComponent>(entity, false);

            var worldPosition = ServerPhysicsManager.Instance.ToWorldPosition(
                actor.transform.position);
            var currentPosition = new WorldPosition(
                component.WorldPositionD.x,
                component.WorldPositionD.y,
                component.WorldPositionD.z);
            if (AreWorldPositionsEqual(currentPosition, worldPosition))
                return false;

            component.WorldPositionD = new Unity.Mathematics.double3(
                worldPosition.x,
                worldPosition.y,
                worldPosition.z);
            _entityManager.SetComponentData(entity, component);
            _entityManager.SetComponentEnabled<Systems.AcceptedPositionDirtyComponent>(entity, true);
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
