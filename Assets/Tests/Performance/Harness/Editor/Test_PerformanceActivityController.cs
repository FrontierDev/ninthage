using Game.Server.Core;
using Game.Server.Systems;
using Game.Shared;
using Game.Shared.Performance;
using Game.Tests.Performance.Fixtures.Actors;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        // Five-second Editor runs can produce thousands of frame/interval observations. Keep the
        // statistical population bounded while provisioning enough capacity for the supported fixtures.
        private const int PERFORMANCE_METRIC_CAPACITY = PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC;

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
        public static readonly PerformanceMetricDefinition GRAPHICAL_OBJECT_COUNT = new(
            "harness.graphical.object_count",
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Client,
            "benchmark_harness",
            "interval");
        public static readonly PerformanceMetricDefinition GRAPHICAL_VISIBLE_OBJECT_COUNT = new(
            "harness.graphical.visible_object_count",
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Client,
            "benchmark_harness",
            "interval");
        public static readonly PerformanceMetricDefinition GRAPHICAL_ACTOR_COUNT = new(
            "harness.graphical.actor_count",
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Client,
            "benchmark_harness",
            "interval");
        public static readonly PerformanceMetricDefinition GRAPHICAL_ROUTE_WAYPOINT_COUNT = new(
            "harness.graphical.route_waypoint_count",
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Client,
            "benchmark_harness",
            "interval");
        public static readonly PerformanceMetricDefinition GRAPHICAL_ROUTE_DISTANCE = new(
            "harness.graphical.route_distance",
            PerformanceMetricUnit.Metres,
            PerformanceMetricSide.Client,
            "benchmark_harness",
            "interval");

        public static void Register()
        {
            PerformanceTelemetry.RegisterMetric(ECS_ENTITY_COUNT, PERFORMANCE_METRIC_CAPACITY);
            PerformanceTelemetry.RegisterMetric(PATROLLING_COUNT, PERFORMANCE_METRIC_CAPACITY);
            PerformanceTelemetry.RegisterMetric(COMBAT_COUNT, PERFORMANCE_METRIC_CAPACITY);
            PerformanceTelemetry.RegisterMetric(WAKE_SURGE_COUNT, PERFORMANCE_METRIC_CAPACITY);
            PerformanceTelemetry.RegisterMetric(SPAWN_EVENT_COUNT, PERFORMANCE_METRIC_CAPACITY);
            PerformanceTelemetry.RegisterMetric(DESPAWN_EVENT_COUNT, PERFORMANCE_METRIC_CAPACITY);
            PerformanceTelemetry.RegisterMetric(TOTAL_ALLOCATED_BYTES, PERFORMANCE_METRIC_CAPACITY);
        }

        public static void RegisterGraphical()
        {
            PerformanceTelemetry.RegisterMetric(GRAPHICAL_OBJECT_COUNT, PERFORMANCE_METRIC_CAPACITY);
            PerformanceTelemetry.RegisterMetric(GRAPHICAL_VISIBLE_OBJECT_COUNT, PERFORMANCE_METRIC_CAPACITY);
            PerformanceTelemetry.RegisterMetric(GRAPHICAL_ACTOR_COUNT, PERFORMANCE_METRIC_CAPACITY);
            PerformanceTelemetry.RegisterMetric(GRAPHICAL_ROUTE_WAYPOINT_COUNT, PERFORMANCE_METRIC_CAPACITY);
            PerformanceTelemetry.RegisterMetric(GRAPHICAL_ROUTE_DISTANCE, PERFORMANCE_METRIC_CAPACITY);
        }

        public static void RecordGraphical(Test_GraphicalFixtureRuntime fixture)
        {
            if (fixture == null)
                throw new ArgumentNullException(nameof(fixture));

            var frame = UnityEngine.Time.frameCount;
            PerformanceTelemetry.TryRecord(GRAPHICAL_OBJECT_COUNT, fixture.ActualObjectCount, frame);
            PerformanceTelemetry.TryRecord(GRAPHICAL_VISIBLE_OBJECT_COUNT, fixture.VisibleObjectCount, frame);
            PerformanceTelemetry.TryRecord(GRAPHICAL_ACTOR_COUNT, fixture.ActualActorCount, frame);
            PerformanceTelemetry.TryRecord(GRAPHICAL_ROUTE_WAYPOINT_COUNT, fixture.RouteWaypointsReached, frame);
            PerformanceTelemetry.TryRecord(GRAPHICAL_ROUTE_DISTANCE, fixture.RouteDistanceTravelled, frame);
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
        private const float SYNTHETIC_CAST_DURATION_SECONDS = 0.5f;
        private const string SYNTHETIC_CAST_DEFINITION_ID = "fireball";
        private const int TIMED_STRESS_COOLDOWNS_PER_ENTITY = 16;
        private const int TIMED_STRESS_AURAS_PER_ENTITY = 4;
        private const int TIMED_STRESS_TICKERS_PER_ENTITY = 4;
        private const float TIMED_STRESS_INTERVAL_SECONDS = 1f;
        private const float TIMED_STRESS_LIFETIME_SECONDS = 300f;
        private static readonly FixedString64Bytes[] TIMED_STRESS_COOLDOWN_IDS =
        {
            new FixedString64Bytes("perf_cd_00"), new FixedString64Bytes("perf_cd_01"),
            new FixedString64Bytes("perf_cd_02"), new FixedString64Bytes("perf_cd_03"),
            new FixedString64Bytes("perf_cd_04"), new FixedString64Bytes("perf_cd_05"),
            new FixedString64Bytes("perf_cd_06"), new FixedString64Bytes("perf_cd_07"),
            new FixedString64Bytes("perf_cd_08"), new FixedString64Bytes("perf_cd_09"),
            new FixedString64Bytes("perf_cd_10"), new FixedString64Bytes("perf_cd_11"),
            new FixedString64Bytes("perf_cd_12"), new FixedString64Bytes("perf_cd_13"),
            new FixedString64Bytes("perf_cd_14"), new FixedString64Bytes("perf_cd_15"),
        };
        private static readonly FixedString64Bytes[] TIMED_STRESS_AURA_IDS =
        {
            new FixedString64Bytes("perf_aura_00"), new FixedString64Bytes("perf_aura_01"),
            new FixedString64Bytes("perf_aura_02"), new FixedString64Bytes("perf_aura_03"),
        };

        private readonly Test_ActorFixtureRuntime _fixture;
        private readonly long _baselineAllocatedBytes;
        private readonly bool _activityEnabled;
        private readonly bool _timedStateStressEnabled;
        private readonly long _syntheticCastDurationTicks;
        private readonly long _timedStressLifetimeTicks;
        private readonly long _timedStressIntervalTicks;
        private readonly HashSet<Entity> _wakeSurgeEntities = new();
        private int _lastRampTarget;
        private int _idleCount;
        private bool _measurementStarted;
        private bool _churnStarted;
        private bool _churnRespawned;
        private bool _timedStateStressPrepared;
        private long _nextSyntheticCastID;
        private int _measurementSpawnEventCount;
        private int _measurementDespawnEventCount;

        public Test_PerformanceActivityController(
            Test_ActorFixtureRuntime fixture,
            bool activityEnabled,
            bool timedStateStressEnabled)
        {
            _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
            _baselineAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong();
            _activityEnabled = activityEnabled;
            _timedStateStressEnabled = timedStateStressEnabled;
            _syntheticCastDurationTicks = activityEnabled || timedStateStressEnabled
                ? GetNonShorteningTickCount(SYNTHETIC_CAST_DURATION_SECONDS)
                : 0L;
            _timedStressLifetimeTicks = timedStateStressEnabled
                ? GetNonShorteningTickCount(TIMED_STRESS_LIFETIME_SECONDS)
                : 0L;
            _timedStressIntervalTicks = timedStateStressEnabled
                ? GetNonShorteningTickCount(TIMED_STRESS_INTERVAL_SECONDS)
                : 0L;
        }

        internal static int TimedStressCooldownsPerEntity => TIMED_STRESS_COOLDOWNS_PER_ENTITY;

        internal static int TimedStressAurasPerEntity => TIMED_STRESS_AURAS_PER_ENTITY;

        internal static int TimedStressTickersPerEntity => TIMED_STRESS_TICKERS_PER_ENTITY;

        internal static int TimedStressPeriodicEventsPerEntity => TIMED_STRESS_AURAS_PER_ENTITY + TIMED_STRESS_TICKERS_PER_ENTITY;

        public int RequestedCount => _fixture.Profile.population.accounting.ecs_data_only_npcs;

        public int SpawnEventCount { get; private set; }

        public int DespawnEventCount { get; private set; }

        public int PatrollingCount { get; private set; }

        public int CombatCount { get; private set; }

        public int WakeSurgeCount => _wakeSurgeEntities.Count;

        public int IdleCount => _idleCount;

        public long BaselineAllocatedBytes => _baselineAllocatedBytes;

        public void PrepareTimedStateStress()
        {
            if (!_timedStateStressEnabled || _timedStateStressPrepared)
                return;

            DisableTimedStateStressMotion(0, _fixture.DataOnlyEntities.Count);

            _timedStateStressPrepared = true;
        }

        private void DisableTimedStateStressMotion(int firstEntityIndex, int entityCount)
        {
            var entityManager = ServerECSManager.DefaultWorld.EntityManager;
            for (var index = firstEntityIndex; index < firstEntityIndex + entityCount; index++)
            {
                var entity = _fixture.DataOnlyEntities[index];
                if (!entityManager.Exists(entity))
                    continue;
                if (!entityManager.HasComponent<MotionStateComponent>(entity))
                    throw new InvalidOperationException($"Timed-state stress entity {index} is missing MotionStateComponent.");

                entityManager.SetComponentEnabled<MotionStateComponent>(entity, false);
            }
        }

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
                var firstSpawnedEntityIndex = _fixture.DataOnlyEntityCount;
                _fixture.SpawnDataOnly(count);
                if (_timedStateStressEnabled)
                    DisableTimedStateStressMotion(firstSpawnedEntityIndex, count);
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

            if (_timedStateStressEnabled)
            {
                MaintainTimedStateStress();
                PatrollingCount = 0;
                CombatCount = 0;
                _idleCount = _fixture.DataOnlyEntityCount;
                return;
            }

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

        public void ThrowIfResourceCeilingExceeded(long maximumGrowthBytes)
        {
            var currentAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong();
            var allocatedGrowthBytes = GetAllocatedGrowthBytes(_baselineAllocatedBytes, currentAllocatedBytes);
            if (allocatedGrowthBytes > maximumGrowthBytes)
                throw new Test_PerformanceResourceCeilingException(
                    $"Unity allocated memory grew by {allocatedGrowthBytes} bytes from the pre-workload baseline "
                    + $"of {_baselineAllocatedBytes} bytes to {currentAllocatedBytes} bytes, exceeding the workload-growth ceiling "
                    + $"of {maximumGrowthBytes} bytes.");
        }

        internal static long GetAllocatedGrowthBytes(long baselineAllocatedBytes, long currentAllocatedBytes)
        {
            return Math.Max(0L, currentAllocatedBytes - baselineAllocatedBytes);
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

                StartSyntheticCastIfDisabled(entityManager, entity);
            }
        }

        private void MaintainTimedStateStress()
        {
            var entityManager = ServerECSManager.DefaultWorld.EntityManager;
            var firstProgressTickID = GetFirstProgressTickID();
            var lifetimeTicks = _timedStressLifetimeTicks;
            var intervalTicks = _timedStressIntervalTicks;
            var expiryTickID = checked(firstProgressTickID + lifetimeTicks - 1L);
            var nextTickID = checked(firstProgressTickID + intervalTicks - 1L);

            for (var index = 0; index < _fixture.DataOnlyEntities.Count; index++)
            {
                var entity = _fixture.DataOnlyEntities[index];
                if (!entityManager.Exists(entity))
                    continue;

                StartSyntheticCastIfDisabled(entityManager, entity);

                if (!entityManager.IsComponentEnabled<CooldownElement>(entity))
                {
                    var cooldowns = entityManager.GetBuffer<CooldownElement>(entity);
                    if (cooldowns.Length != 0)
                        throw new InvalidOperationException($"Timed-state stress cooldown buffer for entity {index} was not empty before initialization.");
                    for (var slot = 0; slot < TIMED_STRESS_COOLDOWNS_PER_ENTITY; slot++)
                    {
                        cooldowns.Add(new CooldownElement
                        {
                            SpellID = TIMED_STRESS_COOLDOWN_IDS[slot],
                            ExpiryTickID = expiryTickID,
                        });
                    }
                    entityManager.SetComponentEnabled<CooldownElement>(entity, true);
                }

                if (!entityManager.IsComponentEnabled<AuraElement>(entity))
                {
                    var auras = entityManager.GetBuffer<AuraElement>(entity);
                    if (auras.Length != 0)
                        throw new InvalidOperationException($"Timed-state stress aura buffer for entity {index} was not empty before initialization.");
                    for (var slot = 0; slot < TIMED_STRESS_AURAS_PER_ENTITY; slot++)
                    {
                        auras.Add(new AuraElement
                        {
                            InstanceID = checked(index * TIMED_STRESS_AURAS_PER_ENTITY + slot + 1),
                            AuraDefinitionID = TIMED_STRESS_AURA_IDS[slot],
                            ExpiryTickID = expiryTickID,
                            NextTickID = nextTickID,
                            TickIntervalTicks = intervalTicks,
                        });
                    }
                    entityManager.SetComponentEnabled<AuraElement>(entity, true);
                }

                if (!entityManager.IsComponentEnabled<TickerElement>(entity))
                {
                    var tickers = entityManager.GetBuffer<TickerElement>(entity);
                    if (tickers.Length != 0)
                        throw new InvalidOperationException($"Timed-state stress ticker buffer for entity {index} was not empty before initialization.");
                    for (var slot = 0; slot < TIMED_STRESS_TICKERS_PER_ENTITY; slot++)
                    {
                        tickers.Add(new TickerElement
                        {
                            TickerID = checked(index * TIMED_STRESS_TICKERS_PER_ENTITY + slot + 1),
                            ExpiryTickID = expiryTickID,
                            NextTickID = nextTickID,
                            TickIntervalTicks = intervalTicks,
                        });
                    }
                    entityManager.SetComponentEnabled<TickerElement>(entity, true);
                }
            }
        }

        private void StartSyntheticCastIfDisabled(EntityManager entityManager, Entity entity)
        {
            if (entityManager.IsComponentEnabled<SpellCastComponent>(entity))
                return;

            var firstProgressTickID = GetFirstProgressTickID();
            var completionTickID = checked(firstProgressTickID + _syntheticCastDurationTicks - 1L);
            entityManager.SetComponentData(entity, new SpellCastComponent
            {
                SpellID = new FixedString64Bytes(SYNTHETIC_CAST_DEFINITION_ID),
                CastID = checked(++_nextSyntheticCastID),
                FirstProgressTickID = firstProgressTickID,
                CompletionTickID = completionTickID,
                TotalCastTime = SYNTHETIC_CAST_DURATION_SECONDS,
                TotalTicks = 0f,
                TimeSinceLastTick = 0f,
            });
            entityManager.SetComponentEnabled<SpellCastComponent>(entity, true);
        }

        private static long GetNonShorteningTickCount(float seconds)
        {
            // Match ServerSimulationClock's decimal round-trip so nominal fixed-step
            // multiples are not shortened or pushed out by binary floating-point noise.
            var secondsDecimal = decimal.Parse(
                seconds.ToString("R", CultureInfo.InvariantCulture),
                NumberStyles.Float,
                CultureInfo.InvariantCulture);
            var tickDurationDecimal = decimal.Parse(
                GameConfigurationManager.Config.ServerSimulationTickDuration.ToString("R", CultureInfo.InvariantCulture),
                NumberStyles.Float,
                CultureInfo.InvariantCulture);
            return Math.Max(
                1L,
                checked((long)Math.Ceiling(secondsDecimal / tickDurationDecimal)));
        }

        private static long GetFirstProgressTickID()
        {
            return GetFirstProgressTickID(ServerSimulationClock.CurrentTickID);
        }

        internal static long GetFirstProgressTickID(long currentSimulationTickID)
        {
            return checked(currentSimulationTickID + 1L);
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

            // Round the calculated boundaries to the same float precision as sampled elapsed time.
            var despawnThreshold = (float)((double)measurementDurationSeconds * CHURN_DESPAWN_FRACTION);
            var respawnThreshold = (float)((double)measurementDurationSeconds * CHURN_RESPAWN_FRACTION);
            if (!churnStarted && elapsedSeconds >= despawnThreshold)
                return Test_PerformanceActivityEvent.Despawn;
            if (churnStarted && !churnRespawned && elapsedSeconds >= respawnThreshold)
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
