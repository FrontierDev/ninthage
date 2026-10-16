using Game.Server.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;

namespace Game.Shared.Tests.Server
{
    /// <summary>Regression coverage for deterministic server simulation event buffering.</summary>
    public sealed class ServerSimulationEventBufferTests
    {
        [Test]
        public void SameTickEventsUseTheApprovedStableOrderAndDispatchOnce()
        {
            var buffer = new BufferHarness();
            Guid actorA = Guid.Parse("00000000-0000-0000-0000-000000000001");
            Guid actorB = Guid.Parse("00000000-0000-0000-0000-000000000002");
            Guid actorC = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var events = new List<string>();

            buffer.BeginTick(1);

            buffer.Enqueue("ActorTickerExpired", actorA, CreateEntity(1, 1), "ticker-a", 4, 41);
            buffer.Enqueue("ActorTickerTick", actorA, CreateEntity(1, 1), "ticker-a", 4, 41);
            buffer.Enqueue("AuraTick", actorB, CreateEntity(2, 2), "aura-b", 1, 31);
            buffer.Enqueue("AuraExpired", actorA, CreateEntity(4, 1), "aura-a", 2, 22);
            buffer.Enqueue("AuraTick", actorA, CreateEntity(4, 1), "aura-a", 2, 22);
            buffer.Enqueue("AuraTick", actorA, CreateEntity(4, 1), "aura-a", 1, 21);
            buffer.Enqueue("SpellCastTick", actorC, CreateEntity(20, 1), "spell-c", 9, 13);
            buffer.Enqueue("SpellCastCompleted", actorA, CreateEntity(20, 1), "spell-a", 9, 11);
            buffer.Enqueue("SpellCastTick", actorA, CreateEntity(20, 1), "spell-a", 9, 11);

            buffer.Dispatch(simulationEvent => events.Add(buffer.Describe(simulationEvent)));

            CollectionAssert.AreEqual(
                new[]
                {
                    BufferHarness.Describe(1, "SpellCastTick", actorA, CreateEntity(20, 1), "spell-a", 9, 11),
                    BufferHarness.Describe(1, "SpellCastCompleted", actorA, CreateEntity(20, 1), "spell-a", 9, 11),
                    BufferHarness.Describe(1, "SpellCastTick", actorC, CreateEntity(20, 1), "spell-c", 9, 13),
                    BufferHarness.Describe(1, "AuraTick", actorA, CreateEntity(4, 1), "aura-a", 1, 21),
                    BufferHarness.Describe(1, "AuraTick", actorA, CreateEntity(4, 1), "aura-a", 2, 22),
                    BufferHarness.Describe(1, "AuraExpired", actorA, CreateEntity(4, 1), "aura-a", 2, 22),
                    BufferHarness.Describe(1, "AuraTick", actorB, CreateEntity(2, 2), "aura-b", 1, 31),
                    BufferHarness.Describe(1, "ActorTickerTick", actorA, CreateEntity(1, 1), "ticker-a", 4, 41),
                    BufferHarness.Describe(1, "ActorTickerExpired", actorA, CreateEntity(1, 1), "ticker-a", 4, 41),
                },
                events);
            Assert.That(events, Has.Count.EqualTo(9));
        }

