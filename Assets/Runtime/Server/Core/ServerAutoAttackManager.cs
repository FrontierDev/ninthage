using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using System;
using System.Collections.Generic;
using UnityEngine;

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

            if (!ServerActorManager.IsInitialized
                || ServerActorManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "Server auto-attack manager requires initialized server actor manager.");
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
                Actor = actor
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

                Actor target = state.Actor.Target;
                if (target == null)
                {
                    _toRemove.Add(actorID);
                    continue;
                }

                if (!state.Actor.SpatialContext.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Auto-attacker '{state.Actor.ID}' has an invalid spatial context.");
                }
                if (!target.SpatialContext.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Auto-attack target '{target.ID}' has an invalid spatial context.");
                }

                if (target.isDead)
                {
                    if (state.Actor.Owner == null)
                    {
                        ServerActorManager.Instance.DropNPCTarget(state.Actor);
                    }
                    else
                    {
                        _toRemove.Add(actorID);
                    }
                    continue;
                }

                if (!state.Actor.SpatialContext.IsCompatibleWith(target.SpatialContext))
                {
                    if (state.Actor.Owner == null)
                    {
                        ServerActorManager.Instance.DropNPCTarget(state.Actor);
                    }
                    else
                    {
                        state.Actor.SetTargetFromServer(null);
                        ActorEndpoint.onServerStopAutoAttack?.Invoke(state.Actor);
                    }

                    continue;
                }

                // Pause auto-attack while casting an ability.
                if (ServerSpellCastManager.Instance.IsCasting(actorID))
                    continue;

                // Check range — skip swing but don't remove (player may walk back).
                double distance = ServerActorManager.Instance.GetAcceptedSurfaceDistance(
                    state.Actor,
                    target);
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
