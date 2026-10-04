using Game.Shared.Persistence;
using Game.Shared.Data;
using Debug = Game.Shared.FormattedDebug;
using PurrNet;
using Game.Server.Persistence;

namespace Game.Server.Services
{
    public sealed class CharacterCreationService
    {
        // Cache references to the definition libraries for easy access during character creation.
        public static ClassDefinitionLibrary classDefinitions;
        public static RaceDefinitionLibrary raceDefinitions;
        private static bool _initialized = false;

        private NewCharacterData characterData;
        private PlayerID playerId;

        public CharacterCreationService(PlayerID _playerId, NewCharacterData _characterData)
        {
            // Load the definition libraries. In a real implementation, you might want to handle errors here.
            if (!_initialized)
            {
                classDefinitions = ClassDefinitionLibrary.Instance;
                raceDefinitions = RaceDefinitionLibrary.Instance;
                _initialized = true;
            }

            playerId = _playerId;
            characterData = _characterData;

            ValidateCharacterData();
        }

        private void ValidateCharacterData()
        {
            // Check that the player selected a valid race.
            bool validRace = false;
            bool validClass = false;
            bool validName = false;

            if (string.IsNullOrEmpty(characterData.RaceID) || raceDefinitions.GetDefinition(characterData.RaceID) == null || !raceDefinitions.GetDefinition(characterData.RaceID).IsPlayable)
                validRace = false;
            else validRace = true;

            // Check that the player selected a valid class.
            if (string.IsNullOrEmpty(characterData.ClassID) || classDefinitions.GetDefinition(characterData.ClassID) == null || !classDefinitions.GetDefinition(characterData.ClassID))
                validClass = false;
            else validClass = true;

            // Check that the player's chosen name is valid (not empty, not too long, etc.)
            if (string.IsNullOrEmpty(characterData.Name) || characterData.Name.Length > 20)
                validName = false;
            else validName = true;

            if (!validRace || !validClass || !validName)
            {
                Debug.Log($"Character creation validation failed for player {playerId}. ValidRace: {validRace}, ValidClass: {validClass}, ValidName: {validName}");
                // In a real implementation, you would send an error response back to the client here.
                return;
            }
            else
            {
                var defaultPVPFaction = raceDefinitions.GetDefinition(characterData.RaceID).DefaultPVPFaction;

                CharacterData validatedCharacter = new CharacterData(
                    characterData.Name,
                    1,
                    characterData.ClassID,
                    characterData.RaceID,
                    defaultPVPFaction.DefinitionId
                );

                AddCharacter(validatedCharacter);
            }
        }

        private void AddCharacter(CharacterData character)
        {
            PlayerAccountData accountData = ServerConnectionManager.Instance.GetAccountData(playerId);
            accountData.Characters.Add(character);
            PlayerDatabase.SavePlayerAccount(accountData);
            Debug.Log($"Character '{character.Name}' created for player {playerId}. Account '{accountData.Username} has {accountData.Characters.Count} characters.");
            Game.Shared.Networking.AccountService.Client_NotifyCharacterCreationResult(playerId, true, $"Character '{character.Name}' created successfully.");
            Game.Shared.Networking.AccountService.Client_UpdateCharacterList(playerId, accountData.Characters);
        }
    }
}