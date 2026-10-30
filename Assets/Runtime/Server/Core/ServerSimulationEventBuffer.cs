using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Game.Server.Core
{
    /// <summary>Identifies a domain event emitted by an authoritative ECS system.</summary>
    internal enum ServerSimulationEventKind : byte
    {
        SpellCastTick,
        SpellCastCompleted,
        AuraTick,
        AuraStackExpired,
        AuraExpired,
        ActorTickerTick,
        ActorTickerExpired,
    }

    /// <summary>
    /// Contains only value data captured during one ECS traversal. The source entity is an
    /// identity token and is never dereferenced during delivery.
    /// </summary>
    internal readonly struct ServerSimulationEvent
    {
        internal ServerSimulationEvent(
            long tickID,
            ServerSimulationEventKind kind,
            Guid actorID,
            Entity sourceEntity,
            FixedString64Bytes definitionID,
            int operationID,
            long castID,
            long sequence)
        {
            TickID = tickID;
            Kind = kind;
            ActorID = actorID;
            SourceEntity = sourceEntity;
            DefinitionID = definitionID;
            OperationID = operationID;
            CastID = castID;
            Sequence = sequence;
        }

        /// <summary>Gets the authoritative tick that produced the event.</summary>
        internal long TickID { get; }

        /// <summary>Gets the event kind.</summary>
        internal ServerSimulationEventKind Kind { get; }

        /// <summary>Gets the stable actor identity associated with the event.</summary>
        internal Guid ActorID { get; }

        /// <summary>Gets the source ECS entity identity without retaining mutable ECS state.</summary>
        internal Entity SourceEntity { get; }

        /// <summary>Gets the spell or aura definition identifier, when applicable.</summary>
        internal FixedString64Bytes DefinitionID { get; }

        /// <summary>Gets the aura instance or ticker identity, when applicable.</summary>
        internal int OperationID { get; }

        /// <summary>Gets the unique spell-cast generation, when applicable.</summary>
        internal long CastID { get; }

        /// <summary>Gets the stable registration sequence used as the final tie-break.</summary>
        internal long Sequence { get; }
    }

    /// <summary>Buffers one authoritative tick's ECS-to-domain events until ECS traversal completes.</summary>
    internal sealed class ServerSimulationEventBuffer
    {
        private const int MAX_EVENTS_PER_TICK = 4096;
        private readonly List<ServerSimulationEvent> _events = new();
        private readonly List<ServerSimulationEvent> _stagedEvents = new();
        private readonly List<ServerSimulationEvent> _dispatchEvents = new();
        private readonly HashSet<Guid> _cancelledActorIDs = new();
        private long _nextSequence;
        private long _tickID;
        private bool _active;
        private bool _dispatching;

        internal bool IsDispatching => _dispatching;

        internal void BeginTick(long tickID)
        {
            if (_active)
                throw new InvalidOperationException("A server simulation event buffer is already active.");
            if (_dispatching)
                throw new InvalidOperationException("A server simulation event buffer cannot begin a tick during domain event delivery.");
            if (tickID <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickID), "A simulation event tick ID must be positive.");

            _events.Clear();
            for (int i = _stagedEvents.Count - 1; i >= 0; i--)
            {
                if (_stagedEvents[i].TickID <= tickID)
                {
                    _events.Add(_stagedEvents[i]);
                    _stagedEvents.RemoveAt(i);
                }
            }

            _tickID = tickID;
            _active = true;
        }

        internal void Enqueue(
            ServerSimulationEventKind kind,
            Guid actorID,
            Entity sourceEntity,
            FixedString64Bytes definitionID,
            int operationID,
            long castID,
            bool nextTick)
        {
            if (!_active)
                throw new InvalidOperationException("A server simulation event cannot be queued outside an authoritative tick.");

            List<ServerSimulationEvent> destination = nextTick || _dispatching ? _stagedEvents : _events;
            long eventTickID = nextTick || _dispatching ? checked(_tickID + 1) : _tickID;
            if (_events.Count + _stagedEvents.Count >= MAX_EVENTS_PER_TICK)
            {
                throw new InvalidOperationException(
                    $"The server simulation event limit of {MAX_EVENTS_PER_TICK} was exceeded for tick {_tickID}.");
            }

            destination.Add(
                new ServerSimulationEvent(
                    eventTickID,
                    kind,
                    actorID,
                    sourceEntity,
                    definitionID,
                    operationID,
                    castID,
                    checked(++_nextSequence)));
        }

        internal void Enqueue(
            ServerSimulationEventKind kind,
            Guid actorID,
            Entity sourceEntity,
            FixedString64Bytes definitionID,
            int operationID,
            long castID)
        {
            Enqueue(kind, actorID, sourceEntity, definitionID, operationID, castID, nextTick: false);
        }

        internal void EnqueueForNextTick(
            ServerSimulationEventKind kind,
            Guid actorID,
            Entity sourceEntity,
            FixedString64Bytes definitionID,
            int operationID,
            long castID)
        {
            Enqueue(kind, actorID, sourceEntity, definitionID, operationID, castID, nextTick: true);
        }

        internal void Dispatch(Action<ServerSimulationEvent> dispatcher)
        {
            if (!_active)
                throw new InvalidOperationException("A server simulation event buffer is not active.");
            if (dispatcher == null)
                throw new ArgumentNullException(nameof(dispatcher));
            if (_dispatching)
                throw new InvalidOperationException("Server simulation event delivery is already in progress.");

            _events.Sort(CompareEvents);
            _dispatchEvents.Clear();
            _dispatchEvents.AddRange(_events);
            _events.Clear();
            _dispatching = true;
            try
            {
                for (int i = 0; i < _dispatchEvents.Count; i++)
                {
                    ServerSimulationEvent simulationEvent = _dispatchEvents[i];
                    if (!_cancelledActorIDs.Contains(simulationEvent.ActorID))
                        dispatcher(simulationEvent);
                }
            }
            finally
            {
                _dispatching = false;
                _dispatchEvents.Clear();
            }
        }

        internal void EndTick()
        {
            _events.Clear();
            _tickID = 0;
            _active = false;
        }

        internal void Clear()
        {
            _events.Clear();
            _stagedEvents.Clear();
            _dispatchEvents.Clear();
            _cancelledActorIDs.Clear();
            _nextSequence = 0;
            _tickID = 0;
            _active = false;
            _dispatching = false;
        }

        internal void CancelActor(Guid actorID)
        {
            _cancelledActorIDs.Add(actorID);
            RemoveActorEvents(_events, actorID);
            RemoveActorEvents(_stagedEvents, actorID);
        }

        internal void ClearActorCancellation(Guid actorID)
        {
            _cancelledActorIDs.Remove(actorID);
        }

        private static void RemoveActorEvents(List<ServerSimulationEvent> events, Guid actorID)
        {
            for (int i = events.Count - 1; i >= 0; i--)
            {
                if (events[i].ActorID == actorID)
                    events.RemoveAt(i);
            }
        }

        private static int CompareEvents(ServerSimulationEvent left, ServerSimulationEvent right)
        {
            int comparison = left.TickID.CompareTo(right.TickID);
            if (comparison != 0)
                return comparison;

            comparison = GetPhase(left.Kind).CompareTo(GetPhase(right.Kind));
            if (comparison != 0)
                return comparison;

            comparison = GetSystemOrder(left.Kind).CompareTo(GetSystemOrder(right.Kind));
            if (comparison != 0)
                return comparison;

            comparison = left.ActorID.CompareTo(right.ActorID);
            if (comparison != 0)
                return comparison;

            comparison = left.SourceEntity.Index.CompareTo(right.SourceEntity.Index);
            if (comparison != 0)
                return comparison;

            comparison = left.SourceEntity.Version.CompareTo(right.SourceEntity.Version);
            if (comparison != 0)
                return comparison;

            comparison = left.OperationID.CompareTo(right.OperationID);
            if (comparison != 0)
                return comparison;

            comparison = GetSubphase(left.Kind).CompareTo(GetSubphase(right.Kind));
            if (comparison != 0)
                return comparison;

            return left.Sequence.CompareTo(right.Sequence);
        }

        private static int GetPhase(ServerSimulationEventKind kind)
        {
            return kind switch
            {
                ServerSimulationEventKind.SpellCastTick => 3,
                ServerSimulationEventKind.SpellCastCompleted => 3,
                ServerSimulationEventKind.AuraTick => 4,
                ServerSimulationEventKind.AuraStackExpired => 4,
                ServerSimulationEventKind.AuraExpired => 4,
                ServerSimulationEventKind.ActorTickerTick => 4,
                ServerSimulationEventKind.ActorTickerExpired => 4,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown server simulation event kind."),
            };
        }

        private static int GetSystemOrder(ServerSimulationEventKind kind)
        {
            return kind switch
            {
                ServerSimulationEventKind.SpellCastTick => 0,
                ServerSimulationEventKind.SpellCastCompleted => 0,
                ServerSimulationEventKind.AuraTick => 1,
                ServerSimulationEventKind.AuraStackExpired => 1,
                ServerSimulationEventKind.AuraExpired => 1,
                ServerSimulationEventKind.ActorTickerTick => 2,
                ServerSimulationEventKind.ActorTickerExpired => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown server simulation event kind."),
            };
        }

        private static int GetSubphase(ServerSimulationEventKind kind)
        {
            return kind switch
            {
                ServerSimulationEventKind.SpellCastTick => 0,
                ServerSimulationEventKind.SpellCastCompleted => 1,
                ServerSimulationEventKind.AuraTick => 0,
                ServerSimulationEventKind.AuraStackExpired => 1,
                ServerSimulationEventKind.AuraExpired => 2,
                ServerSimulationEventKind.ActorTickerTick => 0,
                ServerSimulationEventKind.ActorTickerExpired => 1,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown server simulation event kind."),
            };
        }
    }

    /// <summary>Provides a bounded next-boundary queue for one server-owned mutation type.</summary>
    internal sealed class ServerSimulationMutationQueue<T> where T : struct
    {
        private const int MAX_PENDING_MUTATIONS = 4096;
        private readonly List<T> _pending = new();

        internal void Enqueue(T mutation)
        {
            if (_pending.Count >= MAX_PENDING_MUTATIONS)
            {
                throw new InvalidOperationException(
                    $"The server simulation mutation limit of {MAX_PENDING_MUTATIONS} was exceeded.");
            }

            _pending.Add(mutation);
        }

        internal void TakeSnapshot(List<T> snapshot)
        {
            snapshot.Clear();
            snapshot.AddRange(_pending);
            _pending.Clear();
        }

        internal void Clear()
        {
            _pending.Clear();
        }
    }
}
