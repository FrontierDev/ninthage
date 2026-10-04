using System;
using System.Collections.Generic;
using Game.Shared;
using Game.Shared.Networking;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Server
{
    public sealed class ServerTickerManager : MonoBehaviour
    {
        private static ServerTickerManager _instance;
        public static ServerTickerManager Instance => _instance;
        private static bool _initialized;
        public static bool IsInitialized => _initialized;

        private EntityManager entityManager;
        private EntityQuery actorComponentQuery;

        private readonly Dictionary<int, TickerRegistration> _tickers = new();
        private readonly Dictionary<(Guid, string), int> _tagIndex = new();
        private readonly Dictionary<Guid, List<int>> _actorTickerIds = new();
        private int _nextTickerId;

        private struct TickerRegistration
        {
            public Guid ActorId;
            public string Tag;
            public Action<Actor> OnTick;
            public Action<Actor> OnExpire;
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

            var world = ECS.ServerECSManager.DefaultWorld;
            entityManager = world.EntityManager;
            actorComponentQuery = entityManager.CreateEntityQuery(typeof(ECS.ActorComponent));

            ActorService.onServerActorTick += OnActorTick;
            ActorService.onServerActorTickerExpired += OnActorTickerExpired;
            ActorService.onServerAddTicker += OnAddTickerRequest;
            ActorService.onServerRemoveTickerByTag += OnRemoveTickerByTagRequest;
            ActorService.onServerActorDespawned += OnActorDespawned;

            _initialized = true;
        }

        private void OnDestroy()
        {
            ActorService.onServerActorTick -= OnActorTick;
            ActorService.onServerActorTickerExpired -= OnActorTickerExpired;
            ActorService.onServerAddTicker -= OnAddTickerRequest;
            ActorService.onServerRemoveTickerByTag -= OnRemoveTickerByTagRequest;
            ActorService.onServerActorDespawned -= OnActorDespawned;

            _tickers.Clear();
            _tagIndex.Clear();
            _actorTickerIds.Clear();
            _instance = null;
            _initialized = false;
        }

        // ── Public API (for direct callers in Game.Server) ──────────────────────────

        public int AddTicker(Actor actor, float tickInterval, float duration = float.MaxValue,
            Action<Actor> onTick = null, Action<Actor> onExpire = null, string tag = null)
        {
            var entities = actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = entityManager.GetComponentData<ECS.ActorComponent>(entity);
                    if (component.ActorId != actor.Id) continue;

                    // If a tag is supplied and a ticker with that tag already exists, skip (idempotent).
                    if (tag != null && _tagIndex.ContainsKey((actor.Id, tag)))
                        return -1;

                    int tickerId = _nextTickerId++;

                    entityManager.GetBuffer<ECS.TickerElement>(entity).Add(new ECS.TickerElement
                    {
                        TickerId = tickerId,
                        RemainingDuration = duration,
                        TickInterval = tickInterval,
                        TimeSinceLastTick = 0f
                    });

                    _tickers[tickerId] = new TickerRegistration
                    {
                        ActorId = actor.Id,
                        Tag = tag,
                        OnTick = onTick,
                        OnExpire = onExpire
                    };

                    if (tag != null)
                        _tagIndex[(actor.Id, tag)] = tickerId;

                    if (!_actorTickerIds.TryGetValue(actor.Id, out var list))
                        _actorTickerIds[actor.Id] = list = new List<int>();
                    list.Add(tickerId);

                    return tickerId;
                }
            }
            finally
            {
                entities.Dispose();
            }

            Debug.Warning($"Could not find ECS entity for actor {actor.Name} (ID: {actor.Id})");
            return -1;
        }

        public void RemoveTicker(Actor actor, int tickerId)
        {
            var entities = actorComponentQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var component = entityManager.GetComponentData<ECS.ActorComponent>(entity);
                    if (component.ActorId != actor.Id) continue;

                    var buffer = entityManager.GetBuffer<ECS.TickerElement>(entity);
                    for (int i = buffer.Length - 1; i >= 0; i--)
                    {
                        if (buffer[i].TickerId == tickerId)
                            buffer.RemoveAt(i);
                    }

                    if (_tickers.TryGetValue(tickerId, out var reg))
                    {
                        if (reg.Tag != null)
                            _tagIndex.Remove((actor.Id, reg.Tag));
                        _tickers.Remove(tickerId);
                    }
                    return;
                }
            }
            finally
            {
                entities.Dispose();
            }
        }

        public void RemoveTickerByTag(Actor actor, string tag)
        {
            if (_tagIndex.TryGetValue((actor.Id, tag), out int tickerId))
                RemoveTicker(actor, tickerId);
        }

        public bool HasTicker(Guid actorId, string tag)
        {
            return _tagIndex.ContainsKey((actorId, tag));
        }

        // ── ECS event handlers ───────────────────────────────────────────────────────

        private void OnActorTick(Guid actorId, int tickerId)
        {
            if (!_tickers.TryGetValue(tickerId, out var reg)) return;
            if (reg.OnTick == null) return;
            if (!ServerActorManager.Instance.TryGetActor(actorId, out var actor)) return;

            reg.OnTick(actor);
        }

        private void OnActorTickerExpired(Guid actorId, int tickerId)
        {
            if (!_tickers.TryGetValue(tickerId, out var reg)) return;

            if (reg.OnExpire != null && ServerActorManager.Instance.TryGetActor(actorId, out var actor))
                reg.OnExpire(actor);

            if (reg.Tag != null)
                _tagIndex.Remove((actorId, reg.Tag));
            if (_actorTickerIds.TryGetValue(actorId, out var list))
                list.Remove(tickerId);
            _tickers.Remove(tickerId);
        }

        private void OnActorDespawned(Actor actor)
        {
            if (!_actorTickerIds.TryGetValue(actor.Id, out var list)) return;

            // Copy to avoid modifying the list while iterating.
            var ids = new List<int>(list);
            foreach (var tickerId in ids)
            {
                if (_tickers.TryGetValue(tickerId, out var reg))
                {
                    if (reg.Tag != null)
                        _tagIndex.Remove((actor.Id, reg.Tag));
                    _tickers.Remove(tickerId);
                }
            }
            _actorTickerIds.Remove(actor.Id);
        }

        // ── Cross-assembly bridge (Game.Shared callers via ActorService events) ──────

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
