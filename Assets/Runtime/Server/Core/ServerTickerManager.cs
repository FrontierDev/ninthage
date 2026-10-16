using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>Maintains server-authoritative actor tickers.</summary>
    public sealed class ServerTickerManager : MonoBehaviour
    {
        private static ServerTickerManager _instance;
        /// <summary>Gets the active server ticker manager.</summary>
        public static ServerTickerManager Instance => _instance;
        private static bool _initialized;
        /// <summary>Gets whether the ticker manager is initialized.</summary>
        public static bool IsInitialized => _initialized;

        private EntityManager _entityManager;
        private EntityQuery _actorComponentQuery;

        private readonly Dictionary<int, TickerRegistration> _tickers = new();
        private readonly Dictionary<(Guid, string), int> _tagIndex = new();
        private readonly Dictionary<Guid, List<int>> _actorTickerIDs = new();
        private readonly HashSet<int> _pendingTickerIDs = new();
        private readonly ServerSimulationMutationQueue<TickerMutation> _pendingMutations = new();
        private TickerMutation[] _commitSnapshot;
        private int _nextTickerID;

        private struct TickerRegistration
        {
            public Guid ActorID;
            public string Tag;
            public float TickInterval;
            public float Duration;
            public Action<Actor> onTick;
            public Action<Actor> onExpire;
        }

        private enum TickerMutationKind : byte
        {
            Add,
            Remove,
        }

        private readonly struct TickerMutation
        {
            internal TickerMutation(TickerMutationKind kind, Guid actorID, int tickerID)
            {
                Kind = kind;
                ActorID = actorID;
                TickerID = tickerID;
            }

            internal TickerMutationKind Kind { get; }
            internal Guid ActorID { get; }
            internal int TickerID { get; }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Error("Multiple instances of ServerTickerManager detected! Destroying duplicate.");
                Destroy(this);
                return;
            }
            _instance = this;

            var world = ServerECSManager.DefaultWorld;
            _entityManager = world.EntityManager;
            _actorComponentQuery = _entityManager.CreateEntityQuery(typeof(Systems.ActorComponent));

            ActorEndpoint.onServerActorTick += OnActorTick;
            ActorEndpoint.onServerActorTickerExpired += OnActorTickerExpired;
            ActorEndpoint.onServerAddTicker += OnAddTickerRequest;
            ActorEndpoint.onServerRemoveTickerByTag += OnRemoveTickerByTagRequest;
            ActorEndpoint.onServerActorDespawned += OnActorDespawned;

            _initialized = true;
        }

        private void OnDestroy()
        {
            ActorEndpoint.onServerActorTick -= OnActorTick;
            ActorEndpoint.onServerActorTickerExpired -= OnActorTickerExpired;
            ActorEndpoint.onServerAddTicker -= OnAddTickerRequest;
            ActorEndpoint.onServerRemoveTickerByTag -= OnRemoveTickerByTagRequest;
            ActorEndpoint.onServerActorDespawned -= OnActorDespawned;

            _tickers.Clear();
            _tagIndex.Clear();
            _actorTickerIDs.Clear();
            _pendingTickerIDs.Clear();
            _pendingMutations.Clear();
            _commitSnapshot = null;
            _instance = null;
            _initialized = false;
        }

        internal void PreparePendingMutations()
        {
            _commitSnapshot = _pendingMutations.TakeSnapshot();
        }

        internal void CommitPendingMutations()
        {
            TickerMutation[] mutations = _commitSnapshot ?? _pendingMutations.TakeSnapshot();
            _commitSnapshot = null;
            foreach (TickerMutation mutation in mutations)
            {
                switch (mutation.Kind)
                {
                    case TickerMutationKind.Add:
                        if (!_pendingTickerIDs.Remove(mutation.TickerID)
                            || !_tickers.TryGetValue(mutation.TickerID, out var registration)
                            || registration.ActorID != mutation.ActorID)
                        {
                            continue;
                        }

                        if (!TryAddTickerNow(mutation.ActorID, mutation.TickerID))
                        {
                            Debug.Warning($"Discarding ticker {mutation.TickerID} for retired actor {mutation.ActorID}.");
                            RemoveRegistration(mutation.TickerID);
                            continue;
                        }
                        break;
                    case TickerMutationKind.Remove:
                        RemoveTickerNow(mutation.ActorID, mutation.TickerID);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mutation.Kind), mutation.Kind, "Unknown ticker mutation kind.");
                }
            }
        }

        // ── Public API (for direct callers in Game.Server) ──────────────────────────

        /// <summary>Adds a ticker to an actor.</summary>
        /// <param name="actor">The actor receiving the ticker.</param>
        /// <param name="tickInterval">The interval between ticker callbacks.</param>
        /// <param name="duration">The ticker duration.</param>
        /// <param name="onTick">The callback invoked on each tick.</param>
        /// <param name="onExpire">The callback invoked when the ticker expires.</param>
        /// <param name="tag">An optional ticker tag.</param>
        /// <returns>The ticker identifier, or -1 when the ticker cannot be added.</returns>
        public int AddTicker(Actor actor, float tickInterval, float duration = float.MaxValue,
            Action<Actor> onTick = null, Action<Actor> onExpire = null, string tag = null)
        {
            if (actor == null || !TryFindActorEntity(actor.ID, out _))
            {
                if (actor != null)
                    Debug.Warning($"Could not find ECS entity for actor {actor.Name} (ID: {actor.ID})");
                return -1;
            }

            // If a tag is supplied and a ticker with that tag already exists, skip (idempotent).
            if (tag != null && _tagIndex.ContainsKey((actor.ID, tag)))
                return -1;

            int tickerID = checked(_nextTickerID++);
            _tickers[tickerID] = new TickerRegistration
            {
                ActorID = actor.ID,
                Tag = tag,
                TickInterval = tickInterval,
                Duration = duration,
                onTick = onTick,
                onExpire = onExpire
            };
            if (tag != null)
                _tagIndex[(actor.ID, tag)] = tickerID;
            if (!_actorTickerIDs.TryGetValue(actor.ID, out var list))
                _actorTickerIDs[actor.ID] = list = new List<int>();
            list.Add(tickerID);

            if (ServerECSManager.IsMutationBoundaryActive)
            {
                _pendingTickerIDs.Add(tickerID);
                try
                {
                    _pendingMutations.Enqueue(new TickerMutation(TickerMutationKind.Add, actor.ID, tickerID));
                }
                catch
                {
                    RemoveRegistration(tickerID);
                    throw;
                }

                return tickerID;
            }

            if (!TryAddTickerNow(actor.ID, tickerID))
            {
                RemoveRegistration(tickerID);
                Debug.Warning($"Could not add ticker {tickerID} to actor {actor.Name} (ID: {actor.ID})");
                return -1;
            }

            return tickerID;
        }

        private bool TryAddTickerNow(Guid actorID, int tickerID)
        {
            if (!_tickers.TryGetValue(tickerID, out var registration)
                || registration.ActorID != actorID
                || !TryFindActorEntity(actorID, out var entity)
                || !_entityManager.HasBuffer<Systems.TickerElement>(entity))
            {
                return false;
            }

            _entityManager.GetBuffer<Systems.TickerElement>(entity).Add(new Systems.TickerElement
            {
                TickerID = tickerID,
                RemainingDuration = registration.Duration,
                TickInterval = registration.TickInterval,
                TimeSinceLastTick = 0f
            });
            return true;
        }

        private void RemoveTickerNow(Guid actorID, int tickerID)
        {
            if (TryFindActorEntity(actorID, out var entity)
                && _entityManager.HasBuffer<Systems.TickerElement>(entity))
            {
                var buffer = _entityManager.GetBuffer<Systems.TickerElement>(entity);
                for (int i = buffer.Length - 1; i >= 0; i--)
                {
                    if (buffer[i].TickerID == tickerID)
                        buffer.RemoveAt(i);
                }
            }

            if (_tickers.TryGetValue(tickerID, out var registration)
                && registration.ActorID == actorID)
            {
                RemoveRegistration(tickerID);
            }
        }

        private void RemoveRegistration(int tickerID)
        {
            _pendingTickerIDs.Remove(tickerID);
            if (!_tickers.Remove(tickerID, out var registration))
                return;

            if (registration.Tag != null)
                _tagIndex.Remove((registration.ActorID, registration.Tag));
            if (_actorTickerIDs.TryGetValue(registration.ActorID, out var list))
            {
                list.Remove(tickerID);
                if (list.Count == 0)
                    _actorTickerIDs.Remove(registration.ActorID);
            }
        }

        private bool TryFindActorEntity(Guid actorID, out Entity actorEntity)
        {
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    if (_entityManager.GetComponentData<Systems.ActorComponent>(entity).ActorID != actorID)
                        continue;

                    actorEntity = entity;
                    return true;
                }
            }
            finally
            {
                entities.Dispose();
            }

            actorEntity = Entity.Null;
            return false;
        }

        /// <summary>Removes a ticker from an actor.</summary>
        /// <param name="actor">The actor owning the ticker.</param>
        /// <param name="tickerID">The ticker identifier.</param>
        public void RemoveTicker(Actor actor, int tickerID)
        {
            if (actor == null
                || !_tickers.TryGetValue(tickerID, out var registration)
                || registration.ActorID != actor.ID)
            {
                return;
            }

            if (ServerECSManager.IsMutationBoundaryActive)
            {
                _pendingTickerIDs.Remove(tickerID);
                RemoveRegistration(tickerID);
                _pendingMutations.Enqueue(new TickerMutation(TickerMutationKind.Remove, actor.ID, tickerID));
                return;
            }

            RemoveTickerNow(actor.ID, tickerID);
        }

        /// <summary>Removes the ticker with a tag from an actor.</summary>
        /// <param name="actor">The actor owning the ticker.</param>
        /// <param name="tag">The ticker tag.</param>
        public void RemoveTickerByTag(Actor actor, string tag)
        {
            if (actor == null || tag == null)
                return;

            if (_tagIndex.TryGetValue((actor.ID, tag), out int tickerID))
                RemoveTicker(actor, tickerID);
        }

        /// <summary>Determines whether an actor has a ticker with a tag.</summary>
        /// <param name="actorID">The actor identifier.</param>
        /// <param name="tag">The ticker tag.</param>
        /// <returns><see langword="true"/> when a matching ticker exists.</returns>
        public bool HasTicker(Guid actorID, string tag)
        {
            return _tagIndex.ContainsKey((actorID, tag));
        }

        // ── ECS event handlers ───────────────────────────────────────────────────────

        private void OnActorTick(Guid actorID, int tickerID)
        {
            if (!_tickers.TryGetValue(tickerID, out var reg) || reg.ActorID != actorID) return;
            if (reg.onTick == null) return;
            if (ServerActorManager.Instance == null
                || !ServerActorManager.Instance.TryGetActor(actorID, out var actor)) return;

            reg.onTick(actor);
        }

        private void OnActorTickerExpired(Guid actorID, int tickerID)
        {
            if (!_tickers.TryGetValue(tickerID, out var reg) || reg.ActorID != actorID) return;

            RemoveRegistration(tickerID);

            if (reg.onExpire != null
                && ServerActorManager.Instance != null
                && ServerActorManager.Instance.TryGetActor(actorID, out var actor))
                reg.onExpire(actor);
        }

        private void OnActorDespawned(Actor actor)
        {
            if (actor == null)
                return;

            if (!_actorTickerIDs.TryGetValue(actor.ID, out var list)) return;

            // Copy to avoid modifying the list while iterating.
            var ids = new List<int>(list);
            foreach (var tickerID in ids)
                RemoveRegistration(tickerID);
            _actorTickerIDs.Remove(actor.ID);
        }

        // ── Cross-assembly bridge (Game.Shared callers via ActorEndpoint events) ──────

        private void OnAddTickerRequest(Actor actor, float tickInterval, float duration,
            string tag, Action<Actor> onTick, Action<Actor> onExpire)
        {
            AddTicker(actor, tickInterval, duration, onTick, onExpire, tag);
        }

        private void OnRemoveTickerByTagRequest(Actor actor, string tag)
        {
            RemoveTickerByTag(actor, tag);
        }
    }
}
