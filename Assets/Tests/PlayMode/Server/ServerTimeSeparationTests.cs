using Game.Server.Core;
using Game.Server.Systems;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using NUnit.Framework;
using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using Unity.Entities;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Runtime.Tests.Server
{
    /// <summary>Verifies that server UTC observations cannot advance fixed simulation state.</summary>
    public sealed class ServerTimeSeparationTests
    {
        private static readonly FieldInfo LAST_TIMESTAMP_FIELD =
            typeof(ServerSimulationClock).GetField("_lastTimestamp", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo UPDATE_METHOD =
            typeof(ServerSimulationClock).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo TIME_AUTHORITY_FIELD =
            typeof(ServerInitialization).GetField("_timeAuthority", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo TRY_GET_ACTOR_ENTITY_METHOD =
            typeof(ServerActorManager).GetMethod("TryGetActorEntity", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly Guid ACTOR_ID =
            Guid.Parse("00000000-0000-0000-0000-000000000C01");
        private const string COOLDOWN_ID = "utc-separation-cooldown";
        private const float TICK_DURATION_SECONDS = 0.1f;
        private const long MONOTONIC_FREQUENCY = 1000L;

        private GameConfiguration _configuration;
        private float _originalTickDuration;
        private int _originalMaxCatchUpTicks;
        private float _originalTimeScale;
        private int _originalTargetFrameRate;
        private bool _environmentCaptured;
        private GameObject _actorManagerObject;
        private GameObject _cooldownManagerObject;
        private GameObject _actorObject;
        private GameObject _clockHostObject;
        private Actor _actor;
        private ServerCooldownManager _cooldownManager;
        private ServerSimulationClock _clock;
        private ServerTimeAuthority _timeAuthority;
        private ServerTimeAuthority _previousTimeAuthority;
        private bool _timeAuthorityInjected;
        private DateTimeOffset _utcNow;
        private long _monotonicTimestamp;

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
            _environmentCaptured = true;

            _configuration.ServerSimulationTickDuration = TICK_DURATION_SECONDS;
            _configuration.ServerSimulationMaxCatchUpTicks = 4;
            ServerECSManager.ServerInitializeECSWorlds();

            _actorManagerObject = new GameObject("ServerTimeSeparationActorManager");
            _actorManagerObject.AddComponent<ServerActorManager>();
            _cooldownManagerObject = new GameObject("ServerTimeSeparationCooldownManager");
            _cooldownManager = _cooldownManagerObject.AddComponent<ServerCooldownManager>();
            _actorObject = CreateActor(
                "ServerTimeSeparationActor",
                ACTOR_ID,
                "Time Separation Test Actor",
                out _actor);

            _utcNow = new DateTimeOffset(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
            _monotonicTimestamp = 10_000L;
            _timeAuthority = new ServerTimeAuthority(
                () => _utcNow,
                () => _monotonicTimestamp,
                MONOTONIC_FREQUENCY);
            Assert.That(TIME_AUTHORITY_FIELD, Is.Not.Null);
            _previousTimeAuthority = (ServerTimeAuthority)TIME_AUTHORITY_FIELD.GetValue(null);
            TIME_AUTHORITY_FIELD.SetValue(null, _timeAuthority);
            _timeAuthorityInjected = true;

            _clockHostObject = new GameObject("ServerTimeSeparationClockHost");
            _clock = ServerSimulationClock.Start(_clockHostObject.AddComponent<ClockTestHost>());
            _clock.enabled = false;
        }

        [TearDown]
        public void TearDown()
        {
            StopRuntime();

            if (_environmentCaptured && _configuration != null)
            {
                _configuration.ServerSimulationTickDuration = _originalTickDuration;
                _configuration.ServerSimulationMaxCatchUpTicks = _originalMaxCatchUpTicks;
                Time.timeScale = _originalTimeScale;
                Application.targetFrameRate = _originalTargetFrameRate;
            }
        }

        [UnityTest]
        public IEnumerator UtcObservationDiscontinuitiesDoNotAdvanceClockOrCooldown()
        {
            const float cooldownDuration = 0.3f;
            ActorEndpoint.onServerStartCooldown?.Invoke(_actor.ID, COOLDOWN_ID, cooldownDuration);

            Entity actorEntity = GetActorEntity(_actor);
            long initialTickID = ServerSimulationClock.CurrentTickID;
            long initialExecutedTickCount = _clock.ExecutedTickCount;
            float initialCooldown = _cooldownManager.GetRemainingCooldown(_actor.ID, COOLDOWN_ID);
            float initialBufferCooldown = GetCooldownBufferRemaining(actorEntity, COOLDOWN_ID);

            Assert.That(ServerInitialization.TimeAuthority, Is.SameAs(_timeAuthority));
            Assert.That(initialTickID, Is.EqualTo(0L));
            Assert.That(initialExecutedTickCount, Is.EqualTo(0L));
            Assert.That(initialCooldown, Is.EqualTo(cooldownDuration).Within(0.000001f));
            Assert.That(initialBufferCooldown, Is.EqualTo(cooldownDuration).Within(0.000001f));

            ServerUtcObservation baseline = ServerInitialization.TimeAuthority.ReadObservation();
            _utcNow = baseline.UtcInstant.AddDays(1);
            _monotonicTimestamp++;
            ServerUtcObservation forwardJump = ServerInitialization.TimeAuthority.ReadObservation();
            AssertObservationRejected(baseline, forwardJump, "forward");
            AssertAuthoritativeStateUnchanged(
                actorEntity,
                initialTickID,
                initialExecutedTickCount,
                initialCooldown,
                initialBufferCooldown);

            _utcNow = baseline.UtcInstant;
            _monotonicTimestamp++;
            ServerUtcObservation backwardsBaseline = ServerInitialization.TimeAuthority.ReadObservation();
            _utcNow = baseline.UtcInstant.AddSeconds(-1);
            _monotonicTimestamp++;
            ServerUtcObservation backwardsJump = ServerInitialization.TimeAuthority.ReadObservation();
            AssertObservationRejected(backwardsBaseline, backwardsJump, "backwards");
            AssertAuthoritativeStateUnchanged(
                actorEntity,
                initialTickID,
                initialExecutedTickCount,
                initialCooldown,
                initialBufferCooldown);

            Assert.That(SimulateFrame(_clock, TICK_DURATION_SECONDS + 0.001d), Is.EqualTo(1));
            Assert.That(ServerSimulationClock.CurrentTickID, Is.EqualTo(initialTickID + 1));
            Assert.That(_clock.ExecutedTickCount, Is.EqualTo(initialExecutedTickCount + 1));
            float expectedCooldown = cooldownDuration - TICK_DURATION_SECONDS;
            Assert.That(
                _cooldownManager.GetRemainingCooldown(_actor.ID, COOLDOWN_ID),
                Is.EqualTo(expectedCooldown).Within(0.000001f));
            Assert.That(
                GetCooldownBufferRemaining(actorEntity, COOLDOWN_ID),
                Is.EqualTo(expectedCooldown).Within(0.000001f));

            yield return null;
        }

        private void AssertAuthoritativeStateUnchanged(
            Entity actorEntity,
            long expectedTickID,
            long expectedExecutedTickCount,
            float expectedCooldown,
            float expectedBufferCooldown)
        {
            Assert.That(ServerSimulationClock.CurrentTickID, Is.EqualTo(expectedTickID));
            Assert.That(_clock.ExecutedTickCount, Is.EqualTo(expectedExecutedTickCount));
            Assert.That(
                _cooldownManager.GetRemainingCooldown(_actor.ID, COOLDOWN_ID),
                Is.EqualTo(expectedCooldown).Within(0.000001f));
            Assert.That(
                GetCooldownBufferRemaining(actorEntity, COOLDOWN_ID),
                Is.EqualTo(expectedBufferCooldown).Within(0.000001f));
        }

        private static void AssertObservationRejected(
            ServerUtcObservation earlier,
            ServerUtcObservation later,
            string expectedMessage)
        {
            var exception = Assert.Throws<InvalidOperationException>(
                () => ServerTimeAuthority.ValidateObservationPair(earlier, later, TimeSpan.Zero));
            StringAssert.Contains(expectedMessage, exception.Message);
        }

        private static GameObject CreateActor(string objectName, Guid actorID, string actorName, out Actor actor)
        {
            var actorObject = new GameObject(objectName);
            actor = actorObject.AddComponent<Actor>();
            actor.Initialize(actorID, actorName);
            ActorEndpoint.onServerActorSpawned?.Invoke(actor);
            return actorObject;
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
            try
            {
                UPDATE_METHOD.Invoke(clock, null);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }

            return checked((int)(clock.ExecutedTickCount - executedBefore));
        }

        private static float GetCooldownBufferRemaining(Entity entity, string cooldownID)
        {
            var buffer = ServerECSManager.DefaultWorld.EntityManager.GetBuffer<CooldownElement>(entity, true);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].SpellID.ToString() == cooldownID)
                    return buffer[i].RemainingCooldown;
            }

            return 0f;
        }

        private static Entity GetActorEntity(Actor actor)
        {
            Assert.That(TRY_GET_ACTOR_ENTITY_METHOD, Is.Not.Null);
            object[] arguments = { actor.ID, Entity.Null };
            bool found = (bool)TRY_GET_ACTOR_ENTITY_METHOD.Invoke(ServerActorManager.Instance, arguments);
            Assert.That(found, Is.True);
            return (Entity)arguments[1];
        }

        private void StopRuntime()
        {
            ServerSimulationClock.Stop();

            if (_actorObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_actorObject);
                _actorObject = null;
            }

            if (_cooldownManagerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_cooldownManagerObject);
                _cooldownManagerObject = null;
            }

            if (_actorManagerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_actorManagerObject);
                _actorManagerObject = null;
            }

            ServerInitialization.Stop();

            if (_clockHostObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_clockHostObject);
                _clockHostObject = null;
            }

            if (_timeAuthorityInjected)
            {
                TIME_AUTHORITY_FIELD.SetValue(null, _previousTimeAuthority);
                _previousTimeAuthority = null;
                _timeAuthorityInjected = false;
            }

            _actor = null;
            _cooldownManager = null;
            _clock = null;
            _timeAuthority = null;
        }

        private sealed class ClockTestHost : MonoBehaviour
        {
        }
    }
}
