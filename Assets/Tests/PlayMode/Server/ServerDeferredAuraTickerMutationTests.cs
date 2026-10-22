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
using System.Reflection;
using Unity.Entities;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Runtime.Tests.Server
{
    /// <summary>Verifies aura and ticker callbacks defer buffer mutations to the next tick boundary.</summary>
    public sealed class ServerDeferredAuraTickerMutationTests
    {
        private static readonly Guid TEST_WORLD_ID =
            Guid.Parse("00000000-0000-0000-0000-000000000A23");
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
        private static readonly MethodInfo REGISTER_SPAWNED_ACTOR_METHOD =
            typeof(ServerActorManager).GetMethod("RegisterSpawnedActor", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo CURRENT_SIMULATION_EVENT_FIELD =
            typeof(ServerECSManager).GetField("_currentSimulationEvent", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo HAS_CURRENT_SIMULATION_EVENT_FIELD =
            typeof(ServerECSManager).GetField("_hasCurrentSimulationEvent", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly PropertyInfo CURRENT_EVENT_TICK_ID_PROPERTY =
            CURRENT_SIMULATION_EVENT_FIELD?.FieldType.GetProperty(
                "TickID",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo AURA_LIBRARY_INSTANCE_FIELD =
            typeof(AuraDefinitionLibrary).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);

        private readonly List<AuraDefinition> _auraDefinitions = new();
        private readonly List<Test_TrackingAuraBehaviour> _auraBehaviours = new();
        private readonly List<SpellDefinition> _spellDefinitions = new();
        private GameObject _actorManagerObject;
        private GameObject _auraManagerObject;
        private GameObject _cooldownManagerObject;
        private GameObject _spellCastManagerObject;
        private GameObject _tickerManagerObject;
        private GameObject _networkManagerObject;
        private GameObject _actorPrefab;
        private GameObject _casterObject;
        private GameObject _targetObject;
        private Actor _caster;
        private Actor _target;
        private ServerAuraManager _auraManager;
        private ServerTickerManager _tickerManager;
        private AuraDefinitionLibrary _testAuraLibrary;
        private AuraDefinitionLibrary _originalAuraLibrary;
        private Action<Guid, string, int> _auraTickHandler;

        [SetUp]
        public void SetUp()
        {
            StopRuntime();

            if (!GameConfigurationManager.IsInitialized)
                GameConfigurationManager.Initialize();

            Assert.That(GameConfigurationManager.Config, Is.Not.Null);
            _originalAuraLibrary = (AuraDefinitionLibrary)AURA_LIBRARY_INSTANCE_FIELD.GetValue(null);
            _testAuraLibrary = ScriptableObject.CreateInstance<AuraDefinitionLibrary>();
            SetPrivateField(_testAuraLibrary, "_definitions", new List<AuraDefinition>());
            AURA_LIBRARY_INSTANCE_FIELD.SetValue(null, _testAuraLibrary);

            ServerECSManager.ServerInitializeECSWorlds();

            _actorManagerObject = new GameObject("ServerDeferredAuraTickerActorManager");
            _actorManagerObject.AddComponent<ServerActorManager>();

            _auraManagerObject = new GameObject("ServerDeferredAuraTickerAuraManager");
            _auraManager = _auraManagerObject.AddComponent<ServerAuraManager>();

            _cooldownManagerObject = new GameObject("ServerDeferredAuraTickerCooldownManager");
            _cooldownManagerObject.AddComponent<ServerCooldownManager>();

            _spellCastManagerObject = new GameObject("ServerDeferredAuraTickerSpellCastManager");
            _spellCastManagerObject.AddComponent<ServerSpellCastManager>();

            _tickerManagerObject = new GameObject("ServerDeferredAuraTickerTickerManager");
            _tickerManager = _tickerManagerObject.AddComponent<ServerTickerManager>();

            var networkManagerPrefab = Resources.Load<GameObject>("NetworkManager");
            Assert.That(networkManagerPrefab, Is.Not.Null);
            _networkManagerObject = UnityEngine.Object.Instantiate(networkManagerPrefab);
            _networkManagerObject.name = "ServerDeferredAuraTickerNetworkManager";

            var networkManager = _networkManagerObject.GetComponent<PurrNet.NetworkManager>();
            Assert.That(networkManager, Is.Not.Null);
            networkManager.startServerFlags = PurrNet.StartFlags.None;
            networkManager.startClientFlags = PurrNet.StartFlags.None;

            var localTransport = _networkManagerObject.AddComponent<PurrNet.Transports.LocalTransport>();
            networkManager.transport = localTransport;

            _actorPrefab = new GameObject("ServerDeferredAuraTickerActorPrefab");
            _actorPrefab.SetActive(false);
            _actorPrefab.AddComponent<Actor>();
            networkManager.SetPrefabProvider(new Test_PrefabProvider(_actorPrefab));
            networkManager.StartServer();

            _casterObject = CreateActor(
                "ServerDeferredAuraTickerCaster",
                Guid.Parse("00000000-0000-0000-0000-000000000902"),
                "Deferred Aura Caster");
            _targetObject = CreateActor(
                "ServerDeferredAuraTickerTarget",
                Guid.Parse("00000000-0000-0000-0000-000000000903"),
                "Deferred Aura Target");
        }

        [TearDown]
        public void TearDown()
        {
            StopRuntime();
        }

        [UnityTest]
        public IEnumerator TickerCallbacksDeferAddAndRemovalUntilTheNextTick()
        {
            var callbackSequence = new List<string>();
            int firstTickerID = -1;
            int removedTickerID = -1;
            int addedTickerID = -1;

            firstTickerID = _tickerManager.AddTicker(
                _target,
                tickInterval: GameConfigurationManager.Config.ServerSimulationTickDuration,
                duration: 1f,
                onTick: actor =>
                {
                    callbackSequence.Add($"first:{GetCurrentSimulationEventTickID()}");
                    addedTickerID = _tickerManager.AddTicker(
                        actor,
                        tickInterval: GameConfigurationManager.Config.ServerSimulationTickDuration,
                        duration: float.MaxValue,
                        onTick: addedActor => callbackSequence.Add($"added:{GetCurrentSimulationEventTickID()}"),
                        tag: "deferred-added");
                    _tickerManager.RemoveTicker(actor, firstTickerID);
                    _tickerManager.RemoveTicker(actor, removedTickerID);
                },
                tag: "deferred-first");
            removedTickerID = _tickerManager.AddTicker(
                _target,
                tickInterval: GameConfigurationManager.Config.ServerSimulationTickDuration,
                duration: 1f,
                onTick: actor => callbackSequence.Add($"removed:{GetCurrentSimulationEventTickID()}"),
                tag: "deferred-removed");

            Assert.That(firstTickerID, Is.GreaterThanOrEqualTo(0));
            Assert.That(removedTickerID, Is.GreaterThan(firstTickerID));

            RunSimulationTick(1);

            CollectionAssert.AreEqual(new[] { "first:1" }, callbackSequence);
            Assert.That(addedTickerID, Is.GreaterThan(removedTickerID));
            Assert.That(_tickerManager.HasTicker(_target.ID, "deferred-first"), Is.False);
            Assert.That(_tickerManager.HasTicker(_target.ID, "deferred-removed"), Is.False);
            Assert.That(_tickerManager.HasTicker(_target.ID, "deferred-added"), Is.True);
            CollectionAssert.AreEquivalent(
                new[] { firstTickerID, removedTickerID },
                GetTickerIDs(_target));

            RunSimulationTick(2);

            CollectionAssert.AreEqual(new[] { "first:1", "added:2" }, callbackSequence);
            CollectionAssert.AreEqual(new[] { addedTickerID }, GetTickerIDs(_target));
            Assert.That(_tickerManager.HasTicker(_target.ID, "deferred-added"), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AuraTickCallbacksDeferDispelAndApplyUntilTheNextTick()
        {
            Test_TrackingAuraBehaviour originalBehaviour;
            AuraDefinition originalAura = CreateAura(
                "deferred-aura-original",
                0.5f,
                GameConfigurationManager.Config.ServerSimulationTickDuration,
                out originalBehaviour);
            Test_TrackingAuraBehaviour replacementBehaviour;
            AuraDefinition replacementAura = CreateAura(
                "deferred-aura-replacement",
                0.5f,
                GameConfigurationManager.Config.ServerSimulationTickDuration,
                out replacementBehaviour);

            Assert.That(_auraManager.ApplyAura(originalAura, _caster, _target), Is.True);
            int originalInstanceID = GetAuraEntries(_target)[0].InstanceID;
            var callbackSequence = new List<string>();
            bool mutationRequested = false;

            _auraTickHandler = (targetID, definitionID, instanceID) =>
            {
                callbackSequence.Add($"{GetCurrentSimulationEventTickID()}:{definitionID}");
                if (mutationRequested || instanceID != originalInstanceID)
                    return;

                mutationRequested = true;
                Assert.That(_auraManager.DispelAura(_target, originalInstanceID), Is.True);
                Assert.That(_auraManager.ApplyAura(replacementAura, _caster, _target), Is.True);
            };
            ActorEndpoint.onServerAuraTick += _auraTickHandler;

            RunSimulationTick(1);

            CollectionAssert.AreEqual(new[] { "1:deferred-aura-original" }, callbackSequence);
            CollectionAssert.AreEqual(new[] { originalInstanceID }, GetAuraInstanceIDs(_target));
            Assert.That(_auraManager.HasAura(_target.ID, originalAura.DefinitionID), Is.True);
            Assert.That(_auraManager.HasAura(_target.ID, replacementAura.DefinitionID), Is.False);

            RunSimulationTick(2);

            CollectionAssert.AreEqual(
                new[] { "1:deferred-aura-original", "2:deferred-aura-replacement" },
                callbackSequence);
            Assert.That(_auraManager.HasAura(_target.ID, originalAura.DefinitionID), Is.False);
            Assert.That(_auraManager.HasAura(_target.ID, replacementAura.DefinitionID), Is.True);
            CollectionAssert.AreEqual(new[] { GetAuraEntries(_target)[0].InstanceID }, GetAuraInstanceIDs(_target));
            Assert.That(originalBehaviour.AppliedCount, Is.EqualTo(1));
            Assert.That(originalBehaviour.RemovedCount, Is.EqualTo(1));
            Assert.That(replacementBehaviour.AppliedCount, Is.EqualTo(1));
            Assert.That(replacementBehaviour.RemovedCount, Is.EqualTo(0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator AuraPeriodicTicksOnceAndExpiresOnItsDueBoundary()
        {
            float tickDuration = GameConfigurationManager.Config.ServerSimulationTickDuration;
            Test_TrackingAuraBehaviour auraBehaviour;
            AuraDefinition aura = CreateAura(
                "scheduled-aura-expiry",
                tickDuration * 3f,
                tickDuration,
                out auraBehaviour);

            Assert.That(_auraManager.ApplyAura(aura, _caster, _target), Is.True);
            int instanceID = GetAuraEntries(_target)[0].InstanceID;
            var callbackSequence = new List<string>();
            _auraTickHandler = (targetID, definitionID, callbackInstanceID) =>
            {
                callbackSequence.Add($"{GetCurrentSimulationEventTickID()}:{definitionID}:{callbackInstanceID}");
            };
            ActorEndpoint.onServerAuraTick += _auraTickHandler;

            RunSimulationTick(1);
            RunSimulationTick(2);
            RunSimulationTick(3);

            CollectionAssert.AreEqual(
                new[]
                {
                    $"1:{aura.DefinitionID}:{instanceID}",
                    $"2:{aura.DefinitionID}:{instanceID}",
                    $"3:{aura.DefinitionID}:{instanceID}",
                },
                callbackSequence);
            Assert.That(auraBehaviour.AppliedCount, Is.EqualTo(1));
            Assert.That(auraBehaviour.RemovedCount, Is.EqualTo(1));
            Assert.That(_auraManager.HasAura(_target.ID, aura.DefinitionID), Is.False);
            Assert.That(GetAuraEntries(_target), Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DispelledAuraDoesNotTickAgainBeforeItsNextDueBoundary()
        {
            float tickDuration = GameConfigurationManager.Config.ServerSimulationTickDuration;
            Test_TrackingAuraBehaviour auraBehaviour;
            AuraDefinition aura = CreateAura(
                "scheduled-aura-dispel",
                tickDuration * 4f,
                tickDuration * 2f,
                out auraBehaviour);

            Assert.That(_auraManager.ApplyAura(aura, _caster, _target), Is.True);
            int instanceID = GetAuraEntries(_target)[0].InstanceID;
            var callbackSequence = new List<string>();
            _auraTickHandler = (targetID, definitionID, callbackInstanceID) =>
            {
                callbackSequence.Add($"{GetCurrentSimulationEventTickID()}:{definitionID}:{callbackInstanceID}");
            };
            ActorEndpoint.onServerAuraTick += _auraTickHandler;

            RunSimulationTick(1);
            Assert.That(callbackSequence, Is.Empty);
            Assert.That(_auraManager.DispelAura(_target, instanceID), Is.True);

            RunSimulationTick(2);
            RunSimulationTick(3);

            Assert.That(callbackSequence, Is.Empty);
            Assert.That(auraBehaviour.AppliedCount, Is.EqualTo(1));
            Assert.That(auraBehaviour.RemovedCount, Is.EqualTo(1));
            Assert.That(_auraManager.HasAura(_target.ID, aura.DefinitionID), Is.False);
            Assert.That(GetAuraEntries(_target), Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TickerTicksOnceThenCancellationPreventsLaterCallbacks()
        {
            var callbackSequence = new List<string>();
            int tickerID = _tickerManager.AddTicker(
                _target,
                tickInterval: GameConfigurationManager.Config.ServerSimulationTickDuration,
                duration: GameConfigurationManager.Config.ServerSimulationTickDuration * 4f,
                onTick: actor => callbackSequence.Add($"{GetCurrentSimulationEventTickID()}:{actor.ID}"),
                tag: "scheduled-cancellable");

            Assert.That(tickerID, Is.GreaterThanOrEqualTo(0));

            RunSimulationTick(1);

            CollectionAssert.AreEqual(new[] { $"1:{_target.ID}" }, callbackSequence);
            _tickerManager.RemoveTicker(_target, tickerID);
            Assert.That(_tickerManager.HasTicker(_target.ID, "scheduled-cancellable"), Is.False);
            Assert.That(GetTickerIDs(_target), Is.Empty);

            RunSimulationTick(2);
            RunSimulationTick(3);

            CollectionAssert.AreEqual(new[] { $"1:{_target.ID}" }, callbackSequence);
            Assert.That(_tickerManager.HasTicker(_target.ID, "scheduled-cancellable"), Is.False);
            Assert.That(GetTickerIDs(_target), Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SpellCompletionRefreshesDueAuraAfterItsTick()
        {
            float tickDuration = GameConfigurationManager.Config.ServerSimulationTickDuration;
            Test_TrackingAuraBehaviour auraBehaviour;
            AuraDefinition aura = CreateAura(
                "same-tick-aura-refresh",
                tickDuration * 3f,
                tickDuration,
                out auraBehaviour);

            Assert.That(_auraManager.ApplyAura(aura, _caster, _target), Is.True);
            AuraElement initialEntry = GetAuraEntries(_target)[0];
            int instanceID = initialEntry.InstanceID;
            var callbackSequence = new List<string>();
            _auraTickHandler = (targetID, definitionID, callbackInstanceID) =>
            {
                callbackSequence.Add($"{GetCurrentSimulationEventTickID()}:{definitionID}:{callbackInstanceID}");
            };
            ActorEndpoint.onServerAuraTick += _auraTickHandler;

            SetPrivateField(_caster, "_target", _target);
            SpellOverride refreshSpell = CreateAuraRefreshSpell(aura);
            ActorEndpoint.onServerSpellCastStarted?.Invoke(_caster, refreshSpell);

            RunSimulationTick(1, tickDuration);

            CollectionAssert.AreEqual(
                new[] { $"1:{aura.DefinitionID}:{instanceID}" },
                callbackSequence);
            AuraElement beforeCommitEntry = GetAuraEntries(_target)[0];
            Assert.That(beforeCommitEntry.InstanceID, Is.EqualTo(instanceID));
            Assert.That(beforeCommitEntry.ExpiryTickID, Is.EqualTo(initialEntry.ExpiryTickID));
            Assert.That(auraBehaviour.AppliedCount, Is.EqualTo(1));

            RunSimulationTick(2, tickDuration);

            CollectionAssert.AreEqual(
                new[]
                {
                    $"1:{aura.DefinitionID}:{instanceID}",
                    $"2:{aura.DefinitionID}:{instanceID}",
                },
                callbackSequence);
            AuraElement refreshedEntry = GetAuraEntries(_target)[0];
            Assert.That(refreshedEntry.InstanceID, Is.EqualTo(instanceID));
            Assert.That(refreshedEntry.ExpiryTickID, Is.EqualTo(initialEntry.ExpiryTickID + 1));
            Assert.That(refreshedEntry.NextTickID, Is.EqualTo(3));
            Assert.That(auraBehaviour.AppliedCount, Is.EqualTo(2));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SameTickAuraExpiryWinsOverDeferredRefresh()
        {
            float tickDuration = GameConfigurationManager.Config.ServerSimulationTickDuration;
            Test_TrackingAuraBehaviour auraBehaviour;
            AuraDefinition aura = CreateAura(
                "same-tick-aura-expiry",
                tickDuration,
                tickDuration,
                out auraBehaviour);

            Assert.That(_auraManager.ApplyAura(aura, _caster, _target), Is.True);
            int instanceID = GetAuraEntries(_target)[0].InstanceID;
            var callbackSequence = new List<string>();
            _auraTickHandler = (targetID, definitionID, callbackInstanceID) =>
            {
                callbackSequence.Add($"{GetCurrentSimulationEventTickID()}:{definitionID}:{callbackInstanceID}");
            };
            ActorEndpoint.onServerAuraTick += _auraTickHandler;

            SetPrivateField(_caster, "_target", _target);
            SpellOverride refreshSpell = CreateAuraRefreshSpell(aura);
            ActorEndpoint.onServerSpellCastStarted?.Invoke(_caster, refreshSpell);

            RunSimulationTick(1, tickDuration);

            CollectionAssert.AreEqual(
                new[] { $"1:{aura.DefinitionID}:{instanceID}" },
                callbackSequence);
            Assert.That(auraBehaviour.RemovedCount, Is.EqualTo(1));
            Assert.That(_auraManager.HasAura(_target.ID, aura.DefinitionID), Is.False);
            Assert.That(GetAuraEntries(_target), Is.Empty);

            RunSimulationTick(2, tickDuration);

            CollectionAssert.AreEqual(
                new[] { $"1:{aura.DefinitionID}:{instanceID}" },
                callbackSequence);
            Assert.That(auraBehaviour.RemovedCount, Is.EqualTo(1));
            Assert.That(_auraManager.HasAura(_target.ID, aura.DefinitionID), Is.False);
            Assert.That(GetAuraEntries(_target), Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AuraOnSurvivingTargetRemainsWhenCasterDespawns()
        {
            Test_TrackingAuraBehaviour auraBehaviour;
            AuraDefinition aura = CreateAura(
                "deferred-aura-surviving-target",
                0.5f,
                GameConfigurationManager.Config.ServerSimulationTickDuration,
                out auraBehaviour);
            Assert.That(_auraManager.ApplyAura(aura, _caster, _target), Is.True);
            int instanceID = GetAuraEntries(_target)[0].InstanceID;

            UnityEngine.Object.DestroyImmediate(_casterObject);
            _casterObject = null;
            _caster = null;

            Assert.That(_auraManager.HasAura(_target.ID, aura.DefinitionID), Is.True);
            CollectionAssert.AreEqual(new[] { instanceID }, GetAuraInstanceIDs(_target));
            Assert.That(auraBehaviour.RemovedCount, Is.EqualTo(0));

            RunSimulationTick(1);

            Assert.That(_auraManager.HasAura(_target.ID, aura.DefinitionID), Is.True);
            CollectionAssert.AreEqual(new[] { instanceID }, GetAuraInstanceIDs(_target));
            Assert.That(auraBehaviour.RemovedCount, Is.EqualTo(0));
            yield return null;
        }

        private GameObject CreateActor(string objectName, Guid actorID, string actorName)
        {
            var actorObject = (GameObject)PurrNet.UnityProxy.Instantiate(_actorPrefab);
            Assert.That(actorObject, Is.Not.Null);
            actorObject.name = objectName;
            var actor = actorObject.GetComponent<Actor>();
            Assert.That(actor, Is.Not.Null);
            actor.Initialize(actorID, actorName);
            actor.SetAuthoritativeSpatialContext(
                ActorSpatialContext.CreatePublic(TEST_WORLD_ID, "test-map"));
            actorObject.SetActive(true);
            Assert.That(actor.isSpawned, Is.True);
            RegisterSpawnedActor(
                actor,
                new WorldPosition(
                    actor.transform.position.x,
                    actor.transform.position.y,
                    actor.transform.position.z));
            ActorEndpoint.onServerActorSpawned?.Invoke(actor);

            if (_caster == null)
                _caster = actor;
            else
                _target = actor;

            return actorObject;
        }

        private static void RegisterSpawnedActor(Actor actor, WorldPosition position)
        {
            Assert.That(REGISTER_SPAWNED_ACTOR_METHOD, Is.Not.Null);
            Assert.That(ServerActorManager.Instance, Is.Not.Null);
            REGISTER_SPAWNED_ACTOR_METHOD.Invoke(
                ServerActorManager.Instance,
                new object[] { actor, position });
        }

        private AuraDefinition CreateAura(
            string definitionID,
            float duration,
            float tickInterval,
            out Test_TrackingAuraBehaviour behaviour)
        {
            var definition = ScriptableObject.CreateInstance<AuraDefinition>();
            behaviour = ScriptableObject.CreateInstance<Test_TrackingAuraBehaviour>();
            SetPrivateField(definition, "_definitionID", definitionID);
            SetPrivateField(definition, "_displayName", definitionID);
            SetPrivateField(definition, "_description", definitionID);
            SetPrivateField(definition, "_baseDuration", duration);
            SetPrivateField(definition, "_baseTickInterval", tickInterval);
            SetPrivateField(definition, "_stackBehavior", AuraStackBehavior.Condition_Extend);
            SetPrivateField(definition, "_maxStacks", 1);
            SetPrivateField(definition, "_auraBehaviour", behaviour);

            _auraDefinitions.Add(definition);
            _auraBehaviours.Add(behaviour);
            var definitions = (List<AuraDefinition>)GetPrivateField(_testAuraLibrary, "_definitions");
            definitions.Add(definition);
            return definition;
        }

        private SpellOverride CreateAuraRefreshSpell(AuraDefinition aura)
        {
            var definition = ScriptableObject.CreateInstance<SpellDefinition>();
            SetPrivateField(definition, "_definitionID", "same-tick-aura-refresh-spell");
            SetPrivateField(definition, "_displayName", "same-tick-aura-refresh-spell");
            SetPrivateField(definition, "_spellName", "same-tick-aura-refresh-spell");
            SetPrivateField(definition, "_baseCastTime", GameConfigurationManager.Config.ServerSimulationTickDuration);
            SetPrivateField(definition, "_baseCooldown", 0f);
            SetPrivateField(definition, "_baseRange", 1f);
            SetPrivateField(definition, "_baseTotalTicks", 0f);
            SetPrivateField(definition, "_isChanneled", false);
            SetPrivateField(definition, "_triggersGCD", false);
            SetPrivateField(
                definition,
                "_baseComponents",
                new List<SpellComponent>
                {
                    new()
                    {
                        CastPhase = SpellCastPhase.OnCastEnd,
                        TargetDefinition = new SpellTarget_Single(),
                        EffectDefinition = new SpellEffect_ApplyAura
                        {
                            Aura = aura,
                            Stacks = 1,
                        },
                    },
                });
            _spellDefinitions.Add(definition);
            return new SpellOverride(definition);
        }

        private List<int> GetAuraInstanceIDs(Actor actor)
        {
            var instanceIDs = new List<int>();
            foreach (AuraElement entry in GetAuraEntries(actor))
                instanceIDs.Add(entry.InstanceID);
            return instanceIDs;
        }

        private List<AuraElement> GetAuraEntries(Actor actor)
        {
            Entity entity = GetActorEntity(actor);
            DynamicBuffer<AuraElement> buffer = ServerECSManager.DefaultWorld.EntityManager.GetBuffer<AuraElement>(entity);
            var entries = new List<AuraElement>(buffer.Length);
            for (int i = 0; i < buffer.Length; i++)
                entries.Add(buffer[i]);
            return entries;
        }

        private List<int> GetTickerIDs(Actor actor)
        {
            var tickerIDs = new List<int>();
            Entity entity = GetActorEntity(actor);
            DynamicBuffer<TickerElement> buffer = ServerECSManager.DefaultWorld.EntityManager.GetBuffer<TickerElement>(entity);
            for (int i = 0; i < buffer.Length; i++)
                tickerIDs.Add(buffer[i].TickerID);
            return tickerIDs;
        }

        private Entity GetActorEntity(Actor actor)
        {
            Assert.That(TRY_GET_ACTOR_ENTITY_METHOD, Is.Not.Null);
            object[] arguments = { actor.ID, Entity.Null };
            bool found = (bool)TRY_GET_ACTOR_ENTITY_METHOD.Invoke(ServerActorManager.Instance, arguments);
            Assert.That(found, Is.True);
            return (Entity)arguments[1];
        }

        private static long GetCurrentSimulationEventTickID()
        {
            if (CURRENT_SIMULATION_EVENT_FIELD == null
                || HAS_CURRENT_SIMULATION_EVENT_FIELD == null
                || CURRENT_EVENT_TICK_ID_PROPERTY == null
                || !(bool)HAS_CURRENT_SIMULATION_EVENT_FIELD.GetValue(null))
            {
                return 0;
            }

            object simulationEvent = CURRENT_SIMULATION_EVENT_FIELD.GetValue(null);
            return Convert.ToInt64(CURRENT_EVENT_TICK_ID_PROPERTY.GetValue(simulationEvent));
        }

        private static void RunSimulationTick(long tickID)
        {
            RunSimulationTick(tickID, GameConfigurationManager.Config.ServerSimulationTickDuration);
        }

        private static void RunSimulationTick(long tickID, float deltaTime)
        {
            Assert.That(UPDATE_SIMULATION_METHOD, Is.Not.Null);
            Assert.That(SIMULATION_TICK_CONSTRUCTOR, Is.Not.Null);

            object tick = SIMULATION_TICK_CONSTRUCTOR.Invoke(new object[]
            {
                tickID,
                tickID * (double)deltaTime,
                deltaTime
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

        private static object GetPrivateField(object target, string fieldName)
        {
            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                    return field.GetValue(target);

                type = type.BaseType;
            }

            Assert.Fail($"Expected field '{fieldName}' on {target.GetType().Name}.");
            return null;
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
            if (_auraTickHandler != null)
            {
                ActorEndpoint.onServerAuraTick -= _auraTickHandler;
                _auraTickHandler = null;
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

            if (_tickerManagerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_tickerManagerObject);
                _tickerManagerObject = null;
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

            if (_auraManagerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_auraManagerObject);
                _auraManagerObject = null;
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

            foreach (AuraDefinition definition in _auraDefinitions)
            {
                if (definition != null)
                    UnityEngine.Object.DestroyImmediate(definition);
            }

            foreach (Test_TrackingAuraBehaviour behaviour in _auraBehaviours)
            {
                if (behaviour != null)
                    UnityEngine.Object.DestroyImmediate(behaviour);
            }

            _auraDefinitions.Clear();
            _auraBehaviours.Clear();

            foreach (SpellDefinition definition in _spellDefinitions)
            {
                if (definition != null)
                    UnityEngine.Object.DestroyImmediate(definition);
            }
            _spellDefinitions.Clear();

            if (_testAuraLibrary != null)
            {
                AURA_LIBRARY_INSTANCE_FIELD.SetValue(null, _originalAuraLibrary);
                UnityEngine.Object.DestroyImmediate(_testAuraLibrary);
                _testAuraLibrary = null;
            }

            _originalAuraLibrary = null;
            _caster = null;
            _target = null;
            _auraManager = null;
            _tickerManager = null;
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

        public sealed class Test_TrackingAuraBehaviour : AuraBehaviour
        {
            public int AppliedCount { get; private set; }
            public int RemovedCount { get; private set; }

            public override string Description => "Test aura behaviour.";

            public override void OnApplied(Actor caster, Actor target, int stacks)
            {
                AppliedCount++;
            }

            public override void OnRemoved(Actor caster, Actor target)
            {
                RemovedCount++;
            }
        }
    }
}
