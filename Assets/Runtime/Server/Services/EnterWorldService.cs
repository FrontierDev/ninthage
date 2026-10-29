using CharacterData = Game.Shared.Persistence.CharacterData;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Core;
using Game.Server.Networking;
using Game.Server.Networking.Master;
using Game.Server.Persistence;
using Game.Shared.Data;
using PurrNet;
using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Server.Services
{
    /// <summary>Acquires Master character persistence and enters the player into the world.</summary>
    public sealed class EnterWorldService
    {
        /// <summary>Executes one Master-backed world-entry request.</summary>
        internal IEnumerator Execute(PlayerID playerID, string characterIDValue)
        {
            if (!Guid.TryParse(characterIDValue, out Guid characterID) || characterID == Guid.Empty)
            {
                Debug.Error($"Player {playerID} provided an invalid character GUID.");
                yield break;
            }

            if (!ServerConnectionManager.Instance.TryGetAccountContext(playerID, out ConnectedAccountContext accountContext))
            {
                Debug.Error($"No authenticated account context exists for player {playerID}.");
                yield break;
            }

            WorldPersistenceCoordinator coordinator;
            try
            {
                coordinator = ServerInitialization.PersistenceCoordinator;
            }
            catch (Exception exception)
            {
                Debug.Error($"Could not access persistence coordinator for player {playerID}: {exception.Message}");
                yield break;
            }

            if (coordinator == null)
            {
                Debug.Error($"Persistence coordinator is unavailable for player {playerID}.");
                yield break;
            }

            Task<MasterPersistenceProtocol.CharacterAcquireResponse> acquireTask;
            try
            {
                acquireTask = coordinator.AcquireCharacterAsync(
                    accountContext.AccountID,
                    characterID,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.Error($"Could not start character acquisition for player {playerID}: {exception.Message}");
                yield break;
            }

            while (!acquireTask.IsCompleted)
                yield return null;

            MasterPersistenceProtocol.CharacterAcquireResponse acquireResponse;
            try
            {
                acquireResponse = acquireTask.GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Debug.Error($"Character acquisition failed for player {playerID}: {exception.Message}");
                yield break;
            }

            if (acquireResponse == null
                || acquireResponse.Result != MasterPersistenceProtocol.PersistenceResult.Accepted)
            {
                Debug.Error($"Character acquisition for player {playerID} returned {acquireResponse?.Result}; world entry failed closed.");
                yield break;
            }

            if (acquireResponse.Character == null)
            {
                Debug.Error($"Character acquisition for player {playerID} returned no character state.");
                yield return ReleaseCharacter(coordinator, characterID);
                yield break;
            }

            CharacterData characterData = null;
            Exception projectionException = null;
            try
            {
                RaceDefinition raceDefinition = RaceDefinitionLibrary.Instance.GetDefinition(
                    acquireResponse.Character.RaceDefinitionID);
                ClassDefinition classDefinition = ClassDefinitionLibrary.Instance.GetDefinition(
                    acquireResponse.Character.ClassDefinitionID);
                if (raceDefinition == null || classDefinition == null)
                    throw new InvalidOperationException("The acquired character references an unknown race or class definition.");

                characterData = CharacterPersistenceProjection.ToCharacterData(acquireResponse.Character);
            }
            catch (Exception exception)
            {
                projectionException = exception;
            }

            if (projectionException != null)
            {
                Debug.Error($"Acquired character {characterID} failed definition or state validation: {projectionException.Message}");
                yield return ReleaseCharacter(coordinator, characterID);
                yield break;
            }

            if (!ServerConnectionManager.Instance.IsPlayerConnected(playerID))
            {
                Debug.Warning($"Player {playerID} disconnected while acquiring character {characterID}.");
                yield return ReleaseCharacter(coordinator, characterID);
                yield break;
            }

            Exception sessionException = null;
            try
            {
                ServerConnectionManager.Instance.SetSessionData(
                    playerID,
                    new PlayerSessionData(
                        playerID,
                        accountContext.Username,
                        accountContext.AccountID,
                        characterID,
                        characterData,
                        acquireResponse.Character));
            }
            catch (Exception exception)
            {
                sessionException = exception;
            }

            if (sessionException != null)
            {
                Debug.Error($"Could not establish world-entry session for player {playerID}: {sessionException.Message}");
                yield return ReleaseCharacter(coordinator, characterID);
                yield break;
            }

            yield return SpawnAndReleaseOnFailure(coordinator, playerID, characterID);
        }

        private IEnumerator SpawnAndReleaseOnFailure(
            WorldPersistenceCoordinator coordinator,
            PlayerID playerID,
            Guid characterID)
        {
            bool spawnSucceeded = false;
            IEnumerator spawnCoroutine = null;
            Exception spawnStartException = null;
            try
            {
                spawnCoroutine = ServerSpawnManager.Instance.SpawnPlayerCoroutine(
                    playerID,
                    succeeded => spawnSucceeded = succeeded);
            }
            catch (Exception exception)
            {
                spawnStartException = exception;
            }

            if (spawnStartException != null)
            {
                Debug.Error($"Could not start player spawn for {playerID}: {spawnStartException.Message}");
                ServerConnectionManager.Instance.ClearSessionData(playerID);
                yield return ReleaseCharacter(coordinator, characterID);
                yield break;
            }

            while (true)
            {
                bool hasNext;
                object yieldedValue = null;
                try
                {
                    hasNext = spawnCoroutine.MoveNext();
                    if (hasNext)
                        yieldedValue = spawnCoroutine.Current;
                }
                catch (Exception exception)
                {
                    Debug.Error($"Player spawn failed for {playerID}: {exception.Message}");
                    break;
                }

                if (!hasNext)
                    break;

                yield return yieldedValue;
            }

            if (spawnSucceeded)
                yield break;

            ServerConnectionManager.Instance.ClearSessionData(playerID);
            yield return ReleaseCharacter(coordinator, characterID);
            Debug.Error($"Player spawn failed for {playerID}; character persistence lease {characterID} was released.");
        }

        private static IEnumerator ReleaseCharacter(
            WorldPersistenceCoordinator coordinator,
            Guid characterID)
        {
            Task<MasterPersistenceProtocol.PersistenceResponse> releaseTask;
            try
            {
                releaseTask = coordinator.ReleaseCharacterAsync(characterID, CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.Warning($"Could not start persistence release for character {characterID}: {exception.Message}");
                yield break;
            }

            while (!releaseTask.IsCompleted)
                yield return null;

            try
            {
                MasterPersistenceProtocol.PersistenceResponse response = releaseTask.GetAwaiter().GetResult();
                if (response == null || response.Result != MasterPersistenceProtocol.PersistenceResult.Accepted)
                    Debug.Error($"Persistence release for character {characterID} returned {response?.Result}.");
            }
            catch (Exception exception)
            {
                Debug.Error($"Persistence release failed for character {characterID}: {exception.Message}");
            }
        }
    }
}
