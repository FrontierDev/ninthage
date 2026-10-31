using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>Owns authoritative simulation-tick logout eligibility for live actors.</summary>
    public sealed class ServerLogoutEligibilityManager : MonoBehaviour
    {
        private static ServerLogoutEligibilityManager _instance;

        /// <summary>Gets the active server logout-eligibility manager.</summary>
        public static ServerLogoutEligibilityManager Instance => _instance;

        /// <summary>Gets whether the logout-eligibility manager is initialized.</summary>
        public static bool IsInitialized => _instance != null;

        private readonly Dictionary<Guid, LogoutEligibilityState> _states = new();
        private long _combatLockTicks;
        private long _hostileDamageLockTicks;

        private struct LogoutEligibilityState
        {
            public long CombatLockDeadlineTickID;
            public long HostileDamageLockDeadlineTickID;
        }

        private void Awake()
        {
            if (_instance != null)
                throw new InvalidOperationException("ServerLogoutEligibilityManager is already initialized.");
            if (!ServerSimulationClock.IsRunning)
                throw new InvalidOperationException(
                    "Server logout eligibility requires the authoritative simulation clock to be running.");
            if (!ServerActorManager.IsInitialized)
                throw new InvalidOperationException(
                    "Server logout eligibility requires the server actor manager to be initialized.");
            if (!GameConfigurationManager.IsInitialized || GameConfigurationManager.Config == null)
                throw new InvalidOperationException(
                    "Server logout eligibility requires initialized game configuration.");

            GameConfiguration configuration = GameConfigurationManager.Config;
            _combatLockTicks = ServerSimulationClock.GetNonShorteningTickCount(
                configuration.CombatLockSeconds,
                configuration.ServerSimulationTickDuration);
            _hostileDamageLockTicks = ServerSimulationClock.GetNonShorteningTickCount(
                configuration.HostileDamageLockSeconds,
                configuration.ServerSimulationTickDuration);
            if (_combatLockTicks <= 0 || _hostileDamageLockTicks <= 0)
            {
                throw new InvalidOperationException(
                    "Server logout eligibility lock durations must convert to positive tick counts.");
            }

            ActorEndpoint.onServerHostileCombatActivity += OnHostileCombatActivity;
            ActorEndpoint.onServerHostileDamageCommitted += OnHostileDamageCommitted;
            ActorEndpoint.onServerActorDespawned += OnActorDespawned;
            _instance = this;
        }

        private void OnDestroy()
        {
            if (!ReferenceEquals(_instance, this))
                return;

            ActorEndpoint.onServerHostileCombatActivity -= OnHostileCombatActivity;
            ActorEndpoint.onServerHostileDamageCommitted -= OnHostileDamageCommitted;
            ActorEndpoint.onServerActorDespawned -= OnActorDespawned;
            _states.Clear();
            _instance = null;
        }

        /// <summary>Gets whether the actor is currently free of authoritative logout locks.</summary>
        /// <param name="actorID">The live actor identifier.</param>
        /// <returns><see langword="true"/> when all authoritative logout locks have expired.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="actorID"/> is empty.</exception>
        internal bool CanCompleteLogout(Guid actorID)
        {
            ValidateActorID(actorID);
            long currentTickID = ServerSimulationClock.CurrentTickID;
            return GetEarliestLogoutCompletionTickID(actorID, currentTickID) <= currentTickID;
        }

        /// <summary>Gets the earliest authoritative simulation tick at which the actor's logout locks can be clear.</summary>
        /// <param name="actorID">The live actor identifier.</param>
        /// <returns>The aggregate lock-completion tick.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="actorID"/> is empty.</exception>
        internal long GetEarliestLogoutCompletionTickID(Guid actorID)
        {
            ValidateActorID(actorID);
            long currentTickID = ServerSimulationClock.CurrentTickID;
            return GetEarliestLogoutCompletionTickID(actorID, currentTickID);
        }

        /// <summary>Determines whether an actor is standing in an authored public safe-logout location.</summary>
        /// <param name="actor">The live actor to evaluate.</param>
        /// <returns><see langword="true"/> when the actor is within a matching authored safe-logout radius.</returns>
        internal bool IsInSafeLogoutLocation(Actor actor)
        {
            if (actor == null)
                return false;

            ActorSpatialContext context = actor.SpatialContext;
            if (!context.IsValid || context.InstanceID != Guid.Empty)
                return false;
            if (ServerActorManager.Instance == null
                || !ServerActorManager.Instance.TryGetAcceptedWorldPosition(actor.ID, out WorldPosition position))
            {
                return false;
            }

            WorldLocationDefinitionLibrary library = WorldLocationDefinitionLibrary.Instance;
            if (library == null)
                return false;

            IReadOnlyList<WorldLocationDefinition> definitions = library.GetAllDefinitions();
            foreach (WorldLocationDefinition definition in definitions)
            {
                if (definition == null
                    || !definition.IsValid()
                    || (definition.Purposes & WorldLocationPurpose.SafeLogout) == 0
                    || !StringComparer.Ordinal.Equals(definition.MapID, context.MapID))
                {
                    continue;
                }

                double deltaX = position.x - definition.PositionX;
                double deltaY = position.y - definition.PositionY;
                double deltaZ = position.z - definition.PositionZ;
                double distanceSquared = deltaX * deltaX
                    + deltaY * deltaY
                    + deltaZ * deltaZ;
                double radius = definition.SafeLogoutRadius;
                if (distanceSquared <= radius * radius)
                    return true;
            }

            return false;
        }

        private void OnHostileCombatActivity(Actor source, Actor target)
        {
            if (source == null || target == null)
                return;

            Guid sourceID = GetRegisteredActorID(source);
            Guid targetID = GetRegisteredActorID(target);
            long currentTickID = ServerSimulationClock.CurrentTickID;
            ExtendCombatLock(sourceID, currentTickID);
            ExtendCombatLock(targetID, currentTickID);
        }

        private void OnHostileDamageCommitted(Actor source, Actor target)
        {
            if (source == null || target == null)
                return;

            Guid sourceID = GetRegisteredActorID(source);
            Guid targetID = GetRegisteredActorID(target);
            long currentTickID = ServerSimulationClock.CurrentTickID;
            ExtendCombatLock(sourceID, currentTickID);
            ExtendCombatLock(targetID, currentTickID);
            ExtendHostileDamageLock(targetID, currentTickID);
        }

        private void OnActorDespawned(Actor actor)
        {
            if (actor == null)
                return;

            _states.Remove(GetRegisteredActorID(actor));
        }

        private long GetEarliestLogoutCompletionTickID(Guid actorID, long currentTickID)
        {
            if (!_states.TryGetValue(actorID, out LogoutEligibilityState state))
                return currentTickID;

            return Math.Max(
                currentTickID,
                Math.Max(state.CombatLockDeadlineTickID, state.HostileDamageLockDeadlineTickID));
        }

        private void ExtendCombatLock(Guid actorID, long currentTickID)
        {
            LogoutEligibilityState state = GetState(actorID);
            long deadlineTickID = checked(currentTickID + _combatLockTicks);
            if (deadlineTickID > state.CombatLockDeadlineTickID)
                state.CombatLockDeadlineTickID = deadlineTickID;

            _states[actorID] = state;
        }

        private void ExtendHostileDamageLock(Guid actorID, long currentTickID)
        {
            LogoutEligibilityState state = GetState(actorID);
            long deadlineTickID = checked(currentTickID + _hostileDamageLockTicks);
            if (deadlineTickID > state.HostileDamageLockDeadlineTickID)
                state.HostileDamageLockDeadlineTickID = deadlineTickID;

            _states[actorID] = state;
        }

        private LogoutEligibilityState GetState(Guid actorID)
        {
            return _states.TryGetValue(actorID, out LogoutEligibilityState state)
                ? state
                : default;
        }

        private static Guid GetRegisteredActorID(Actor actor)
        {
            Guid actorID = actor.ID;
            ValidateActorID(actorID);
            return actorID;
        }

        private static void ValidateActorID(Guid actorID)
        {
            if (actorID == Guid.Empty)
                throw new ArgumentException("A nonempty actor ID is required.", nameof(actorID));
        }
    }
}
