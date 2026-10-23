using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
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

            _activeAutoAttackers[actor.ID] = new AutoAttackState
            {
                Actor = actor,
                Agent = actor.GetComponent<NavMeshAgent>()
            };
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

                // NPC chase — continuously update NavMeshAgent destination and stopping distance.
                if (state.Agent != null && state.Agent.enabled)
                {
                    if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
                    {
                        throw new InvalidOperationException(
                            "Enabled NPC auto-attack movement requires an initialized server physics manager.");
                    }

                    if (!ServerPhysicsManager.Instance.TryGetActorNavigationRegion(
                        state.Actor.ID,
                        out var actorRegion,
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

                    if (!ServerPhysicsManager.Instance.TryGetActorRegionDescriptor(
                        state.Actor.Target.ID,
                        out var targetRegion))
                    {
                        throw new InvalidOperationException(
                            $"Auto-attack target {state.Actor.Target.ID} is not bound to an active simulation region.");
                    }

                    if (targetRegion.Key != actorRegion.Key)
                    {
                        throw new InvalidOperationException(
                            $"Auto-attack chase from simulation region {actorRegion.Key} to {targetRegion.Key} requires the Stage 1.2 region-transition implementation.");
                    }

                    if (!state.Actor.SpatialContext.IsCompatibleWith(state.Actor.Target.SpatialContext))
                    {
                        throw new InvalidOperationException(
                            $"Auto-attack actor {state.Actor.ID} and target {state.Actor.Target.ID} have incompatible spatial contexts.");
                    }

                    state.Agent.stoppingDistance = _autoAttackSpell.BaseRange
                        + state.Actor.HitboxRadius + state.Actor.Target.HitboxRadius;
                    state.Agent.SetDestination(state.Actor.Target.transform.position);
                }
            }

            RemoveQueuedAutoAttackers();
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

                // Pause auto-attack while casting an ability.
                if (ServerSpellCastManager.Instance.IsCasting(actorID))
                    continue;

                // Check range — skip swing but don't remove (player may walk back).
                float distance = state.Actor.DistanceTo(state.Actor.Target);
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
