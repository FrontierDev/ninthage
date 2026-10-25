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
        /// <summary>Gets whether the cooldown manager is initialized.</summary>
        public static bool IsInitialized => _instance != null;

        /// <summary>The identifier used for the global cooldown.</summary>
        public const string GCD_ID = "GCD";

        private EntityManager _entityManager;
        private readonly ServerSimulationMutationQueue<CooldownMutation> _pendingMutations = new();
        private readonly Dictionary<(Guid ActorID, string SpellID), long?> _pendingCooldowns = new();
        private readonly List<(Guid ActorID, string SpellID)> _pendingCooldownKeysToRemove = new();
        private readonly List<CooldownMutation> _commitSnapshot = new();

        private enum CooldownMutationKind : byte
        {
            Start,
            Reduce,
            Increase,
            ClearAll,
        }

        private readonly struct CooldownMutation
        {
            internal CooldownMutation(Guid actorID, string spellID, float duration)
            {
                Kind = CooldownMutationKind.Start;
                ActorID = actorID;
                SpellID = spellID;
                Duration = duration;
                DurationTicks = default;
            }

            internal CooldownMutation(Guid actorID)
            {
                Kind = CooldownMutationKind.ClearAll;
                ActorID = actorID;
                SpellID = default;
                Duration = default;
                DurationTicks = default;
            }

            internal CooldownMutation(
                CooldownMutationKind kind,
                Guid actorID,
                string spellID,
                long durationTicks)
            {
                Kind = kind;
                ActorID = actorID;
                SpellID = spellID;
                Duration = default;
                DurationTicks = durationTicks;
            }

            internal CooldownMutationKind Kind { get; }
            internal Guid ActorID { get; }
            internal string SpellID { get; }
            internal float Duration { get; }
            internal long DurationTicks { get; }
        }

        private void Awake()
        {
            if (_instance != null)
                throw new InvalidOperationException("ServerCooldownManager is already initialized.");

            var world = ServerECSManager.DefaultWorld;
            _entityManager = world.EntityManager;

            ActorEndpoint.onServerStartCooldown += StartCooldown;

            _instance = this;
        }

        private void OnDestroy()
        {
            if (!ReferenceEquals(_instance, this))
                return;

            ActorEndpoint.onServerStartCooldown -= StartCooldown;

            _pendingMutations.Clear();
            _pendingCooldowns.Clear();
            _pendingCooldownKeysToRemove.Clear();
            _commitSnapshot.Clear();
            _instance = null;
        }

        internal void PreparePendingMutations()
        {
            _pendingMutations.TakeSnapshot(_commitSnapshot);
            _pendingCooldowns.Clear();
        }

        internal void CommitPendingMutations()
        {
            try
            {
                foreach (CooldownMutation mutation in _commitSnapshot)
                {
                    switch (mutation.Kind)
                    {
                        case CooldownMutationKind.Start:
                            StartCooldownNow(mutation.ActorID, mutation.SpellID, mutation.Duration);
                            break;
                        case CooldownMutationKind.Reduce:
                            ReduceCooldownNow(mutation.ActorID, mutation.SpellID, mutation.DurationTicks);
                            break;
                        case CooldownMutationKind.Increase:
                            IncreaseCooldownNow(mutation.ActorID, mutation.SpellID, mutation.DurationTicks);
                            break;
                        case CooldownMutationKind.ClearAll:
                            ClearAllCooldownsNow(mutation.ActorID);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(
                                nameof(mutation.Kind),
                                mutation.Kind,
                                "Unknown cooldown mutation kind.");
                    }
                }
            }
            finally
            {
                _commitSnapshot.Clear();
            }
        }

        /// <summary>Starts or refreshes a cooldown for an actor.</summary>
        /// <param name="actorID">The actor receiving the cooldown.</param>
        /// <param name="spellID">The spell or cooldown identifier.</param>
        /// <param name="duration">The cooldown duration in seconds.</param>
        public void StartCooldown(Guid actorID, string spellID, float duration)
        {
            if (string.IsNullOrWhiteSpace(spellID))
                throw new ArgumentException("A cooldown identifier is required.", nameof(spellID));
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(duration),
                    "A cooldown duration must be finite and strictly positive.");
            }

            if (ServerECSManager.IsMutationBoundaryActive)
            {
                if (!TryGetAuthoritativeActorEntity(actorID, out _))
                    return;

                var durationTicks = ServerSimulationClock.GetNonShorteningTickCount(
                    duration,
                    GameConfigurationManager.Config.ServerSimulationTickDuration);
                var firstProgressTickID = GetFirstCooldownProgressTickID();
                var expiryTickID = checked(firstProgressTickID + durationTicks - 1L);
                _pendingCooldowns[(actorID, spellID)] = expiryTickID;
                _pendingMutations.Enqueue(new CooldownMutation(actorID, spellID, duration));
                return;
            }

            StartCooldownNow(actorID, spellID, duration);
        }

        /// <summary>Reduces an actor's active cooldown by a duration.</summary>
        /// <param name="actorID">The actor owning the cooldown.</param>
        /// <param name="spellID">The spell or cooldown identifier.</param>
        /// <param name="duration">The duration to remove in seconds.</param>
        /// <returns><see langword="true"/> when an active cooldown was adjusted.</returns>
        public bool ReduceCooldown(Guid actorID, string spellID, float duration)
        {
            if (string.IsNullOrWhiteSpace(spellID))
                throw new ArgumentException("A cooldown identifier is required.", nameof(spellID));
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(duration),
                    "A cooldown duration must be finite and strictly positive.");
            }

            var durationTicks = ServerSimulationClock.GetNonShorteningTickCount(
                duration,
                GameConfigurationManager.Config.ServerSimulationTickDuration);
            if (ServerECSManager.IsMutationBoundaryActive)
            {
                if (!TryGetAuthoritativeActorEntity(actorID, out _)
                    || !TryGetPendingCooldownExpiry(actorID, spellID, out long currentExpiryTickID))
                {
                    return false;
                }

                var newExpiryTickID = currentExpiryTickID - durationTicks;
                _pendingCooldowns[(actorID, spellID)] = newExpiryTickID > ServerSimulationClock.CurrentTickID
                    ? newExpiryTickID
                    : null;
                _pendingMutations.Enqueue(new CooldownMutation(
                    CooldownMutationKind.Reduce,
                    actorID,
                    spellID,
                    durationTicks));
                return true;
            }

            return ReduceCooldownNow(actorID, spellID, durationTicks);
        }

        /// <summary>Increases an actor's active cooldown by a duration.</summary>
        /// <param name="actorID">The actor owning the cooldown.</param>
        /// <param name="spellID">The spell or cooldown identifier.</param>
        /// <param name="duration">The duration to add in seconds.</param>
        /// <returns><see langword="true"/> when an active cooldown was extended.</returns>
        public bool IncreaseCooldown(Guid actorID, string spellID, float duration)
        {
            if (string.IsNullOrWhiteSpace(spellID))
                throw new ArgumentException("A cooldown identifier is required.", nameof(spellID));
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(duration),
                    "A cooldown duration must be finite and strictly positive.");
            }

            var durationTicks = ServerSimulationClock.GetNonShorteningTickCount(
                duration,
                GameConfigurationManager.Config.ServerSimulationTickDuration);
            if (ServerECSManager.IsMutationBoundaryActive)
            {
                if (!TryGetAuthoritativeActorEntity(actorID, out _)
                    || !TryGetPendingCooldownExpiry(actorID, spellID, out long currentExpiryTickID))
                {
                    return false;
                }

                _pendingCooldowns[(actorID, spellID)] = checked(currentExpiryTickID + durationTicks);
                _pendingMutations.Enqueue(new CooldownMutation(
                    CooldownMutationKind.Increase,
                    actorID,
                    spellID,
                    durationTicks));
                return true;
            }

            return IncreaseCooldownNow(actorID, spellID, durationTicks);
        }

        /// <summary>Clears all cooldowns belonging to an actor.</summary>
        /// <param name="actorID">The actor whose cooldowns should be cleared.</param>
        /// <returns><see langword="true"/> when the clear was accepted or completed.</returns>
        public bool ClearAllCooldowns(Guid actorID)
        {
            if (ServerECSManager.IsMutationBoundaryActive)
            {
                if (!TryGetAuthoritativeActorEntity(actorID, out Entity entity))
                    return false;

                _pendingCooldownKeysToRemove.Clear();
                try
                {
                    foreach (var pendingCooldown in _pendingCooldowns.Keys)
                    {
                        if (pendingCooldown.ActorID == actorID)
                            _pendingCooldownKeysToRemove.Add(pendingCooldown);
                    }

                    foreach (var pendingCooldown in _pendingCooldownKeysToRemove)
                        _pendingCooldowns.Remove(pendingCooldown);

                    var buffer = _entityManager.GetBuffer<Systems.CooldownElement>(entity, true);
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        _pendingCooldowns[(actorID, buffer[i].SpellID.ToString())] = null;
                    }

                    _pendingMutations.Enqueue(new CooldownMutation(actorID));
                }
                finally
                {
                    _pendingCooldownKeysToRemove.Clear();
                }

                return true;
            }

            return ClearAllCooldownsNow(actorID);
        }

        private void StartCooldownNow(Guid actorID, string spellID, float duration)
        {
            if (!TryGetAuthoritativeActorEntity(actorID, out Entity entity))
                return;

            var fixedSpellID = new Unity.Collections.FixedString64Bytes(spellID);
            var buffer = _entityManager.GetBuffer<Systems.CooldownElement>(entity);
            var firstProgressTickID = GetFirstCooldownProgressTickID();
            var durationTicks = ServerSimulationClock.GetNonShorteningTickCount(
                duration,
                GameConfigurationManager.Config.ServerSimulationTickDuration);
            var expiryTickID = checked(
                firstProgressTickID + durationTicks - 1L);

            // Update existing entry or append a new one.
            bool found = false;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].SpellID == fixedSpellID)
                {
                    buffer[i] = new Systems.CooldownElement
                    {
                        SpellID = fixedSpellID,
                        ExpiryTickID = expiryTickID
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
                    ExpiryTickID = expiryTickID
                });
            }

            _entityManager.SetComponentEnabled<Systems.CooldownElement>(
                entity,
                true);
        }

        private bool ReduceCooldownNow(Guid actorID, string spellID, long durationTicks)
        {
            if (!TryGetAuthoritativeActorEntity(actorID, out Entity entity))
                return false;

            var fixedSpellID = new Unity.Collections.FixedString64Bytes(spellID);
            var buffer = _entityManager.GetBuffer<Systems.CooldownElement>(entity);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].SpellID != fixedSpellID)
                    continue;

                if (buffer[i].ExpiryTickID <= ServerSimulationClock.CurrentTickID)
                    return false;

                var newExpiryTickID = buffer[i].ExpiryTickID - durationTicks;
                if (newExpiryTickID <= ServerSimulationClock.CurrentTickID)
                {
                    buffer.RemoveAt(i);
                    if (buffer.Length == 0)
                    {
                        _entityManager.SetComponentEnabled<Systems.CooldownElement>(
                            entity,
                            false);
                    }
                }
                else
                {
                    var cooldown = buffer[i];
                    cooldown.ExpiryTickID = newExpiryTickID;
                    buffer[i] = cooldown;
                }

                return true;
            }

            return false;
        }

        private bool IncreaseCooldownNow(Guid actorID, string spellID, long durationTicks)
        {
            if (!TryGetAuthoritativeActorEntity(actorID, out Entity entity))
                return false;

            var fixedSpellID = new Unity.Collections.FixedString64Bytes(spellID);
            var buffer = _entityManager.GetBuffer<Systems.CooldownElement>(entity);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].SpellID != fixedSpellID)
                    continue;

                if (buffer[i].ExpiryTickID <= ServerSimulationClock.CurrentTickID)
                    return false;

                var cooldown = buffer[i];
                cooldown.ExpiryTickID = checked(cooldown.ExpiryTickID + durationTicks);
                buffer[i] = cooldown;
                return true;
            }

            return false;
        }

        private bool ClearAllCooldownsNow(Guid actorID)
        {
            if (!TryGetAuthoritativeActorEntity(actorID, out Entity entity))
                return false;

            var buffer = _entityManager.GetBuffer<Systems.CooldownElement>(entity);
            buffer.Clear();
            _entityManager.SetComponentEnabled<Systems.CooldownElement>(entity, false);

            _pendingCooldownKeysToRemove.Clear();
            try
            {
                foreach (var pendingCooldown in _pendingCooldowns.Keys)
                {
                    if (pendingCooldown.ActorID == actorID)
                        _pendingCooldownKeysToRemove.Add(pendingCooldown);
                }

                foreach (var pendingCooldown in _pendingCooldownKeysToRemove)
                    _pendingCooldowns.Remove(pendingCooldown);
            }
            finally
            {
                _pendingCooldownKeysToRemove.Clear();
            }

            return true;
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
            var fixedTickDuration = GameConfigurationManager.Config.ServerSimulationTickDuration;

            if (_pendingCooldowns.TryGetValue((actorID, spellID), out long? pendingExpiryTickID))
            {
                if (!pendingExpiryTickID.HasValue)
                    return 0f;

                var pendingRemainingTicks = Math.Max(
                    0L,
                    pendingExpiryTickID.Value - ServerSimulationClock.CurrentTickID);
                return (float)(pendingRemainingTicks * (double)fixedTickDuration);
            }

            var buffer = _entityManager.GetBuffer<Systems.CooldownElement>(entity, true);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].SpellID == fixedSpellID)
                {
                    var remainingTicks = Math.Max(
                        0L,
                        buffer[i].ExpiryTickID - ServerSimulationClock.CurrentTickID);
                    var remainingSeconds = (float)(remainingTicks
                        * (double)fixedTickDuration);
                    return remainingSeconds;
                }
            }

            return 0f;
        }

        private bool TryGetPendingCooldownExpiry(
            Guid actorID,
            string spellID,
            out long expiryTickID)
        {
            if (_pendingCooldowns.TryGetValue((actorID, spellID), out long? pendingExpiryTickID))
            {
                expiryTickID = pendingExpiryTickID.GetValueOrDefault();
                return pendingExpiryTickID.HasValue
                    && expiryTickID > ServerSimulationClock.CurrentTickID;
            }

            if (!TryGetAuthoritativeActorEntity(actorID, out Entity entity))
            {
                expiryTickID = default;
                return false;
            }

            var fixedSpellID = new Unity.Collections.FixedString64Bytes(spellID);
            var buffer = _entityManager.GetBuffer<Systems.CooldownElement>(entity, true);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].SpellID == fixedSpellID)
                {
                    expiryTickID = buffer[i].ExpiryTickID;
                    return expiryTickID > ServerSimulationClock.CurrentTickID;
                }
            }

            expiryTickID = default;
            return false;
        }

        private static long GetFirstCooldownProgressTickID()
        {
            var currentSimulationTickID = ServerECSManager.CurrentSimulationTickID;
            if (currentSimulationTickID > 0)
                return currentSimulationTickID;

            return checked(ServerSimulationClock.CurrentTickID + 1L);
        }

        private bool TryGetAuthoritativeActorEntity(Guid actorID, out Entity entity)
        {
            return ServerActorManager.Instance.TryGetActorEntity(actorID, out entity);
        }
    }
}
