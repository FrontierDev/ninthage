using Game.Server.Core;
using Game.Shared;
using Game.Shared.Data;
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
    /// <summary>Verifies auto-attacks request spells only from authoritative tick eligibility.</summary>
    public sealed class ServerAutoAttackSchedulingTests
    {
        private static readonly Guid ATTACKER_ID =
            Guid.Parse("00000000-0000-0000-0000-000000000A11");
        private static readonly Guid TARGET_ID =
            Guid.Parse("00000000-0000-0000-0000-000000000A12");
        private static readonly PlayerID TARGET_OWNER_ID = new PlayerID(3UL, false);
        private static readonly FieldInfo LAST_TIMESTAMP_FIELD =
            typeof(ServerSimulationClock).GetField("_lastTimestamp", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo UPDATE_CLOCK_METHOD =
            typeof(ServerSimulationClock).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo ACTOR_OWNER_FIELD =
            typeof(Actor).GetField("<Owner>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<long> _acceptedSpellStartTickIDs = new List<long>();
        private GameConfiguration _configuration;
        private float _originalTickDuration;
        private int _originalMaxCatchUpTicks;
        private GameObject _networkManagerObject;
        private NetworkManager _networkManager;
        private GameObject _clockHostObject;
        private GameObject _actorManagerObject;
        private GameObject _cooldownManagerObject;
        private GameObject _spellCastManagerObject;
        private GameObject _autoAttackManagerObject;
        private GameObject _groundObject;
        private GameObject _attackerObject;
        private GameObject _targetObject;
        private ServerSimulationClock _clock;
        private NPCActor _attacker;
        private PlayerActor _target;
        private SpellDefinition _autoAttackSpell;
        private Action<Actor, SpellOverride> _spellStartedHandler;

        [SetUp]
        public void SetUp()
        {
            StopRuntime();

            if (!GameConfigurationManager.IsInitialized)
                GameConfigurationManager.Initialize();

            _configuration = GameConfigurationManager.Config;
            Assert.That(_configuration, Is.Not.Null);
            _autoAttackSpell = _configuration.AutoAttackSpell;
            Assert.That(_autoAttackSpell, Is.Not.Null);
            Assert.That(_configuration.CritResistanceStat, Is.Not.Null);

            _originalTickDuration = _configuration.ServerSimulationTickDuration;
            _originalMaxCatchUpTicks = _configuration.ServerSimulationMaxCatchUpTicks;
            _configuration.ServerSimulationTickDuration = 0.05f;
            _configuration.ServerSimulationMaxCatchUpTicks = 4;

            ServerECSManager.ServerInitializeECSWorlds();

            GameObject networkManagerPrefab = Resources.Load<GameObject>("NetworkManager");
            Assert.That(networkManagerPrefab, Is.Not.Null);
            _networkManagerObject = UnityEngine.Object.Instantiate(networkManagerPrefab);
            _networkManagerObject.name = "ServerAutoAttackSchedulingNetworkManager";
            _networkManager = _networkManagerObject.GetComponent<NetworkManager>();
            Assert.That(_networkManager, Is.Not.Null);
            _networkManager.startServerFlags = StartFlags.None;
            _networkManager.startClientFlags = StartFlags.None;
            var localTransport = _networkManagerObject.AddComponent<PurrNet.Transports.LocalTransport>();
            _networkManager.transport = localTransport;
            _networkManager.StartServer();

            _clockHostObject = new GameObject("ServerAutoAttackSchedulingClockHost");
            _clock = ServerSimulationClock.Start(_clockHostObject.AddComponent<ClockHost>());
            _clock.enabled = false;

            _actorManagerObject = new GameObject("ServerAutoAttackSchedulingActorManager");
            _actorManagerObject.AddComponent<ServerActorManager>();

            _cooldownManagerObject = new GameObject("ServerAutoAttackSchedulingCooldownManager");
            _cooldownManagerObject.AddComponent<ServerCooldownManager>();

            _spellCastManagerObject = new GameObject("ServerAutoAttackSchedulingSpellCastManager");
            _spellCastManagerObject.AddComponent<ServerSpellCastManager>();

            _autoAttackManagerObject = new GameObject("ServerAutoAttackSchedulingAutoAttackManager");
            _autoAttackManagerObject.AddComponent<ServerAutoAttackManager>();
        }

        [TearDown]
        public void TearDown()
        {
            StopRuntime();
        }

        [UnityTest]
        public IEnumerator AutoAttackRequestsAtEligibleTickAndStopsAfterRetirement()
        {
            yield return null;
            yield return WaitForServer();

            CreateActors();
            _spellStartedHandler = (caster, spell) =>
            {
                if (ReferenceEquals(caster, _attacker)
                    && spell != null
                    && spell.BaseDefinition != null
                    && spell.BaseDefinition.DefinitionID == _autoAttackSpell.DefinitionID)
                {
                    _acceptedSpellStartTickIDs.Add(ServerSimulationClock.CurrentTickID);
                }
            };
            ActorEndpoint.onServerSpellCastStarted += _spellStartedHandler;

            ActorEndpoint.onServerStartAutoAttack?.Invoke(_attacker);

            AdvanceTicks(1);
            Assert.That(_clock.LastExecutedTickID, Is.EqualTo(1));
            Assert.That(_acceptedSpellStartTickIDs, Is.Empty);
            Assert.That(_attacker.transform.position.y, Is.EqualTo(0f).Within(0.01f));

            _target.transform.position = new Vector3(1f, 0f, 0f);
            AdvanceTicks(1);
            Assert.That(_clock.LastExecutedTickID, Is.EqualTo(2));
            CollectionAssert.AreEqual(new long[] { 2 }, _acceptedSpellStartTickIDs);
            Assert.That(ServerCooldownManager.Instance.IsOnCooldown(_attacker.ID, _autoAttackSpell.DefinitionID), Is.True);

            AdvanceTicks(1);
            Assert.That(_clock.LastExecutedTickID, Is.EqualTo(3));
            CollectionAssert.AreEqual(new long[] { 2 }, _acceptedSpellStartTickIDs);

            ActorEndpoint.onServerStopAutoAttack?.Invoke(_attacker);
            AdvanceTicks(41);
            Assert.That(_clock.LastExecutedTickID, Is.GreaterThanOrEqualTo(44));
            Assert.That(ServerCooldownManager.Instance.IsOnCooldown(_attacker.ID, _autoAttackSpell.DefinitionID), Is.False);
            CollectionAssert.AreEqual(new long[] { 2 }, _acceptedSpellStartTickIDs);
            Assert.That(_attacker.transform.position.y, Is.EqualTo(0f).Within(0.01f));

            ActorEndpoint.onServerStartAutoAttack?.Invoke(_attacker);
            _target.GetComponent<ActorStatContainer>().SetStat(
                "health",
                new StatInstance(100f, 0f, 0f, 100f, 0f));
            Assert.That(_target.isDead, Is.True);

            AdvanceTicks(41);
            Assert.That(_clock.LastExecutedTickID, Is.GreaterThanOrEqualTo(85));
            CollectionAssert.AreEqual(new long[] { 2 }, _acceptedSpellStartTickIDs);
        }

        private void CreateActors()
        {
            Assert.That(ACTOR_OWNER_FIELD, Is.Not.Null);
            CreateGround();

            _attackerObject = new GameObject("Server Auto Attack Scheduling Attacker");
            _attackerObject.transform.position = Vector3.zero;
            _attacker = _attackerObject.AddComponent<NPCActor>();
            _attacker.Initialize(ATTACKER_ID, "Server Auto Attack Scheduling Attacker");
            _attacker.WorldPos = new WorldPosition(0d, 0d, 0d);
            ActorSpellcaster attackerSpellcaster = _attackerObject.AddComponent<ActorSpellcaster>();
            ActorStatContainer attackerStats = _attackerObject.AddComponent<ActorStatContainer>();
            AddCombatStats(attackerStats, 100f, _configuration.CritResistanceStat.DefinitionID);
            attackerSpellcaster.LearnSpell(_autoAttackSpell.DefinitionID);
            Assert.That(attackerSpellcaster.Spells.ContainsKey(_autoAttackSpell.DefinitionID), Is.True);

            _targetObject = new GameObject("Server Auto Attack Scheduling Target");
            _targetObject.transform.position = new Vector3(5f, 0f, 0f);
            _target = _targetObject.AddComponent<PlayerActor>();
            _target.Initialize(TARGET_ID, "Server Auto Attack Scheduling Target");
            _target.WorldPos = new WorldPosition(5d, 0d, 0d);
            ACTOR_OWNER_FIELD.SetValue(_target, (PlayerID?)TARGET_OWNER_ID);
            ActorStatContainer targetStats = _targetObject.AddComponent<ActorStatContainer>();
            AddCombatStats(targetStats, 100f, _configuration.CritResistanceStat.DefinitionID);

            _networkManager.Spawn(_attackerObject);
            _networkManager.Spawn(_targetObject);
            Assert.That(_attacker.isServer, Is.True);
            Assert.That(_target.isServer, Is.True);
            ActorEndpoint.onServerActorSpawned?.Invoke(_attacker);
            ActorEndpoint.onServerActorSpawned?.Invoke(_target);
            _attacker.SetTargetFromServer(_target);
            Assert.That(_attacker.Target, Is.SameAs(_target));
        }

        private void CreateGround()
        {
            string groundLayerName = _configuration.GroundDetectionMaskName;
            int groundLayer = LayerMask.NameToLayer(groundLayerName);
            Assert.That(groundLayer, Is.GreaterThanOrEqualTo(0),
                $"Configured ground layer '{groundLayerName}' does not exist.");

            _groundObject = new GameObject("Server Auto Attack Scheduling Ground");
            _groundObject.layer = groundLayer;
            BoxCollider groundCollider = _groundObject.AddComponent<BoxCollider>();
            groundCollider.size = new Vector3(100f, 1f, 100f);
            groundCollider.center = new Vector3(0f, -0.5f, 0f);

            Physics.SyncTransforms();
            Assert.That(groundCollider.bounds.max.y, Is.EqualTo(0f).Within(0.001f));
        }

        private static void AddCombatStats(ActorStatContainer stats, float health, string critResistanceStatID)
        {
            stats.SetStat("health", new StatInstance(health, 0f, 0f, health, health));
            stats.SetStat("melee_hit_chance", new StatInstance(100f, 0f, 0f, 100f, 100f));
            stats.SetStat("melee_crit_chance", new StatInstance(0f, 0f, 0f, 100f, 0f));
            stats.SetStat(critResistanceStatID, new StatInstance(0f, 0f, 0f, 100f, 0f));
        }

        private IEnumerator WaitForServer()
        {
            int waitedFrames = 0;
            while (!_networkManager.isServer)
            {
                if (++waitedFrames > 120)
                    Assert.Fail("The test NetworkManager did not become a server within 120 frames.");

                yield return null;
            }
        }

        private void AdvanceTicks(int count)
        {
            long targetTickID = checked(_clock.LastExecutedTickID + count);
            while (_clock.LastExecutedTickID < targetTickID)
                Assert.That(SimulateFrame(_clock, 0.051d), Is.GreaterThan(0));
        }

        private static int SimulateFrame(ServerSimulationClock clock, double elapsedSeconds)
        {
            Assert.That(LAST_TIMESTAMP_FIELD, Is.Not.Null);
            Assert.That(UPDATE_CLOCK_METHOD, Is.Not.Null);

            long elapsedTimestampTicks = checked((long)Math.Round(elapsedSeconds * Stopwatch.Frequency));
            LAST_TIMESTAMP_FIELD.SetValue(
                clock,
                checked(Stopwatch.GetTimestamp() - elapsedTimestampTicks));

            long executedBefore = clock.ExecutedTickCount;
            UPDATE_CLOCK_METHOD.Invoke(clock, null);
            return checked((int)(clock.ExecutedTickCount - executedBefore));
        }

        private void StopRuntime()
        {
            if (_spellStartedHandler != null)
            {
                ActorEndpoint.onServerSpellCastStarted -= _spellStartedHandler;
                _spellStartedHandler = null;
            }

            DestroyObject(_attackerObject);
            DestroyObject(_targetObject);
            DestroyObject(_groundObject);
            DestroyObject(_autoAttackManagerObject);
            DestroyObject(_spellCastManagerObject);
            DestroyObject(_cooldownManagerObject);
            DestroyObject(_actorManagerObject);

            if (_networkManager != null && _networkManager.isServer)
                _networkManager.StopServer();

            ServerInitialization.Stop();

            DestroyObject(_clockHostObject);
            DestroyObject(_networkManagerObject);

            if (_configuration != null)
            {
                _configuration.ServerSimulationTickDuration = _originalTickDuration;
                _configuration.ServerSimulationMaxCatchUpTicks = _originalMaxCatchUpTicks;
            }

            _acceptedSpellStartTickIDs.Clear();
            _attackerObject = null;
            _targetObject = null;
            _groundObject = null;
            _autoAttackManagerObject = null;
            _spellCastManagerObject = null;
            _cooldownManagerObject = null;
            _actorManagerObject = null;
            _clockHostObject = null;
            _networkManagerObject = null;
            _configuration = null;
            _networkManager = null;
            _clock = null;
            _attacker = null;
            _target = null;
            _autoAttackSpell = null;
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
