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
                || session.PlayerActor == null
                || !TryGetSessionData(session.PlayerID, out PlayerSessionData attachedSession)
                || attachedSession == null
                || attachedSession.GameplaySessionID != gameplaySessionID
                || !IsPlayerConnected(session.PlayerID))
            {
                MarkSessionUnsettled(
                    gameplaySessionID,
                    "the live Player ID attachment changed before actor retirement.");
                yield break;
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

            if (disposition != CharacterFinalizationDisposition.ReturnToCharacterSelection)
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
                    $"Character {characterID} left durably, but the live account attachment changed before roster publication.");
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
