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
            long sequence)
        {
            TickID = tickID;
            Kind = kind;
            ActorID = actorID;
            SourceEntity = sourceEntity;
            DefinitionID = definitionID;
            OperationID = operationID;
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

        /// <summary>Gets the stable registration sequence used as the final tie-break.</summary>
        internal long Sequence { get; }
    }

    /// <summary>Buffers one authoritative tick's ECS-to-domain events until ECS traversal completes.</summary>
    internal sealed class ServerSimulationEventBuffer
    {
        private readonly List<ServerSimulationEvent> _events = new();
        private long _nextSequence;
        private long _tickID;
        private bool _active;
        private bool _dispatching;

        internal void BeginTick(long tickID)
        {
            if (_active)
                throw new InvalidOperationException("A server simulation event buffer is already active.");
            if (_dispatching)
                throw new InvalidOperationException("A server simulation event buffer cannot begin a tick during domain event delivery.");
            if (tickID <= 0)
                throw new ArgumentOutOfRangeException(nameof(tickID), "A simulation event tick ID must be positive.");

            _events.Clear();
            _nextSequence = 0;
            _tickID = tickID;
            _active = true;
        }

        internal void Enqueue(
            ServerSimulationEventKind kind,
            Guid actorID,
            Entity sourceEntity,
            FixedString64Bytes definitionID,
            int operationID)
        {
            if (!_active)
                throw new InvalidOperationException("A server simulation event cannot be queued outside an authoritative tick.");
            if (_dispatching)
                throw new InvalidOperationException("A server simulation event cannot be queued during domain event delivery.");

            _events.Add(
                new ServerSimulationEvent(
                    _tickID,
                    kind,
                    actorID,
                    sourceEntity,
                    definitionID,
                    operationID,
                    checked(++_nextSequence)));
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
            _dispatching = true;
            try
            {
                for (int i = 0; i < _events.Count; i++)
                    dispatcher(_events[i]);
            }
            finally
            {
                _dispatching = false;
                Clear();
            }
        }

        internal void Clear()
        {
            _events.Clear();
            _nextSequence = 0;
            _tickID = 0;
            _active = false;
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
                ServerSimulationEventKind.AuraExpired => 1,
                ServerSimulationEventKind.ActorTickerTick => 0,
                ServerSimulationEventKind.ActorTickerExpired => 1,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown server simulation event kind."),
            };
        }
    }
}
