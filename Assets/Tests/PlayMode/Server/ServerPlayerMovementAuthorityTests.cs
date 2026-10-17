using Game.Server.Core;
using Game.Server.Systems;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using NUnit.Framework;
using PurrNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Runtime.Tests.Server
{
    /// <summary>Verifies owner-authored movement reaches ECS only after server validation accepts it.</summary>
    public sealed class ServerPlayerMovementAuthorityTests
    {
        private static readonly Guid ACTOR_ID =
            Guid.Parse("00000000-0000-0000-0000-000000000913");
        private static readonly PlayerID OWNER_ID = new(1UL, false);
        private static readonly MethodInfo UPDATE_SIMULATION_METHOD =
            typeof(ServerECSManager).GetMethod("UpdateSimulation", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly ConstructorInfo SIMULATION_TICK_CONSTRUCTOR =
            typeof(ServerSimulationTick).GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(long), typeof(double), typeof(float) },
                modifiers: null);
        private static readonly MethodInfo TRY_GET_ACTOR_ENTITY_METHOD =
            typeof(ServerActorManager).GetMethod("TryGetActorEntity", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ACTOR_OWNER_FIELD =
            typeof(Actor).GetField("<Owner>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo POSITION_MANAGER_INSTANCE_FIELD =
            typeof(ServerPositionManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo POSITION_MANAGER_INITIALIZED_FIELD =
            typeof(ServerPositionManager).GetField("_initialized", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo VIOLATION_COUNTERS_FIELD =
            typeof(ServerActorManager).GetField("_violationCounters", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo LAST_VALIDATED_POSITIONS_FIELD =
            typeof(ServerActorManager).GetField("_lastValidatedPositions", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo NEXT_VALIDATION_TICK_IDS_FIELD =
            typeof(ServerActorManager).GetField("_nextValidationTickIDs", BindingFlags.Instance | BindingFlags.NonPublic);

        private GameConfiguration _configuration;
        private float _originalTickDuration;
        private GameObject _actorManagerObject;
        private GameObject _positionManagerObject;
        private GameObject _clockHostObject;
        private GameObject _actorObject;
        private Actor _actor;

        [SetUp]
        public void SetUp()
        {
            StopRuntime();

            if (!GameConfigurationManager.IsInitialized)
                GameConfigurationManager.Initialize();

            _configuration = GameConfigurationManager.Config;
            Assert.That(_configuration, Is.Not.Null);
            _originalTickDuration = _configuration.ServerSimulationTickDuration;
            _configuration.ServerSimulationTickDuration = 0.1f;

            ServerECSManager.ServerInitializeECSWorlds();

            _positionManagerObject = new GameObject("ServerPlayerMovementAuthorityPositionManager");
            _positionManagerObject.AddComponent<ServerPositionManager>();

            _actorManagerObject = new GameObject("ServerPlayerMovementAuthorityActorManager");
            _actorManagerObject.AddComponent<ServerActorManager>();

            _clockHostObject = new GameObject("ServerPlayerMovementAuthorityClockHost");
            var clock = ServerSimulationClock.Start(_clockHostObject.AddComponent<ClockTestHost>());
            clock.enabled = false;

            _actorObject = new GameObject("ServerPlayerMovementAuthorityActor");
            _actor = _actorObject.AddComponent<Actor>();
            _actor.Initialize(ACTOR_ID, "Server Player Movement Authority Actor");
            ACTOR_OWNER_FIELD.SetValue(_actor, (PlayerID?)OWNER_ID);
            ActorEndpoint.onServerActorSpawned?.Invoke(_actor);
        }

        [TearDown]
        public void TearDown()
        {
            StopRuntime();
        }

        [UnityTest]
        public IEnumerator OwnerAuthoredPositionIsCorrectedBeforeECSCommitOnThirdViolation()
        {
            Entity actorEntity = GetActorEntity();
            Assert.That(_actor.Owner.HasValue, Is.True);
            Assert.That(_actor.Owner.Value, Is.EqualTo(OWNER_ID));
            Assert.That(
                ServerECSManager.DefaultWorld.EntityManager.GetComponentData<ActorComponent>(actorEntity).Owner,
                Is.EqualTo(OWNER_ID));

            // The fixture has no live PurrNet client transport. The TargetRpc delivery remains an
            // owner manual check; this test intentionally uses only the server state boundary.
            _actor.transform.position = new Vector3(5f, 0f, 0f);
            RunSimulationTick(5);
            AssertActorAndECSPosition(actorEntity, 5f);
            AssertValidationState(expectedViolationCount: 0, expectedLastPosition: 5f, expectedNextTickID: 10);

            _actor.transform.position = new Vector3(15f, 0f, 0f);
            RunSimulationTick(10);
            AssertActorAndECSPosition(actorEntity, 15f);
            AssertValidationState(expectedViolationCount: 1, expectedLastPosition: 15f, expectedNextTickID: 15);

            _actor.transform.position = new Vector3(25f, 0f, 0f);
            RunSimulationTick(15);
            AssertActorAndECSPosition(actorEntity, 25f);
            AssertValidationState(expectedViolationCount: 2, expectedLastPosition: 25f, expectedNextTickID: 20);

            _actor.transform.position = new Vector3(35f, 0f, 0f);
            bool previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                // The fixture has no NetworkManager, so PurrNet may emit a transport diagnostic
                // while the production TargetRpc attempts delivery. RPC delivery remains manual.
                RunSimulationTick(20);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
            }
            Assert.That(_actor.transform.position.x, Is.EqualTo(25f).Within(0.0001f));
            AssertECSPosition(actorEntity, 25f);
            Assert.That(GetECSPosition(actorEntity).x, Is.Not.EqualTo(35d).Within(0.0001d));
            AssertValidationState(expectedViolationCount: 0, expectedLastPosition: 25f, expectedNextTickID: 25);

            RunSimulationTick(21);
            Assert.That(_actor.transform.position.x, Is.EqualTo(25f).Within(0.0001f));
            AssertECSPosition(actorEntity, 25f);
            AssertValidationState(expectedViolationCount: 0, expectedLastPosition: 25f, expectedNextTickID: 25);

            yield return null;
        }

        private Entity GetActorEntity()
        {
            Assert.That(TRY_GET_ACTOR_ENTITY_METHOD, Is.Not.Null);
            object[] arguments = { ACTOR_ID, Entity.Null };
            bool found = (bool)TRY_GET_ACTOR_ENTITY_METHOD.Invoke(ServerActorManager.Instance, arguments);
            Assert.That(found, Is.True);
            return (Entity)arguments[1];
        }

        private void AssertActorAndECSPosition(Entity actorEntity, float expectedX)
        {
            Assert.That(_actor.transform.position.x, Is.EqualTo(expectedX).Within(0.0001f));
            AssertECSPosition(actorEntity, expectedX);
        }

        private static void AssertECSPosition(Entity actorEntity, float expectedX)
        {
            var position = GetECSPosition(actorEntity);
            Assert.That(position.x, Is.EqualTo(expectedX).Within(0.0001d));
            Assert.That(position.y, Is.EqualTo(0d).Within(0.0001d));
            Assert.That(position.z, Is.EqualTo(0d).Within(0.0001d));
        }

        private static Unity.Mathematics.double3 GetECSPosition(Entity actorEntity)
        {
            var component = ServerECSManager.DefaultWorld.EntityManager.GetComponentData<ActorComponent>(actorEntity);
            return component.WorldPositionD;
        }

        private static void AssertValidationState(int expectedViolationCount, float expectedLastPosition, long expectedNextTickID)
        {
            var manager = ServerActorManager.Instance;
            var violationCounters = (Dictionary<PlayerID, int>)VIOLATION_COUNTERS_FIELD.GetValue(manager);
            var lastValidatedPositions = (Dictionary<PlayerID, Vector3>)LAST_VALIDATED_POSITIONS_FIELD.GetValue(manager);
            var nextValidationTickIDs = (Dictionary<PlayerID, long>)NEXT_VALIDATION_TICK_IDS_FIELD.GetValue(manager);

            Assert.That(violationCounters[OWNER_ID], Is.EqualTo(expectedViolationCount));
            Assert.That(lastValidatedPositions[OWNER_ID].x, Is.EqualTo(expectedLastPosition).Within(0.0001f));
            Assert.That(nextValidationTickIDs[OWNER_ID], Is.EqualTo(expectedNextTickID));
        }

        private static void RunSimulationTick(long tickID)
        {
            Assert.That(UPDATE_SIMULATION_METHOD, Is.Not.Null);
            Assert.That(SIMULATION_TICK_CONSTRUCTOR, Is.Not.Null);

            float deltaTime = GameConfigurationManager.Config.ServerSimulationTickDuration;
            object tick = SIMULATION_TICK_CONSTRUCTOR.Invoke(new object[]
            {
                tickID,
                tickID * (double)deltaTime,
                deltaTime,
            });

            try
            {
                UPDATE_SIMULATION_METHOD.Invoke(null, new[] { tick });
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private void StopRuntime()
        {
            if (_actorObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_actorObject);
                _actorObject = null;
            }

            if (_actorManagerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_actorManagerObject);
                _actorManagerObject = null;
            }

            if (_positionManagerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_positionManagerObject);
                _positionManagerObject = null;
            }

            ServerInitialization.Stop();

            if (_clockHostObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_clockHostObject);
                _clockHostObject = null;
            }

            if (_configuration != null)
            {
                _configuration.ServerSimulationTickDuration = _originalTickDuration;
                _configuration = null;
            }

            _actor = null;
            ResetPositionManagerStatics();
        }

        private static void ResetPositionManagerStatics()
        {
            POSITION_MANAGER_INSTANCE_FIELD.SetValue(null, null);
            POSITION_MANAGER_INITIALIZED_FIELD.SetValue(null, false);
        }

        private sealed class ClockTestHost : MonoBehaviour
        {
        }
    }
}
