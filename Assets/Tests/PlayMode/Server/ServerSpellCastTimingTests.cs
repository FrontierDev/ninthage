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
    /// <summary>Verifies authoritative tick ordering for server spell casts.</summary>
    public sealed class ServerSpellCastTimingTests
    {
        private static readonly Guid TEST_WORLD_ID =
            Guid.Parse("00000000-0000-0000-0000-000000000904");
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
        private static readonly FieldInfo CURRENT_SIMULATION_EVENT_FIELD =
            typeof(ServerECSManager).GetField("_currentSimulationEvent", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo HAS_CURRENT_SIMULATION_EVENT_FIELD =
            typeof(ServerECSManager).GetField("_hasCurrentSimulationEvent", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly PropertyInfo CURRENT_EVENT_CAST_ID_PROPERTY =
            CURRENT_SIMULATION_EVENT_FIELD?.FieldType.GetProperty(
                "CastID",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private readonly List<CastEventObservation> _events = new();
        private GameObject _actorManagerObject;
        private GameObject _cooldownManagerObject;
        private GameObject _spellCastManagerObject;
        private GameObject _actorObject;
        private SpellDefinition _spellDefinition;
        private Actor _actor;
        private ServerSpellCastManager _spellCastManager;
        private Action<Guid, string> _tickHandler;
        private Action<Guid, string> _completionHandler;
        private Action<Guid, string> _interruptionHandler;
        private long _executingTickID;

        [SetUp]
        public void SetUp()
        {
            StopRuntime();

            if (!GameConfigurationManager.IsInitialized)
                GameConfigurationManager.Initialize();

            Assert.That(GameConfigurationManager.Config, Is.Not.Null);
            ServerECSManager.ServerInitializeECSWorlds();

            _actorManagerObject = new GameObject("ServerSpellCastTimingActorManager");
            _actorManagerObject.AddComponent<ServerActorManager>();

            _cooldownManagerObject = new GameObject("ServerSpellCastTimingCooldownManager");
            _cooldownManagerObject.AddComponent<ServerCooldownManager>();

            _spellCastManagerObject = new GameObject("ServerSpellCastTimingSpellCastManager");
            _spellCastManager = _spellCastManagerObject.AddComponent<ServerSpellCastManager>();

            _actorObject = new GameObject("ServerSpellCastTimingActor");
            _actor = _actorObject.AddComponent<Actor>();
            _actor.Initialize(Guid.Parse("00000000-0000-0000-0000-000000000902"), "Spell Cast Timing Test Actor");
            _actor.SetAuthoritativeSpatialContext(
                ActorSpatialContext.CreatePublic(TEST_WORLD_ID, "test-map"));
            RegisterSpawnedActor(_actor, new WorldPosition(0d, 0d, 0d));
            ActorEndpoint.onServerActorSpawned?.Invoke(_actor);

            CastState initialState = GetCastState(out var state);
            Assert.That(initialState.CastID, Is.EqualTo(0));
            Assert.That(state.Enabled, Is.False);
        }

        [TearDown]
        public void TearDown()
        {
            StopRuntime();
        }

        [UnityTest]
        public IEnumerator ChannelTicksPrecedeCompletionAtTheAuthoritativeBoundary()
        {
            SubscribeToCastEvents();
            SpellOverride spell = CreateSpell("timing-channel", castTime: 0.2f, totalTicks: 2f);
            long castID = StartCast(spell);

            _executingTickID = 1;
            RunSimulationTick(1);

            Assert.That(_events, Has.Count.EqualTo(1));
            AssertEvent(0, CastEventKind.Tick, tickID: 1, castID);
            CastState afterFirstTick = GetCastState(out var firstTickState);
            Assert.That(firstTickState.Enabled, Is.True);
            Assert.That(afterFirstTick.CastID, Is.EqualTo(castID));
            Assert.That(afterFirstTick.RemainingCastTime, Is.EqualTo(0.1f).Within(0.000001f));

            _executingTickID = 2;
            RunSimulationTick(2);

            Assert.That(_events, Has.Count.EqualTo(3));
            AssertEvent(1, CastEventKind.Tick, tickID: 2, castID);
            AssertEvent(2, CastEventKind.Completed, tickID: 2, castID);

            CastState completedState = GetCastState(out var state);
            Assert.That(state.Enabled, Is.False);
            Assert.That(completedState.CastID, Is.EqualTo(castID));
            Assert.That(completedState.RemainingCastTime, Is.EqualTo(0f));
            Assert.That(_spellCastManager.GetRemainingCastTime(_actor.ID), Is.EqualTo(0f));
            Assert.That(_spellCastManager.IsCasting(_actor.ID), Is.False);

            _executingTickID = 3;
            RunSimulationTick(3);
            Assert.That(_events, Has.Count.EqualTo(3));
            yield return null;
        }

        [UnityTest]
        public IEnumerator InterruptionAfterAnEligibleTickSuppressesStaleCompletion()
        {
            SubscribeToCastEvents();
            int interruptionCount = 0;
            _interruptionHandler = (actorID, spellID) => interruptionCount++;
            ActorEndpoint.onServerSpellCastInterrupted += _interruptionHandler;

            SpellOverride spell = CreateSpell("timing-interrupted", castTime: 0.2f, totalTicks: 2f);
            long castID = StartCast(spell);

            _executingTickID = 1;
            RunSimulationTick(1);

            Assert.That(_events, Has.Count.EqualTo(1));
            AssertEvent(0, CastEventKind.Tick, tickID: 1, castID);
            Assert.That(GetCastState(out var activeState).Enabled, Is.True);
            Assert.That(activeState.CastID, Is.EqualTo(castID));

            ActorEndpoint.onServerSpellCastInterrupted?.Invoke(_actor.ID, spell.BaseDefinition.DefinitionID);
            Assert.That(interruptionCount, Is.EqualTo(1));

            _executingTickID = 2;
            RunSimulationTick(2);
            _executingTickID = 3;
            RunSimulationTick(3);

            Assert.That(_events, Has.Count.EqualTo(1));
            Assert.That(_spellCastManager.IsCasting(_actor.ID), Is.False);
            Assert.That(_spellCastManager.GetRemainingCastTime(_actor.ID), Is.EqualTo(0f));
            CastState interruptedState = GetCastState(out var state);
            Assert.That(state.Enabled, Is.False);
            Assert.That(interruptedState.CastID, Is.EqualTo(castID));
            Assert.That(interruptedState.RemainingCastTime, Is.GreaterThanOrEqualTo(0f));
            yield return null;
        }

        private void SubscribeToCastEvents()
        {
            _tickHandler = (actorID, spellID) => RecordCurrentEvent(CastEventKind.Tick);
            _completionHandler = (actorID, spellID) => RecordCurrentEvent(CastEventKind.Completed);
            ActorEndpoint.onServerSpellCastTick += _tickHandler;
            ActorEndpoint.onServerSpellCastCompleted += _completionHandler;
        }

        private void RecordCurrentEvent(CastEventKind kind)
        {
            _events.Add(new CastEventObservation(kind, _executingTickID, GetCurrentSimulationEventCastID()));
        }

        private void AssertEvent(int index, CastEventKind kind, long tickID, long castID)
        {
            Assert.That(_events[index].Kind, Is.EqualTo(kind));
            Assert.That(_events[index].TickID, Is.EqualTo(tickID));
            Assert.That(_events[index].CastID, Is.EqualTo(castID));
        }

        private long StartCast(SpellOverride spell)
        {
            ActorEndpoint.onServerSpellCastStarted?.Invoke(_actor, spell);
            CastState state = GetCastState(out var castState);
            Assert.That(castState.Enabled, Is.True);
            return state.CastID;
        }

        private SpellOverride CreateSpell(string definitionID, float castTime, float totalTicks)
        {
            _spellDefinition = ScriptableObject.CreateInstance<SpellDefinition>();
            SetPrivateField(_spellDefinition, "_definitionID", definitionID);
            SetPrivateField(_spellDefinition, "_displayName", definitionID);
            SetPrivateField(_spellDefinition, "_spellName", definitionID);
            SetPrivateField(_spellDefinition, "_baseCastTime", castTime);
            SetPrivateField(_spellDefinition, "_baseCooldown", 0f);
            SetPrivateField(_spellDefinition, "_baseTotalTicks", totalTicks);
            SetPrivateField(_spellDefinition, "_isChanneled", true);
            SetPrivateField(_spellDefinition, "_triggersGCD", false);

            return new SpellOverride(_spellDefinition);
        }

        private CastState GetCastState(out CastState state)
        {
            Assert.That(TRY_GET_ACTOR_ENTITY_METHOD, Is.Not.Null);
            object[] arguments = { _actor.ID, Entity.Null };
            bool found = (bool)TRY_GET_ACTOR_ENTITY_METHOD.Invoke(
                ServerActorManager.Instance,
                arguments);
            Assert.That(found, Is.True);

            Entity entity = (Entity)arguments[1];
            EntityManager entityManager = ServerECSManager.DefaultWorld.EntityManager;
            var spellCast = entityManager.GetComponentData<SpellCastComponent>(entity);
            state = new CastState(
                spellCast.CastID,
                spellCast.RemainingCastTime,
                entityManager.IsComponentEnabled<SpellCastComponent>(entity));
            return state;
        }

        private static long GetCurrentSimulationEventCastID()
        {
            if (CURRENT_SIMULATION_EVENT_FIELD == null
                || HAS_CURRENT_SIMULATION_EVENT_FIELD == null
                || CURRENT_EVENT_CAST_ID_PROPERTY == null
                || !(bool)HAS_CURRENT_SIMULATION_EVENT_FIELD.GetValue(null))
            {
                return 0;
            }

            object simulationEvent = CURRENT_SIMULATION_EVENT_FIELD.GetValue(null);
            return Convert.ToInt64(CURRENT_EVENT_CAST_ID_PROPERTY.GetValue(simulationEvent));
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

        private static void RegisterSpawnedActor(Actor actor, WorldPosition position)
        {
            Assert.That(REGISTER_SPAWNED_ACTOR_METHOD, Is.Not.Null);
            Assert.That(ServerActorManager.Instance, Is.Not.Null);
            REGISTER_SPAWNED_ACTOR_METHOD.Invoke(
                ServerActorManager.Instance,
                new object[] { actor, position });
        }

        private void StopRuntime()
        {
            if (_tickHandler != null)
            {
                ActorEndpoint.onServerSpellCastTick -= _tickHandler;
                _tickHandler = null;
            }

            if (_completionHandler != null)
            {
                ActorEndpoint.onServerSpellCastCompleted -= _completionHandler;
                _completionHandler = null;
            }

            if (_interruptionHandler != null)
            {
                ActorEndpoint.onServerSpellCastInterrupted -= _interruptionHandler;
                _interruptionHandler = null;
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

            if (_spellDefinition != null)
            {
                UnityEngine.Object.DestroyImmediate(_spellDefinition);
                _spellDefinition = null;
            }

            _actor = null;
            _spellCastManager = null;
            _events.Clear();
            _executingTickID = 0;
        }

        private enum CastEventKind : byte
        {
            Tick,
            Completed
        }

        private readonly struct CastEventObservation
        {
            public CastEventObservation(CastEventKind kind, long tickID, long castID)
            {
                Kind = kind;
                TickID = tickID;
                CastID = castID;
            }

            public CastEventKind Kind { get; }
            public long TickID { get; }
            public long CastID { get; }
        }

        private readonly struct CastState
        {
            public CastState(long castID, float remainingCastTime, bool enabled)
            {
                CastID = castID;
                RemainingCastTime = remainingCastTime;
                Enabled = enabled;
            }

            public long CastID { get; }
            public float RemainingCastTime { get; }
            public bool Enabled { get; }
        }
    }
}
