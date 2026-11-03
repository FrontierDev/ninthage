using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Game.Client
{
    /// <summary>Contains the transport-only client contracts used by the Master selection API.</summary>
    public static class ClientMasterSelectionProtocol
    {
        /// <summary>Describes the outcome of direct Master client authentication.</summary>
        public enum ClientAuthenticationResult
        {
            Accepted = 0,
            InvalidRequest = 1,
            InvalidCredentials = 2,
            AuthenticationThrottled = 3,
            SessionAlreadyActive = 4,
            PersistenceUnavailable = 5
        }

        /// <summary>Requests direct Master authentication before World selection.</summary>
        public sealed class ClientAuthenticationRequest
        {
            [JsonProperty("legacyUsername")]
            public string LegacyUsername { get; set; }

            [JsonProperty("password")]
            public string Password { get; set; }
        }

        /// <summary>Returns the temporary selection session created by Master authentication.</summary>
        public sealed class ClientAuthenticationResponse
        {
            [JsonProperty("result")]
            public ClientAuthenticationResult Result { get; set; }

            [JsonProperty("selectionSessionToken")]
            public string SelectionSessionToken { get; set; }

            [JsonProperty("expiresAtUtc")]
            public DateTimeOffset? ExpiresAtUtc { get; set; }
        }

        /// <summary>Describes the outcome of an authenticated World selection operation.</summary>
        public enum WorldSelectionResult
        {
            Accepted = 0,
            InvalidRequest = 1,
            Unauthorized = 2,
            SessionExpired = 3,
            AccountUnavailable = 4,
            WorldNotFound = 5,
            WorldAccessDenied = 6,
            RevisionConflict = 7,
            PersistenceUnavailable = 8
        }

        /// <summary>Describes the projected access state for one World row.</summary>
        public enum WorldSelectionAccessState
        {
            Available = 0,
            Offline = 1,
            MembershipRequired = 2,
            DevelopmentAccessRequired = 3,
            AdditionalRequirementsNotEvaluated = 4
        }

        /// <summary>Contains the durable and current runtime data shown for one World.</summary>
        public sealed class WorldSelectionWorld
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }

            [JsonProperty("worldName")]
            public string WorldName { get; set; }

            [JsonProperty("location")]
            public string Location { get; set; }

            [JsonProperty("capacity")]
            public int Capacity { get; set; }

            [JsonProperty("onlinePlayerCount")]
            public int OnlinePlayerCount { get; set; }

            [JsonProperty("worldType")]
            public string WorldType { get; set; }

            [JsonProperty("requiresMembership")]
            public bool RequiresMembership { get; set; }

            [JsonProperty("accessRequirementsJson")]
            public string AccessRequirementsJson { get; set; }

            [JsonProperty("isHomeWorld")]
            public bool IsHomeWorld { get; set; }

            [JsonProperty("isOnline")]
            public bool IsOnline { get; set; }

            [JsonProperty("accessState")]
            public WorldSelectionAccessState AccessState { get; set; }
        }

        /// <summary>Returns the authenticated client's World-selection catalogue.</summary>
        public sealed class WorldSelectionCatalogueResponse
        {
            [JsonProperty("result")]
            public WorldSelectionResult Result { get; set; }

            [JsonProperty("homeWorldID")]
            public Guid? HomeWorldID { get; set; }

            [JsonProperty("accountRevision")]
            public long? AccountRevision { get; set; }

            [JsonProperty("worlds")]
            public IReadOnlyList<WorldSelectionWorld> Worlds { get; set; }
        }

        /// <summary>Requests an expected-revision-controlled Home World change.</summary>
        public sealed class HomeWorldUpdateRequest
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }

            [JsonProperty("expectedAccountRevision")]
            public long ExpectedAccountRevision { get; set; }
        }

        /// <summary>Returns the result of a Home World mutation.</summary>
        public sealed class HomeWorldUpdateResponse
        {
            [JsonProperty("result")]
            public WorldSelectionResult Result { get; set; }

            [JsonProperty("homeWorldID")]
            public Guid? HomeWorldID { get; set; }

            [JsonProperty("accountRevision")]
            public long? AccountRevision { get; set; }
        }

        /// <summary>Describes the outcome of requesting entry to an eligible World.</summary>
        public enum WorldEntryResult
        {
            Accepted = 0,
            InvalidRequest = 1,
            Unauthorized = 2,
            SessionExpired = 3,
            AccountUnavailable = 4,
            WorldNotFound = 5,
            WorldUnavailable = 6,
            WorldAccessDenied = 7,
            SessionAlreadyActive = 8,
            HandoffExpired = 9,
            HandoffInvalid = 10,
            PersistenceUnavailable = 11
        }

        /// <summary>Requests Master-approved entry to one selected World.</summary>
        public sealed class WorldEntryRequest
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }
        }

        /// <summary>Returns the endpoint and short-lived handoff for World entry.</summary>
        public sealed class WorldEntryResponse
        {
            [JsonProperty("result")]
            public WorldEntryResult Result { get; set; }

            [JsonProperty("worldID")]
            public Guid? WorldID { get; set; }

            [JsonProperty("clientHost")]
            public string ClientHost { get; set; }

            [JsonProperty("clientPort")]
            public int? ClientPort { get; set; }

            [JsonProperty("handoffToken")]
            public string HandoffToken { get; set; }

            [JsonProperty("expiresAtUtc")]
            public DateTimeOffset? ExpiresAtUtc { get; set; }
        }
    }
}
