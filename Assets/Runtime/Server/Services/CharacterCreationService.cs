using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Core;
using Game.Server.Networking;
using Game.Server.Networking.Master;
using Game.Server.Persistence;
using Game.Shared.Data;
using Game.Shared.Persistence;
using PurrNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Server.Services
{
    /// <summary>Validates and persists character creation requests on the server.</summary>
    public sealed class CharacterCreationService
    {
        /// <summary>Executes one Master-backed character creation request.</summary>
        internal IEnumerator Execute(PlayerID playerID, NewCharacterData characterData)
        {
            if (!ServerConnectionManager.Instance.TryGetAccountContext(playerID, out ConnectedAccountContext accountContext))
            {
                NotifyFailure(playerID, "account session unavailable");
                yield break;
            }

            RaceDefinition raceDefinition = RaceDefinitionLibrary.Instance.GetDefinition(characterData.RaceID);
            ClassDefinition classDefinition = ClassDefinitionLibrary.Instance.GetDefinition(characterData.ClassID);
            if (raceDefinition == null || !raceDefinition.IsPlayable)
            {
                NotifyFailure(playerID, "invalid race");
                yield break;
            }
            if (classDefinition == null)
            {
                NotifyFailure(playerID, "invalid class");
                yield break;
            }
            if (string.IsNullOrWhiteSpace(characterData.FirstName)
                || string.IsNullOrWhiteSpace(characterData.LastName))
            {
                NotifyFailure(playerID, "invalid character name");
                yield break;
            }

            if (raceDefinition.DefaultPVPFaction == null
                || string.IsNullOrWhiteSpace(raceDefinition.DefaultPVPFaction.DefinitionID))
            {
                Debug.Error($"Race '{characterData.RaceID}' has no valid default PvP faction.");
                NotifyFailure(playerID, "invalid race configuration");
                yield break;
            }

            if (ServerWorldManager.Instance == null
                || !ServerWorldManager.Instance.CurrentRuntimeContext.IsValid)
            {
                Debug.Error($"Cannot create a character for player {playerID} without a valid public world context.");
                NotifyFailure(playerID, "world unavailable");
                yield break;
            }

            List<CharacterCreationPersistenceCommand.InitialReputation> reputations = new();
            if (raceDefinition.StartingReputations != null)
            {
                foreach (PlayerStartingReputation startingReputation in raceDefinition.StartingReputations)
                {
                    if (startingReputation == null
                        || startingReputation.Faction == null
                        || string.IsNullOrWhiteSpace(startingReputation.Faction.DefinitionID))
                    {
                        Debug.Error($"Race '{characterData.RaceID}' has an invalid starting reputation entry.");
                        NotifyFailure(playerID, "invalid race configuration");
                        yield break;
                    }

                    reputations.Add(new CharacterCreationPersistenceCommand.InitialReputation(
                        startingReputation.Faction.DefinitionID,
                        startingReputation.Reputation));
                }
            }

            var spawnPosition = ServerSpawnManager.GetPrototypePlayerSpawnWorldPosition();
            var worldContext = ServerWorldManager.Instance.CurrentRuntimeContext;
            CharacterCreationPersistenceCommand command;
            try
            {
                command = new CharacterCreationPersistenceCommand(
                    accountContext.AccountID,
                    characterData.FirstName,
                    characterData.LastName,
                    characterData.RaceID,
                    characterData.ClassID,
                    raceDefinition.DefaultPVPFaction.DefinitionID,
                    new CharacterCreationPersistenceCommand.InitialWorldState(
                        worldContext.MapID,
                        null,
                        spawnPosition.x,
                        spawnPosition.y,
                        spawnPosition.z,
                        0f,
                        0f,
                        0f,
                        1f,
                        null),
                    Array.Empty<CharacterCreationPersistenceCommand.InitialResource>(),
                    reputations.ToArray(),
                    Array.Empty<CharacterCreationPersistenceCommand.InitialItem>());
            }
            catch (Exception exception)
            {
                Debug.Error($"Could not construct character creation command for player {playerID}: {exception.Message}");
                NotifyFailure(playerID, "invalid character request");
                yield break;
            }

            WorldPersistenceCoordinator coordinator;
            try
            {
                coordinator = ServerInitialization.PersistenceCoordinator;
            }
            catch (Exception exception)
            {
                Debug.Error($"Could not access persistence coordinator for character creation by player {playerID}: {exception.Message}");
                NotifyFailure(playerID, "character creation temporarily unavailable");
                yield break;
            }

            if (coordinator == null)
            {
                NotifyFailure(playerID, "character creation temporarily unavailable");
                yield break;
            }

            Task<MasterPersistenceProtocol.CharacterMutationResponse> creationTask;
            try
            {
                creationTask = coordinator.CreateCharacterAsync(command, CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.Error($"Could not start character creation for player {playerID}: {exception.Message}");
                NotifyFailure(playerID, "character creation temporarily unavailable");
                yield break;
            }

            while (!creationTask.IsCompleted)
                yield return null;

            MasterPersistenceProtocol.CharacterMutationResponse response;
            try
            {
                response = creationTask.GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Debug.Error($"Character creation failed for player {playerID}: {exception.Message}");
                NotifyFailure(playerID, "character creation temporarily unavailable");
                yield break;
            }

            if (response == null)
            {
                Debug.Error($"Character creation returned no response for player {playerID}.");
                NotifyFailure(playerID, "character creation temporarily unavailable");
                yield break;
            }

            switch (response.Result)
            {
                case MasterPersistenceProtocol.PersistenceResult.Accepted:
                    break;
                case MasterPersistenceProtocol.PersistenceResult.InvalidRequest:
                    NotifyFailure(playerID, "invalid character request");
                    yield break;
                case MasterPersistenceProtocol.PersistenceResult.CharacterLimitReached:
                    NotifyFailure(playerID, "active character limit reached");
                    yield break;
                case MasterPersistenceProtocol.PersistenceResult.NameConflict:
                    NotifyFailure(playerID, "full name already in use");
                    yield break;
                case MasterPersistenceProtocol.PersistenceResult.AccountNotFound:
                    NotifyFailure(playerID, "account unavailable");
                    yield break;
                case MasterPersistenceProtocol.PersistenceResult.PersistenceUnavailable:
                    NotifyFailure(playerID, "character creation temporarily unavailable");
                    yield break;
                default:
                    Debug.Error($"Character creation for player {playerID} returned unhandled result {response.Result}.");
                    NotifyFailure(playerID, "character creation failed");
                    yield break;
            }

            string displayName = $"{characterData.FirstName} {characterData.LastName}";
            Game.Shared.Networking.AccountEndpoint.Client_NotifyCharacterCreationResult(
                playerID,
                true,
                $"Character '{displayName}' created successfully.");

            Task<MasterPersistenceProtocol.CharacterRosterResponse> rosterTask;
            try
            {
                rosterTask = coordinator.GetCharacterRosterAsync(accountContext.AccountID, CancellationToken.None);
            }
            catch (Exception exception)
            {
                Debug.Error($"Could not refresh the character roster for player {playerID}: {exception.Message}");
                yield break;
            }

            while (!rosterTask.IsCompleted)
                yield return null;

            MasterPersistenceProtocol.CharacterRosterResponse rosterResponse;
            try
            {
                rosterResponse = rosterTask.GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Debug.Error($"Character roster refresh failed for player {playerID}: {exception.Message}");
                yield break;
            }

            if (rosterResponse == null
                || rosterResponse.Result != MasterPersistenceProtocol.PersistenceResult.Accepted)
            {
                Debug.Error($"Character roster refresh for player {playerID} returned {rosterResponse?.Result}; creation remains successful.");
                yield break;
            }

            List<CharacterData> roster;
            try
            {
                roster = CharacterPersistenceProjection.ToActiveRoster(rosterResponse);
            }
            catch (Exception exception)
            {
                Debug.Error($"Character roster projection failed for player {playerID}: {exception.Message}");
                yield break;
            }

            if (!ServerConnectionManager.Instance.IsPlayerConnected(playerID))
            {
                Debug.Warning($"Player {playerID} disconnected before the refreshed character roster could be sent.");
                yield break;
            }

            Game.Shared.Networking.AccountEndpoint.Client_UpdateCharacterList(playerID, roster);
        }

        private static void NotifyFailure(PlayerID playerID, string message)
        {
            Game.Shared.Networking.AccountEndpoint.Client_NotifyCharacterCreationResult(playerID, false, message);
        }
    }
}