        [Test]
        public void DispatchStagesNewEventsForTheNextTickAndSkipsCancelledActors()
        {
            var buffer = new BufferHarness();
            Guid activeActor = Guid.Parse("00000000-0000-0000-0000-000000000011");
            Guid cancelledActor = Guid.Parse("00000000-0000-0000-0000-000000000012");
            Guid deferredActor = Guid.Parse("00000000-0000-0000-0000-000000000013");
            var events = new List<string>();
            bool deferredEventQueued = false;

            buffer.BeginTick(10);
            buffer.Enqueue("SpellCastTick", activeActor, CreateEntity(10, 1), "active", 1, 1);
            buffer.Enqueue("SpellCastCompleted", cancelledActor, CreateEntity(11, 1), "cancelled", 2, 2);
            buffer.CancelActor(cancelledActor);

            buffer.Dispatch(simulationEvent =>
            {
                events.Add(buffer.Describe(simulationEvent));
                if (deferredEventQueued)
                    return;

                deferredEventQueued = true;
                buffer.Enqueue("AuraTick", deferredActor, CreateEntity(12, 1), "deferred", 3, 3);
            });

            Assert.That(events, Has.Count.EqualTo(1));
            Assert.That(
                events[0],
                Is.EqualTo(BufferHarness.Describe(10, "SpellCastTick", activeActor, CreateEntity(10, 1), "active", 1, 1)));

            buffer.BeginTick(11);
            buffer.Dispatch(simulationEvent => events.Add(buffer.Describe(simulationEvent)));

            CollectionAssert.AreEqual(
                new[]
                {
                    BufferHarness.Describe(10, "SpellCastTick", activeActor, CreateEntity(10, 1), "active", 1, 1),
                    BufferHarness.Describe(11, "AuraTick", deferredActor, CreateEntity(12, 1), "deferred", 3, 3),
                },
                events);
        }

        [Test]
        public void ReentrantDispatchRepeatedBeginTickAndQueueOverflowAreRejected()
        {
            var buffer = new BufferHarness();
            Guid actorID = Guid.Parse("00000000-0000-0000-0000-000000000021");

            buffer.BeginTick(1);
            Assert.That(() => buffer.BeginTick(2), Throws.TypeOf<InvalidOperationException>());
            buffer.Enqueue("SpellCastTick", actorID, CreateEntity(1, 1), "reentrant", 1, 1);

            Exception reentrantException = null;
            buffer.Dispatch(_ =>
            {
                try
                {
                    buffer.Dispatch(__ => { });
                }
                catch (Exception exception)
                {
                    reentrantException = exception;
                }
            });

            Assert.That(reentrantException, Is.TypeOf<InvalidOperationException>());

            var boundedBuffer = new BufferHarness();
            boundedBuffer.BeginTick(1);
            for (int i = 0; i < 4096; i++)
                boundedBuffer.Enqueue("SpellCastTick", actorID, CreateEntity(i, 1), "bounded", i, i);

            Assert.That(
                () => boundedBuffer.Enqueue("SpellCastTick", actorID, CreateEntity(4096, 1), "overflow", 4096, 4096),
                Throws.TypeOf<InvalidOperationException>());
            boundedBuffer.EndTick();
        }

        private static Entity CreateEntity(int index, int version)
        {
            return new Entity
            {
                Index = index,
                Version = version,
            };
        }

        private sealed class BufferHarness
        {
            private static readonly Type BUFFER_TYPE = typeof(ServerECSManager).Assembly.GetType(
                "Game.Server.Core.ServerSimulationEventBuffer",
                throwOnError: true);
            private static readonly Type EVENT_KIND_TYPE = typeof(ServerECSManager).Assembly.GetType(
                "Game.Server.Core.ServerSimulationEventKind",
                throwOnError: true);
            private static readonly Type EVENT_TYPE = typeof(ServerECSManager).Assembly.GetType(
                "Game.Server.Core.ServerSimulationEvent",
                throwOnError: true);
            private static readonly Type DISPATCHER_TYPE = typeof(Action<>).MakeGenericType(EVENT_TYPE);
            private static readonly MethodInfo BEGIN_TICK_METHOD = GetMethod("BeginTick", typeof(long));
            private static readonly MethodInfo ENQUEUE_METHOD = GetMethod(
                "Enqueue",
                EVENT_KIND_TYPE,
                typeof(Guid),
                typeof(Entity),
                typeof(FixedString64Bytes),
                typeof(int),
                typeof(long),
                typeof(bool));
            private static readonly MethodInfo DISPATCH_METHOD = GetMethod("Dispatch", DISPATCHER_TYPE);
            private static readonly MethodInfo END_TICK_METHOD = GetMethod("EndTick");
            private static readonly MethodInfo CANCEL_ACTOR_METHOD = GetMethod("CancelActor", typeof(Guid));
            private static readonly PropertyInfo TICK_ID_PROPERTY = GetProperty("TickID");
            private static readonly PropertyInfo KIND_PROPERTY = GetProperty("Kind");
            private static readonly PropertyInfo ACTOR_ID_PROPERTY = GetProperty("ActorID");
            private static readonly PropertyInfo SOURCE_ENTITY_PROPERTY = GetProperty("SourceEntity");
            private static readonly PropertyInfo DEFINITION_ID_PROPERTY = GetProperty("DefinitionID");
            private static readonly PropertyInfo OPERATION_ID_PROPERTY = GetProperty("OperationID");
            private static readonly PropertyInfo CAST_ID_PROPERTY = GetProperty("CastID");

