using Game.Server.Core;
using Game.Server.Systems;
using Game.Shared.Performance;
using Game.Tests.Performance.Fixtures.Actors;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Profiling;

namespace Game.Tests.Performance.Harness
{
    internal enum Test_PerformanceActivityPhase
    {
        Warmup,
        Measurement,
    }

    internal enum Test_PerformanceActivityEvent
    {
        None,
        Despawn,
        Respawn,
    }

    internal sealed class Test_PerformanceActivitySnapshot
    {
        public Test_PerformanceActivitySnapshot(
            int idleCount,
            int patrollingCount,
            int combatCount,
            int wakeSurgeCount,
            int spawnEventCount,
            int despawnEventCount,
            int totalSpawnEventCount,
            int totalDespawnEventCount)
        {
            IdleCount = idleCount;
            PatrollingCount = patrollingCount;
            CombatCount = combatCount;
            WakeSurgeCount = wakeSurgeCount;
            SpawnEventCount = spawnEventCount;
            DespawnEventCount = despawnEventCount;
            TotalSpawnEventCount = totalSpawnEventCount;
            TotalDespawnEventCount = totalDespawnEventCount;
        }

        public int IdleCount { get; }

        public int PatrollingCount { get; }

        public int CombatCount { get; }

        public int WakeSurgeCount { get; }

        public int SpawnEventCount { get; }

        public int DespawnEventCount { get; }

        public int TotalSpawnEventCount { get; }

        public int TotalDespawnEventCount { get; }
    }

    /// <summary>Records bounded samples produced by the test-only workload controller.</summary>
    internal static class Test_PerformanceHarnessMetrics
    {
        public static readonly PerformanceMetricDefinition ECS_ENTITY_COUNT = new(
            "harness.workload.ecs_entity_count",
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "benchmark_harness",
            "interval");
        public static readonly PerformanceMetricDefinition PATROLLING_COUNT = new(
            "harness.workload.patrolling_count",
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "benchmark_harness",
            "interval");
        public static readonly PerformanceMetricDefinition COMBAT_COUNT = new(
            "harness.workload.combat_count",
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "benchmark_harness",
            "interval");
        public static readonly PerformanceMetricDefinition WAKE_SURGE_COUNT = new(
            "harness.workload.wake_surge_count",
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "benchmark_harness",
            "interval");
        public static readonly PerformanceMetricDefinition SPAWN_EVENT_COUNT = new(
            "harness.workload.spawn_event_count",
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "benchmark_harness",
            "run");
        public static readonly PerformanceMetricDefinition DESPAWN_EVENT_COUNT = new(
            "harness.workload.despawn_event_count",
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "benchmark_harness",
            "run");
        public static readonly PerformanceMetricDefinition TOTAL_ALLOCATED_BYTES = new(
            "harness.resource.total_allocated_bytes",
            PerformanceMetricUnit.Bytes,
            PerformanceMetricSide.Server,
            "benchmark_harness",
            "interval");

        public static void Register()
        {
            PerformanceTelemetry.RegisterMetric(ECS_ENTITY_COUNT, 512);
            PerformanceTelemetry.RegisterMetric(PATROLLING_COUNT, 512);
            PerformanceTelemetry.RegisterMetric(COMBAT_COUNT, 512);
            PerformanceTelemetry.RegisterMetric(WAKE_SURGE_COUNT, 512);
            PerformanceTelemetry.RegisterMetric(SPAWN_EVENT_COUNT, 512);
            PerformanceTelemetry.RegisterMetric(DESPAWN_EVENT_COUNT, 512);
            PerformanceTelemetry.RegisterMetric(TOTAL_ALLOCATED_BYTES, 512);
        }
    }

    /// <summary>
    /// Applies the supported data-only ramp and activity schedule directly to the server ECS
    /// entities created by <see cref="Test_ActorFixtureRuntime"/>.
    /// </summary>
    internal sealed class Test_PerformanceActivityController
    {
        private const int CHURN_COUNT = 5;
        private const float CHURN_DESPAWN_FRACTION = 0.25f;
        private const float CHURN_RESPAWN_FRACTION = 0.4f;

