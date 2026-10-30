using Newtonsoft.Json;
using System;

namespace Game.Server.Networking.Master
{
    /// <summary>Contains the transport-only gameplay-session contracts used by the Master API.</summary>
    internal static class MasterSessionProtocol
    {
        internal const string AUTHENTICATE_PATH = "/world/v1/sessions/authenticate";
        internal const string DISCONNECT_PATH = "/world/v1/sessions/disconnect";
        internal const string CHARACTER_ROSTER_PATH = "/world/v1/sessions/characters/roster";
        internal const string CHARACTER_CREATE_PATH = "/world/v1/sessions/characters/create";
        internal const string CHARACTER_DELETE_PATH = "/world/v1/sessions/characters/delete";
        internal const string CHARACTER_RESTORE_PATH = "/world/v1/sessions/characters/restore";

        /// <summary>Canonical outcomes for a gameplay-session request.</summary>
        internal enum GameplaySessionResult
        {
            Accepted = 0,
            InvalidRequest = 1,
            Unauthorized = 2,
            WorldNotFound = 3,
            WorldNotActive = 4,
            StaleWorldServerRun = 5,
            InvalidCredentials = 6,
            HomeWorldMismatch = 7,
            SessionAlreadyActive = 8,
            StaleSession = 9,
            PersistenceUnavailable = 10,
            AuthenticationThrottled = 11
        }

        /// <summary>Describes how the Master established an accepted gameplay session.</summary>
        internal enum GameplaySessionDisposition
        {
            Created = 0,
            Reclaimed = 1
        }

        /// <summary>Loads the character roster for an authenticated gameplay session.</summary>
        internal sealed class CharacterRosterRequest
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }

            [JsonProperty("worldServerRunID")]
            public Guid WorldServerRunID { get; set; }

            [JsonProperty("gameplaySessionID")]
            public Guid GameplaySessionID { get; set; }
        }

        /// <summary>Creates a character for an authenticated gameplay session.</summary>
        internal sealed class CharacterCreateRequest
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }

            [JsonProperty("worldServerRunID")]
            public Guid WorldServerRunID { get; set; }

            [JsonProperty("gameplaySessionID")]
            public Guid GameplaySessionID { get; set; }

            [JsonProperty("operationID")]
            public Guid OperationID { get; set; }

            [JsonProperty("firstName")]
            public string FirstName { get; set; }

            [JsonProperty("lastName")]
            public string LastName { get; set; }

            [JsonProperty("raceDefinitionID")]
            public string RaceDefinitionID { get; set; }

            [JsonProperty("classDefinitionID")]
            public string ClassDefinitionID { get; set; }

            [JsonProperty("factionDefinitionID")]
            public string FactionDefinitionID { get; set; }

            [JsonProperty("worldState")]
            public MasterPersistenceProtocol.CharacterCreateRequest.InitialWorldState WorldState { get; set; }

            [JsonProperty("resources")]
            public MasterPersistenceProtocol.CharacterCreateRequest.InitialResource[] Resources { get; set; }

            [JsonProperty("abilities")]
            public MasterPersistenceProtocol.CharacterCreateRequest.InitialAbility[] Abilities { get; set; }

            [JsonProperty("weaponSkills")]
            public MasterPersistenceProtocol.CharacterCreateRequest.InitialWeaponSkill[] WeaponSkills { get; set; }

            [JsonProperty("reputations")]
            public MasterPersistenceProtocol.CharacterCreateRequest.InitialReputation[] Reputations { get; set; }

            [JsonProperty("items")]
            public MasterPersistenceProtocol.CharacterCreateRequest.InitialItem[] Items { get; set; }
        }

        /// <summary>Soft-deletes a character for an authenticated gameplay session.</summary>
        internal sealed class CharacterDeleteRequest
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }

            [JsonProperty("worldServerRunID")]
            public Guid WorldServerRunID { get; set; }

            [JsonProperty("gameplaySessionID")]
            public Guid GameplaySessionID { get; set; }

            [JsonProperty("operationID")]
            public Guid OperationID { get; set; }

            [JsonProperty("characterID")]
            public Guid CharacterID { get; set; }

            [JsonProperty("expectedRevision")]
            public long ExpectedRevision { get; set; }
        }

        /// <summary>Restores a character for an authenticated gameplay session.</summary>
        internal sealed class CharacterRestoreRequest
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }

            [JsonProperty("worldServerRunID")]
            public Guid WorldServerRunID { get; set; }

            [JsonProperty("gameplaySessionID")]
            public Guid GameplaySessionID { get; set; }

            [JsonProperty("operationID")]
            public Guid OperationID { get; set; }

            [JsonProperty("characterID")]
            public Guid CharacterID { get; set; }

            [JsonProperty("expectedRevision")]
            public long ExpectedRevision { get; set; }
        }

        /// <summary>Combines the gameplay-session and durable roster outcomes.</summary>
        internal sealed class CharacterRosterResponse
        {
            [JsonProperty("sessionResult")]
            public GameplaySessionResult SessionResult { get; set; }

            [JsonProperty("persistenceResponse")]
            public MasterPersistenceProtocol.CharacterRosterResponse PersistenceResponse { get; set; }
        }

        /// <summary>Combines the gameplay-session and durable character mutation outcomes.</summary>
        internal sealed class CharacterMutationResponse
        {
            [JsonProperty("sessionResult")]
            public GameplaySessionResult SessionResult { get; set; }

            [JsonProperty("persistenceResponse")]
            public MasterPersistenceProtocol.CharacterMutationResponse PersistenceResponse { get; set; }
        }

        /// <summary>
        /// Authenticates one account for a gameplay session.
        /// Replaying one logical operation must reuse its nonempty World-generated attempt ID.
        /// </summary>
        internal sealed class AuthenticateRequest
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }

            [JsonProperty("worldServerRunID")]
            public Guid WorldServerRunID { get; set; }

            /// <summary>
            /// Gets or sets the nonempty World-generated ID for this logical authentication operation.
            /// Replays reuse this value; a new login attempt generates a new value.
            /// </summary>
            [JsonProperty("authenticationAttemptID")]
            public Guid AuthenticationAttemptID { get; set; }

            [JsonProperty("legacyUsername")]
            public string LegacyUsername { get; set; }

            [JsonProperty("password")]
            public string Password { get; set; }
        }

        /// <summary>Master response returned after gameplay-session authentication.</summary>
        internal sealed class AuthenticateResponse
        {
            [JsonProperty("result")]
            public GameplaySessionResult Result { get; set; }

            [JsonProperty("gameplaySessionID")]
            public Guid? GameplaySessionID { get; set; }

            [JsonProperty("accountID")]
            public Guid? AccountID { get; set; }

            [JsonProperty("homeWorldID")]
            public Guid? HomeWorldID { get; set; }

            [JsonProperty("revision")]
            public long? Revision { get; set; }

            [JsonProperty("disposition")]
            public GameplaySessionDisposition? Disposition { get; set; }

            [JsonProperty("selectedCharacterID")]
            public Guid? SelectedCharacterID { get; set; }
        }

        /// <summary>Disconnects one previously authenticated gameplay session.</summary>
        internal sealed class DisconnectRequest
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }

            [JsonProperty("worldServerRunID")]
            public Guid WorldServerRunID { get; set; }

            [JsonProperty("gameplaySessionID")]
            public Guid GameplaySessionID { get; set; }
        }

        /// <summary>Master response returned by a gameplay-session operation without a payload.</summary>
        internal sealed class SessionResponse
        {
            [JsonProperty("result")]
            public GameplaySessionResult Result { get; set; }
        }
    }
}
