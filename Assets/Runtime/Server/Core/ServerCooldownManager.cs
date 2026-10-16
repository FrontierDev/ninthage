using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Networking;
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>Maintains server-authoritative actor cooldowns.</summary>
    public sealed class ServerCooldownManager : MonoBehaviour
    {
        private static ServerCooldownManager _instance;
        /// <summary>Gets the active server cooldown manager.</summary>
        public static ServerCooldownManager Instance => _instance;
        private static bool _initialized;
        /// <summary>Gets whether the cooldown manager is initialized.</summary>
        public static bool IsInitialized => _initialized;

        /// <summary>The identifier used for the global cooldown.</summary>
        public const string GCD_ID = "GCD";

        private EntityManager _entityManager;
        private readonly ServerSimulationMutationQueue<CooldownMutation> _pendingMutations = new();
        private readonly Dictionary<(Guid ActorID, string SpellID), float> _pendingCooldowns = new();
        private CooldownMutation[] _commitSnapshot;

        private readonly struct CooldownMutation
        {
            internal CooldownMutation(Guid actorID, string spellID, float duration)
            {
                ActorID = actorID;
                SpellID = spellID;
                Duration = duration;
            }

            internal Guid ActorID { get; }
            internal string SpellID { get; }
            internal float Duration { get; }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Error("Multiple instances of ServerCooldownManager detected! Destroying duplicate.");
                Destroy(this);
                return;
            }
            _instance = this;

            var world = ServerECSManager.DefaultWorld;
            _entityManager = world.EntityManager;

            ActorEndpoint.onServerStartCooldown += StartCooldown;

            _initialized = true;
        }

        private void OnDestroy()
        {
            ActorEndpoint.onServerStartCooldown -= StartCooldown;

            _pendingMutations.Clear();
            _pendingCooldowns.Clear();
            _commitSnapshot = null;
            _instance = null;
            _initialized = false;
        }

        internal void PreparePendingMutations()
        {
            _commitSnapshot = _pendingMutations.TakeSnapshot();
            _pendingCooldowns.Clear();
        }

        internal void CommitPendingMutations()
        {
            CooldownMutation[] mutations = _commitSnapshot ?? _pendingMutations.TakeSnapshot();
            _commitSnapshot = null;
            foreach (CooldownMutation mutation in mutations)
                StartCooldownNow(mutation.ActorID, mutation.SpellID, mutation.Duration);
        }

        /// <summary>Starts or refreshes a cooldown for an actor.</summary>
        /// <param name="actorID">The actor receiving the cooldown.</param>
        /// <param name="spellID">The spell or cooldown identifier.</param>
        /// <param name="duration">The cooldown duration in seconds.</param>
        public void StartCooldown(Guid actorID, string spellID, float duration)
        {
            if (ServerECSManager.IsMutationBoundaryActive)
            {
                if (!TryGetAuthoritativeActorEntity(actorID, out _))
                    return;

                _pendingCooldowns[(actorID, spellID)] = duration;
                _pendingMutations.Enqueue(new CooldownMutation(actorID, spellID, duration));
                return;
            }

            StartCooldownNow(actorID, spellID, duration);
        }

        private void StartCooldownNow(Guid actorID, string spellID, float duration)
        {
            if (!TryGetAuthoritativeActorEntity(actorID, out Entity entity))
                return;

            var fixedSpellID = new Unity.Collections.FixedString64Bytes(spellID);
            var buffer = _entityManager.GetBuffer<Systems.CooldownElement>(entity);

            // Update existing entry or append a new one.
            bool found = false;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].SpellID == fixedSpellID)
                {
                    buffer[i] = new Systems.CooldownElement
                    {
                        SpellID = fixedSpellID,
                        RemainingCooldown = duration
                    };
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                buffer.Add(new Systems.CooldownElement
                {
                    SpellID = fixedSpellID,
                    RemainingCooldown = duration
                });
            }
        }

        /// <summary>Determines whether an actor currently has a cooldown.</summary>
        /// <param name="actorID">The actor to query.</param>
        /// <param name="spellID">The spell or cooldown identifier.</param>
        /// <returns><see langword="true"/> when the cooldown is active.</returns>
        public bool IsOnCooldown(Guid actorID, string spellID)
        {
            return GetRemainingCooldown(actorID, spellID) > 0f;
        }

        /// <summary>Gets the remaining duration of an actor's cooldown.</summary>
        /// <param name="actorID">The actor to query.</param>
        /// <param name="spellID">The spell or cooldown identifier.</param>
        /// <returns>The remaining duration in seconds, or zero when inactive.</returns>
        public float GetRemainingCooldown(Guid actorID, string spellID)
        {
            if (!TryGetAuthoritativeActorEntity(actorID, out Entity entity))
                return 0f;

            var fixedSpellID = new Unity.Collections.FixedString64Bytes(spellID);
            float pendingCooldown = _pendingCooldowns.TryGetValue((actorID, spellID), out float pending)
                ? pending
                : 0f;

            var buffer = _entityManager.GetBuffer<Systems.CooldownElement>(entity, true);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].SpellID == fixedSpellID)
                    return Mathf.Max(buffer[i].RemainingCooldown, pendingCooldown);
            }

            return pendingCooldown;
        }

        private bool TryGetAuthoritativeActorEntity(Guid actorID, out Entity entity)
        {
            if (ServerActorManager.Instance == null
                || !ServerActorManager.Instance.TryGetActorEntity(actorID, out entity)
                || !_entityManager.Exists(entity)
                || !_entityManager.HasComponent<Systems.ActorComponent>(entity)
                || !_entityManager.HasBuffer<Systems.CooldownElement>(entity))
            {
                entity = Entity.Null;
                return false;
            }

            return _entityManager.GetComponentData<Systems.ActorComponent>(entity).ActorID == actorID;
        }
    }
}