        private readonly Test_ActorFixtureRuntime _fixture;
        private readonly bool _activityEnabled;
        private readonly HashSet<Entity> _wakeSurgeEntities = new();
        private int _lastRampTarget;
        private int _idleCount;
        private bool _measurementStarted;
        private bool _churnStarted;
        private bool _churnRespawned;
        private int _measurementSpawnEventCount;
        private int _measurementDespawnEventCount;

        public Test_PerformanceActivityController(Test_ActorFixtureRuntime fixture, bool activityEnabled)
        {
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            _activityEnabled = activityEnabled;
        }

        public int RequestedCount => _fixture.Profile.population.accounting.ecs_data_only_npcs;

        public int SpawnEventCount { get; private set; }

        public int DespawnEventCount { get; private set; }

        public int PatrollingCount { get; private set; }

        public int CombatCount { get; private set; }

        public int WakeSurgeCount => _wakeSurgeEntities.Count;

        public int IdleCount => _idleCount;

        public void AdvanceRamp(float elapsedSeconds, float durationSeconds)
        {
            if (durationSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));

            var normalized = math.saturate(elapsedSeconds / durationSeconds);
            var target = math.min(RequestedCount, math.max(1, (int)math.ceil(normalized * RequestedCount)));
            if (target <= _lastRampTarget)
                return;

            var count = target - _fixture.DataOnlyEntityCount;
            if (count > 0)
            {
                _fixture.SpawnDataOnly(count);
                RecordSpawnEvent(count);
            }

            _lastRampTarget = target;
        }

        public void BeginMeasurement()
        {
            _measurementStarted = true;
            _measurementSpawnEventCount = 0;
            _measurementDespawnEventCount = 0;
            _churnStarted = false;
            _churnRespawned = false;
            _wakeSurgeEntities.Clear();
            RecountActivity();
        }

        public Test_PerformanceActivitySnapshot CaptureSnapshot()
        {
            RecountActivity();
            return new Test_PerformanceActivitySnapshot(
                IdleCount,
                PatrollingCount,
                CombatCount,
                WakeSurgeCount,
                _measurementSpawnEventCount,
                _measurementDespawnEventCount,
                SpawnEventCount,
                DespawnEventCount);
        }

