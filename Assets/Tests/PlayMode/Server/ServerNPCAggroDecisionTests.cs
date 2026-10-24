using Game.Server.Core;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using NUnit.Framework;
using PurrNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Runtime.Tests.Server
{
    /// <summary>Verifies delayed NPC aggro decisions use the authoritative scheduler and live query state.</summary>
    public sealed class ServerNPCAggroDecisionTests
    {
        private static readonly FieldInfo LAST_TIMESTAMP_FIELD =
            typeof(ServerSimulationClock).GetField("_lastTimestamp", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo UPDATE_METHOD =
            typeof(ServerSimulationClock).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo NPC_AGGRO_RANGE_FIELD =
            typeof(NPCBehavior).GetField("aggroRange", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo NPC_AGGRO_DELAY_FIELD =
            typeof(NPCBehavior).GetField("aggroDelay", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ACTOR_OWNER_FIELD =
            typeof(Actor).GetField("<Owner>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly Guid NPC_ID = Guid.Parse("00000000-0000-0000-0000-000000000914");
        private static readonly Guid PLAYER_ID = Guid.Parse("00000000-0000-0000-0000-000000000915");
        private static readonly Guid TEST_WORLD_ID = Guid.Parse("00000000-0000-0000-0000-000000000916");
        private static readonly PlayerID PLAYER_OWNER_ID = new PlayerID(2UL, false);
        private static readonly MethodInfo REGISTER_SPAWNED_ACTOR_METHOD =
            typeof(ServerActorManager).GetMethod("RegisterSpawnedActor", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo CLEAR_INCOMPATIBLE_TARGETS_METHOD =
            typeof(ServerActorManager).GetMethod("ClearIncompatibleTargetsForTransfer", BindingFlags.Instance | BindingFlags.NonPublic);

        private GameConfiguration _configuration;
        private float _originalTickDuration;
        private int _originalMaxCatchUpTicks;
        private float _originalNPCAggroDecisionInterval;
        private GameObject _networkManagerObject;
        private NetworkManager _networkManager;
        private GameObject _clockHostObject;
        private GameObject _actorManagerObject;
        private GameObject _groundObject;
        private GameObject _npcObject;
        private GameObject _playerObject;
        private ServerSimulationClock _clock;
        private ServerActorManager _actorManager;
        private NPCActor _npc;
        private PlayerActor _player;
        private Collider _playerCollider;
        private Action<Actor> _startAutoAttackHandler;
        private int _startAutoAttackCount;
        private readonly List<Actor> _startAutoAttackActors = new List<Actor>();

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
            _originalNPCAggroDecisionInterval = _configuration.NPCAggroDecisionIntervalSeconds;

            _configuration.ServerSimulationTickDuration = 0.05f;
            _configuration.ServerSimulationMaxCatchUpTicks = 4;
            _configuration.NPCAggroDecisionIntervalSeconds = 0.2f;

            ServerECSManager.ServerInitializeECSWorlds();

            GameObject networkManagerPrefab = Resources.Load<GameObject>("NetworkManager");
            Assert.That(networkManagerPrefab, Is.Not.Null);
            _networkManagerObject = UnityEngine.Object.Instantiate(networkManagerPrefab);
            _networkManagerObject.name = "ServerNPCAggroDecisionNetworkManager";

            _networkManager = _networkManagerObject.GetComponent<NetworkManager>();
            Assert.That(_networkManager, Is.Not.Null);
            _networkManager.startServerFlags = StartFlags.None;
            _networkManager.startClientFlags = StartFlags.None;
            var localTransport = _networkManagerObject.AddComponent<PurrNet.Transports.LocalTransport>();
            _networkManager.transport = localTransport;
            _networkManager.StartServer();

            _clockHostObject = new GameObject("ServerNPCAggroDecisionClockHost");
            var clockHost = _clockHostObject.AddComponent<ClockHost>();
            _clock = ServerSimulationClock.Start(clockHost);
            _clock.enabled = false;

            _actorManagerObject = new GameObject("ServerNPCAggroDecisionActorManager");
            _actorManager = _actorManagerObject.AddComponent<ServerActorManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_startAutoAttackHandler != null)
                ActorEndpoint.onServerStartAutoAttack -= _startAutoAttackHandler;

            DestroyObject(_npcObject);
            DestroyObject(_playerObject);
            DestroyObject(_groundObject);
            DestroyObject(_actorManagerObject);

            if (_networkManagerObject != null)
            {
                if (_networkManager != null && _networkManager.isServer)
                    _networkManager.StopServer();
            }

            ServerInitialization.Stop();

            DestroyObject(_clockHostObject);
            DestroyObject(_networkManagerObject);

            if (_configuration != null)
            {
                _configuration.ServerSimulationTickDuration = _originalTickDuration;
                _configuration.ServerSimulationMaxCatchUpTicks = _originalMaxCatchUpTicks;
                _configuration.NPCAggroDecisionIntervalSeconds = _originalNPCAggroDecisionInterval;
            }

            _startAutoAttackHandler = null;
            _startAutoAttackCount = 0;
            _startAutoAttackActors.Clear();
            _networkManager = null;
            _npc = null;
            _player = null;
            _playerCollider = null;
            _groundObject = null;
            _actorManager = null;
            _clock = null;
        }

        [UnityTest]
        public IEnumerator NPCDoesNotAcquireBeforeDelayAndClearsDepartedCandidate()
        {
            yield return null;

            int waitedFrames = 0;
            while (!_networkManager.isServer)
            {
                if (++waitedFrames > 120)
                    Assert.Fail("The test NetworkManager did not become a server within 120 frames.");

                yield return null;
            }

            CreateActors();
            Physics.SyncTransforms();

            _startAutoAttackHandler = actor =>
            {
                if (!ReferenceEquals(actor, _npc))
                    return;

                _startAutoAttackCount++;
                _startAutoAttackActors.Add(actor);
            };
            ActorEndpoint.onServerStartAutoAttack += _startAutoAttackHandler;

            AdvanceTicks(1);
            Assert.That(_clock.LastExecutedTickID, Is.EqualTo(1));
            AssertNoAcquisition();

            AdvanceTicks(4);
            Assert.That(_clock.LastExecutedTickID, Is.EqualTo(5));
            AssertNoAcquisition();

            _playerCollider.enabled = false;
            Physics.SyncTransforms();

            AdvanceTicks(4);
            Assert.That(_clock.LastExecutedTickID, Is.EqualTo(9));
            AssertNoAcquisition();

            AdvanceTicks(4);
            Assert.That(_clock.LastExecutedTickID, Is.EqualTo(13));
            AssertNoAcquisition();

            _playerCollider.enabled = true;
            _playerObject.transform.position = new Vector3(1f, 0f, 0f);
            Physics.SyncTransforms();
            AssertPlayerColliderInAggroOverlap();

            AdvanceTicks(4);
            Assert.That(_clock.LastExecutedTickID, Is.EqualTo(17));
            AssertNoAcquisition();

            AdvanceTicks(4);
            Assert.That(_clock.LastExecutedTickID, Is.EqualTo(21));
            AssertNoAcquisition();

            AdvanceTicks(4);
            Assert.That(_clock.LastExecutedTickID, Is.EqualTo(25));
            Assert.That(_npc.Target, Is.SameAs(_player));
            Assert.That(_startAutoAttackCount, Is.EqualTo(1));
            CollectionAssert.AreEqual(new Actor[] { _npc }, _startAutoAttackActors);
            Assert.That(_npc.transform.position.y, Is.EqualTo(0f).Within(0.01f));

            AdvanceTicks(4);
            Assert.That(_clock.LastExecutedTickID, Is.EqualTo(29));
            Assert.That(_npc.Target, Is.SameAs(_player));
            Assert.That(_startAutoAttackCount, Is.EqualTo(1));
            Assert.That(_npc.transform.position.y, Is.EqualTo(0f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator ContextTransferRetiresOldNPCTargetAndStopsAttack()
        {
            yield return null;

            int waitedFrames = 0;
            while (!_networkManager.isServer)
            {
                if (++waitedFrames > 120)
                    Assert.Fail("The test NetworkManager did not become a server within 120 frames.");

                yield return null;
            }

            CreateActors();
            Assert.That(CLEAR_INCOMPATIBLE_TARGETS_METHOD, Is.Not.Null);
            _npc.SetTargetFromServer(_player);

            int stoppedAttacks = 0;
            void OnStopAutoAttack(Actor actor)
            {
                if (ReferenceEquals(actor, _npc))
                    stoppedAttacks++;
            }

            ActorEndpoint.onServerStopAutoAttack += OnStopAutoAttack;
            try
            {
                var publicContext = ActorSpatialContext.CreatePublic(TEST_WORLD_ID, "test-map");
                CLEAR_INCOMPATIBLE_TARGETS_METHOD.Invoke(
                    _actorManager,
                    new object[] { _player, publicContext });
                Assert.That(_npc.Target, Is.SameAs(_player));
                Assert.That(stoppedAttacks, Is.Zero);

                var instanceContext = ActorSpatialContext.CreateInstanced(
                    TEST_WORLD_ID,
                    "test-map",
                    Guid.NewGuid());
                CLEAR_INCOMPATIBLE_TARGETS_METHOD.Invoke(
                    _actorManager,
                    new object[] { _player, instanceContext });
                Assert.That(_npc.Target, Is.Null);
                Assert.That(stoppedAttacks, Is.EqualTo(1));
            }
            finally
            {
                ActorEndpoint.onServerStopAutoAttack -= OnStopAutoAttack;
            }
        }

        private void CreateActors()
        {
            Assert.That(NPC_AGGRO_RANGE_FIELD, Is.Not.Null);
            Assert.That(NPC_AGGRO_DELAY_FIELD, Is.Not.Null);
            Assert.That(ACTOR_OWNER_FIELD, Is.Not.Null);
            CreateGround();

            _npcObject = new GameObject("Server NPC Aggro Decision NPC");
            _npcObject.transform.position = Vector3.zero;
            _npc = _npcObject.AddComponent<NPCActor>();
            _npc.Initialize(NPC_ID, "Server NPC Aggro Decision NPC");
            _npc.SetAuthoritativeSpatialContext(
                ActorSpatialContext.CreatePublic(TEST_WORLD_ID, "test-map"));
            NPCBehavior npcBehavior = _npcObject.AddComponent<NPCBehavior>();
            NPC_AGGRO_RANGE_FIELD.SetValue(npcBehavior, 3f);
            NPC_AGGRO_DELAY_FIELD.SetValue(npcBehavior, 0.4f);

            _playerObject = new GameObject("Server NPC Aggro Decision Player");
            _playerObject.transform.position = new Vector3(1f, 0f, 0f);
            _player = _playerObject.AddComponent<PlayerActor>();
            _player.Initialize(PLAYER_ID, "Server NPC Aggro Decision Player");
            ACTOR_OWNER_FIELD.SetValue(_player, (PlayerID?)PLAYER_OWNER_ID);
            _player.SetAuthoritativeSpatialContext(
                ActorSpatialContext.CreatePublic(TEST_WORLD_ID, "test-map"));
            _playerCollider = _playerObject.AddComponent<SphereCollider>();

            _networkManager.Spawn(_npcObject);
            _networkManager.Spawn(_playerObject);
            Assert.That(_npc.isServer, Is.True);
            Assert.That(_player.isServer, Is.True);
            RegisterSpawnedActor(_npc, new WorldPosition(0d, 0d, 0d));
            RegisterSpawnedActor(_player, new WorldPosition(1d, 0d, 0d));
            ActorEndpoint.onServerActorSpawned?.Invoke(_npc);
            ActorEndpoint.onServerActorSpawned?.Invoke(_player);
        }

        private static void RegisterSpawnedActor(Actor actor, WorldPosition position)
        {
            Assert.That(REGISTER_SPAWNED_ACTOR_METHOD, Is.Not.Null);
            Assert.That(ServerActorManager.Instance, Is.Not.Null);
            REGISTER_SPAWNED_ACTOR_METHOD.Invoke(
                ServerActorManager.Instance,
                new object[] { actor, position });
        }

        private void CreateGround()
        {
            string groundLayerName = _configuration.GroundDetectionMaskName;
            int groundLayer = LayerMask.NameToLayer(groundLayerName);
            Assert.That(groundLayer, Is.GreaterThanOrEqualTo(0),
                $"Configured ground layer '{groundLayerName}' does not exist.");

            _groundObject = new GameObject("Server NPC Aggro Decision Ground");
            _groundObject.layer = groundLayer;
            BoxCollider groundCollider = _groundObject.AddComponent<BoxCollider>();
            groundCollider.size = new Vector3(100f, 1f, 100f);
            groundCollider.center = new Vector3(0f, -0.5f, 0f);

            Physics.SyncTransforms();
            Assert.That(groundCollider.bounds.max.y, Is.EqualTo(0f).Within(0.001f));
        }

        private void AssertPlayerColliderInAggroOverlap()
        {
            Assert.That(_playerCollider.enabled, Is.True);

            bool playerFound = false;
            foreach (Collider collider in Physics.OverlapSphere(_npc.transform.position, 3f))
            {
                if (collider.GetComponentInParent<PlayerActor>() == _player)
                {
                    playerFound = true;
                    break;
                }
            }

            Assert.That(playerFound, Is.True);
        }

        private void AdvanceTicks(int count)
        {
            for (int i = 0; i < count; i++)
                Assert.That(SimulateFrame(_clock, 0.051d), Is.EqualTo(1));
        }

        private void AssertNoAcquisition()
        {
            Assert.That(_npc.Target, Is.Null);
            Assert.That(_startAutoAttackCount, Is.EqualTo(0));
            Assert.That(_startAutoAttackActors, Is.Empty);
            Assert.That(_npc.transform.position.y, Is.EqualTo(0f).Within(0.01f));
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
            ServerInitialization.Stop();
        }

        private static void DestroyObject(GameObject target)
        {
            if (target != null)
                UnityEngine.Object.DestroyImmediate(target);
        }

        private sealed class ClockHost : MonoBehaviour
        {
        }
    }
}
