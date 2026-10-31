using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Networking;
using Game.Server.Networking.Master;
using Game.Server.Persistence;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Persistence;
using Game.Shared.Runtime.Actors;
using PurrNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.Server.Core
{
    public partial class ServerConnectionManager
    {
        private ServerLogoutEligibilityManager _logoutEligibilityManager;
        private Coroutine _logoutEligibilitySubscriptionCoroutine;

        private void BeginLogoutEligibilitySubscription()
        {
            if (_logoutEligibilityManager != null || _logoutEligibilitySubscriptionCoroutine != null)
                return;

            _logoutEligibilitySubscriptionCoroutine = StartCoroutine(SubscribeToLogoutEligibilityWhenReady());
        }

        private IEnumerator SubscribeToLogoutEligibilityWhenReady()
        {
            while (!ServerLogoutEligibilityManager.IsInitialized)
                yield return null;

            SubscribeToLogoutEligibilityIfReady();
            _logoutEligibilitySubscriptionCoroutine = null;
        }

        private void SubscribeToLogoutEligibilityIfReady()
        {
            if (_logoutEligibilityManager != null)
                return;
            if (!ServerLogoutEligibilityManager.IsInitialized)
                return;

            ServerLogoutEligibilityManager manager = ServerLogoutEligibilityManager.Instance;
            if (manager == null)
            {
                throw new InvalidOperationException(
                    "The server logout eligibility manager reported initialized without an instance.");
            }

            manager.LogoutEligibilityBlocked += OnLogoutEligibilityBlocked;
            _logoutEligibilityManager = manager;
        }

        private void UnsubscribeFromLogoutEligibility()
        {
            if (_logoutEligibilitySubscriptionCoroutine != null)
            {
                StopCoroutine(_logoutEligibilitySubscriptionCoroutine);
                _logoutEligibilitySubscriptionCoroutine = null;
            }

            if (_logoutEligibilityManager == null)
                return;

            _logoutEligibilityManager.LogoutEligibilityBlocked -= OnLogoutEligibilityBlocked;
            _logoutEligibilityManager = null;
        }

        /// <summary>Requests normal logout for the current admitted character of a player.</summary>
        /// <param name="playerID">The connected player requesting logout.</param>
        internal void RequestNormalLogout(PlayerID playerID)
        {
            if (!IsAcceptingCharacterControlPlane)
            {
                Debug.Warning($"Normal logout for player {playerID} was rejected because the server is shutting down.");
                return;
            }

            if (!TryGetSessionData(playerID, out PlayerSessionData session)
                || session == null
                || session.PlayerActor == null
                || session.LifecycleState != PlayerSessionData.SessionLifecycleState.Connected)
            {
                return;
            }

            SubscribeToLogoutEligibilityIfReady();
            ServerLogoutEligibilityManager eligibilityManager = _logoutEligibilityManager
                ?? throw new InvalidOperationException("The server logout eligibility manager is unavailable.");
            Actor actor = session.PlayerActor;
            if (!eligibilityManager.CanCompleteLogout(actor.ID))
                return;

            if (eligibilityManager.IsInSafeLogoutLocation(actor))
            {
                StartCharacterFinalization(
                    session.GameplaySessionID,
                    CharacterFinalizationDisposition.ReturnToCharacterSelection);
                return;
            }

            GameConfiguration configuration = GameConfigurationManager.Config;
            if (configuration == null)
                throw new InvalidOperationException("Game configuration is unavailable for normal logout.");

            long countdownTicks = ServerSimulationClock.GetNonShorteningTickCount(
                configuration.LogoutCountdownSeconds,
                configuration.ServerSimulationTickDuration);
            long currentTickID = ServerSimulationClock.CurrentTickID;
            long dueTickID = checked(currentTickID + countdownTicks);
            Guid gameplaySessionID = session.GameplaySessionID;

            session.LifecycleState = PlayerSessionData.SessionLifecycleState.LogoutCountdown;
            session.DeadlineTickID = dueTickID;
            try
            {
                session.DeadlineRegistration = ServerSimulationClock.RegisterDue(
                    dueTickID,
                    tick => OnLogoutCountdownDue(gameplaySessionID, tick.TickID));
            }
            catch
            {
                session.LifecycleState = PlayerSessionData.SessionLifecycleState.Connected;
                session.DeadlineTickID = 0;
                session.DeadlineRegistration = default;
                throw;
            }
        }

        private void OnLogoutEligibilityBlocked(Guid actorID)
        {
            foreach (PlayerSessionData session in _admittedSessionsByGameplaySessionID.Values)
            {
                if (session == null
                    || session.PlayerActor == null
                    || session.PlayerActor.ID != actorID)
                {
                    continue;
                }

                if (session.LifecycleState != PlayerSessionData.SessionLifecycleState.LogoutCountdown)
                    return;

                CancelSessionDeadline(session);
                session.LifecycleState = PlayerSessionData.SessionLifecycleState.Connected;
                return;
            }
        }

        private void OnLogoutCountdownDue(Guid gameplaySessionID, long callbackTickID)
        {
            if (!TryGetAdmittedSession(gameplaySessionID, out PlayerSessionData session)
                || session == null
                || session.LifecycleState != PlayerSessionData.SessionLifecycleState.LogoutCountdown
                || session.DeadlineTickID != callbackTickID)
            {
                return;
            }

            CancelSessionDeadline(session);
            if (!TryGetSessionData(session.PlayerID, out PlayerSessionData currentSession)
                || currentSession == null
                || currentSession.GameplaySessionID != gameplaySessionID
                || !IsPlayerConnected(session.PlayerID))
            {
                session.LifecycleState = PlayerSessionData.SessionLifecycleState.Connected;
                return;
            }

            if (currentSession.PlayerActor == null)
            {
                session.LifecycleState = PlayerSessionData.SessionLifecycleState.Connected;
                return;
            }

            ServerLogoutEligibilityManager eligibilityManager = _logoutEligibilityManager
                ?? throw new InvalidOperationException("The server logout eligibility manager is unavailable.");
            if (!eligibilityManager.CanCompleteLogout(currentSession.PlayerActor.ID))
            {
                session.LifecycleState = PlayerSessionData.SessionLifecycleState.Connected;
                return;
            }

            StartCharacterFinalization(
                gameplaySessionID,
                CharacterFinalizationDisposition.ReturnToCharacterSelection);
        }

        private List<Guid> SnapshotAdmittedGameplaySessionIDsForShutdown()
        {
            List<PlayerSessionData> sessions = new();
            foreach (PlayerSessionData session in _admittedSessionsByGameplaySessionID.Values)
            {
                if (session != null)
                    sessions.Add(session);
            }

            sessions.Sort((left, right) =>
            {
                int characterComparison = left.CharacterID.CompareTo(right.CharacterID);
                return characterComparison != 0
                    ? characterComparison
                    : left.GameplaySessionID.CompareTo(right.GameplaySessionID);
            });

            List<Guid> gameplaySessionIDs = new(sessions.Count);
            foreach (PlayerSessionData session in sessions)
                gameplaySessionIDs.Add(session.GameplaySessionID);

            return gameplaySessionIDs;
        }

        /// <summary>Finalizes all admitted characters sequentially for graceful shutdown.</summary>
        /// <param name="completed">Receives whether every admitted session settled successfully.</param>
        internal IEnumerator FinalizeAdmittedSessionsForShutdown(Action<bool> completed)
        {
            if (completed == null)
                throw new ArgumentNullException(nameof(completed));

            bool allSettled = true;
            try
            {
                foreach (Guid gameplaySessionID in SnapshotAdmittedGameplaySessionIDsForShutdown())
                {
                    if (!TryGetAdmittedSession(gameplaySessionID, out PlayerSessionData session)
                        || session == null)
                    {
                        continue;
                    }

                    CancelSessionDeadline(session);
                    if (!TryGetAdmittedSession(gameplaySessionID, out PlayerSessionData currentSession)
                        || !ReferenceEquals(currentSession, session))
                    {
                        continue;
                    }

                    if (session.LifecycleState == PlayerSessionData.SessionLifecycleState.Unsettled)
                    {
                        allSettled = false;
                        Debug.Error(
                            $"Shutdown finalization skipped unsettled gameplay session {gameplaySessionID} "
                            + $"for character {session.CharacterID}.");
                        continue;
                    }

                    if (session.LifecycleState == PlayerSessionData.SessionLifecycleState.Finalizing)
                    {
                        while (TryGetAdmittedSession(gameplaySessionID, out currentSession)
                            && ReferenceEquals(currentSession, session)
                            && currentSession.LifecycleState == PlayerSessionData.SessionLifecycleState.Finalizing)
                        {
                            yield return null;
                        }

                        if (TryGetAdmittedSession(gameplaySessionID, out currentSession)
                            && ReferenceEquals(currentSession, session))
                        {
                            allSettled = false;
                            Debug.Error(
                                $"Shutdown finalization left gameplay session {gameplaySessionID} "
                                + $"for character {session.CharacterID} in state {currentSession.LifecycleState}.");
                        }

                        continue;
                    }

                    bool finalizationStarted = true;
                    try
                    {
                        StartCharacterFinalization(
                            gameplaySessionID,
                            CharacterFinalizationDisposition.Shutdown);
                    }
                    catch (Exception exception)
                    {
                        finalizationStarted = false;
                        allSettled = false;
                        Debug.Error(
                            $"Shutdown finalization could not start for gameplay session {gameplaySessionID} "
                            + $"and character {session.CharacterID}: {exception.Message}");
                    }

                    if (!finalizationStarted)
                        continue;

                    while (TryGetAdmittedSession(gameplaySessionID, out currentSession)
                        && ReferenceEquals(currentSession, session)
                        && currentSession.LifecycleState == PlayerSessionData.SessionLifecycleState.Finalizing)
                    {
                        yield return null;
                    }

                    if (TryGetAdmittedSession(gameplaySessionID, out currentSession)
                        && ReferenceEquals(currentSession, session))
                    {
                        allSettled = false;
                        Debug.Error(
                            $"Shutdown finalization left gameplay session {gameplaySessionID} "
                            + $"for character {session.CharacterID} in state {currentSession.LifecycleState}.");
                    }
                }
            }
            finally
            {
                if (!allSettled)
                    LogRemainingShutdownSessions();
                completed(allSettled);
            }
        }

        private void LogRemainingShutdownSessions()
        {
            List<string> remainingSessions = new();
            foreach (KeyValuePair<Guid, PlayerSessionData> admittedSession in _admittedSessionsByGameplaySessionID)
            {
                if (admittedSession.Value != null)
                {
                    remainingSessions.Add(
                        $"{admittedSession.Key} (CharacterID: {admittedSession.Value.CharacterID}, "
                        + $"State: {admittedSession.Value.LifecycleState})");
                }
            }

            Debug.Error(
                "Graceful shutdown left admitted gameplay sessions unsettled: "
                + (remainingSessions.Count == 0 ? "<none>" : string.Join(", ", remainingSessions)));
        }

        private void StartDisconnectGrace(PlayerSessionData session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (!TryGetAdmittedSession(session.GameplaySessionID, out PlayerSessionData admittedSession)
                || !ReferenceEquals(admittedSession, session))
            {
                throw new InvalidOperationException("Disconnect grace requires an admitted session.");
            }
            if (session.HasPlayerAttachment)
                throw new InvalidOperationException("Disconnect grace requires a detached session.");
            if (session.PlayerActor == null)
                throw new InvalidOperationException("Disconnect grace requires a live player actor.");

            GameConfiguration configuration = GameConfigurationManager.Config;
            if (configuration == null)
                throw new InvalidOperationException("Game configuration is unavailable for disconnect grace.");

            long graceTicks = ServerSimulationClock.GetNonShorteningTickCount(
                configuration.DisconnectGraceSeconds,
                configuration.ServerSimulationTickDuration);
            long dueTickID = checked(ServerSimulationClock.CurrentTickID + graceTicks);
            session.LifecycleState = PlayerSessionData.SessionLifecycleState.DisconnectGrace;
            ScheduleDisconnectGraceDeadline(session, dueTickID);
        }

        private void ScheduleDisconnectGraceDeadline(
            PlayerSessionData session,
            long dueTickID)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (!TryGetAdmittedSession(session.GameplaySessionID, out PlayerSessionData admittedSession)
                || !ReferenceEquals(admittedSession, session))
            {
                throw new InvalidOperationException("Disconnect grace scheduling requires an admitted session.");
            }
            if (session.HasPlayerAttachment
                || session.LifecycleState != PlayerSessionData.SessionLifecycleState.DisconnectGrace)
            {
                throw new InvalidOperationException(
                    "Disconnect grace scheduling requires a detached DisconnectGrace session.");
            }

            long currentTickID = ServerSimulationClock.CurrentTickID;
            if (dueTickID <= currentTickID)
                throw new ArgumentOutOfRangeException(nameof(dueTickID), "The disconnect grace deadline must be in the future.");
            if (session.DeadlineRegistration.IsValid)
                throw new InvalidOperationException("The disconnect grace session already has a deadline registration.");

            Guid gameplaySessionID = session.GameplaySessionID;
            session.DeadlineTickID = dueTickID;
            try
            {
                session.DeadlineRegistration = ServerSimulationClock.RegisterDue(
                    dueTickID,
                    _ => OnDisconnectGraceDeadline(gameplaySessionID, dueTickID));
            }
            catch
            {
                session.DeadlineTickID = 0;
                session.DeadlineRegistration = default;
                throw;
            }
        }

        private void OnDisconnectGraceDeadline(Guid gameplaySessionID, long callbackDeadlineTickID)
        {
            if (!TryGetAdmittedSession(gameplaySessionID, out PlayerSessionData session)
                || session == null
                || session.LifecycleState != PlayerSessionData.SessionLifecycleState.DisconnectGrace
                || session.HasPlayerAttachment
                || session.DeadlineTickID != callbackDeadlineTickID)
            {
                return;
            }

            CancelSessionDeadline(session);
            if (session.PlayerActor == null
                || !ServerActorManager.IsInitialized
                || !ServerActorManager.Instance.TryGetActor(session.PlayerActor.ID, out Actor registeredActor)
                || !ReferenceEquals(registeredActor, session.PlayerActor))
            {
                MarkSessionUnsettled(
                    gameplaySessionID,
                    "the disconnected actor was no longer registered at grace expiry.");
                return;
            }

            ServerLogoutEligibilityManager eligibilityManager = ServerLogoutEligibilityManager.Instance;
            if (eligibilityManager == null)
            {
                MarkSessionUnsettled(
                    gameplaySessionID,
                    "the logout eligibility manager was unavailable at grace expiry.");
                return;
            }

            if (eligibilityManager.CanCompleteLogout(session.PlayerActor.ID))
            {
                StartCharacterFinalization(
                    gameplaySessionID,
                    CharacterFinalizationDisposition.CloseGameplaySession);
                return;
            }

            long currentTickID = ServerSimulationClock.CurrentTickID;
            long eligibilityTickID = eligibilityManager.GetEarliestLogoutCompletionTickID(session.PlayerActor.ID);
            long nextTickID = checked(currentTickID + 1);
            if (eligibilityTickID > nextTickID)
                nextTickID = eligibilityTickID;

            ScheduleDisconnectGraceDeadline(session, nextTickID);
        }

        private IEnumerator InitializeReclaimedPlayer(
            PlayerID newPlayerID,
            AuthenticatedSessionContext authenticatedSession)
        {
            if (!TryValidateReclaimedActor(
                    newPlayerID,
                    authenticatedSession,
                    out PlayerSessionData session,
                    out NetworkIdentity identity,
                    out string failureReason))
            {
                yield return HandleReclaimFailure(authenticatedSession, failureReason);
                yield break;
            }

            try
            {
                CancelSessionDeadline(session);
                AccountEndpoint.Client_PrepareCharacterReclaim(newPlayerID, session.CharacterData);

                if (ChunkSubscriptions.PLAYER_ACTORS.TryGetValue(
                        session.PlayerID,
                        out Actor oldRegisteredActor)
                    && ReferenceEquals(oldRegisteredActor, session.PlayerActor))
                {
                    ChunkSubscriptions.UnregisterPlayerActor(session.PlayerID);
                }

                Actor actor = session.PlayerActor;
                identity.GiveOwnership(newPlayerID);
                actor.SetOwner(newPlayerID);
                AttachPlayerToAdmittedSession(newPlayerID, session);

                ConnectedAccountContext accountContext = new(
                    authenticatedSession.Username,
                    authenticatedSession.GameplaySessionID,
                    authenticatedSession.AccountID,
                    authenticatedSession.HomeWorldID,
                    authenticatedSession.Revision,
                    authenticatedSession.WasReclaimed,
                    authenticatedSession.SelectedCharacterID);
                _connectedAccounts[newPlayerID] = accountContext;
                session.LifecycleState = PlayerSessionData.SessionLifecycleState.Connected;

                try
                {
                    PlayerReconnectStatePublisher.Publish(session, newPlayerID);
                }
                catch (Exception exception)
                {
                    Debug.Error(
                        $"Reconnect bootstrap for gameplay session {authenticatedSession.GameplaySessionID} failed after ownership transfer: "
                        + exception.Message);
                }
            }
            catch (Exception exception)
            {
                if (session.HasPlayerAttachment)
                    session.LifecycleState = PlayerSessionData.SessionLifecycleState.Connected;
                Debug.Error(
                    $"Reconnect ownership transfer for gameplay session {authenticatedSession.GameplaySessionID} failed: "
                    + exception.Message);
            }
        }

        private bool TryValidateReclaimedActor(
            PlayerID newPlayerID,
            AuthenticatedSessionContext authenticatedSession,
            out PlayerSessionData session,
            out NetworkIdentity identity,
            out string failureReason)
        {
            session = null;
            identity = null;
            failureReason = null;
            if (authenticatedSession == null)
            {
                failureReason = "the authenticated session context was null.";
                return false;
            }
            if (!authenticatedSession.SelectedCharacterID.HasValue
                || authenticatedSession.SelectedCharacterID.Value == Guid.Empty)
            {
                failureReason = "the reclaimed authentication had no selected character.";
                return false;
            }
            if (!TryGetAdmittedSession(authenticatedSession.GameplaySessionID, out session)
                || session == null)
            {
                failureReason = "the reclaimed gameplay session was not admitted locally.";
                return false;
            }
            if (session.LifecycleState != PlayerSessionData.SessionLifecycleState.DisconnectGrace)
            {
                failureReason = $"the local gameplay session was in lifecycle state {session.LifecycleState}.";
                return false;
            }
            if (session.HasPlayerAttachment)
            {
                failureReason = "the local gameplay session was already attached to a Player ID.";
                return false;
            }
            if (session.AccountID != authenticatedSession.AccountID)
            {
                failureReason = "the reclaimed Account ID did not match the admitted session.";
                return false;
            }
            if (session.CharacterID != authenticatedSession.SelectedCharacterID.Value)
            {
                failureReason = "the reclaimed Character ID did not match the admitted session.";
                return false;
            }
            if (session.PlayerActor == null || session.PlayerActor.ID == Guid.Empty)
            {
                failureReason = "the admitted session did not retain a live actor.";
                return false;
            }
            if (!ServerWorldManager.IsInitialized
                || !session.PlayerActor.SpatialContext.IsCompatibleWith(
                    ServerWorldManager.Instance.CurrentRuntimeContext))
            {
                failureReason = "the live actor did not belong to the current World runtime context.";
                return false;
            }
            if (!ServerActorManager.IsInitialized
                || !ServerActorManager.Instance.TryGetActor(session.PlayerActor.ID, out Actor registeredActor)
                || !ReferenceEquals(registeredActor, session.PlayerActor))
            {
                failureReason = "the live actor was not registered by the authoritative actor manager.";
                return false;
            }

            identity = session.PlayerActor.GetComponent<NetworkIdentity>();
            if (identity == null
                || !ServerActorManager.Instance.TryGetActorIdentity(session.PlayerActor.ID, out NetworkIdentity registeredIdentity)
                || !ReferenceEquals(registeredIdentity, identity))
            {
                failureReason = "the live actor did not resolve to its authoritative NetworkIdentity.";
                return false;
            }
            if (_gameplaySessionIDsByPlayerID.ContainsKey(newPlayerID))
            {
                failureReason = "the new Player ID already had a local admitted-session attachment.";
                return false;
            }

            return true;
        }

        private IEnumerator HandleReclaimFailure(
            AuthenticatedSessionContext authenticatedSession,
            string failureReason)
        {
            Debug.Error(
                $"Reclaimed gameplay session {authenticatedSession.GameplaySessionID} failed local actor validation: "
                + failureReason);

            PlayerSessionData admittedSession = null;
            bool hasAdmittedSession = TryGetAdmittedSession(
                authenticatedSession.GameplaySessionID,
                out admittedSession)
                && admittedSession != null;
            WorldPersistenceCoordinator coordinator = null;
            try
            {
                coordinator = ServerInitialization.PersistenceCoordinator;
            }
            catch (Exception exception)
            {
                Debug.Error($"Could not inspect local persistence during reclaim failure handling: {exception.Message}");
            }

            bool hasOwnedLease = coordinator != null
                && coordinator.TryGetCharacterCheckpointSettlement(
                    authenticatedSession.SelectedCharacterID.Value,
                    out Guid settledAccountID,
                    out _,
                    out _,
                    out _)
                && settledAccountID == authenticatedSession.AccountID;
            if (coordinator != null && hasOwnedLease)
            {
                Task<MasterSessionProtocol.CharacterLeaveResponse> leaveTask = null;
                try
                {
                    leaveTask = coordinator.LeaveAdmittedCharacterAsync(
                        authenticatedSession.GameplaySessionID,
                        authenticatedSession.SelectedCharacterID.Value,
                        CancellationToken.None);
                }
                catch (Exception exception)
                {
                    Debug.Error($"Reclaim failure could not start local character leave: {exception.Message}");
                }

                if (leaveTask != null)
                {
                    while (!leaveTask.IsCompleted)
                        yield return null;

                    bool leaveAccepted = false;
                    try
                    {
                        MasterSessionProtocol.CharacterLeaveResponse leaveResponse =
                            leaveTask.GetAwaiter().GetResult();
                        leaveAccepted = leaveResponse != null
                            && leaveResponse.SessionResult == MasterSessionProtocol.GameplaySessionResult.Accepted
                            && leaveResponse.PersistenceResult == MasterPersistenceProtocol.PersistenceResult.Accepted;
                    }
                    catch (Exception exception)
                    {
                        Debug.Error($"Reclaim failure character leave failed: {exception.Message}");
                    }

                    if (leaveAccepted)
                    {
                        yield return CloseDisconnectedGameplaySession(authenticatedSession.GameplaySessionID);
                        yield break;
                    }
                }
            }

            if (!hasAdmittedSession && !hasOwnedLease)
            {
                Debug.Error(
                    $"Reclaimed gameplay session {authenticatedSession.GameplaySessionID} has neither a local admitted session "
                    + "nor a local persistence lease; World-run invalidation remains the final cleanup owner.");
                yield break;
            }

            yield return DisconnectPublishedGameplaySession(authenticatedSession.GameplaySessionID);
        }

        private IEnumerator CloseDisconnectedGameplaySession(Guid gameplaySessionID)
        {
            Task<MasterSessionProtocol.SessionResponse> closeTask;
            try
            {
                closeTask = ServerInitialization.SessionCoordinator.CloseGameplaySessionAsync(
                    gameplaySessionID,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.Error($"Could not close disconnected gameplay session {gameplaySessionID}: {exception.Message}");
                yield break;
            }

            while (!closeTask.IsCompleted)
                yield return null;

            try
            {
                MasterSessionProtocol.SessionResponse response = closeTask.GetAwaiter().GetResult();
                if (response == null
                    || (response.Result != MasterSessionProtocol.GameplaySessionResult.Accepted
                        && response.Result != MasterSessionProtocol.GameplaySessionResult.StaleSession))
                {
                    Debug.Error(
                        $"Master gameplay-session close for {gameplaySessionID} returned {response?.Result}.");
                }
            }
            catch (Exception exception)
            {
                Debug.Error($"Master gameplay-session close for {gameplaySessionID} failed: {exception.Message}");
            }
        }

        private enum CharacterFinalizationDisposition : byte
        {
            ReturnToCharacterSelection = 0,
            CloseGameplaySession = 1,
            Shutdown = 2
        }

        private void StartCharacterFinalization(
            Guid gameplaySessionID,
            CharacterFinalizationDisposition disposition)
        {
            if (!TryGetAdmittedSession(gameplaySessionID, out PlayerSessionData session)
                || session == null)
            {
                throw new InvalidOperationException(
                    $"The gameplay session {gameplaySessionID} is not admitted.");
            }
            if (session.LifecycleState == PlayerSessionData.SessionLifecycleState.Finalizing
                || session.LifecycleState == PlayerSessionData.SessionLifecycleState.Unsettled)
            {
                return;
            }
            if (session.PlayerActor == null)
                throw new InvalidOperationException("Character finalization requires a live player actor.");
            if (session.CharacterID == Guid.Empty)
                throw new InvalidOperationException("Character finalization requires a nonempty Character ID.");

            if (session.LifecycleState != PlayerSessionData.SessionLifecycleState.Connected
                && session.LifecycleState != PlayerSessionData.SessionLifecycleState.LogoutCountdown
                && session.LifecycleState != PlayerSessionData.SessionLifecycleState.DisconnectGrace)
            {
                throw new InvalidOperationException(
                    $"Character finalization cannot start from lifecycle state {session.LifecycleState}.");
            }

            CancelSessionDeadline(session);
            session.LifecycleState = PlayerSessionData.SessionLifecycleState.Finalizing;
            StartCoroutine(FinalizeCharacterSession(gameplaySessionID, disposition));
        }

        private IEnumerator FinalizeCharacterSession(
            Guid gameplaySessionID,
            CharacterFinalizationDisposition disposition)
        {
            if (!TryGetAdmittedSession(gameplaySessionID, out PlayerSessionData session)
                || session == null)
            {
                yield break;
            }

            if (!TryCaptureAndQueueFinalCheckpoint(session, out string failureReason))
            {
                MarkSessionUnsettled(
                    gameplaySessionID,
                    $"final checkpoint capture or queue failed: {failureReason}");
                yield break;
            }

            WorldPersistenceCoordinator coordinator;
            try
            {
                coordinator = ServerInitialization.PersistenceCoordinator;
            }
            catch (Exception exception)
            {
                MarkSessionUnsettled(
                    gameplaySessionID,
                    $"persistence coordinator became unavailable: {exception.Message}");
                yield break;
            }

            if (coordinator == null)
            {
                MarkSessionUnsettled(gameplaySessionID, "persistence coordinator became unavailable.");
                yield break;
            }

            Task flushTask;
            try
            {
                flushTask = coordinator.FlushPendingCheckpointsAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                MarkSessionUnsettled(
                    gameplaySessionID,
                    $"final checkpoint flush could not start: {exception.Message}");
                yield break;
            }

            if (flushTask == null)
            {
                MarkSessionUnsettled(gameplaySessionID, "final checkpoint flush returned no task.");
                yield break;
            }

            while (!flushTask.IsCompleted)
                yield return null;

            try
            {
                flushTask.GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                MarkSessionUnsettled(
                    gameplaySessionID,
                    $"final checkpoint flush failed: {exception.Message}");
                yield break;
            }

            if (!TryGetAdmittedSession(gameplaySessionID, out session)
                || session == null)
            {
                yield break;
            }

            if (!coordinator.TryGetCharacterCheckpointSettlement(
                    session.CharacterID,
                    out Guid settledAccountID,
                    out long settledRevision,
                    out bool hasPendingCheckpoint,
                    out MasterPersistenceProtocol.PersistenceResult? blockingResult)
                || settledAccountID != session.AccountID
                || settledRevision < 1
                || hasPendingCheckpoint
                || blockingResult.HasValue)
            {
                MarkSessionUnsettled(
                    gameplaySessionID,
                    "the final checkpoint settlement could not be proven current and complete.");
                yield break;
            }

            Task<MasterSessionProtocol.CharacterLeaveResponse> leaveTask;
            try
            {
                leaveTask = coordinator.LeaveAdmittedCharacterAsync(
                    session.GameplaySessionID,
                    session.CharacterID,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                MarkSessionUnsettled(
                    gameplaySessionID,
                    $"Master character leave could not start: {exception.Message}");
                yield break;
            }

            if (leaveTask == null)
            {
                MarkSessionUnsettled(gameplaySessionID, "Master character leave returned no task.");
                yield break;
            }

            while (!leaveTask.IsCompleted)
                yield return null;

            MasterSessionProtocol.CharacterLeaveResponse leaveResponse;
            try
            {
                leaveResponse = leaveTask.GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                MarkSessionUnsettled(
                    gameplaySessionID,
                    $"Master character leave failed: {exception.Message}");
                yield break;
            }

            if (leaveResponse == null
                || leaveResponse.SessionResult != MasterSessionProtocol.GameplaySessionResult.Accepted
                || leaveResponse.PersistenceResult != MasterPersistenceProtocol.PersistenceResult.Accepted)
            {
                MarkSessionUnsettled(
                    gameplaySessionID,
                    $"Master character leave returned session result {leaveResponse?.SessionResult} and "
                    + $"persistence result {leaveResponse?.PersistenceResult}.");
                yield break;
            }

            if (!TryGetAdmittedSession(gameplaySessionID, out session)
                || session == null
                || session.PlayerActor == null)
            {
                MarkSessionUnsettled(
                    gameplaySessionID,
                    "the live actor disappeared before actor retirement.");
                yield break;
            }

            bool hasLiveAttachment = session.HasPlayerAttachment
                && TryGetSessionData(session.PlayerID, out PlayerSessionData attachedSession)
                && attachedSession != null
                && attachedSession.GameplaySessionID == gameplaySessionID
                && IsPlayerConnected(session.PlayerID);
            if (!hasLiveAttachment && disposition == CharacterFinalizationDisposition.ReturnToCharacterSelection)
            {
                Debug.Warning(
                    $"Character {session.CharacterID} disconnected during finalization; closing gameplay session instead of publishing a roster.");
                disposition = CharacterFinalizationDisposition.CloseGameplaySession;
            }

            PlayerID playerID = session.PlayerID;
            Guid accountID = session.AccountID;
            Guid characterID = session.CharacterID;
            Actor actor = session.PlayerActor;
            if (ServerStatManager.IsInitialized)
                ServerStatManager.Instance.UnregisterActor(actor);

            actor.RetireFromServer();

            NetworkIdentity identity = actor.GetComponent<NetworkIdentity>();
            if (identity != null)
                identity.Despawn();

            RemoveAdmittedSession(gameplaySessionID);

            if (disposition == CharacterFinalizationDisposition.CloseGameplaySession)
            {
                yield return CloseDisconnectedGameplaySession(gameplaySessionID);
                yield break;
            }
            if (disposition == CharacterFinalizationDisposition.Shutdown)
                yield break;

            Task<MasterSessionProtocol.CharacterRosterResponse> rosterTask;
            try
            {
                rosterTask = coordinator.GetCharacterRosterAsync(
                    gameplaySessionID,
                    accountID,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.Error(
                    $"Character {characterID} left durably, but its roster refresh could not start: {exception.Message}");
                yield break;
            }

            if (rosterTask == null)
            {
                Debug.Error($"Character {characterID} left durably, but its roster refresh returned no task.");
                yield break;
            }

            while (!rosterTask.IsCompleted)
                yield return null;

            MasterSessionProtocol.CharacterRosterResponse rosterResponse;
            try
            {
                rosterResponse = rosterTask.GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Debug.Error(
                    $"Character {characterID} left durably, but its roster refresh failed: {exception.Message}");
                yield break;
            }

            if (rosterResponse == null
                || rosterResponse.SessionResult != MasterSessionProtocol.GameplaySessionResult.Accepted
                || rosterResponse.PersistenceResponse == null
                || rosterResponse.PersistenceResponse.Result
                    != MasterPersistenceProtocol.PersistenceResult.Accepted)
            {
                Debug.Error(
                    $"Character {characterID} left durably, but roster refresh returned session result "
                    + $"{rosterResponse?.SessionResult} and persistence result "
                    + $"{rosterResponse?.PersistenceResponse?.Result}.");
                yield break;
            }

            List<CharacterData> roster;
            try
            {
                roster = CharacterPersistenceProjection.ToActiveRoster(rosterResponse.PersistenceResponse);
            }
            catch (Exception exception)
            {
                Debug.Error(
                    $"Character {characterID} left durably, but roster projection failed: {exception.Message}");
                yield break;
            }

            if (!IsPlayerConnected(playerID)
                || !TryGetAccountContext(playerID, out ConnectedAccountContext accountContext)
                || accountContext.GameplaySessionID != gameplaySessionID
                || accountContext.AccountID != accountID)
            {
                Debug.Warning(
                    $"Character {characterID} left durably, but the live account attachment changed before roster publication; closing gameplay session.");
                yield return CloseDisconnectedGameplaySession(gameplaySessionID);
                yield break;
            }

            AccountEndpoint.Client_UpdateCharacterList(playerID, roster);
        }

        private void CancelSessionDeadline(PlayerSessionData session)
        {
            if (session.DeadlineRegistration.IsValid)
                session.DeadlineRegistration.Cancel();

            session.DeadlineRegistration = default;
            session.DeadlineTickID = 0;
        }

        private void MarkSessionUnsettled(Guid gameplaySessionID, string reason)
        {
            if (TryGetAdmittedSession(gameplaySessionID, out PlayerSessionData session)
                && session != null)
            {
                CancelSessionDeadline(session);
                session.LifecycleState = PlayerSessionData.SessionLifecycleState.Unsettled;
            }

            Debug.Error($"Character finalization for gameplay session {gameplaySessionID} is unsettled: {reason}");
        }
    }
}
