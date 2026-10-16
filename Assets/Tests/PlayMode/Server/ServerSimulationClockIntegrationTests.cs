using Game.Server.Core;
using Game.Shared;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Entities;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Runtime.Tests.Server
{
    /// <summary>Integration coverage for server clock ownership across Unity Play Mode frames.</summary>
    public sealed class ServerSimulationClockIntegrationTests
    {
        private static readonly FieldInfo LAST_TIMESTAMP_FIELD =
            typeof(ServerSimulationClock).GetField("_lastTimestamp", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo UPDATE_METHOD =
            typeof(ServerSimulationClock).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);

        private GameConfiguration _configuration;
        private float _originalTickDuration;
        private int _originalMaxCatchUpTicks;
        private float _originalTimeScale;
        private int _originalTargetFrameRate;
        private GameObject _hostObject;

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
            StopRuntime();

            if (_configuration != null)
            {
                _configuration.ServerSimulationTickDuration = _originalTickDuration;
                _configuration.ServerSimulationMaxCatchUpTicks = _originalMaxCatchUpTicks;
            }

            Time.timeScale = _originalTimeScale;
            Application.targetFrameRate = _originalTargetFrameRate;
        }

        [UnityTest]
        public IEnumerator OneClockOwnsTheWorldAndRestartBeginsAtTickZero()
        {
            ServerSimulationClock firstClock = StartClock(4, 0.1f);
            World firstWorld = ServerECSManager.DefaultWorld;
            Assert.That(ScriptBehaviourUpdateOrder.IsWorldInCurrentPlayerLoop(firstWorld), Is.False);

            var firstObservations = new List<TickObservation>();
            ServerSimulationRegistrationHandle firstHandle = ServerSimulationClock.RegisterEachTick(
                tick =>
                {
                    var worldTime = firstWorld.Time;
                    firstObservations.Add(
                        new TickObservation(
                            tick.TickID,
                            tick.ElapsedTime,
                            tick.DeltaTime,
                            worldTime.ElapsedTime,
                            worldTime.DeltaTime));
                });

            Assert.That(SimulateFrame(firstClock, 0.205d), Is.EqualTo(2));
            yield return null;

            Assert.That(firstObservations, Has.Count.EqualTo(2));
            Assert.That(firstClock.ExecutedTickCount, Is.EqualTo(2));
            Assert.That(ScriptBehaviourUpdateOrder.IsWorldInCurrentPlayerLoop(firstWorld), Is.False);
            Assert.That(firstObservations[0].TickID, Is.EqualTo(1));
            Assert.That(firstObservations[1].TickID, Is.EqualTo(2));
            Assert.That(firstObservations[1].WorldElapsedTime, Is.EqualTo(0.2d).Within(0.000001d));
            Assert.That(firstObservations[1].WorldDeltaTime, Is.EqualTo(0.1f));

            firstHandle.Cancel();
            var competingHostObject = new GameObject("ServerSimulationClockCompetingHost");
            var competingHost = competingHostObject.AddComponent<ClockTestHost>();
            Assert.That(
                () => ServerSimulationClock.Start(competingHost),
                Throws.TypeOf<InvalidOperationException>());
            UnityEngine.Object.DestroyImmediate(competingHostObject);

            ServerInitialization.Stop();
            UnityEngine.Object.DestroyImmediate(_hostObject);
            _hostObject = null;

            Assert.That(ServerSimulationClock.IsRunning, Is.False);
            Assert.That(ServerECSManager.Initialized, Is.False);
            Assert.That(ServerECSManager.DefaultWorld, Is.Null);

            ServerSimulationClock secondClock = StartClock(4, 0.1f);
            var secondTicks = new List<long>();
            ServerSimulationRegistrationHandle secondHandle = ServerSimulationClock.RegisterEachTick(
                tick => secondTicks.Add(tick.TickID));

            Assert.That(SimulateFrame(secondClock, 0.105d), Is.EqualTo(1));
            yield return null;

            CollectionAssert.AreEqual(new long[] { 1 }, secondTicks);
            Assert.That(secondClock.LastExecutedTickID, Is.EqualTo(1));
            Assert.That(ScriptBehaviourUpdateOrder.IsWorldInCurrentPlayerLoop(ServerECSManager.DefaultWorld), Is.False);
            secondHandle.Cancel();
        }

        [UnityTest]
        public IEnumerator TimeScaleAndRenderCadenceCannotChangeFixedWorldTime()
        {
            Time.timeScale = 0f;
            Application.targetFrameRate = 15;
            ServerSimulationClock slowCadenceClock = StartClock(10, 0.1f);
            TickRunResult slowCadenceRun = CaptureTicks(slowCadenceClock, new[] { 0.305d });
            yield return null;

            StopRuntime();

            Time.timeScale = 1.5f;
            Application.targetFrameRate = 144;
            ServerSimulationClock fastCadenceClock = StartClock(10, 0.1f);
            TickRunResult fastCadenceRun = CaptureTicks(
                fastCadenceClock,
                new[] { 0.105d, 0.105d, 0.105d });
            yield return null;

            Assert.That(slowCadenceRun.Ticks, Has.Count.EqualTo(3));
            Assert.That(fastCadenceRun.Ticks, Has.Count.EqualTo(3));
            for (int i = 0; i < 3; i++)
            {
                Assert.That(slowCadenceRun.Ticks[i].TickID, Is.EqualTo(i + 1));
                Assert.That(fastCadenceRun.Ticks[i].TickID, Is.EqualTo(i + 1));
                Assert.That(slowCadenceRun.Ticks[i].ElapsedTime, Is.EqualTo((i + 1) * 0.1d).Within(0.000001d));
                Assert.That(fastCadenceRun.Ticks[i].ElapsedTime, Is.EqualTo((i + 1) * 0.1d).Within(0.000001d));
                Assert.That(slowCadenceRun.Ticks[i].DeltaTime, Is.EqualTo(0.1f));
                Assert.That(fastCadenceRun.Ticks[i].DeltaTime, Is.EqualTo(0.1f));
                Assert.That(slowCadenceRun.Ticks[i].WorldDeltaTime, Is.EqualTo(0.1f));
                Assert.That(fastCadenceRun.Ticks[i].WorldDeltaTime, Is.EqualTo(0.1f));
            }
        }

        private ServerSimulationClock StartClock(int maxCatchUpTicks, float tickDuration)
        {
            _configuration.ServerSimulationTickDuration = tickDuration;
            _configuration.ServerSimulationMaxCatchUpTicks = maxCatchUpTicks;

            Assert.That(ServerECSManager.Initialized, Is.False);
            ServerECSManager.ServerInitializeECSWorlds();
            Assert.That(ServerECSManager.DefaultWorld, Is.Not.Null);

            _hostObject = new GameObject("ServerSimulationClockIntegrationHost");
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

        private static TickRunResult CaptureTicks(ServerSimulationClock clock, IEnumerable<double> frameCadence)
        {
            var ticks = new List<TickObservation>();
            ServerSimulationRegistrationHandle handle = ServerSimulationClock.RegisterEachTick(
                tick =>
                {
                    var worldTime = ServerECSManager.DefaultWorld.Time;
                    ticks.Add(
                        new TickObservation(
                            tick.TickID,
                            tick.ElapsedTime,
                            tick.DeltaTime,
                            worldTime.ElapsedTime,
                            worldTime.DeltaTime));
                });

            foreach (double elapsedSeconds in frameCadence)
                SimulateFrame(clock, elapsedSeconds);

            while (clock.BacklogTicks > 0)
            {
                SimulateFrame(clock, 0d);
                Assert.That(ticks.Count, Is.LessThan(100));
            }

            handle.Cancel();
            return new TickRunResult(ticks);
        }

        private void StopRuntime()
        {
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
            public TickRunResult(List<TickObservation> ticks)
            {
                Ticks = ticks;
            }

            public List<TickObservation> Ticks { get; }
        }

        private sealed class ClockTestHost : MonoBehaviour
        {
        }
    }
}
