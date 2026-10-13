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
        private int _nextTickerID;

        private struct TickerRegistration
        {
            public Guid ActorID;
            public string Tag;
            public Action<Actor> onTick;
            public Action<Actor> onExpire;
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
            _instance = null;
            _initialized = false;
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
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                    if (component.ActorID != actor.ID) continue;

                    // If a tag is supplied and a ticker with that tag already exists, skip (idempotent).
                    if (tag != null && _tagIndex.ContainsKey((actor.ID, tag)))
                        return -1;

                    int tickerID = _nextTickerID++;

                    _entityManager.GetBuffer<Systems.TickerElement>(entity).Add(new Systems.TickerElement
                    {
                        TickerID = tickerID,
                        RemainingDuration = duration,
                        TickInterval = tickInterval,
                        TimeSinceLastTick = 0f
                    });

                    _tickers[tickerID] = new TickerRegistration
                    {
                        ActorID = actor.ID,
                        Tag = tag,
                        onTick = onTick,
                        onExpire = onExpire
                    };

                    if (tag != null)
                        _tagIndex[(actor.ID, tag)] = tickerID;

                    if (!_actorTickerIDs.TryGetValue(actor.ID, out var list))
                        _actorTickerIDs[actor.ID] = list = new List<int>();
                    list.Add(tickerID);

                    return tickerID;
                }
            }
            finally
            {
                entities.Dispose();
            }

            Debug.Warning($"Could not find ECS entity for actor {actor.Name} (ID: {actor.ID})");
            return -1;
        }

        /// <summary>Removes a ticker from an actor.</summary>
        /// <param name="actor">The actor owning the ticker.</param>
        /// <param name="tickerID">The ticker identifier.</param>
        public void RemoveTicker(Actor actor, int tickerID)
        {
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = _entityManager.GetComponentData<Systems.ActorComponent>(entity);
                    if (component.ActorID != actor.ID) continue;

                    var buffer = _entityManager.GetBuffer<Systems.TickerElement>(entity);
                    for (int i = buffer.Length - 1; i >= 0; i--)
                    {
                        if (buffer[i].TickerID == tickerID)
                            buffer.RemoveAt(i);
                    }

                    if (_tickers.TryGetValue(tickerID, out var reg))
                    {
                        if (reg.Tag != null)
                            _tagIndex.Remove((actor.ID, reg.Tag));
                        _tickers.Remove(tickerID);
                    }
                    return;
                }
            }
            finally
            {
                entities.Dispose();
            }
        }

        /// <summary>Removes the ticker with a tag from an actor.</summary>
        /// <param name="actor">The actor owning the ticker.</param>
        /// <param name="tag">The ticker tag.</param>
        public void RemoveTickerByTag(Actor actor, string tag)
        {
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
            if (!_tickers.TryGetValue(tickerID, out var reg)) return;
            if (reg.onTick == null) return;
            if (!ServerActorManager.Instance.TryGetActor(actorID, out var actor)) return;

            reg.onTick(actor);
        }

        private void OnActorTickerExpired(Guid actorID, int tickerID)
        {
            if (!_tickers.TryGetValue(tickerID, out var reg)) return;

            if (reg.onExpire != null && ServerActorManager.Instance.TryGetActor(actorID, out var actor))
                reg.onExpire(actor);

            if (reg.Tag != null)
                _tagIndex.Remove((actorID, reg.Tag));
            if (_actorTickerIDs.TryGetValue(actorID, out var list))
                list.Remove(tickerID);
            _tickers.Remove(tickerID);
        }

        private void OnActorDespawned(Actor actor)
        {
            if (!_actorTickerIDs.TryGetValue(actor.ID, out var list)) return;

            // Copy to avoid modifying the list while iterating.
            var ids = new List<int>(list);
            foreach (var tickerID in ids)
            {
                if (_tickers.TryGetValue(tickerID, out var reg))
                {
                    if (reg.Tag != null)
                        _tagIndex.Remove((actor.ID, reg.Tag));
                    _tickers.Remove(tickerID);
                }
            }
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
