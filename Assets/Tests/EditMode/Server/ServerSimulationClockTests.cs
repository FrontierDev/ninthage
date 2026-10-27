using Game.Server.Core;
using Game.Server.Systems;
using Game.Shared;
using Game.Shared.Performance;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Shared.Tests.Server
{
    /// <summary>Regression coverage for the authoritative server simulation clock contract.</summary>
    public sealed class ServerSimulationClockTests
    {
        private static readonly FieldInfo LAST_TIMESTAMP_FIELD =
            typeof(ServerSimulationClock).GetField("_lastTimestamp", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo UPDATE_METHOD =
            typeof(ServerSimulationClock).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo STOP_CLOCK_METHOD =
            typeof(ServerSimulationClock).GetMethod("StopClock", BindingFlags.Instance | BindingFlags.NonPublic);

        private GameConfiguration _configuration;
        private float _originalTickDuration;
        private int _originalMaxCatchUpTicks;
        private float _originalTimeScale;
        private int _originalTargetFrameRate;
        private GameObject _hostObject;
        private string _telemetryDirectory;

        [SetUp]
        public void SetUp()
        {
            StopRuntime();

            if (!GameConfigurationManager.IsInitialized)
                GameConfigurationManager.Initialize();

            _configuration = GameConfigurationManager.Config;
            Assert.That(_configuration, Is.Not.Null);

            _originalTickDuration = _configuration.ServerSimulationTickDuration;
            _originalMaxCatchUpTicks = _configuration.ServerSimulationMaxCatchUpTicks;
            _originalTimeScale = Time.timeScale;
            _originalTargetFrameRate = Application.targetFrameRate;

            _configuration.ServerSimulationTickDuration = 0.1f;
            _configuration.ServerSimulationMaxCatchUpTicks = 4;
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                StopRuntime();

                if (PerformanceTelemetry.IsEnabled)
                {
                    PerformanceTelemetry.StopLiveSession(
                        PerformanceRunStatus.FailCorrectness,
                        "TEST_CLEANUP",
                        "The clock regression test did not finish its telemetry session.");
                }
            }
            finally
            {
                if (_configuration != null)
                {
                    _configuration.ServerSimulationTickDuration = _originalTickDuration;
                    _configuration.ServerSimulationMaxCatchUpTicks = _originalMaxCatchUpTicks;
                }

                Time.timeScale = _originalTimeScale;
                Application.targetFrameRate = _originalTargetFrameRate;

                if (_telemetryDirectory != null && Directory.Exists(_telemetryDirectory))
                    Directory.Delete(_telemetryDirectory, true);

                _telemetryDirectory = null;
            }
        }

        [Test]
        public void EqualElapsedTotalsProduceTheSameTickSequenceAcrossFrameCadences()
        {
            var fineCadence = new List<double>();
            for (int i = 0; i < 10; i++)
                fineCadence.Add(0.105d);

            TickRunResult fineRun = CaptureTicks(fineCadence, 4);

            StopRuntime();

            TickRunResult coarseRun = CaptureTicks(new[] { 1.05d }, 4);

            Assert.That(fineRun.Ticks, Has.Count.EqualTo(10));
            Assert.That(coarseRun.Ticks, Has.Count.EqualTo(10));
            CollectionAssert.AreEqual(
                fineRun.Ticks.Select(tick => tick.TickID).ToArray(),
                coarseRun.Ticks.Select(tick => tick.TickID).ToArray());
            CollectionAssert.AreEqual(
                fineRun.Ticks.Select(tick => tick.ElapsedTime).ToArray(),
                coarseRun.Ticks.Select(tick => tick.ElapsedTime).ToArray());
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }, fineRun.ExecutedPerFrame);
            CollectionAssert.AreEqual(new[] { 4, 4, 2 }, coarseRun.ExecutedPerFrame);

            for (int i = 0; i < coarseRun.Ticks.Count; i++)
            {
                Assert.That(coarseRun.Ticks[i].TickID, Is.EqualTo(i + 1));
                Assert.That(coarseRun.Ticks[i].DeltaTime, Is.EqualTo(0.1f));
            }
        }

        [Test]
        public void FixedTicksUpdateTheSixSystemsOnceInRegisteredOrderAndSetWorldTime()
        {
            ServerSimulationClock clock = StartClock(4, 0.1f);
            World world = ServerECSManager.DefaultWorld;
            var simulationGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();
            Assert.That(simulationGroup, Is.Not.Null);

            simulationGroup.SortSystems();
            var expectedSystemTypes = new[]
            {
                typeof(WorldSpawnSystem),
                typeof(ActorUpdateSystem),
                typeof(SpellCastSystem),
                typeof(CooldownTickSystem),
                typeof(AuraTickSystem),
                typeof(ActorTickerSystem),
            };

            using (NativeList<SystemHandle> actualSystems = simulationGroup.GetAllSystems(Allocator.Temp))
            {
                Assert.That(actualSystems.Length, Is.EqualTo(expectedSystemTypes.Length));
                for (int i = 0; i < expectedSystemTypes.Length; i++)
                {
                    string actualSystemName = TypeManager.GetSystemName(
                        world.Unmanaged.GetSystemTypeIndex(actualSystems[i])).ToString();
                    Assert.That(
                        actualSystemName,
                        Is.EqualTo(expectedSystemTypes[i].FullName),
                        "Unexpected server ECS system at sorted index {0}.",
                        i);
                }
            }

            Assert.That(ScriptBehaviourUpdateOrder.IsWorldInCurrentPlayerLoop(world), Is.False);

            var observations = new List<TickObservation>();
            ServerSimulationRegistrationHandle handle = ServerSimulationClock.RegisterEachTick(
                tick =>
                {
                    var worldTime = world.Time;
                    observations.Add(
                        new TickObservation(
                            tick.TickID,
                            tick.ElapsedTime,
                            tick.DeltaTime,
                            worldTime.ElapsedTime,
                            worldTime.DeltaTime));
                });

            Assert.That(SimulateFrame(clock, 0.305d), Is.EqualTo(3));
            handle.Cancel();

            Assert.That(clock.ExecutedTickCount, Is.EqualTo(3));
            Assert.That(observations, Has.Count.EqualTo(3));
            Assert.That(ScriptBehaviourUpdateOrder.IsWorldInCurrentPlayerLoop(world), Is.False);

            for (int i = 0; i < observations.Count; i++)
            {
                TickObservation observation = observations[i];
                Assert.That(observation.TickID, Is.EqualTo(i + 1));
                Assert.That(observation.ElapsedTime, Is.EqualTo((i + 1) * 0.1d).Within(0.000001d));
                Assert.That(observation.DeltaTime, Is.EqualTo(0.1f));
                Assert.That(observation.WorldElapsedTime, Is.EqualTo(observation.ElapsedTime).Within(0.000001d));
                Assert.That(observation.WorldDeltaTime, Is.EqualTo(observation.DeltaTime));
            }
        }

        [Test]
        public void CatchUpCapPreservesBacklogAndReportsOverdueRecovery()
        {
            ServerSimulationClock clock = StartClock(3, 0.1f);
            Assert.That(SimulateFrame(clock, 0.105d), Is.EqualTo(1));

            var overdueTicks = new List<long>();
            ServerSimulationRegistrationHandle dueHandle = ServerSimulationClock.RegisterDue(
                1,
                tick => overdueTicks.Add(tick.TickID));

            Assert.That(SimulateFrame(clock, 1.05d), Is.EqualTo(3));
            Assert.That(clock.ScheduledTickCount, Is.EqualTo(11));
            Assert.That(clock.ExecutedTickCount, Is.EqualTo(4));
            Assert.That(clock.BacklogTicks, Is.EqualTo(7));
            Assert.That(clock.CatchUpCapHitCount, Is.EqualTo(1));
            Assert.That(clock.DeadlineMissCount, Is.EqualTo(1));
            CollectionAssert.AreEqual(new long[] { 2 }, overdueTicks);
            Assert.That(dueHandle.IsValid, Is.True);

            var recoveryFrames = new List<int>();
            while (clock.BacklogTicks > 0)
            {
                recoveryFrames.Add(SimulateFrame(clock, 0d));
                Assert.That(recoveryFrames.Count, Is.LessThan(10));
            }

            CollectionAssert.AreEqual(new[] { 3, 3, 1 }, recoveryFrames);
            Assert.That(clock.ExecutedTickCount, Is.EqualTo(11));
            Assert.That(clock.ScheduledTickCount, Is.EqualTo(11));
            Assert.That(clock.BacklogTicks, Is.EqualTo(0));
            Assert.That(clock.CatchUpCapHitCount, Is.EqualTo(3));
            Assert.That(clock.DeadlineMissCount, Is.EqualTo(1));
        }

        [TestCase(0f)]
        [TestCase(-0.001f)]
        [TestCase(1.001f)]
        public void InvalidTickDurationConfigurationIsRejected(float invalidDuration)
        {
            _configuration.ServerSimulationTickDuration = invalidDuration;

            Assert.That(
                () => ServerECSManager.ServerInitializeECSWorlds(),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(ServerECSManager.Initialized, Is.False);
            Assert.That(ServerSimulationClock.IsRunning, Is.False);
        }

        [Test]
        public void NonFiniteTickDurationConfigurationIsRejected()
        {
            _configuration.ServerSimulationTickDuration = float.NaN;

            Assert.That(
                () => ServerECSManager.ServerInitializeECSWorlds(),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(ServerECSManager.Initialized, Is.False);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(1001)]
        public void InvalidCatchUpConfigurationIsRejected(int invalidCatchUpTicks)
        {
            _configuration.ServerSimulationMaxCatchUpTicks = invalidCatchUpTicks;

            Assert.That(
                () => ServerECSManager.ServerInitializeECSWorlds(),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(ServerECSManager.Initialized, Is.False);
        }

        [Test]
        public void NoTickOrRegistrationIsAcceptedBeforeInitialization()
        {
            Assert.That(ServerSimulationClock.CurrentTickID, Is.EqualTo(0));
            Assert.That(ServerSimulationClock.IsRunning, Is.False);
            Assert.That(
                () => ServerSimulationClock.RegisterEachTick(_ => { }),
                Throws.TypeOf<InvalidOperationException>());

            _hostObject = new GameObject("ServerSimulationClockUninitializedHost");
            var host = _hostObject.AddComponent<ClockTestHost>();
            Assert.That(
                () => ServerSimulationClock.Start(host),
                Throws.TypeOf<InvalidOperationException>());
            Assert.That(ServerSimulationClock.CurrentTickID, Is.EqualTo(0));
        }

        [Test]
        public void CancellationAndDueWorkUseStableEligibleTickOrder()
        {
            ServerSimulationClock clock = StartClock(4, 0.1f);
            var events = new List<string>();

            ServerSimulationRegistrationHandle everyTick = ServerSimulationClock.RegisterEachTick(
                tick => events.Add("each:" + tick.TickID));
            ServerSimulationRegistrationHandle due = ServerSimulationClock.RegisterDue(
                2,
                tick => events.Add("due:" + tick.TickID));
            ServerSimulationRegistrationHandle cancelled = ServerSimulationClock.RegisterEachTick(
                _ => events.Add("cancelled"));

            cancelled.Cancel();
            cancelled.Cancel();

            Assert.That(SimulateFrame(clock, 0.205d), Is.EqualTo(2));

            CollectionAssert.AreEqual(
                new[] { "each:1", "each:2", "due:2" },
                events);
            Assert.That(events.Any(value => value.StartsWith("due:", StringComparison.Ordinal)), Is.True);
            Assert.That(everyTick.IsValid, Is.True);
            Assert.That(due.IsValid, Is.True);
            Assert.That(cancelled.IsValid, Is.True);
        }

        [Test]
        public void InvalidRegistrationArgumentsAreRejected()
        {
            StartClock(4, 0.1f);

            Assert.That(
                () => ServerSimulationClock.RegisterLowerRate(0, _ => { }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => ServerSimulationClock.RegisterLowerRate(-1, _ => { }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => ServerSimulationClock.RegisterDue(0, _ => { }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => ServerSimulationClock.RegisterDue(-1, _ => { }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => ServerSimulationClock.RegisterEachTick(null),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void TickTelemetryProducesACompleteLiveServerReport()
        {
            _telemetryDirectory = Path.Combine(
                Path.GetTempPath(),
                "NinthAge-ServerSimulationClock-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_telemetryDirectory);

            PerformanceTelemetry.StartLiveSession(
                new PerformanceSessionContext(
                    "server-simulation-clock-regression",
                    _telemetryDirectory,
                    "server-simulation-clock-regression.json",
                    "edit-mode-clock-test"));
            ServerInitialization.RegisterPerformanceMetrics();

            Time.timeScale = 0f;
            Application.targetFrameRate = 1;
            ServerSimulationClock clock = StartClock(10, 0.1f);
            Assert.That(SimulateFrame(clock, 0.305d), Is.EqualTo(3));

            StopRuntime();
            Assert.That(
                PerformanceTelemetry.StopLiveSession(
                    PerformanceRunStatus.CompleteMeasured,
                    "TEST_COMPLETE",
                    "Authoritative tick attribution was captured."),
                Is.True);

            string reportPath = Path.Combine(_telemetryDirectory, "server-simulation-clock-regression.json");
            Assert.That(File.Exists(reportPath), Is.True);
            string report = File.ReadAllText(reportPath);
            Assert.That(report, Does.Contain("server-performance-report/v1"));
            Assert.That(report, Does.Contain(PerformanceMetricIds.SERVER_SIMULATION_TICK_DURATION));
            Assert.That(report, Does.Not.Contain("samples.ndjson"));

            var systemMetricIDs = new[]
            {
                PerformanceMetricIds.SERVER_ECS_WORLD_SPAWN_DURATION,
                PerformanceMetricIds.SERVER_ECS_ACTOR_UPDATE_DURATION,
                PerformanceMetricIds.SERVER_ECS_SPELL_CAST_DURATION,
                PerformanceMetricIds.SERVER_ECS_COOLDOWN_TICK_DURATION,
                PerformanceMetricIds.SERVER_ECS_AURA_TICK_DURATION,
                PerformanceMetricIds.SERVER_ECS_ACTOR_TICKER_DURATION,
            };

            foreach (string systemMetricID in systemMetricIDs)
            {
                Assert.That(report, Does.Contain(systemMetricID));
            }
        }

        private TickRunResult CaptureTicks(IEnumerable<double> frameCadence, int maxCatchUpTicks)
        {
            ServerSimulationClock clock = StartClock(maxCatchUpTicks, 0.1f);
            var ticks = new List<ServerSimulationTick>();
            var executedPerFrame = new List<int>();
            ServerSimulationRegistrationHandle handle = ServerSimulationClock.RegisterEachTick(ticks.Add);

            foreach (double elapsedSeconds in frameCadence)
                executedPerFrame.Add(SimulateFrame(clock, elapsedSeconds));

            while (clock.BacklogTicks > 0)
            {
                executedPerFrame.Add(SimulateFrame(clock, 0d));
                Assert.That(executedPerFrame.Count, Is.LessThan(100));
            }

            handle.Cancel();
            return new TickRunResult(ticks, executedPerFrame);
        }

        private ServerSimulationClock StartClock(int maxCatchUpTicks, float tickDuration)
        {
            _configuration.ServerSimulationTickDuration = tickDuration;
            _configuration.ServerSimulationMaxCatchUpTicks = maxCatchUpTicks;

            Assert.That(ServerECSManager.Initialized, Is.False);
            ServerECSManager.ServerInitializeECSWorlds();
            Assert.That(ServerECSManager.DefaultWorld, Is.Not.Null);

            _hostObject = new GameObject("ServerSimulationClockTestHost");
            var host = _hostObject.AddComponent<ClockTestHost>();
            ServerSimulationClock clock = ServerSimulationClock.Start(host);
            clock.enabled = false;
            return clock;
        }

        private static int SimulateFrame(ServerSimulationClock clock, double elapsedSeconds)
        {
            Assert.That(LAST_TIMESTAMP_FIELD, Is.Not.Null);
            Assert.That(UPDATE_METHOD, Is.Not.Null);

            long elapsedTimestampTicks = checked((long)Math.Round(elapsedSeconds * Stopwatch.Frequency));
            LAST_TIMESTAMP_FIELD.SetValue(
                clock,
                checked(Stopwatch.GetTimestamp() - elapsedTimestampTicks));

            long executedBefore = clock.ExecutedTickCount;
            UPDATE_METHOD.Invoke(clock, null);
            return checked((int)(clock.ExecutedTickCount - executedBefore));
        }

        private void StopRuntime()
        {
            if (!Application.isPlaying && ServerSimulationClock.Active != null)
            {
                Assert.That(STOP_CLOCK_METHOD, Is.Not.Null);
                ServerSimulationClock activeClock = ServerSimulationClock.Active;
                STOP_CLOCK_METHOD.Invoke(activeClock, null);
                UnityEngine.Object.DestroyImmediate(activeClock);
            }

            ServerInitialization.Stop();

            if (_hostObject == null)
                return;

            UnityEngine.Object.DestroyImmediate(_hostObject);
            _hostObject = null;
        }

        private readonly struct TickObservation
        {
            public TickObservation(
                long tickID,
                double elapsedTime,
                float deltaTime,
                double worldElapsedTime,
                float worldDeltaTime)
            {
                TickID = tickID;
                ElapsedTime = elapsedTime;
                DeltaTime = deltaTime;
                WorldElapsedTime = worldElapsedTime;
                WorldDeltaTime = worldDeltaTime;
            }

            public long TickID { get; }
            public double ElapsedTime { get; }
            public float DeltaTime { get; }
            public double WorldElapsedTime { get; }
            public float WorldDeltaTime { get; }
        }

        private sealed class TickRunResult
        {
            public TickRunResult(List<ServerSimulationTick> ticks, List<int> executedPerFrame)
            {
                Ticks = ticks;
                ExecutedPerFrame = executedPerFrame;
            }

            public List<ServerSimulationTick> Ticks { get; }
            public List<int> ExecutedPerFrame { get; }
        }

        [Serializable]

        private sealed class ClockTestHost : MonoBehaviour
        {
        }
    }
}
