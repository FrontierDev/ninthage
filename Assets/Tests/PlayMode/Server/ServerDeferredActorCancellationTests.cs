using Game.Server.Core;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Runtime.Tests.Server
{
    /// <summary>Verifies actor retirement cancels deferred simulation events without hiding unrelated work.</summary>
    public sealed class ServerDeferredActorCancellationTests
    {
        private static readonly Guid ACTOR_A_ID =
            Guid.Parse("00000000-0000-0000-0000-000000000911");
        private static readonly Guid ACTOR_B_ID =
            Guid.Parse("00000000-0000-0000-0000-000000000912");
        private static readonly Guid TEST_WORLD_ID =
            Guid.Parse("00000000-0000-0000-0000-000000000913");
        private static readonly MethodInfo UPDATE_SIMULATION_METHOD =
            typeof(ServerECSManager).GetMethod("UpdateSimulation", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo QUEUE_SIMULATION_EVENT_METHOD =
            typeof(ServerECSManager).GetMethod("QueueSimulationEvent", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly Type SIMULATION_EVENT_KIND_TYPE =
            typeof(ServerECSManager).Assembly.GetType("Game.Server.Core.ServerSimulationEventKind");
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

        private GameObject _actorManagerObject;
        private GameObject _tickerManagerObject;
        private GameObject _hostObject;
        private GameObject _actorAObject;
        private GameObject _actorBObject;
        private Actor _actorA;
        private Actor _actorB;
        private ServerTickerManager _tickerManager;
        private Action<Guid, int> _actorTickHandler;

        [SetUp]
        public void SetUp()
        {
            StopRuntime();
            StartRuntime();
        }

        [TearDown]
        public void TearDown()
        {
            StopRuntime();
        }

        [UnityTest]
        public IEnumerator RetiringActorCancelsCurrentAndStagedEventsButPreservesUnrelatedActorEvents()
        {
            var callbackSequence = new List<string>();
            int actorAFirstTickerID = _tickerManager.AddTicker(
                _actorA,
                tickInterval: GameConfigurationManager.Config.ServerSimulationTickDuration,
                duration: float.MaxValue,
                tag: "actor-a-first");
            int actorASecondTickerID = _tickerManager.AddTicker(
                _actorA,
                tickInterval: GameConfigurationManager.Config.ServerSimulationTickDuration,
                duration: float.MaxValue,
                tag: "actor-a-second");
            int actorBTickerID = _tickerManager.AddTicker(
                _actorB,
                tickInterval: GameConfigurationManager.Config.ServerSimulationTickDuration,
                duration: float.MaxValue,
                tag: "actor-b");
            Entity actorAEntity = GetActorEntity(_actorA);
            Entity actorBEntity = GetActorEntity(_actorB);
            bool actorRetired = false;

            _actorTickHandler = (actorID, tickerID) =>
            {
                callbackSequence.Add(DescribeCallback(actorID, tickerID));
                if (actorRetired || actorID != ACTOR_A_ID)
                    return;

                actorRetired = true;
                QueueSimulationEvent("ActorTickerTick", ACTOR_A_ID, actorAEntity, 9001);
                QueueSimulationEvent("ActorTickerTick", ACTOR_B_ID, actorBEntity, 9002);
                _tickerManager.RemoveTicker(_actorB, actorBTickerID);
                ActorEndpoint.onServerActorDespawned?.Invoke(_actorA);
            };
            ActorEndpoint.onServerActorTick += _actorTickHandler;

            RunSimulationTick(1);

            CollectionAssert.AreEqual(
                new[]
                {
                    DescribeCallback(ACTOR_A_ID, actorAFirstTickerID),
                    DescribeCallback(ACTOR_B_ID, actorBTickerID),
                },
                callbackSequence);
            Assert.That(actorASecondTickerID, Is.GreaterThan(actorAFirstTickerID));
            Assert.That(ServerActorManager.Instance.TryGetActor(ACTOR_A_ID, out _), Is.False);
            Assert.That(ServerActorManager.Instance.TryGetActor(ACTOR_B_ID, out _), Is.True);
            Assert.That(ServerECSManager.DefaultWorld.EntityManager.Exists(actorAEntity), Is.True);

            RunSimulationTick(2);

            CollectionAssert.AreEqual(
                new[]
                {
                    DescribeCallback(ACTOR_A_ID, actorAFirstTickerID),
                    DescribeCallback(ACTOR_B_ID, actorBTickerID),
                    DescribeCallback(ACTOR_B_ID, 9002),
                },
                callbackSequence);
            Assert.That(ServerECSManager.DefaultWorld.EntityManager.Exists(actorAEntity), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator StopAndRestartClearsActorCancellationStateForAReusedIdentity()
        {
            Entity retiredEntity = GetActorEntity(_actorA);
            ActorEndpoint.onServerActorDespawned?.Invoke(_actorA);

            Assert.That(ServerActorManager.Instance.TryGetActor(ACTOR_A_ID, out _), Is.False);
            Assert.That(ServerECSManager.DefaultWorld.EntityManager.Exists(retiredEntity), Is.True);

            StopRuntime();
            StartRuntime();

            var callbackSequence = new List<string>();
            _actorTickHandler = (actorID, tickerID) => callbackSequence.Add(DescribeCallback(actorID, tickerID));
            ActorEndpoint.onServerActorTick += _actorTickHandler;
            int newTickerID = _tickerManager.AddTicker(
                _actorA,
                tickInterval: GameConfigurationManager.Config.ServerSimulationTickDuration,
                duration: float.MaxValue,
                tag: "restarted-actor");

            RunSimulationTick(1);

            CollectionAssert.AreEqual(
                new[] { DescribeCallback(ACTOR_A_ID, newTickerID) },
                callbackSequence);
            Assert.That(ServerActorManager.Instance.TryGetActor(ACTOR_A_ID, out _), Is.True);
            Assert.That(ServerECSManager.DefaultWorld.EntityManager.Exists(GetActorEntity(_actorA)), Is.True);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ThrowingDispatchHandlerIsVisibleAndStopsRemainingCurrentCallbacks()
        {
            var callbackSequence = new List<string>();
            int actorATickerID = _tickerManager.AddTicker(
                _actorA,
                tickInterval: GameConfigurationManager.Config.ServerSimulationTickDuration,
                duration: float.MaxValue,
                tag: "throwing-actor-a");
            int actorBTickerID = _tickerManager.AddTicker(
                _actorB,
                tickInterval: GameConfigurationManager.Config.ServerSimulationTickDuration,
                duration: float.MaxValue,
                tag: "throwing-actor-b");
            _actorTickHandler = (actorID, tickerID) =>
            {
                callbackSequence.Add(DescribeCallback(actorID, tickerID));
                if (actorID == ACTOR_A_ID)
                    throw new InvalidOperationException("Test dispatch failure.");
            };
            ActorEndpoint.onServerActorTick += _actorTickHandler;

            Assert.That(
                () => RunSimulationTick(1),
                Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Test dispatch failure."));

            CollectionAssert.AreEqual(
                new[] { DescribeCallback(ACTOR_A_ID, actorATickerID) },
                callbackSequence);
            Assert.That(actorBTickerID, Is.GreaterThan(actorATickerID));
            Assert.That(ServerECSManager.Initialized, Is.True);
            yield return null;
        }

        private void StartRuntime()
        {
            if (!GameConfigurationManager.IsInitialized)
                GameConfigurationManager.Initialize();

            Assert.That(GameConfigurationManager.Config, Is.Not.Null);
            ServerECSManager.ServerInitializeECSWorlds();

            _actorManagerObject = new GameObject("ServerDeferredActorCancellationActorManager");
            _actorManagerObject.AddComponent<ServerActorManager>();

            _tickerManagerObject = new GameObject("ServerDeferredActorCancellationTickerManager");
            _tickerManager = _tickerManagerObject.AddComponent<ServerTickerManager>();

            _hostObject = new GameObject("ServerDeferredActorCancellationClockHost");
            var clock = ServerSimulationClock.Start(_hostObject.AddComponent<ClockTestHost>());
            clock.enabled = false;

            _actorAObject = CreateActor(
                "ServerDeferredActorCancellationActorA",
                ACTOR_A_ID,
                "Deferred Actor A",
                out _actorA);
            _actorBObject = CreateActor(
                "ServerDeferredActorCancellationActorB",
                ACTOR_B_ID,
                "Deferred Actor B",
                out _actorB);
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

        private Entity GetActorEntity(Actor actor)
        {
            Assert.That(TRY_GET_ACTOR_ENTITY_METHOD, Is.Not.Null);
            object[] arguments = { actor.ID, Entity.Null };
            bool found = (bool)TRY_GET_ACTOR_ENTITY_METHOD.Invoke(ServerActorManager.Instance, arguments);
            Assert.That(found, Is.True);
            return (Entity)arguments[1];
        }

        private static string DescribeCallback(Guid actorID, int tickerID)
        {
            return $"{actorID:D}:{tickerID}";
        }

        private static void QueueSimulationEvent(
            string eventKind,
            Guid actorID,
            Entity sourceEntity,
            int operationID)
        {
            Assert.That(QUEUE_SIMULATION_EVENT_METHOD, Is.Not.Null);
            Assert.That(SIMULATION_EVENT_KIND_TYPE, Is.Not.Null);

            object[] arguments =
            {
                Enum.Parse(SIMULATION_EVENT_KIND_TYPE, eventKind),
                actorID,
                sourceEntity,
                new FixedString64Bytes("actor-cancellation"),
                operationID,
                0L,
            };

            try
            {
                QUEUE_SIMULATION_EVENT_METHOD.Invoke(null, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
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
            if (_actorTickHandler != null)
            {
                ActorEndpoint.onServerActorTick -= _actorTickHandler;
                _actorTickHandler = null;
            }

            if (_actorBObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_actorBObject);
                _actorBObject = null;
            }

            if (_actorAObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_actorAObject);
                _actorAObject = null;
            }

            if (_tickerManagerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_tickerManagerObject);
                _tickerManagerObject = null;
            }

            if (_actorManagerObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_actorManagerObject);
                _actorManagerObject = null;
            }

            ServerInitialization.Stop();

            if (_hostObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_hostObject);
                _hostObject = null;
            }

            _actorA = null;
            _actorB = null;
            _tickerManager = null;
        }

        private sealed class ClockTestHost : MonoBehaviour
        {
        }
    }
}