        public void AdvanceActivity(float elapsedSeconds, float phaseDurationSeconds, Test_PerformanceActivityPhase phase)
        {
            if (elapsedSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (phaseDurationSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(phaseDurationSeconds));

            if (!_activityEnabled)
            {
                PatrollingCount = 0;
                CombatCount = 0;
                RecountActivity();
                return;
            }

            UpdatePatrolPositions(elapsedSeconds);
            UpdateCombatCasts(elapsedSeconds);
            if (phase == Test_PerformanceActivityPhase.Measurement)
            {
                ActivateWakeSurge(elapsedSeconds, phaseDurationSeconds);
                ApplySpawnDespawnChurn(elapsedSeconds, phaseDurationSeconds, phase);
            }
            RecountActivity();
        }

        public void RecordSample(double intervalSeconds)
        {
            var frame = UnityEngine.Time.frameCount;
            PerformanceTelemetry.TryRecord(Test_PerformanceHarnessMetrics.ECS_ENTITY_COUNT, _fixture.DataOnlyEntityCount, frame, intervalSeconds);
            PerformanceTelemetry.TryRecord(Test_PerformanceHarnessMetrics.PATROLLING_COUNT, PatrollingCount, frame, intervalSeconds);
            PerformanceTelemetry.TryRecord(Test_PerformanceHarnessMetrics.COMBAT_COUNT, CombatCount, frame, intervalSeconds);
            PerformanceTelemetry.TryRecord(Test_PerformanceHarnessMetrics.WAKE_SURGE_COUNT, WakeSurgeCount, frame, intervalSeconds);
            PerformanceTelemetry.TryRecord(Test_PerformanceHarnessMetrics.SPAWN_EVENT_COUNT, SpawnEventCount, frame, intervalSeconds);
            PerformanceTelemetry.TryRecord(Test_PerformanceHarnessMetrics.DESPAWN_EVENT_COUNT, DespawnEventCount, frame, intervalSeconds);
            PerformanceTelemetry.TryRecord(Test_PerformanceHarnessMetrics.TOTAL_ALLOCATED_BYTES, Profiler.GetTotalAllocatedMemoryLong(), frame, intervalSeconds);
        }

        public void ThrowIfResourceCeilingExceeded(long maximumAllocatedBytes)
        {
            var allocatedBytes = Profiler.GetTotalAllocatedMemoryLong();
            if (allocatedBytes > maximumAllocatedBytes)
                throw new Test_PerformanceResourceCeilingException(
                    $"Unity total allocated memory {allocatedBytes} exceeded the runner ceiling {maximumAllocatedBytes} bytes.");
        }

        private void UpdatePatrolPositions(float elapsedSeconds)
        {
            var entityManager = ServerECSManager.DefaultWorld.EntityManager;
            for (var index = 0; index < _fixture.DataOnlyEntities.Count; index++)
            {
                var entity = _fixture.DataOnlyEntities[index];
                if (!entityManager.Exists(entity) || _fixture.DataOnlyPlacements[index].group_id != "npc-patrol")
                    continue;

                var placement = _fixture.DataOnlyPlacements[index];
                var phase = elapsedSeconds * 0.5f + index * 0.25f;
                var position = new double3(
                    placement.world_x + math.cos(phase) * 24d,
                    placement.world_y,
                    placement.world_z + math.sin(phase) * 24d);
                var actor = entityManager.GetComponentData<ActorComponent>(entity);
                actor.WorldPositionD = position;
                entityManager.SetComponentData(entity, actor);
                var motion = entityManager.GetComponentData<MotionStateComponent>(entity);
                motion.IsGrounded = true;
                motion.UsesNavMeshAgent = true;
                motion.VerticalVelocity = 0f;
                entityManager.SetComponentData(entity, motion);
            }
        }

        private void ActivateWakeSurge(float elapsedSeconds, float measurementDurationSeconds)
        {
            if (elapsedSeconds < measurementDurationSeconds * 0.4f)
                return;

            var entityManager = ServerECSManager.DefaultWorld.EntityManager;
            for (var index = 0; index < _fixture.DataOnlyEntities.Count && _wakeSurgeEntities.Count < 10; index++)
            {
                if (_fixture.DataOnlyPlacements[index].group_id != "npc-idle")
                    continue;

                var entity = _fixture.DataOnlyEntities[index];
                if (!entityManager.Exists(entity))
                    continue;

                _wakeSurgeEntities.Add(entity);
            }
        }

        private void UpdateCombatCasts(float elapsedSeconds)
        {
            var entityManager = ServerECSManager.DefaultWorld.EntityManager;
            for (var index = 0; index < _fixture.DataOnlyEntities.Count; index++)
            {
                var entity = _fixture.DataOnlyEntities[index];
                if (!entityManager.Exists(entity))
                    continue;

                var placement = _fixture.DataOnlyPlacements[index];
                var isCombat = placement.group_id == "npc-engaged" || _wakeSurgeEntities.Contains(entity);
                if (!isCombat)
                    continue;

                if (!entityManager.IsComponentEnabled<SpellCastComponent>(entity))
                {
                    entityManager.SetComponentData(entity, new SpellCastComponent
                    {
                        SpellID = new FixedString64Bytes("fireball"),
                        RemainingCastTime = 0.5f,
                        TotalCastTime = 0.5f,
                        TotalTicks = 0f,
                        TimeSinceLastTick = 0f,
                    });
                    entityManager.SetComponentEnabled<SpellCastComponent>(entity, true);
                }
            }
        }

        internal static Test_PerformanceActivityEvent ResolveChurnEvent(
            Test_PerformanceActivityPhase phase,
            float elapsedSeconds,
            float measurementDurationSeconds,
            bool churnStarted,
            bool churnRespawned)
        {
            if (phase != Test_PerformanceActivityPhase.Measurement)
                return Test_PerformanceActivityEvent.None;
            if (elapsedSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (measurementDurationSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(measurementDurationSeconds));

            if (!churnStarted && elapsedSeconds >= measurementDurationSeconds * CHURN_DESPAWN_FRACTION)
                return Test_PerformanceActivityEvent.Despawn;
            if (churnStarted && !churnRespawned && elapsedSeconds >= measurementDurationSeconds * CHURN_RESPAWN_FRACTION)
                return Test_PerformanceActivityEvent.Respawn;
            return Test_PerformanceActivityEvent.None;
        }

        private void ApplySpawnDespawnChurn(float elapsedSeconds, float measurementDurationSeconds, Test_PerformanceActivityPhase phase)
        {
            var activityEvent = ResolveChurnEvent(phase, elapsedSeconds, measurementDurationSeconds, _churnStarted, _churnRespawned);
            if (activityEvent == Test_PerformanceActivityEvent.Despawn)
            {
                var entityCountBeforeDespawn = _fixture.DataOnlyEntityCount;
                _fixture.DespawnDataOnly(CHURN_COUNT);
                if (_fixture.DataOnlyEntityCount != entityCountBeforeDespawn - CHURN_COUNT)
                {
                    throw new InvalidOperationException(
                        $"Activity churn despawn changed the fixture population from {entityCountBeforeDespawn} to {_fixture.DataOnlyEntityCount}; expected {entityCountBeforeDespawn - CHURN_COUNT}.");
                }
                DespawnEventCount += CHURN_COUNT;
                if (_measurementStarted)
                    _measurementDespawnEventCount += CHURN_COUNT;
                _churnStarted = true;
                return;
            }

            if (activityEvent == Test_PerformanceActivityEvent.Respawn)
            {
                var entityCountBeforeRespawn = _fixture.DataOnlyEntityCount;
                _fixture.SpawnDataOnly(CHURN_COUNT);
                if (_fixture.DataOnlyEntityCount != entityCountBeforeRespawn + CHURN_COUNT)
                {
                    throw new InvalidOperationException(
                        $"Activity churn respawn changed the fixture population from {entityCountBeforeRespawn} to {_fixture.DataOnlyEntityCount}; expected {entityCountBeforeRespawn + CHURN_COUNT}.");
                }
                RecordSpawnEvent(CHURN_COUNT);
                _churnRespawned = true;
            }
        }

        private void RecordSpawnEvent(int count)
        {
            SpawnEventCount += count;
            if (_measurementStarted)
                _measurementSpawnEventCount += count;
        }

        private void RecountActivity()
        {
            PatrollingCount = 0;
            CombatCount = 0;
            for (var index = 0; index < _fixture.DataOnlyEntities.Count; index++)
            {
                var entity = _fixture.DataOnlyEntities[index];
                if (!ServerECSManager.DefaultWorld.EntityManager.Exists(entity))
                    continue;

                if (_activityEnabled && _fixture.DataOnlyPlacements[index].group_id == "npc-patrol")
                    PatrollingCount++;
                if (_activityEnabled && (_fixture.DataOnlyPlacements[index].group_id == "npc-engaged" || _wakeSurgeEntities.Contains(entity)))
                    CombatCount++;
            }
            _idleCount = math.max(0, _fixture.DataOnlyEntityCount - PatrollingCount - CombatCount);
        }
    }

    /// <summary>Signals a measured resource ceiling breach without silently reducing the workload.</summary>
    internal sealed class Test_PerformanceResourceCeilingException : InvalidOperationException
    {
        public Test_PerformanceResourceCeilingException(string message)
            : base(message)
        {
        }
    }

    /// <summary>Signals a deliberate user cancellation so it is not reported as a timeout.</summary>
    internal sealed class Test_PerformanceUserStopException : OperationCanceledException
    {
        public Test_PerformanceUserStopException(string message)
            : base(message)
        {
        }
    }
}