            private readonly object _buffer;

            internal BufferHarness()
            {
                ConstructorInfo constructor = BUFFER_TYPE.GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null);
                _buffer = constructor.Invoke(null);
            }

            internal void BeginTick(long tickID) => Invoke(BEGIN_TICK_METHOD, tickID);

            internal void Enqueue(
                string kind,
                Guid actorID,
                Entity sourceEntity,
                string definitionID,
                int operationID,
                long castID)
            {
                Invoke(
                    ENQUEUE_METHOD,
                    Enum.Parse(EVENT_KIND_TYPE, kind),
                    actorID,
                    sourceEntity,
                    new FixedString64Bytes(definitionID),
                    operationID,
                    castID,
                    false);
            }

            internal void Dispatch(Action<object> dispatcher)
            {
                ParameterExpression eventParameter = Expression.Parameter(EVENT_TYPE, "simulationEvent");
                UnaryExpression boxedEvent = Expression.Convert(eventParameter, typeof(object));
                InvocationExpression invokeDispatcher = Expression.Invoke(
                    Expression.Constant(dispatcher),
                    boxedEvent);
                Delegate typedDispatcher = Expression.Lambda(
                    DISPATCHER_TYPE,
                    invokeDispatcher,
                    eventParameter).Compile();

                Invoke(DISPATCH_METHOD, typedDispatcher);
            }

            internal void CancelActor(Guid actorID) => Invoke(CANCEL_ACTOR_METHOD, actorID);

            internal void EndTick() => Invoke(END_TICK_METHOD);

            internal string Describe(object simulationEvent)
            {
                return Describe(
                    (long)TICK_ID_PROPERTY.GetValue(simulationEvent),
                    KIND_PROPERTY.GetValue(simulationEvent).ToString(),
                    (Guid)ACTOR_ID_PROPERTY.GetValue(simulationEvent),
                    (Entity)SOURCE_ENTITY_PROPERTY.GetValue(simulationEvent),
                    DEFINITION_ID_PROPERTY.GetValue(simulationEvent).ToString(),
                    (int)OPERATION_ID_PROPERTY.GetValue(simulationEvent),
                    (long)CAST_ID_PROPERTY.GetValue(simulationEvent));
            }

            internal static string Describe(
                long tickID,
                string kind,
                Guid actorID,
                Entity sourceEntity,
                string definitionID,
                int operationID,
                long castID)
            {
                return $"{tickID}:{kind}:{actorID:D}:{sourceEntity.Index}:{sourceEntity.Version}:{definitionID}:{operationID}:{castID}";
            }

            private static MethodInfo GetMethod(string name, params Type[] parameterTypes)
            {
                return BUFFER_TYPE.GetMethod(
                    name,
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    binder: null,
                    types: parameterTypes,
                    modifiers: null);
            }

            private static PropertyInfo GetProperty(string name)
            {
                return EVENT_TYPE.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic);
            }

            private object Invoke(MethodInfo method, params object[] arguments)
            {
                try
                {
                    return method.Invoke(_buffer, arguments);
                }
                catch (TargetInvocationException exception) when (exception.InnerException != null)
                {
                    throw exception.InnerException;
                }
            }
        }
    }
}
