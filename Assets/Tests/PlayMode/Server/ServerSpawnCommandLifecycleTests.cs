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
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Game.Runtime.Tests.Server
{
    /// <summary>Verifies server spawn commands are consumed once and retire cleanly.</summary>
    public sealed class ServerSpawnCommandLifecycleTests
    {
        private const string TEST_PREFAB_NAME = "Test Spawnable Object";
        private static readonly Guid TEST_WORLD_ID =
            Guid.Parse("00000000-0000-0000-0000-000000000917");
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
        private static readonly MethodInfo INITIALIZE_RUNTIME_CONTEXT_METHOD =
            typeof(ServerWorldManager).GetMethod("InitializeRuntimeContext", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo ACTOR_OBJECTS_FIELD =
            typeof(ServerActorManager).GetField("_actorObjects", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ACTOR_ENTITIES_FIELD =
            typeof(ServerActorManager).GetField("_actorEntities", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<Actor> _spawnObservations = new();
        private GameConfiguration _configuration;
        private float _originalTickDuration;
        private GameObject _networkManagerObject;
        private NetworkManager _networkManager;
        private GameObject _clockHostObject;
        private GameObject _worldManagerObject;
        private GameObject _positionManagerObject;
        private GameObject _physicsManagerObject;
        private WorldChunkKey _sourceChunk;
        private GameObject _statManagerObject;
        private GameObject _actorManagerObject;
        private GameObject _spawnManagerObject;
        private ServerSpawnManager _spawnManager;
        private SceneID _destinationSceneID;
        private GameObject _testPrefab;
        private Action<Actor> _spawnHandler;

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

            _clockHostObject = new GameObject("ServerSpawnCommandLifecycleClockHost");
            var clock = ServerSimulationClock.Start(_clockHostObject.AddComponent<ClockTestHost>());
            clock.enabled = false;

            GameObject networkManagerPrefab = Resources.Load<GameObject>("NetworkManager");
            Assert.That(networkManagerPrefab, Is.Not.Null);
            _networkManagerObject = UnityEngine.Object.Instantiate(networkManagerPrefab);
            _networkManagerObject.name = "ServerSpawnCommandLifecycleNetworkManager";
            _networkManager = _networkManagerObject.GetComponent<NetworkManager>();
            Assert.That(_networkManager, Is.Not.Null);
            _networkManager.startServerFlags = StartFlags.None;
            _networkManager.startClientFlags = StartFlags.None;
        }

        [TearDown]
        public void TearDown()
        {
            StopRuntime();
        }

        [UnityTest]
        public IEnumerator SpawnCommandsCommitOnceRejectInvalidCommandsAndRetireActors()
        {
            yield return StartServerFixture();

            _spawnManager.SpawnAt(
                _sourceChunk,
                WorldPosition.FromChunkLocal(
                    _sourceChunk.ChunkCoord,
                    new Vector3(4f, 0f, 4f)),
                _testPrefab,
                _destinationSceneID,
                default);
            Assert.That(GetSpawnCommandCount(), Is.EqualTo(1));

            RunSimulationTick(1);

            Assert.That(_spawnObservations, Has.Count.EqualTo(1));
            Actor firstActor = _spawnObservations[0];
            AssertActorRegisteredOnce(firstActor, expectedOwner: default);
            Assert.That(GetSpawnCommandCount(), Is.EqualTo(0));

            RunSimulationTick(2);

            Assert.That(_spawnObservations, Has.Count.EqualTo(1));
            AssertActorRegisteredOnce(firstActor, expectedOwner: default);
            Assert.That(GetSpawnCommandCount(), Is.EqualTo(0));

            Guid firstActorID = firstActor.ID;
            PurrNet.UnityProxy.Destroy(firstActor.gameObject);
            yield return null;
            Assert.That(ServerActorManager.Instance.TryGetActor(firstActorID, out _), Is.False);

            _spawnManager.SpawnAt(
                _sourceChunk,
                WorldPosition.FromChunkLocal(
                    _sourceChunk.ChunkCoord,
                    new Vector3(8f, 0f, 8f)),
                _testPrefab,
                _destinationSceneID,
                default);
            _spawnManager.SpawnAt(
                _sourceChunk,
                WorldPosition.FromChunkLocal(
                    _sourceChunk.ChunkCoord,
                    new Vector3(12f, 0f, 12f)),
                _testPrefab,
                _destinationSceneID,
                default);
            InvalidateFirstQueuedSpawnCommand();
            Assert.That(GetSpawnCommandCount(), Is.EqualTo(2));

            bool previousIgnoreFailingMessages = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            try
            {
                // The invalid registry entry is expected to be rejected by the production commit
                // path; this fixture asserts state rather than the diagnostic wording.
                RunSimulationTick(3);
            }
            finally
            {
                LogAssert.ignoreFailingMessages = previousIgnoreFailingMessages;
            }

            Assert.That(_spawnObservations, Has.Count.EqualTo(2));
            Actor neighboringActor = _spawnObservations[1];
            Assert.That(neighboringActor, Is.Not.SameAs(firstActor));
            AssertActorRegisteredOnce(neighboringActor, expectedOwner: default);
            Assert.That(GetActorMapCount(), Is.EqualTo(1));
            Assert.That(GetSpawnCommandCount(), Is.EqualTo(0));

            RunSimulationTick(4);

            Assert.That(_spawnObservations, Has.Count.EqualTo(2));
            AssertActorRegisteredOnce(neighboringActor, expectedOwner: default);
            Assert.That(GetSpawnCommandCount(), Is.EqualTo(0));

            Guid neighboringActorID = neighboringActor.ID;
            PurrNet.UnityProxy.Destroy(neighboringActor.gameObject);
            yield return null;
            Assert.That(ServerActorManager.Instance.TryGetActor(neighboringActorID, out _), Is.False);
        }

        private IEnumerator StartServerFixture()
        {
            yield return null;

            _networkManager.StartServer();
            int waitedFrames = 0;
            while (!_networkManager.isServer)
            {
                if (++waitedFrames > 120)
                    Assert.Fail("The test NetworkManager did not become a server within 120 frames.");
                yield return null;
            }

            Assert.That(INITIALIZE_RUNTIME_CONTEXT_METHOD, Is.Not.Null);
            INITIALIZE_RUNTIME_CONTEXT_METHOD.Invoke(null, new object[] { TEST_WORLD_ID });

            _worldManagerObject = new GameObject("ServerSpawnCommandLifecycleWorldManager");
            _worldManagerObject.AddComponent<ServerWorldManager>();
            _positionManagerObject =
                new GameObject("ServerSpawnCommandLifecyclePositionManager");
            _positionManagerObject.AddComponent<ServerPositionManager>();
            _physicsManagerObject =
                new GameObject("ServerSpawnCommandLifecyclePhysicsManager");
            _physicsManagerObject.AddComponent<ServerPhysicsManager>();

            _sourceChunk = new WorldChunkKey(
                ServerWorldManager.Instance.CurrentRuntimeContext,
                Vector2Int.zero);
            ServerPhysicsManager.Instance.BindEnvironmentScene(
                _sourceChunk,
                SceneManager.GetActiveScene());

            _statManagerObject = new GameObject("ServerSpawnCommandLifecycleStatManager");
            _statManagerObject.AddComponent<ServerStatManager>();
            _actorManagerObject = new GameObject("ServerSpawnCommandLifecycleActorManager");
            _actorManagerObject.AddComponent<ServerActorManager>();
            _spawnManagerObject = new GameObject("ServerSpawnCommandLifecycleSpawnManager");
            _spawnManager = _spawnManagerObject.AddComponent<ServerSpawnManager>();

            _spawnHandler = actor => _spawnObservations.Add(actor);
            ActorEndpoint.onServerActorSpawned += _spawnHandler;

            Assert.That(
                _networkManager.sceneModule.TryGetSceneID(SceneManager.GetActiveScene(), out _destinationSceneID),
                Is.True);
            _testPrefab = FindRegisteredPrefab(TEST_PREFAB_NAME);
        }

        private static GameObject FindRegisteredPrefab(string prefabName)
        {
            Assert.That(NetworkManager.main, Is.Not.Null);
            Assert.That(NetworkManager.main.prefabProvider, Is.Not.Null);

            foreach (PrefabData prefabData in NetworkManager.main.prefabProvider.allPrefabs)
            {
                if (prefabData.prefab != null && prefabData.prefab.name == prefabName)
                    return prefabData.prefab;
            }

            Assert.Fail($"Network prefab '{prefabName}' was not found in the registered prefab provider.");
            return null;
        }

        private void InvalidateFirstQueuedSpawnCommand()
        {
            EntityQuery query = ServerECSManager.DefaultWorld.EntityManager.CreateEntityQuery(typeof(SpawnCommand));
            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            Assert.That(entities.Length, Is.EqualTo(2));

            EntityManager entityManager = ServerECSManager.DefaultWorld.EntityManager;
            bool invalidated = false;
            foreach (Entity entity in entities)
            {
                SpawnCommand command = entityManager.GetComponentData<SpawnCommand>(entity);
                if (command.WorldPositionD.x != 8d)
                    continue;

                command.PrefabIndex = int.MaxValue;
                entityManager.SetComponentData(entity, command);
                invalidated = true;
                break;
            }

            Assert.That(invalidated, Is.True, "The intended neighboring spawn command was not queued.");
        }

        private void AssertActorRegisteredOnce(Actor actor, PlayerID expectedOwner)
        {
            Assert.That(actor, Is.Not.Null);
            Assert.That(ServerActorManager.Instance.TryGetActor(actor.ID, out var mappedActor), Is.True);
            Assert.That(mappedActor, Is.SameAs(actor));
            Assert.That(TRY_GET_ACTOR_ENTITY_METHOD, Is.Not.Null);

            object[] arguments = { actor.ID, Entity.Null };
            bool foundEntity = (bool)TRY_GET_ACTOR_ENTITY_METHOD.Invoke(ServerActorManager.Instance, arguments);
            Assert.That(foundEntity, Is.True);
            Entity entity = (Entity)arguments[1];
            Assert.That(ServerECSManager.DefaultWorld.EntityManager.Exists(entity), Is.True);

            ActorComponent component =
                ServerECSManager.DefaultWorld.EntityManager.GetComponentData<ActorComponent>(entity);
            Assert.That(component.ActorID, Is.EqualTo(actor.ID));
            Assert.That(component.Owner, Is.EqualTo(expectedOwner));
            Assert.That(actor.GetComponent<NetworkIdentity>(), Is.Not.Null);
            Assert.That(GetActorMapCount(), Is.EqualTo(1));
            Assert.That(GetActorEntityMapCount(), Is.EqualTo(1));
        }

        private static int GetSpawnCommandCount()
        {
            EntityQuery query = ServerECSManager.DefaultWorld.EntityManager.CreateEntityQuery(typeof(SpawnCommand));
            return query.CalculateEntityCount();
        }

        private static int GetActorMapCount()
        {
            var actors = (Dictionary<Guid, Actor>)ACTOR_OBJECTS_FIELD.GetValue(ServerActorManager.Instance);
            return actors.Count;
        }

        private static int GetActorEntityMapCount()
        {
            var entities = (Dictionary<Guid, Entity>)ACTOR_ENTITIES_FIELD.GetValue(ServerActorManager.Instance);
            return entities.Count;
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
            if (_spawnHandler != null)
            {
                ActorEndpoint.onServerActorSpawned -= _spawnHandler;
                _spawnHandler = null;
            }

            foreach (Actor actor in _spawnObservations)
            {
                if (actor != null)
                    PurrNet.UnityProxy.DestroyImmediate(actor.gameObject);
            }

            if (_spawnManagerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_spawnManagerObject);
                _spawnManagerObject = null;
            }

            if (_actorManagerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_actorManagerObject);
                _actorManagerObject = null;
            }

            if (_statManagerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_statManagerObject);
                _statManagerObject = null;
            }

            if (_physicsManagerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_physicsManagerObject);
                _physicsManagerObject = null;
            }

            if (_positionManagerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_positionManagerObject);
                _positionManagerObject = null;
            }

            if (_worldManagerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_worldManagerObject);
                _worldManagerObject = null;
            }

            if (_networkManager != null && _networkManager.isServer)
                _networkManager.StopServer();

            if (_networkManagerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_networkManagerObject);
                _networkManagerObject = null;
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

            _networkManager = null;
            _spawnManager = null;
            _sourceChunk = default;
            _testPrefab = null;
            _spawnObservations.Clear();
        }

        private sealed class ClockTestHost : MonoBehaviour
        {
        }
    }
}
