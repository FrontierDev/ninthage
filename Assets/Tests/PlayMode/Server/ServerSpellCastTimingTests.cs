using Game.Server.Core;
using Game.Server.Systems;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using Game.Shared.Runtime.Spellcasting;
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
    /// <summary>Verifies spell-cast timing is driven by authoritative simulation ticks.</summary>
    public sealed class ServerSpellCastTimingTests
    {
        private static readonly MethodInfo UPDATE_CLOCK_METHOD =
            typeof(ServerSimulationClock).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo LAST_TIMESTAMP_FIELD =
            typeof(ServerSimulationClock).GetField("_lastTimestamp", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo TRY_GET_ACTOR_ENTITY_METHOD =
            typeof(ServerActorManager).GetMethod("TryGetActorEntity", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo CURRENT_SIMULATION_EVENT_FIELD =
            typeof(ServerECSManager).GetField("_currentSimulationEvent", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo HAS_CURRENT_SIMULATION_EVENT_FIELD =
            typeof(ServerECSManager).GetField("_hasCurrentSimulationEvent", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly PropertyInfo CURRENT_EVENT_CAST_ID_PROPERTY =
            CURRENT_SIMULATION_EVENT_FIELD?.FieldType.GetProperty(
                "CastID",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private GameConfiguration _configuration;
        private float _originalTickDuration;
        private int _originalMaxCatchUpTicks;
        private float _originalTimeScale;
        private int _originalTargetFrameRate;
        private GameObject _actorManagerObject;
        private GameObject _cooldownManagerObject;
        private GameObject _spellCastManagerObject;
        private GameObject _actorObject;
        private GameObject _clockHostObject;
        private ServerSimulationClock _clock;
        private ServerSpellCastManager _spellCastManager;
        private ActorEvents _actorEvents;
        private Actor _actor;
        private readonly List<SpellDefinition> _spellDefinitions = new();
        private ServerSimulationRegistrationHandle _tickRegistration;
        private Action<Guid, string> _tickHandler;
        private Action<Guid, string> _completionHandler;
        private Action<SpellContext> _completionContextHandler;

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
            _originalTimeScale = Time.timeScale;
            _originalTargetFrameRate = Application.targetFrameRate;

            _configuration.ServerSimulationTickDuration = 0.1f;
            _configuration.ServerSimulationMaxCatchUpTicks = 2;
        }

        [TearDown]
        public void TearDown()
        {
            StopRuntime();

            if (_configuration != null)
            {
                _configuration.ServerSimulationTickDuration = _originalTickDuration;
                _configuration.ServerSimulationMaxCatchUpTicks = _originalMaxCatchUpTicks;
            }

            Time.timeScale = _originalTimeScale;
            Application.targetFrameRate = _originalTargetFrameRate;
        }

        [UnityTest]
        public IEnumerator AuthoritativeCastTimingIsIndependentOfRenderCadence()
        {
            TimingRun[] runs =
            {
                RunChannelScenario(CreateCadence(12, 0.034d)),
                RunChannelScenario(CreateCadence(24, 0.017d)),
                RunChannelScenario(CreateCadence(58, 0.007d)),
                RunChannelScenario(new[] { 0.405d })
            };

            foreach (TimingRun run in runs)
                AssertChannelTiming(run);

            Assert.That(runs[3].CatchUpCapHitCount, Is.EqualTo(1));
            Assert.That(runs[3].MaxTicksInOneFrame, Is.EqualTo(2));
            Assert.That(runs[3].BacklogTicksAfterDrain, Is.EqualTo(0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator InterruptionAtAnEligibleBoundarySuppressesStaleCompletion()
        {
            StartRuntime();
            var events = new List<CastEventObservation>();
            bool interruptionRequested = false;

            _tickHandler = (actorID, spellID) =>
            {
                events.Add(new CastEventObservation(
                    CastEventKind.Tick,
                    ServerSimulationClock.CurrentTickID,
                    GetCurrentSimulationEventCastID()));

                if (interruptionRequested || ServerSimulationClock.CurrentTickID != 2)
                    return;

                interruptionRequested = true;
                ActorEndpoint.onServerSpellCastInterrupted?.Invoke(actorID, spellID);
            };
            _completionHandler = (actorID, spellID) =>
                events.Add(new CastEventObservation(
                    CastEventKind.Completed,
                    ServerSimulationClock.CurrentTickID,
                    GetCurrentSimulationEventCastID()));
            ActorEndpoint.onServerSpellCastTick += _tickHandler;
            ActorEndpoint.onServerSpellCastCompleted += _completionHandler;

            long castID = StartCast(CreateSpell("timing-interrupted", 0.4f, 2f, true));
            Assert.That(GetCastState(out var startedState).Enabled, Is.True);
            Assert.That(startedState.CastID, Is.EqualTo(castID));

            SimulateFrame(0.205d);
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(events[0].Kind, Is.EqualTo(CastEventKind.Tick));
            Assert.That(events[0].TickID, Is.EqualTo(2));
            Assert.That(events[0].CastID, Is.EqualTo(castID));
            Assert.That(GetCastState(out var pendingInterruptState).Enabled, Is.True);

            SimulateFrame(0.105d);
            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(_spellCastManager.IsCasting(_actor.ID), Is.False);
            Assert.That(_spellCastManager.GetRemainingCastTime(_actor.ID), Is.EqualTo(0f));
            Assert.That(GetCastState(out var interruptedState).Enabled, Is.False);
            Assert.That(interruptedState.CastID, Is.EqualTo(castID));

            SimulateFrame(0.105d);
            Assert.That(events, Has.Count.EqualTo(1));
            StopRuntime();
            yield return null;
        }

        [UnityTest]
        public IEnumerator InstantAndNormalCastsResolveExactlyOnceAtAuthoritativeBoundaries()
        {
            StartRuntime();
            var completionTicks = new List<long>();
            var completionCastIDs = new List<long>();
            _completionContextHandler = context => completionTicks.Add(ServerSimulationClock.CurrentTickID);
            _completionHandler = (actorID, spellID) => completionCastIDs.Add(GetCurrentSimulationEventCastID());
            _actorEvents.onServerSpellCastComplete += _completionContextHandler;
            ActorEndpoint.onServerSpellCastCompleted += _completionHandler;

            long instantCastID = StartCast(CreateSpell("timing-instant", 0f, 0f, false));
            Assert.That(completionTicks, Has.Count.EqualTo(1));
            Assert.That(completionTicks[0], Is.EqualTo(0));
            Assert.That(completionCastIDs, Is.Empty);
            Assert.That(_spellCastManager.IsCasting(_actor.ID), Is.False);
            Assert.That(_spellCastManager.GetRemainingCastTime(_actor.ID), Is.EqualTo(0f));
            Assert.That(GetCastState(out var instantState).CastID, Is.EqualTo(instantCastID));
            Assert.That(instantState.Enabled, Is.False);
            Assert.That(instantState.RemainingCastTime, Is.EqualTo(0f));

            long normalCastID = StartCast(CreateSpell("timing-normal", 0.2f, 0f, false));
            Assert.That(normalCastID, Is.GreaterThan(instantCastID));
            Assert.That(completionTicks, Has.Count.EqualTo(1));
            Assert.That(_spellCastManager.IsCasting(_actor.ID), Is.True);
            Assert.That(_spellCastManager.GetRemainingCastTime(_actor.ID), Is.EqualTo(0.2f).Within(0.000001f));

            SimulateFrame(0.205d);
            Assert.That(completionTicks, Has.Count.EqualTo(2));
            Assert.That(completionTicks[1], Is.EqualTo(2));
            CollectionAssert.AreEqual(new[] { normalCastID }, completionCastIDs);
            Assert.That(_spellCastManager.IsCasting(_actor.ID), Is.False);
            Assert.That(_spellCastManager.GetRemainingCastTime(_actor.ID), Is.EqualTo(0f));
            Assert.That(GetCastState(out var normalState).CastID, Is.EqualTo(normalCastID));
            Assert.That(normalState.Enabled, Is.False);
            Assert.That(normalState.RemainingCastTime, Is.EqualTo(0f));
            StopRuntime();
            yield return null;
        }

        private TimingRun RunChannelScenario(IEnumerable<double> frameCadence)
        {
            StartRuntime();
            var result = new TimingRun();
            _tickHandler = (actorID, spellID) => result.Events.Add(
                new CastEventObservation(
                    CastEventKind.Tick,
                    ServerSimulationClock.CurrentTickID,
                    GetCurrentSimulationEventCastID()));
            _completionHandler = (actorID, spellID) => result.Events.Add(
                new CastEventObservation(
                    CastEventKind.Completed,
                    ServerSimulationClock.CurrentTickID,
                    GetCurrentSimulationEventCastID()));
            ActorEndpoint.onServerSpellCastTick += _tickHandler;
            ActorEndpoint.onServerSpellCastCompleted += _completionHandler;

            SpellOverride spell = CreateSpell("timing-channel", 0.4f, 2f, true);
            result.CastID = StartCast(spell);
            result.AuthoredCastTime = spell.CastTime;
            Assert.That(GetCastState(out var startedState).TotalCastTime, Is.EqualTo(spell.CastTime).Within(0.000001f));
            Assert.That(startedState.TotalTicks, Is.EqualTo(spell.TotalTicks).Within(0.000001f));

            _tickRegistration = ServerSimulationClock.RegisterEachTick(
                tick => result.Ticks.Add(
                    new TickObservation(
                        tick.TickID,
                        _spellCastManager.GetRemainingCastTime(_actor.ID),
                        GetCastState(out var state).Enabled,
                        state.RemainingCastTime)));

            foreach (double elapsedSeconds in frameCadence)
            {
                int executedTicks = SimulateFrame(elapsedSeconds);
                if (executedTicks > result.MaxTicksInOneFrame)
                    result.MaxTicksInOneFrame = executedTicks;
            }

            while (_clock.BacklogTicks > 0)
            {
                int executedTicks = SimulateFrame(0d);
                if (executedTicks > result.MaxTicksInOneFrame)
                    result.MaxTicksInOneFrame = executedTicks;
            }

            result.CatchUpCapHitCount = _clock.CatchUpCapHitCount;
            result.BacklogTicksAfterDrain = _clock.BacklogTicks;
            result.FinalRemainingCastTime = _spellCastManager.GetRemainingCastTime(_actor.ID);
            result.FinalState = GetCastState(out var finalState);
            result.IsCasting = _spellCastManager.IsCasting(_actor.ID);
            StopRuntime();
            return result;
        }

        private void StartRuntime()
        {
            ServerECSManager.ServerInitializeECSWorlds();

            _actorManagerObject = new GameObject("ServerSpellCastTimingActorManager");
            _actorManagerObject.AddComponent<ServerActorManager>();

            _cooldownManagerObject = new GameObject("ServerSpellCastTimingCooldownManager");
            _cooldownManagerObject.AddComponent<ServerCooldownManager>();

            _spellCastManagerObject = new GameObject("ServerSpellCastTimingSpellCastManager");
            _spellCastManager = _spellCastManagerObject.AddComponent<ServerSpellCastManager>();

            _actorObject = new GameObject("ServerSpellCastTimingActor");
            _actor = _actorObject.AddComponent<Actor>();
            _actorEvents = _actorObject.AddComponent<ActorEvents>();
            _actor.Initialize(Guid.Parse("00000000-0000-0000-0000-000000000902"), "Spell Cast Timing Test Actor");
            ActorEndpoint.onServerActorSpawned?.Invoke(_actor);

            _clockHostObject = new GameObject("ServerSpellCastTimingClockHost");
            _clock = ServerSimulationClock.Start(_clockHostObject.AddComponent<ClockTestHost>());
            _clock.enabled = false;
        }

        private long StartCast(SpellOverride spell)
        {
            ActorEndpoint.onServerSpellCastStarted?.Invoke(_actor, spell);
            CastState state = GetCastState(out var castState);
            Assert.That(castState.CastID, Is.GreaterThan(0));
            return state.CastID;
        }

        private SpellOverride CreateSpell(string definitionID, float castTime, float totalTicks, bool isChanneled)
        {
            var spellDefinition = ScriptableObject.CreateInstance<SpellDefinition>();
            SetPrivateField(spellDefinition, "_definitionID", definitionID);
            SetPrivateField(spellDefinition, "_displayName", definitionID);
            SetPrivateField(spellDefinition, "_spellName", definitionID);
            SetPrivateField(spellDefinition, "_baseCastTime", castTime);
            SetPrivateField(spellDefinition, "_baseCooldown", 0f);
            SetPrivateField(spellDefinition, "_baseTotalTicks", totalTicks);
            SetPrivateField(spellDefinition, "_isChanneled", isChanneled);
            SetPrivateField(spellDefinition, "_triggersGCD", false);
            _spellDefinitions.Add(spellDefinition);

            return new SpellOverride(spellDefinition);
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
                spellCast.TotalCastTime,
                spellCast.TotalTicks,
                entityManager.IsComponentEnabled<SpellCastComponent>(entity));
            return state;
        }

        private int SimulateFrame(double elapsedSeconds)
        {
            Assert.That(LAST_TIMESTAMP_FIELD, Is.Not.Null);
            Assert.That(UPDATE_CLOCK_METHOD, Is.Not.Null);

            long elapsedTimestampTicks = checked((long)Math.Round(elapsedSeconds * Stopwatch.Frequency));
            LAST_TIMESTAMP_FIELD.SetValue(
                _clock,
                checked(Stopwatch.GetTimestamp() - elapsedTimestampTicks));

            long executedBefore = _clock.ExecutedTickCount;
            try
            {
                UPDATE_CLOCK_METHOD.Invoke(_clock, null);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }

            return checked((int)(_clock.ExecutedTickCount - executedBefore));
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

        private static void AssertChannelTiming(TimingRun run)
        {
            Assert.That(run.CastID, Is.GreaterThan(0));
            Assert.That(run.AuthoredCastTime, Is.EqualTo(0.4f).Within(0.000001f));
            Assert.That(run.Events, Has.Count.EqualTo(3));
            Assert.That(run.Events[0].Kind, Is.EqualTo(CastEventKind.Tick));
            Assert.That(run.Events[0].TickID, Is.EqualTo(2));
            Assert.That(run.Events[0].CastID, Is.EqualTo(run.CastID));
            Assert.That(run.Events[1].Kind, Is.EqualTo(CastEventKind.Tick));
            Assert.That(run.Events[1].TickID, Is.EqualTo(4));
            Assert.That(run.Events[1].CastID, Is.EqualTo(run.CastID));
            Assert.That(run.Events[2].Kind, Is.EqualTo(CastEventKind.Completed));
            Assert.That(run.Events[2].TickID, Is.EqualTo(4));
            Assert.That(run.Events[2].CastID, Is.EqualTo(run.CastID));

            Assert.That(run.Ticks, Has.Count.EqualTo(4));
            for (int i = 0; i < run.Ticks.Count; i++)
            {
                TickObservation observation = run.Ticks[i];
                Assert.That(observation.TickID, Is.EqualTo(i + 1));
                Assert.That(observation.RemainingCastTimeFromManager, Is.GreaterThanOrEqualTo(0f));
                Assert.That(
                    observation.RemainingCastTimeFromManager,
                    Is.EqualTo(Mathf.Max(0f, 0.4f - (i + 1) * 0.1f)).Within(0.000001f));
                Assert.That(
                    observation.RemainingCastTimeFromComponent,
                    Is.EqualTo(observation.RemainingCastTimeFromManager).Within(0.000001f));
                Assert.That(observation.Enabled, Is.EqualTo(i < 3));
            }

            Assert.That(run.FinalRemainingCastTime, Is.EqualTo(0f));
            Assert.That(run.FinalState.Enabled, Is.False);
            Assert.That(run.FinalState.RemainingCastTime, Is.EqualTo(0f));
            Assert.That(run.IsCasting, Is.False);
        }

        private static double[] CreateCadence(int frameCount, double elapsedSeconds)
        {
            var cadence = new double[frameCount];
            for (int i = 0; i < cadence.Length; i++)
                cadence[i] = elapsedSeconds;

            return cadence;
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
            if (_tickRegistration.IsValid)
            {
                _tickRegistration.Cancel();
                _tickRegistration = default;
            }

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

            if (_actorEvents != null && _completionContextHandler != null)
            {
                _actorEvents.onServerSpellCastComplete -= _completionContextHandler;
                _completionContextHandler = null;
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

            _clock = null;
            _actor = null;
            _actorEvents = null;
            _spellCastManager = null;
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

        private readonly struct TickObservation
        {
            public TickObservation(
                long tickID,
                float remainingCastTimeFromManager,
                bool enabled,
                float remainingCastTimeFromComponent)
            {
                TickID = tickID;
                RemainingCastTimeFromManager = remainingCastTimeFromManager;
                Enabled = enabled;
                RemainingCastTimeFromComponent = remainingCastTimeFromComponent;
            }

            public long TickID { get; }
            public float RemainingCastTimeFromManager { get; }
            public bool Enabled { get; }
            public float RemainingCastTimeFromComponent { get; }
        }

        private sealed class TimingRun
        {
            public readonly List<CastEventObservation> Events = new();
            public readonly List<TickObservation> Ticks = new();
            public long CastID;
            public float AuthoredCastTime;
            public int MaxTicksInOneFrame;
            public long CatchUpCapHitCount;
            public long BacklogTicksAfterDrain;
            public float FinalRemainingCastTime;
            public CastState FinalState;
            public bool IsCasting;
        }

        private readonly struct CastState
        {
            public CastState(
                long castID,
                float remainingCastTime,
                float totalCastTime,
                float totalTicks,
                bool enabled)
            {
                CastID = castID;
                RemainingCastTime = remainingCastTime;
                TotalCastTime = totalCastTime;
                TotalTicks = totalTicks;
                Enabled = enabled;
            }

            public long CastID { get; }
            public float RemainingCastTime { get; }
            public float TotalCastTime { get; }
            public float TotalTicks { get; }
            public bool Enabled { get; }
        }

        private sealed class ClockTestHost : MonoBehaviour
        {
        }
    }
}
