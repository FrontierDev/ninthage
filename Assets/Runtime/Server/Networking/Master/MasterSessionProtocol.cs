using Newtonsoft.Json;
using System;

namespace Game.Server.Networking.Master
{
    /// <summary>Contains the transport-only gameplay-session contracts used by the Master API.</summary>
    internal static class MasterSessionProtocol
    {
        internal const string AUTHENTICATE_PATH = "/world/v1/sessions/authenticate";
        internal const string DISCONNECT_PATH = "/world/v1/sessions/disconnect";

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
