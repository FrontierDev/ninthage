using Game.Server.Core;
using Game.Server.Systems;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Data.SpellEffectDefinitions;
using Game.Shared.Data.SpellTargetDefinitions;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
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
    /// <summary>Verifies authoritative delayed spell-action deadlines and cancellation boundaries.</summary>
    public sealed class ServerDelayedSpellActionTimingTests
    {
        private static readonly Guid CASTER_ID =
            Guid.Parse("00000000-0000-0000-0000-000000000B01");
        private static readonly Guid TARGET_ID =
            Guid.Parse("00000000-0000-0000-0000-000000000B02");
        private static readonly Guid RETIRED_CASTER_ID =
            Guid.Parse("00000000-0000-0000-0000-000000000B03");
        private static readonly Guid RETIRED_TARGET_ID =
            Guid.Parse("00000000-0000-0000-0000-000000000B04");
        private static readonly Guid TEST_WORLD_ID =
            Guid.Parse("00000000-0000-0000-0000-000000000B05");
        private static readonly FieldInfo LAST_TIMESTAMP_FIELD =
            typeof(ServerSimulationClock).GetField("_lastTimestamp", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo UPDATE_CLOCK_METHOD =
            typeof(ServerSimulationClock).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo TRY_GET_ACTOR_ENTITY_METHOD =
            typeof(ServerActorManager).GetMethod("TryGetActorEntity", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo REGISTER_SPAWNED_ACTOR_METHOD =
            typeof(ServerActorManager).GetMethod("RegisterSpawnedActor", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly HashSet<Guid> _impactTargetIDs = new();
        private readonly List<ImpactObservation> _impactObservations = new();
        private readonly List<CallbackObservation> _callbackObservations = new();
        private readonly Dictionary<Guid, float> _lastObservedHealth = new();
        private readonly List<SpellDefinition> _spellDefinitions = new();
        private GameConfiguration _configuration;
        private float _originalTickDuration;
        private int _originalMaxCatchUpTicks;
        private GameObject _actorManagerObject;
        private GameObject _cooldownManagerObject;
        private GameObject _spellCastManagerObject;
        private GameObject _clockHostObject;
        private GameObject _networkManagerObject;
        private GameObject _actorPrefab;
        private GameObject _casterObject;
        private GameObject _targetObject;
        private GameObject _retiredCasterObject;
        private GameObject _retiredTargetObject;
        private Actor _caster;
        private Actor _target;
        private Actor _retiredCaster;
        private Actor _retiredTarget;
        private ServerSimulationClock _clock;
        private ServerSpellCastManager _spellCastManager;

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
            _configuration.ServerSimulationTickDuration = 0.05f;
            _configuration.ServerSimulationMaxCatchUpTicks = 8;

            ServerECSManager.ServerInitializeECSWorlds();

            _actorManagerObject = new GameObject("ServerDelayedSpellActionTimingActorManager");
            _actorManagerObject.AddComponent<ServerActorManager>();

            _cooldownManagerObject = new GameObject("ServerDelayedSpellActionTimingCooldownManager");
            _cooldownManagerObject.AddComponent<ServerCooldownManager>();

            _spellCastManagerObject = new GameObject("ServerDelayedSpellActionTimingSpellCastManager");
            _spellCastManager = _spellCastManagerObject.AddComponent<ServerSpellCastManager>();

            _clockHostObject = new GameObject("ServerDelayedSpellActionTimingClockHost");
            _clock = ServerSimulationClock.Start(_clockHostObject.AddComponent<ClockTestHost>());
            _clock.enabled = false;

            var networkManagerPrefab = Resources.Load<GameObject>("NetworkManager");
            Assert.That(networkManagerPrefab, Is.Not.Null);
            _networkManagerObject = UnityEngine.Object.Instantiate(networkManagerPrefab);
            _networkManagerObject.name = "ServerDelayedSpellActionTimingNetworkManager";

            var networkManager = _networkManagerObject.GetComponent<PurrNet.NetworkManager>();
            Assert.That(networkManager, Is.Not.Null);
            networkManager.startServerFlags = PurrNet.StartFlags.None;
            networkManager.startClientFlags = PurrNet.StartFlags.None;

            var localTransport = _networkManagerObject.AddComponent<PurrNet.Transports.LocalTransport>();
            networkManager.transport = localTransport;

            _actorPrefab = new GameObject("ServerDelayedSpellActionTimingActorPrefab");
            _actorPrefab.SetActive(false);
            _actorPrefab.AddComponent<Actor>();
            _actorPrefab.AddComponent<ActorStatContainer>();
            networkManager.SetPrefabProvider(new Test_PrefabProvider(_actorPrefab));
            networkManager.StartServer();

            _casterObject = CreateActor(
                "ServerDelayedSpellActionTimingCaster",
                CASTER_ID,
                "Delayed Spell Action Timing Caster",
                Vector3.zero,
                out _caster);
            _targetObject = CreateActor(
                "ServerDelayedSpellActionTimingTarget",
                TARGET_ID,
                "Delayed Spell Action Timing Target",
                new Vector3(1.5f, 0f, 0f),
                out _target);
            PrepareTarget(_caster, _target);
            _lastObservedHealth[TARGET_ID] = GetHealth(_target);
        }

        [TearDown]
        public void TearDown()
        {
            StopRuntime();
        }

        [UnityTest]
        public IEnumerator AuthoredProjectileImpactFiresOnItsDueTickExactlyOnce()
        {
            _impactTargetIDs.Add(TARGET_ID);
            SpellOverride spell = CreateProjectileHealSpell("delayed-projectile-timing");
            long castID = StartCast(_caster, spell);

            Assert.That(castID, Is.GreaterThan(0));
            AdvanceOneTick();
            ObserveHealthChange(_target);
            Assert.That(_clock.LastExecutedTickID, Is.EqualTo(1));
            Assert.That(_spellCastManager.IsCasting(_caster.ID), Is.False);
            Assert.That(_impactObservations, Is.Empty);
            Assert.That(GetHealth(_target), Is.EqualTo(50f));

            AdvanceOneTick();
            ObserveHealthChange(_target);
            Assert.That(_impactObservations, Is.Empty);
            Assert.That(GetHealth(_target), Is.EqualTo(50f));

            AdvanceOneTick();
            ObserveHealthChange(_target);
            Assert.That(_impactObservations, Is.Empty);
            Assert.That(GetHealth(_target), Is.EqualTo(50f));

            AdvanceOneTick();
            ObserveHealthChange(_target);
            Assert.That(_impactObservations, Has.Count.EqualTo(1));
            Assert.That(_impactObservations[0].ActorID, Is.EqualTo(TARGET_ID));
            Assert.That(_impactObservations[0].TickID, Is.EqualTo(4));
            Assert.That(_impactObservations[0].Health, Is.EqualTo(51f));

            AdvanceOneTick();
            ObserveHealthChange(_target);
            Assert.That(_impactObservations, Has.Count.EqualTo(1));
            Assert.That(GetHealth(_target), Is.EqualTo(51f));

            yield return null;
        }

        [UnityTest]
        public IEnumerator CompletedImpactSurvivesCastCompletionButRetiredCasterCancelsItsOwnImpact()
        {
            _retiredCasterObject = CreateActor(
                "ServerDelayedSpellActionTimingRetiredCaster",
                RETIRED_CASTER_ID,
                "Delayed Spell Action Retired Caster",
                Vector3.zero,
                out _retiredCaster);
            _retiredTargetObject = CreateActor(
                "ServerDelayedSpellActionTimingRetiredTarget",
                RETIRED_TARGET_ID,
                "Delayed Spell Action Retired Target",
                new Vector3(1.5f, 0f, 0f),
                out _retiredTarget);
            PrepareTarget(_retiredCaster, _retiredTarget);
            _lastObservedHealth[RETIRED_TARGET_ID] = GetHealth(_retiredTarget);
            _impactTargetIDs.Add(TARGET_ID);
            _impactTargetIDs.Add(RETIRED_TARGET_ID);

            SpellOverride spell = CreateProjectileHealSpell("delayed-projectile-lifetime");
            long completedCastID = StartCast(_caster, spell);
            long retiredCastID = StartCast(_retiredCaster, spell);
            Entity retiredEntity = GetActorEntity(_retiredCaster);

            Assert.That(retiredCastID, Is.Not.EqualTo(completedCastID));
            AdvanceOneTick();
            Assert.That(_spellCastManager.IsCasting(_caster.ID), Is.False);
            Assert.That(_spellCastManager.IsCasting(_retiredCaster.ID), Is.False);
            Assert.That(_impactObservations, Is.Empty);
            ObserveHealthChange(_target);
            ObserveHealthChange(_retiredTarget);

            ActorEndpoint.onServerActorDespawned?.Invoke(_retiredCaster);
            Assert.That(ServerActorManager.Instance.TryGetActor(RETIRED_CASTER_ID, out _), Is.False);
            Assert.That(ServerECSManager.DefaultWorld.EntityManager.Exists(retiredEntity), Is.True);

            AdvanceOneTick();
            ObserveHealthChange(_target);
            ObserveHealthChange(_retiredTarget);
            AdvanceOneTick();
            ObserveHealthChange(_target);
            ObserveHealthChange(_retiredTarget);
            Assert.That(_impactObservations, Is.Empty);

            AdvanceOneTick();
            ObserveHealthChange(_target);
            ObserveHealthChange(_retiredTarget);
            Assert.That(_impactObservations, Has.Count.EqualTo(1));
            Assert.That(_impactObservations[0].ActorID, Is.EqualTo(TARGET_ID));
            Assert.That(_impactObservations[0].TickID, Is.EqualTo(4));
            Assert.That(GetHealth(_target), Is.EqualTo(51f));
            Assert.That(GetHealth(_retiredTarget), Is.EqualTo(50f));

            AdvanceOneTick();
            ObserveHealthChange(_target);
            ObserveHealthChange(_retiredTarget);
            Assert.That(_impactObservations, Has.Count.EqualTo(1));
            Assert.That(GetHealth(_target), Is.EqualTo(51f));
            Assert.That(GetHealth(_retiredTarget), Is.EqualTo(50f));

            yield return null;
        }

        [UnityTest]
        public IEnumerator CallbackOriginatedActionWaitsForTheNextSchedulerBoundary()
        {
            _impactTargetIDs.Add(TARGET_ID);
            SpellOverride spell = CreateProjectileHealSpell("delayed-projectile-reentrant");
            StartCast(_caster, spell);

            AdvanceOneTick();
            ObserveHealthChange(_target);
            Assert.That(_impactObservations, Is.Empty);
            Assert.That(_callbackObservations, Is.Empty);

            // Register a second due callback at the projectile's boundary. The first callback models
            // impact-side follow-up work without relying on an observer RPC to execute on the server.
            ActorEndpoint.onServerSpellCastDelayedAction?.Invoke(
                0.15f,
                ()
                =>
                {
                    _callbackObservations.Add(
                        new CallbackObservation("first", ServerSimulationClock.CurrentTickID));
                    ActorEndpoint.onServerSpellCastDelayedAction?.Invoke(
                        0f,
                        () => _callbackObservations.Add(
                            new CallbackObservation("second", ServerSimulationClock.CurrentTickID)));
                });

            AdvanceOneTick();
            ObserveHealthChange(_target);
            AdvanceOneTick();
            ObserveHealthChange(_target);
            Assert.That(_impactObservations, Is.Empty);
            Assert.That(_callbackObservations, Is.Empty);

            AdvanceOneTick();
            ObserveHealthChange(_target);
            Assert.That(_impactObservations, Has.Count.EqualTo(1));
            Assert.That(_callbackObservations, Has.Count.EqualTo(1));
            Assert.That(_callbackObservations[0].Name, Is.EqualTo("first"));
            Assert.That(_callbackObservations[0].TickID, Is.EqualTo(4));

            AdvanceOneTick();
            ObserveHealthChange(_target);
            Assert.That(_callbackObservations, Has.Count.EqualTo(2));
            Assert.That(_callbackObservations[1].Name, Is.EqualTo("second"));
            Assert.That(_callbackObservations[1].TickID, Is.EqualTo(5));

            AdvanceOneTick();
            ObserveHealthChange(_target);
            Assert.That(_callbackObservations, Has.Count.EqualTo(2));
            yield return null;
        }

        private void ObserveHealthChange(Actor actor)
        {
            if (actor == null || !_impactTargetIDs.Contains(actor.ID))
                return;

            float health = GetHealth(actor);
            if (_lastObservedHealth.TryGetValue(actor.ID, out float previousHealth)
                && health == previousHealth)
            {
                return;
            }

            _lastObservedHealth[actor.ID] = health;
            _impactObservations.Add(new ImpactObservation(
                actor.ID,
                ServerSimulationClock.CurrentTickID,
                health));
        }

        private SpellOverride CreateProjectileHealSpell(string definitionID)
        {
            var spellDefinition = ScriptableObject.CreateInstance<SpellDefinition>();
            SetPrivateField(spellDefinition, "_definitionID", definitionID);
            SetPrivateField(spellDefinition, "_displayName", definitionID);
            SetPrivateField(spellDefinition, "_spellName", definitionID);
            SetPrivateField(spellDefinition, "_baseCastTime", 0.05f);
            SetPrivateField(spellDefinition, "_baseCooldown", 0f);
            SetPrivateField(spellDefinition, "_baseRange", 10f);
            SetPrivateField(spellDefinition, "_baseTotalTicks", 0f);
            SetPrivateField(spellDefinition, "_isChanneled", false);
            SetPrivateField(spellDefinition, "_triggersGCD", false);
            SetPrivateField(
                spellDefinition,
                "_baseComponents",
                new List<SpellComponent>
                {
                    new()
                    {
                        CastPhase = SpellCastPhase.OnCastEnd,
                        TargetDefinition = new SpellTarget_Single(),
                        EffectDefinition = new SpellEffect_Heal
                        {
                            BaseHealing = 1f,
                            UsesProjectile = true,
                            ProjectileAddressablePath = string.Empty,
                            ProjectileSpeed = 10f
                        }
                    }
                });
            _spellDefinitions.Add(spellDefinition);
            return new SpellOverride(spellDefinition);
        }

        private GameObject CreateActor(
            string objectName,
            Guid actorID,
            string actorName,
            Vector3 position,
            out Actor actor)
        {
            var actorObject = (GameObject)PurrNet.UnityProxy.Instantiate(_actorPrefab);
            Assert.That(actorObject, Is.Not.Null);
            actorObject.name = objectName;
            actorObject.transform.position = position;
            actor = actorObject.GetComponent<Actor>();
            Assert.That(actor, Is.Not.Null);
            actor.Initialize(actorID, actorName);
            actor.SetAuthoritativeSpatialContext(
                ActorSpatialContext.CreatePublic(TEST_WORLD_ID, "test-map"));
            actorObject.SetActive(true);
            Assert.That(actor.isSpawned, Is.True);
            Assert.That(REGISTER_SPAWNED_ACTOR_METHOD, Is.Not.Null);
            Assert.That(ServerActorManager.Instance, Is.Not.Null);
            REGISTER_SPAWNED_ACTOR_METHOD.Invoke(
                ServerActorManager.Instance,
                new object[]
                {
                    actor,
                    new WorldPosition(position.x, position.y, position.z)
                });
            ActorEndpoint.onServerActorSpawned?.Invoke(actor);
            return actorObject;
        }

        private static void PrepareTarget(Actor caster, Actor target)
        {
            SetPrivateField(caster, "_target", target);
            target.GetComponent<ActorStatContainer>().SetStat(
                "health",
                new StatInstance(100f, 0f, 0f, 100f, 50f));
        }

        private long StartCast(Actor caster, SpellOverride spell)
        {
            ActorEndpoint.onServerSpellCastStarted?.Invoke(caster, spell);
            Assert.That(_spellCastManager.IsCasting(caster.ID), Is.True);
            return GetCastID(caster);
        }

        private long GetCastID(Actor actor)
        {
            Entity entity = GetActorEntity(actor);
            SpellCastComponent spellCast = ServerECSManager.DefaultWorld.EntityManager
                .GetComponentData<SpellCastComponent>(entity);
            return spellCast.CastID;
        }

        private Entity GetActorEntity(Actor actor)
        {
            Assert.That(TRY_GET_ACTOR_ENTITY_METHOD, Is.Not.Null);
            object[] arguments = { actor.ID, Entity.Null };
            bool found = (bool)TRY_GET_ACTOR_ENTITY_METHOD.Invoke(ServerActorManager.Instance, arguments);
            Assert.That(found, Is.True);
            return (Entity)arguments[1];
        }

        private static float GetHealth(Actor actor)
        {
            return actor.GetComponent<ActorStatContainer>().GetStat("health").CurrentValue;
        }

        private void AdvanceOneTick()
        {
            long expectedTickID = _clock.LastExecutedTickID + 1;
            SimulateFrame(0.05d);
            Assert.That(_clock.LastExecutedTickID, Is.EqualTo(expectedTickID));
        }

        private void SimulateFrame(double elapsedSeconds)
        {
            Assert.That(LAST_TIMESTAMP_FIELD, Is.Not.Null);
            Assert.That(UPDATE_CLOCK_METHOD, Is.Not.Null);
            long elapsedTimestampTicks = checked((long)Math.Round(elapsedSeconds * Stopwatch.Frequency));
            LAST_TIMESTAMP_FIELD.SetValue(
                _clock,
                checked(Stopwatch.GetTimestamp() - elapsedTimestampTicks));

            try
            {
                UPDATE_CLOCK_METHOD.Invoke(_clock, null);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            Type type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                type = type.BaseType;
            }

            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private void StopRuntime()
        {
            if (_retiredTargetObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_retiredTargetObject);
                _retiredTargetObject = null;
            }

            if (_retiredCasterObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_retiredCasterObject);
                _retiredCasterObject = null;
            }

            if (_targetObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_targetObject);
                _targetObject = null;
            }

            if (_casterObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_casterObject);
                _casterObject = null;
            }

            if (_spellCastManagerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_spellCastManagerObject);
                _spellCastManagerObject = null;
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

            if (_actorPrefab != null)
            {
                UnityEngine.Object.DestroyImmediate(_actorPrefab);
                _actorPrefab = null;
            }

            if (_networkManagerObject != null)
            {
                var networkManager = _networkManagerObject.GetComponent<PurrNet.NetworkManager>();
                if (networkManager != null && networkManager.isServer)
                    networkManager.StopServer();

                UnityEngine.Object.DestroyImmediate(_networkManagerObject);
                _networkManagerObject = null;
            }

            ServerInitialization.Stop();

            if (_clockHostObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_clockHostObject);
                _clockHostObject = null;
            }

            foreach (SpellDefinition spellDefinition in _spellDefinitions)
            {
                if (spellDefinition != null)
                    UnityEngine.Object.DestroyImmediate(spellDefinition);
            }
            _spellDefinitions.Clear();

            if (_configuration != null)
            {
                _configuration.ServerSimulationTickDuration = _originalTickDuration;
                _configuration.ServerSimulationMaxCatchUpTicks = _originalMaxCatchUpTicks;
                _configuration = null;
            }

            _caster = null;
            _target = null;
            _retiredCaster = null;
            _retiredTarget = null;
            _clock = null;
            _spellCastManager = null;
            _impactTargetIDs.Clear();
            _impactObservations.Clear();
            _callbackObservations.Clear();
            _lastObservedHealth.Clear();
        }

        private readonly struct ImpactObservation
        {
            public ImpactObservation(Guid actorID, long tickID, float health)
            {
                ActorID = actorID;
                TickID = tickID;
                Health = health;
            }

            public Guid ActorID { get; }
            public long TickID { get; }
            public float Health { get; }
        }

        private readonly struct CallbackObservation
        {
            public CallbackObservation(string name, long tickID)
            {
                Name = name;
                TickID = tickID;
            }

            public string Name { get; }
            public long TickID { get; }
        }

        private sealed class ClockTestHost : MonoBehaviour
        {
        }

        private sealed class Test_PrefabProvider : PurrNet.IPrefabProvider
        {
            private readonly PurrNet.PrefabData _prefabData;

            public Test_PrefabProvider(GameObject prefab)
            {
                _prefabData = new PurrNet.PrefabData
                {
                    prefabId = 0,
                    prefab = prefab,
                    pooled = false,
                    warmupCount = 0
                };
            }

            public IEnumerable<PurrNet.PrefabData> allPrefabs
            {
                get
                {
                    yield return _prefabData;
                }
            }

            public bool TryGetPrefabData(int prefabId, out PurrNet.PrefabData prefabData)
            {
                if (prefabId == _prefabData.prefabId)
                {
                    prefabData = _prefabData;
                    return true;
                }

                prefabData = default;
                return false;
            }

            public bool TryGetPrefabData(GameObject prefab, out PurrNet.PrefabData prefabData)
            {
                if (prefab == _prefabData.prefab)
                {
                    prefabData = _prefabData;
                    return true;
                }

                prefabData = default;
                return false;
            }

            public void Refresh()
            {
            }
        }
    }
}
