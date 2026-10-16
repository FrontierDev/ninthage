using Game.Server.Core;
using Game.Server.Systems;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
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
    /// <summary>Verifies deferred spell mutations preserve cast-generation identity.</summary>
    public sealed class ServerDeferredCastIdentityTests
    {
        private static readonly MethodInfo UPDATE_SIMULATION_METHOD =
            typeof(ServerECSManager).GetMethod("UpdateSimulation", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo TRY_GET_ACTOR_ENTITY_METHOD =
            typeof(ServerActorManager).GetMethod("TryGetActorEntity", BindingFlags.Instance | BindingFlags.NonPublic);
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

        private readonly List<long> _completedCastIDs = new();
        private GameObject _actorManagerObject;
        private GameObject _cooldownManagerObject;
        private GameObject _spellCastManagerObject;
        private GameObject _actorObject;
        private SpellDefinition _spellDefinition;
        private Actor _actor;
        private ServerSpellCastManager _spellCastManager;
        private Action<Guid, string> _tickHandler;
        private Action<Guid, string> _completionHandler;

        [SetUp]
        public void SetUp()
        {
            StopRuntime();

            if (!GameConfigurationManager.IsInitialized)
                GameConfigurationManager.Initialize();

            Assert.That(GameConfigurationManager.Config, Is.Not.Null);
            ServerECSManager.ServerInitializeECSWorlds();

            _actorManagerObject = new GameObject("ServerDeferredCastIdentityActorManager");
            _actorManagerObject.AddComponent<ServerActorManager>();

            _cooldownManagerObject = new GameObject("ServerDeferredCastIdentityCooldownManager");
            _cooldownManagerObject.AddComponent<ServerCooldownManager>();

            _spellCastManagerObject = new GameObject("ServerDeferredCastIdentitySpellCastManager");
            _spellCastManager = _spellCastManagerObject.AddComponent<ServerSpellCastManager>();

            _actorObject = new GameObject("ServerDeferredCastIdentityActor");
            _actor = _actorObject.AddComponent<Actor>();
            _actor.Initialize(Guid.Parse("00000000-0000-0000-0000-000000000901"), "Deferred Cast Test Actor");
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
        public IEnumerator RejectedPendingReplacementPreservesExistingCastTicksAndCompletion()
        {
            SpellOverride spell = CreateSpell("deferred-rejected", castTime: 0.4f, cooldown: 1f, totalTicks: 2f);
            long originalCastID = StartCast(spell);
            var tickCastIDs = new List<long>();
            var callbackOrder = new List<string>();
            bool replacementRequested = false;

            _tickHandler = (actorID, spellID) =>
            {
                callbackOrder.Add("tick");
                tickCastIDs.Add(GetCurrentSimulationEventCastID());
                if (replacementRequested)
                    return;

                replacementRequested = true;
                ActorEndpoint.onServerSpellCastStarted?.Invoke(_actor, spell);
            };
            _completionHandler = (actorID, spellID) =>
            {
                callbackOrder.Add("complete");
                _completedCastIDs.Add(GetCurrentSimulationEventCastID());
            };
            ActorEndpoint.onServerSpellCastTick += _tickHandler;
            ActorEndpoint.onServerSpellCastCompleted += _completionHandler;

            RunSimulationTick(1);
            Assert.That(GetPendingDictionaryCount("_pendingStarts"), Is.EqualTo(0));
            Assert.That(GetCastState(out var stateAfterFirstTick).CastID, Is.EqualTo(originalCastID));
            Assert.That(stateAfterFirstTick.Enabled, Is.True);

            RunSimulationTick(2);
            Assert.That(GetPendingDictionaryCount("_pendingStarts"), Is.EqualTo(1));
            Assert.That(GetCastState(out var stateBeforeRejectedReplacement).CastID, Is.EqualTo(originalCastID));
            Assert.That(stateBeforeRejectedReplacement.Enabled, Is.True);

            RunSimulationTick(3);
            Assert.That(GetPendingDictionaryCount("_pendingStarts"), Is.EqualTo(0));
            Assert.That(GetCastState(out var stateAfterRejectedReplacement).CastID, Is.EqualTo(originalCastID));
            Assert.That(stateAfterRejectedReplacement.Enabled, Is.True);

            RunSimulationTick(4);
            RunSimulationTick(5);

            CollectionAssert.AreEqual(new[] { originalCastID, originalCastID }, tickCastIDs);
            CollectionAssert.AreEqual(new[] { "tick", "tick", "complete" }, callbackOrder);
            CollectionAssert.AreEqual(new[] { originalCastID }, _completedCastIDs);
            Assert.That(GetCastState(out var completedState).Enabled, Is.False);
            Assert.That(_spellCastManager.IsCasting(_actor.ID), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AcceptedReplacementIgnoresStaleFormerGenerationEvents()
        {
            SpellOverride spell = CreateSpell("deferred-accepted", castTime: 0.5f, cooldown: 0f, totalTicks: 5f);
            long originalCastID = StartCast(spell);
            bool replacementRequested = false;

            _tickHandler = (actorID, spellID) =>
            {
                if (replacementRequested)
                    return;

                replacementRequested = true;
                ActorEndpoint.onServerSpellCastStarted?.Invoke(_actor, spell);
                ActorEndpoint.onServerSpellCastCompleted -= _completionHandler;
                ActorEndpoint.onServerSpellCastCompleted?.Invoke(actorID, spellID);
                ActorEndpoint.onServerSpellCastCompleted += _completionHandler;
                ActorEndpoint.onServerSpellCastInterrupted?.Invoke(actorID, spellID);
            };
            _completionHandler = (actorID, spellID) => _completedCastIDs.Add(GetCurrentSimulationEventCastID());
            ActorEndpoint.onServerSpellCastTick += _tickHandler;
            ActorEndpoint.onServerSpellCastCompleted += _completionHandler;

            RunSimulationTick(1);
            Assert.That(GetPendingDictionaryCount("_pendingStarts"), Is.EqualTo(1));
            Assert.That(GetPendingDictionaryCount("_pendingInterrupts"), Is.EqualTo(1));
            Assert.That(GetCastState(out var stateBeforeReplacement).CastID, Is.EqualTo(originalCastID));
            Assert.That(stateBeforeReplacement.Enabled, Is.True);

            RunSimulationTick(2);
            CastState replacementState = GetCastState(out var stateAfterReplacement);
            long replacementCastID = replacementState.CastID;
            Assert.That(replacementCastID, Is.GreaterThan(originalCastID));
            Assert.That(stateAfterReplacement.Enabled, Is.True);
            Assert.That(GetPendingDictionaryCount("_pendingStarts"), Is.EqualTo(0));
            Assert.That(GetPendingDictionaryCount("_pendingInterrupts"), Is.EqualTo(0));
            Assert.That(_completedCastIDs, Is.Empty);

            RunSimulationTick(3);
            RunSimulationTick(4);
            RunSimulationTick(5);
            RunSimulationTick(6);
            RunSimulationTick(7);

            CollectionAssert.AreEqual(new[] { replacementCastID }, _completedCastIDs);
            Assert.That(GetCastState(out var completedState).Enabled, Is.False);
            Assert.That(_spellCastManager.IsCasting(_actor.ID), Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ActorLevelCancellationRemovesAllPendingSameSpellGenerations()
        {
            SpellOverride spell = CreateSpell("deferred-actor-cancel", castTime: 0.5f, cooldown: 0f, totalTicks: 5f);
            long originalCastID = StartCast(spell);
            long[] pendingGenerationIDs = null;
            bool cancellationResult = false;
            bool cancellationRequested = false;

            _tickHandler = (actorID, spellID) =>
            {
                if (cancellationRequested)
                    return;

                cancellationRequested = true;
                ActorEndpoint.onServerSpellCastStarted?.Invoke(_actor, spell);
                ActorEndpoint.onServerSpellCastStarted?.Invoke(_actor, spell);
                pendingGenerationIDs = GetPendingStartIDs();
                cancellationResult = _spellCastManager.TryInterruptCast(_actor.ID);
            };
            _completionHandler = (actorID, spellID) => _completedCastIDs.Add(GetCurrentSimulationEventCastID());
            ActorEndpoint.onServerSpellCastTick += _tickHandler;
            ActorEndpoint.onServerSpellCastCompleted += _completionHandler;

            RunSimulationTick(1);

            Assert.That(cancellationResult, Is.True);
            Assert.That(pendingGenerationIDs, Is.Not.Null);
            Assert.That(pendingGenerationIDs, Has.Length.EqualTo(2));
            Assert.That(new HashSet<long>(pendingGenerationIDs), Has.Count.EqualTo(2));
            Assert.That(GetPendingDictionaryCount("_pendingStarts"), Is.EqualTo(0));
            Assert.That(GetPendingDictionaryCount("_pendingInterrupts"), Is.EqualTo(1));

            RunSimulationTick(2);

            Assert.That(GetPendingDictionaryCount("_pendingStarts"), Is.EqualTo(0));
            Assert.That(GetPendingDictionaryCount("_pendingInterrupts"), Is.EqualTo(0));
            Assert.That(GetCastState(out var interruptedState).CastID, Is.EqualTo(originalCastID));
            Assert.That(interruptedState.Enabled, Is.False);
            Assert.That(_spellCastManager.IsCasting(_actor.ID), Is.False);
            Assert.That(_completedCastIDs, Is.Empty);
            yield return null;
        }

        private long StartCast(SpellOverride spell)
        {
            ActorEndpoint.onServerSpellCastStarted?.Invoke(_actor, spell);
            CastState state = GetCastState(out var castState);
            Assert.That(castState.Enabled, Is.True);
            return state.CastID;
        }

        private SpellOverride CreateSpell(string definitionID, float castTime, float cooldown, float totalTicks)
        {
            _spellDefinition = ScriptableObject.CreateInstance<SpellDefinition>();
            SetPrivateField(_spellDefinition, "_definitionID", definitionID);
            SetPrivateField(_spellDefinition, "_displayName", definitionID);
            SetPrivateField(_spellDefinition, "_spellName", definitionID);
            SetPrivateField(_spellDefinition, "_baseCastTime", castTime);
            SetPrivateField(_spellDefinition, "_baseCooldown", cooldown);
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
                entityManager.IsComponentEnabled<SpellCastComponent>(entity));
            return state;
        }

        private long[] GetPendingStartIDs()
        {
            var field = typeof(ServerSpellCastManager).GetField(
                "_pendingStartIDsByActor",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);

            var pendingByActor = (IDictionary)field.GetValue(_spellCastManager);
            Assert.That(pendingByActor.Contains(_actor.ID), Is.True);

            var pendingIDs = (IEnumerable)pendingByActor[_actor.ID];
            var result = new List<long>();
            foreach (object pendingID in pendingIDs)
                result.Add(Convert.ToInt64(pendingID));

            return result.ToArray();
        }

        private int GetPendingDictionaryCount(string fieldName)
        {
            var field = typeof(ServerSpellCastManager).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return ((ICollection)field.GetValue(_spellCastManager)).Count;
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
            _completedCastIDs.Clear();
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
