using Game.Server.Core;
using Game.Server.Systems;
using Game.Shared;
using Game.Shared.Data;
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
    /// <summary>Verifies authoritative cooldown expiry, deferred visibility and actor retirement.</summary>
    public sealed class ServerCooldownTimingTests
    {
        private static readonly Guid ACTOR_ID =
            Guid.Parse("00000000-0000-0000-0000-000000000A01");
        private static readonly Guid CONTROL_ACTOR_ID =
            Guid.Parse("00000000-0000-0000-0000-000000000A02");
        private static readonly Guid TEST_WORLD_ID =
            Guid.Parse("00000000-0000-0000-0000-000000000A03");
        private static readonly MethodInfo UPDATE_SIMULATION_METHOD =
            typeof(ServerECSManager).GetMethod("UpdateSimulation", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo TRY_GET_ACTOR_ENTITY_METHOD =
            typeof(ServerActorManager).GetMethod("TryGetActorEntity", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo REGISTER_SPAWNED_ACTOR_METHOD =
            typeof(ServerActorManager).GetMethod("RegisterSpawnedActor", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly ConstructorInfo SIMULATION_TICK_CONSTRUCTOR =
            typeof(ServerSimulationTick).GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(long), typeof(double), typeof(float) },
                modifiers: null);

        private readonly List<SpellDefinition> _spellDefinitions = new();
        private GameObject _actorManagerObject;
        private GameObject _cooldownManagerObject;
        private GameObject _spellCastManagerObject;
        private GameObject _actorObject;
        private GameObject _controlActorObject;
        private Actor _actor;
        private Actor _controlActor;
        private ServerCooldownManager _cooldownManager;
        private ServerSpellCastManager _spellCastManager;
        private Action<Guid, string> _tickHandler;

        [SetUp]
        public void SetUp()
        {
            StopRuntime();

            if (!GameConfigurationManager.IsInitialized)
                GameConfigurationManager.Initialize();

            Assert.That(GameConfigurationManager.Config, Is.Not.Null);
            ServerECSManager.ServerInitializeECSWorlds();

            _actorManagerObject = new GameObject("ServerCooldownTimingActorManager");
            _actorManagerObject.AddComponent<ServerActorManager>();

            _cooldownManagerObject = new GameObject("ServerCooldownTimingCooldownManager");
            _cooldownManager = _cooldownManagerObject.AddComponent<ServerCooldownManager>();

            _spellCastManagerObject = new GameObject("ServerCooldownTimingSpellCastManager");
            _spellCastManager = _spellCastManagerObject.AddComponent<ServerSpellCastManager>();

            _actorObject = CreateActor(
                "ServerCooldownTimingActor",
                ACTOR_ID,
                "Cooldown Timing Test Actor",
                out _actor);
        }

        [TearDown]
        public void TearDown()
        {
            StopRuntime();
        }

        [UnityTest]
        public IEnumerator GCDAndIndividualCooldownsBlockUntilBothExpire()
        {
            SpellOverride gcdSpell = CreateSpell(
                "cooldown-gated",
                castTime: 0.5f,
                totalTicks: 0f,
                triggersGCD: true,
                isChanneled: false);
            SpellOverride nonGCDSpell = CreateSpell(
                "cooldown-independent",
                castTime: 0.5f,
                totalTicks: 0f,
                triggersGCD: false,
                isChanneled: false);

            ActorEndpoint.onServerStartCooldown?.Invoke(_actor.ID, ServerCooldownManager.GCD_ID, 0.5f);
            ActorEndpoint.onServerStartCooldown?.Invoke(_actor.ID, gcdSpell.BaseDefinition.DefinitionID, 0.25f);

            Assert.That(_cooldownManager.GetRemainingCooldown(_actor.ID, ServerCooldownManager.GCD_ID), Is.EqualTo(0.5f).Within(0.000001f));
            Assert.That(_cooldownManager.GetRemainingCooldown(_actor.ID, gcdSpell.BaseDefinition.DefinitionID), Is.EqualTo(0.25f).Within(0.000001f));
            Assert.That(GetCooldownBufferCount(GetActorEntity(_actor)), Is.EqualTo(2));

            Assert.That(TryStartCast(gcdSpell, out _), Is.False);
            Assert.That(TryStartCast(nonGCDSpell, out long nonGCDCastID), Is.True);
            InterruptCast(nonGCDSpell.BaseDefinition.DefinitionID);

            RunSimulationTick(1, 0.125f);
            AssertCooldown(_actor.ID, ServerCooldownManager.GCD_ID, 0.375f);
            AssertCooldown(_actor.ID, gcdSpell.BaseDefinition.DefinitionID, 0.125f);
            Assert.That(TryStartCast(gcdSpell, out _), Is.False);

            RunSimulationTick(2, 0.125f);
            AssertCooldown(_actor.ID, ServerCooldownManager.GCD_ID, 0.25f);
            Assert.That(_cooldownManager.GetRemainingCooldown(_actor.ID, gcdSpell.BaseDefinition.DefinitionID), Is.EqualTo(0f));
            Assert.That(GetCooldownBufferRemaining(GetActorEntity(_actor), gcdSpell.BaseDefinition.DefinitionID), Is.EqualTo(0f));
            Assert.That(TryStartCast(gcdSpell, out _), Is.False);

            RunSimulationTick(3, 0.125f);
            AssertCooldown(_actor.ID, ServerCooldownManager.GCD_ID, 0.125f);
            Assert.That(TryStartCast(gcdSpell, out _), Is.False);

            RunSimulationTick(4, 0.125f);
            AssertCooldown(_actor.ID, ServerCooldownManager.GCD_ID, 0f);
            Assert.That(GetCooldownBufferCount(GetActorEntity(_actor)), Is.EqualTo(0));
            Assert.That(TryStartCast(gcdSpell, out long acceptedCastID), Is.True);
            Assert.That(acceptedCastID, Is.GreaterThan(nonGCDCastID));
            InterruptCast(gcdSpell.BaseDefinition.DefinitionID);

            yield return null;
        }

        [UnityTest]
        public IEnumerator PendingCooldownRejectsDuplicateCastBeforeCommit()
        {
            SpellOverride triggerSpell = CreateSpell(
                "cooldown-boundary-trigger",
                castTime: 0.2f,
                totalTicks: 2f,
                triggersGCD: false,
                isChanneled: true);
            SpellOverride gatedSpell = CreateSpell(
                "cooldown-boundary-gated",
                castTime: 0.5f,
                totalTicks: 0f,
                triggersGCD: true,
                isChanneled: false);
            long triggerCastID = StartCast(triggerSpell);
            bool callbackRan = false;
            bool pendingCooldownVisible = false;
            _tickHandler = (actorID, spellID) =>
            {
                if (callbackRan)
                    return;

                callbackRan = true;
                ActorEndpoint.onServerStartCooldown?.Invoke(actorID, ServerCooldownManager.GCD_ID, 0.3f);
                pendingCooldownVisible = _cooldownManager.IsOnCooldown(actorID, ServerCooldownManager.GCD_ID);
                ActorEndpoint.onServerSpellCastStarted?.Invoke(_actor, gatedSpell);
                ActorEndpoint.onServerSpellCastStarted?.Invoke(_actor, gatedSpell);
            };
            ActorEndpoint.onServerSpellCastTick += _tickHandler;

            RunSimulationTick(1);

            Assert.That(callbackRan, Is.True);
            Assert.That(pendingCooldownVisible, Is.True);
            Assert.That(_cooldownManager.IsOnCooldown(_actor.ID, ServerCooldownManager.GCD_ID), Is.True);
            CastState beforeCommit = GetCastState();
            Assert.That(beforeCommit.Enabled, Is.True);
            Assert.That(beforeCommit.CastID, Is.EqualTo(triggerCastID));

            RunSimulationTick(2);

            Assert.That(GetCooldownBufferCount(GetActorEntity(_actor)), Is.EqualTo(1));
            Assert.That(_cooldownManager.GetRemainingCooldown(_actor.ID, ServerCooldownManager.GCD_ID), Is.EqualTo(0.2f).Within(0.000001f));
            Assert.That(GetCooldownBufferRemaining(GetActorEntity(_actor), ServerCooldownManager.GCD_ID), Is.EqualTo(0.2f).Within(0.000001f));
            CastState afterCommit = GetCastState();
            Assert.That(afterCommit.Enabled, Is.False);
            Assert.That(afterCommit.CastID, Is.EqualTo(triggerCastID));
            Assert.That(_spellCastManager.IsCasting(_actor.ID), Is.False);

            yield return null;
        }

        [UnityTest]
        public IEnumerator RetiredActorCooldownDoesNotTickWhileRegisteredControlAdvances()
        {
            _controlActorObject = CreateActor(
                "ServerCooldownTimingControlActor",
                CONTROL_ACTOR_ID,
                "Cooldown Timing Control Actor",
                out _controlActor);
            Entity retiredEntity = GetActorEntity(_actor);
            Entity controlEntity = GetActorEntity(_controlActor);
            const string cooldownID = "cooldown-retired";

            ActorEndpoint.onServerStartCooldown?.Invoke(_actor.ID, cooldownID, 0.3f);
            ActorEndpoint.onServerStartCooldown?.Invoke(_controlActor.ID, cooldownID, 0.3f);
            ActorEndpoint.onServerActorDespawned?.Invoke(_actor);

            Assert.That(ServerECSManager.DefaultWorld.EntityManager.Exists(retiredEntity), Is.True);
            Assert.That(_cooldownManager.GetRemainingCooldown(_actor.ID, cooldownID), Is.EqualTo(0f));
            Assert.That(GetCooldownBufferRemaining(retiredEntity, cooldownID), Is.EqualTo(0.3f).Within(0.000001f));
            Assert.That(GetCooldownBufferCount(retiredEntity), Is.EqualTo(1));

            RunSimulationTick(1);

            Assert.That(_cooldownManager.GetRemainingCooldown(_actor.ID, cooldownID), Is.EqualTo(0f));
            Assert.That(GetCooldownBufferRemaining(retiredEntity, cooldownID), Is.EqualTo(0.3f).Within(0.000001f));
            Assert.That(GetCooldownBufferCount(retiredEntity), Is.EqualTo(1));
            Assert.That(_cooldownManager.GetRemainingCooldown(_controlActor.ID, cooldownID), Is.EqualTo(0.2f).Within(0.000001f));
            Assert.That(GetCooldownBufferRemaining(controlEntity, cooldownID), Is.EqualTo(0.2f).Within(0.000001f));
            Assert.That(GetCooldownBufferCount(controlEntity), Is.EqualTo(1));

            yield return null;
        }

        private static GameObject CreateActor(string objectName, Guid actorID, string actorName, out Actor actor)
        {
            var actorObject = new GameObject(objectName);
            actor = actorObject.AddComponent<Actor>();
            actor.Initialize(actorID, actorName);
            actor.SetAuthoritativeSpatialContext(
                ActorSpatialContext.CreatePublic(TEST_WORLD_ID, "test-map"));
            RegisterSpawnedActor(
                actor,
                new WorldPosition(
                    actor.transform.position.x,
                    actor.transform.position.y,
                    actor.transform.position.z));
            ActorEndpoint.onServerActorSpawned?.Invoke(actor);
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

        private SpellOverride CreateSpell(
            string definitionID,
            float castTime,
            float totalTicks,
            bool triggersGCD,
            bool isChanneled)
        {
            var spellDefinition = ScriptableObject.CreateInstance<SpellDefinition>();
            SetPrivateField(spellDefinition, "_definitionID", definitionID);
            SetPrivateField(spellDefinition, "_displayName", definitionID);
            SetPrivateField(spellDefinition, "_spellName", definitionID);
            SetPrivateField(spellDefinition, "_baseCastTime", castTime);
            SetPrivateField(spellDefinition, "_baseCooldown", 0f);
            SetPrivateField(spellDefinition, "_baseTotalTicks", totalTicks);
            SetPrivateField(spellDefinition, "_isChanneled", isChanneled);
            SetPrivateField(spellDefinition, "_triggersGCD", triggersGCD);
            _spellDefinitions.Add(spellDefinition);
            return new SpellOverride(spellDefinition);
        }

        private bool TryStartCast(SpellOverride spell, out long castID)
        {
            CastState before = GetCastState();
            ActorEndpoint.onServerSpellCastStarted?.Invoke(_actor, spell);
            CastState after = GetCastState();
            castID = after.CastID;
            return after.Enabled && after.CastID != before.CastID;
        }

        private long StartCast(SpellOverride spell)
        {
            ActorEndpoint.onServerSpellCastStarted?.Invoke(_actor, spell);
            CastState state = GetCastState();
            Assert.That(state.Enabled, Is.True);
            return state.CastID;
        }

        private void InterruptCast(string spellID)
        {
            ActorEndpoint.onServerSpellCastInterrupted?.Invoke(_actor.ID, spellID);
            Assert.That(_spellCastManager.IsCasting(_actor.ID), Is.False);
        }

        private void AssertCooldown(Guid actorID, string cooldownID, float expectedRemaining)
        {
            Assert.That(
                _cooldownManager.GetRemainingCooldown(actorID, cooldownID),
                Is.EqualTo(expectedRemaining).Within(0.000001f));
            Entity entity = GetActorEntity(actorID == ACTOR_ID ? _actor : _controlActor);
            Assert.That(
                GetCooldownBufferRemaining(entity, cooldownID),
                Is.EqualTo(expectedRemaining).Within(0.000001f));
        }

        private CastState GetCastState()
        {
            Entity entity = GetActorEntity(_actor);
            EntityManager entityManager = ServerECSManager.DefaultWorld.EntityManager;
            var spellCast = entityManager.GetComponentData<SpellCastComponent>(entity);
            return new CastState(
                spellCast.CastID,
                entityManager.IsComponentEnabled<SpellCastComponent>(entity));
        }

        private Entity GetActorEntity(Actor actor)
        {
            Assert.That(TRY_GET_ACTOR_ENTITY_METHOD, Is.Not.Null);
            object[] arguments = { actor.ID, Entity.Null };
            bool found = (bool)TRY_GET_ACTOR_ENTITY_METHOD.Invoke(ServerActorManager.Instance, arguments);
            Assert.That(found, Is.True);
            return (Entity)arguments[1];
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

        private static int GetCooldownBufferCount(Entity entity)
        {
            return ServerECSManager.DefaultWorld.EntityManager.GetBuffer<CooldownElement>(entity, true).Length;
        }

        private static void RunSimulationTick(long tickID, float deltaTime = 0.1f)
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
            if (_tickHandler != null)
            {
                ActorEndpoint.onServerSpellCastTick -= _tickHandler;
                _tickHandler = null;
            }

            if (_controlActorObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_controlActorObject);
                _controlActorObject = null;
            }

            if (_actorObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_actorObject);
                _actorObject = null;
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

            ServerInitialization.Stop();

            foreach (SpellDefinition spellDefinition in _spellDefinitions)
            {
                if (spellDefinition != null)
                    UnityEngine.Object.DestroyImmediate(spellDefinition);
            }
            _spellDefinitions.Clear();
            _actor = null;
            _controlActor = null;
            _cooldownManager = null;
            _spellCastManager = null;
        }

        private readonly struct CastState
        {
            public CastState(long castID, bool enabled)
            {
                CastID = castID;
                Enabled = enabled;
            }

            public long CastID { get; }
            public bool Enabled { get; }
        }
    }
}
