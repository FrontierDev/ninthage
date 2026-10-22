using Game.Client;
using Game.Server.Core;
using Game.Server.Systems;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
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
        private static readonly Guid TEST_WORLD_ID =
            Guid.Parse("00000000-0000-0000-0000-000000000918");
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
        private static readonly FieldInfo PERIODIC_VALIDATION_BASELINES_FIELD =
            typeof(ServerActorManager).GetField("_periodicValidationBaselines", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo NEXT_VALIDATION_TICK_IDS_FIELD =
            typeof(ServerActorManager).GetField("_nextValidationTickIDs", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo REGISTER_SPAWNED_ACTOR_METHOD =
            typeof(ServerActorManager).GetMethod("RegisterSpawnedActor", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ACTOR_SPATIAL_CONTEXT_FIELD =
            typeof(Actor).GetField("_spatialContext", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo CLIENT_POSITION_MANAGER_INSTANCE_FIELD =
            typeof(ClientPositionManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo CLIENT_POSITION_MANAGER_INITIALIZED_FIELD =
            typeof(ClientPositionManager).GetField("_initialized", BindingFlags.Static | BindingFlags.NonPublic);

        private GameConfiguration _configuration;
        private float _originalTickDuration;
        private GameObject _actorManagerObject;
        private GameObject _positionManagerObject;
        private GameObject _clockHostObject;
        private GameObject _actorObject;
        private GameObject _clientWorldManagerObject;
        private GameObject _clientPositionManagerObject;
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
            Assert.That(ACTOR_SPATIAL_CONTEXT_FIELD, Is.Not.Null);
            ACTOR_SPATIAL_CONTEXT_FIELD.SetValue(
                _actor,
                ActorSpatialContext.CreatePublic(TEST_WORLD_ID, "test-map"));
            Assert.That(_actor.SpatialContext.IsValid, Is.True);
            RegisterSpawnedActor(_actor, new WorldPosition(0d, 0d, 0d));
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
            // owner manual check; proposals are submitted through the server receive seam.
            _actor.transform.position = new Vector3(500f, 0f, 0f);
            SubmitPosition(1d, 1L);
            RunSimulationTick(1);
            AssertECSPosition(actorEntity, 1f);
            Assert.That(_actor.transform.position.x, Is.EqualTo(500f).Within(0.0001f));

            RunSimulationTick(5);
            AssertECSPosition(actorEntity, 1f);
            AssertValidationState(expectedViolationCount: 0, expectedBaselinePosition: 1d, expectedNextTickID: 10);

            SubmitPosition(20d, 2L);
            RunSimulationTick(6);
            AssertECSPosition(actorEntity, 20f);
            RunSimulationTick(10);
            AssertECSPosition(actorEntity, 20f);
            AssertValidationState(expectedViolationCount: 1, expectedBaselinePosition: 20d, expectedNextTickID: 15);

            SubmitPosition(40d, 3L);
            RunSimulationTick(11);
            AssertECSPosition(actorEntity, 40f);
            RunSimulationTick(15);
            AssertECSPosition(actorEntity, 40f);
            AssertValidationState(expectedViolationCount: 2, expectedBaselinePosition: 40d, expectedNextTickID: 20);

            SubmitPosition(60d, 4L);
            RunSimulationTick(16);
            AssertECSPosition(actorEntity, 60f);
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
            Assert.That(_actor.transform.position.x, Is.EqualTo(40f).Within(0.0001f));
            AssertECSPosition(actorEntity, 40f);
            AssertValidationState(expectedViolationCount: 0, expectedBaselinePosition: 40d, expectedNextTickID: 25);

            SubmitPosition(60d, 5L);
            RunSimulationTick(21);
            AssertECSPosition(actorEntity, 60f);
            RunSimulationTick(25);
            AssertECSPosition(actorEntity, 60f);
            AssertValidationState(expectedViolationCount: 1, expectedBaselinePosition: 60d, expectedNextTickID: 30);

            SubmitPosition(80d, 6L);
            RunSimulationTick(26);
            AssertECSPosition(actorEntity, 80f);
            RunSimulationTick(30);
            AssertECSPosition(actorEntity, 80f);
            AssertValidationState(expectedViolationCount: 2, expectedBaselinePosition: 80d, expectedNextTickID: 35);

            RunSimulationTick(31);
            SubmitPosition(120d, 7L);
            previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                RunSimulationTick(35);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
            }
            Assert.That(_actor.transform.position.x, Is.EqualTo(80f).Within(0.0001f));
            AssertECSPosition(actorEntity, 80f);
            AssertValidationState(expectedViolationCount: 0, expectedBaselinePosition: 80d, expectedNextTickID: 40);

            yield return null;
        }

        [UnityTest]
        public IEnumerator OriginRebasePreservesAcceptedPositionAndCrossCellMovementCommits()
        {
            Entity actorEntity = GetActorEntity();
            CreateClientOriginFixture();

            var positionManager = ClientPositionManager.Instance;
            var initialAbsolutePosition = new WorldPosition(-100.25d, 4d, -50.5d);
            var movedAbsolutePosition = new WorldPosition(-99.25d, 4d, -50.5d);

            positionManager.SetOriginChunk(new Vector2Int(-2, -1));
            Assert.That(positionManager.OriginChunk, Is.EqualTo(new Vector2Int(-2, -1)));
            WorldPosition initialProposal = EncodeFromClientOrigin(
                positionManager,
                initialAbsolutePosition,
                out Vector3 initialLocalRepresentation);
            Assert.That(initialProposal.Chunk, Is.EqualTo(new Vector2Int(-2, -1)));
            Assert.That(initialLocalRepresentation.x, Is.EqualTo(99.75f).Within(0.0001f));
            SubmitPosition(initialProposal, 1L);
            RunSimulationTick(1);
            AssertECSPosition(actorEntity, initialAbsolutePosition);

            positionManager.SetOriginChunk(new Vector2Int(-1, -1));
            Assert.That(positionManager.OriginChunk, Is.EqualTo(new Vector2Int(-1, -1)));
            WorldPosition rebasedProposal = EncodeFromClientOrigin(
                positionManager,
                initialAbsolutePosition,
                out Vector3 rebasedLocalRepresentation);
            Assert.That(rebasedLocalRepresentation.x, Is.EqualTo(-0.25f).Within(0.0001f));
            Assert.That(rebasedLocalRepresentation.x, Is.Not.EqualTo(initialLocalRepresentation.x));
            AssertWorldPosition(rebasedProposal, initialAbsolutePosition);
            SubmitPosition(rebasedProposal, 2L);
            RunSimulationTick(2);
            AssertECSPosition(actorEntity, initialAbsolutePosition);

            WorldPosition movedProposal = EncodeFromClientOrigin(
                positionManager,
                movedAbsolutePosition,
                out _);
            Assert.That(movedProposal.Chunk, Is.EqualTo(new Vector2Int(-1, -1)));
            SubmitPosition(movedProposal, 3L);
            RunSimulationTick(3);
            AssertECSPosition(actorEntity, movedAbsolutePosition);

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

        private static void AssertECSPosition(Entity actorEntity, float expectedX)
        {
            var position = GetECSPosition(actorEntity);
            Assert.That(position.x, Is.EqualTo(expectedX).Within(0.0001d));
            Assert.That(position.y, Is.EqualTo(0d).Within(0.0001d));
            Assert.That(position.z, Is.EqualTo(0d).Within(0.0001d));
        }

        private static void AssertECSPosition(Entity actorEntity, WorldPosition expectedPosition)
        {
            var position = GetECSPosition(actorEntity);
            Assert.That(position.x, Is.EqualTo(expectedPosition.x).Within(0.000000001d));
            Assert.That(position.y, Is.EqualTo(expectedPosition.y).Within(0.000000001d));
            Assert.That(position.z, Is.EqualTo(expectedPosition.z).Within(0.000000001d));
        }

        private static void AssertWorldPosition(WorldPosition actual, WorldPosition expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.000000001d));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.000000001d));
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.000000001d));
        }

        private static Unity.Mathematics.double3 GetECSPosition(Entity actorEntity)
        {
            var component = ServerECSManager.DefaultWorld.EntityManager.GetComponentData<ActorComponent>(actorEntity);
            return component.WorldPositionD;
        }

        private static void AssertValidationState(
            int expectedViolationCount,
            double expectedBaselinePosition,
            long expectedNextTickID)
        {
            var manager = ServerActorManager.Instance;
            var violationCounters = (Dictionary<PlayerID, int>)VIOLATION_COUNTERS_FIELD.GetValue(manager);
            var periodicValidationBaselines =
                (Dictionary<PlayerID, WorldPosition>)PERIODIC_VALIDATION_BASELINES_FIELD.GetValue(manager);
            var nextValidationTickIDs = (Dictionary<PlayerID, long>)NEXT_VALIDATION_TICK_IDS_FIELD.GetValue(manager);

            Assert.That(violationCounters[OWNER_ID], Is.EqualTo(expectedViolationCount));
            Assert.That(
                periodicValidationBaselines[OWNER_ID].x,
                Is.EqualTo(expectedBaselinePosition).Within(0.0001d));
            Assert.That(nextValidationTickIDs[OWNER_ID], Is.EqualTo(expectedNextTickID));
        }

        private static void SubmitPosition(double x, long sequence)
        {
            SubmitPosition(new WorldPosition(x, 0d, 0d), sequence);
        }

        private static void SubmitPosition(WorldPosition position, long sequence)
        {
            MovementEndpoint.onServerPositionProposal?.Invoke(OWNER_ID, position, 1L, sequence);
        }

        private static WorldPosition EncodeFromClientOrigin(
            ClientPositionManager positionManager,
            WorldPosition absolutePosition,
            out Vector3 localRepresentation)
        {
            // Mirrors the client producer's local-frame reconstruction before handing the
            // resulting origin-qualified WorldPosition to the server receive seam.
            localRepresentation = positionManager.ChunkToLocal(
                absolutePosition.Chunk,
                absolutePosition.LocalPosition);
            var (chunk, localPosition) = positionManager.LocalToChunkRelative(localRepresentation);
            return WorldPosition.FromChunkLocal(chunk, localPosition);
        }

        private void CreateClientOriginFixture()
        {
            _clientWorldManagerObject = new GameObject("ServerPlayerMovementAuthorityClientWorldManager");
            _clientWorldManagerObject.AddComponent<ClientWorldManager>();

            _clientPositionManagerObject = new GameObject("ServerPlayerMovementAuthorityClientPositionManager");
            _clientPositionManagerObject.AddComponent<ClientPositionManager>();
            Assert.That(ClientPositionManager.Initialized, Is.True);
        }

        private static void RegisterSpawnedActor(Actor actor, WorldPosition position)
        {
            Assert.That(REGISTER_SPAWNED_ACTOR_METHOD, Is.Not.Null);
            Assert.That(ServerActorManager.Instance, Is.Not.Null);
            REGISTER_SPAWNED_ACTOR_METHOD.Invoke(
                ServerActorManager.Instance,
                new object[] { actor, position });
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

            if (_clientPositionManagerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_clientPositionManagerObject);
                _clientPositionManagerObject = null;
            }

            if (_clientWorldManagerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_clientWorldManagerObject);
                _clientWorldManagerObject = null;
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
            ResetClientPositionManagerStatics();
        }

        private static void ResetPositionManagerStatics()
        {
            POSITION_MANAGER_INSTANCE_FIELD.SetValue(null, null);
            POSITION_MANAGER_INITIALIZED_FIELD.SetValue(null, false);
        }

        private static void ResetClientPositionManagerStatics()
        {
            CLIENT_POSITION_MANAGER_INSTANCE_FIELD.SetValue(null, null);
            CLIENT_POSITION_MANAGER_INITIALIZED_FIELD.SetValue(null, false);
        }

        private sealed class ClockTestHost : MonoBehaviour
        {
        }
    }
}
