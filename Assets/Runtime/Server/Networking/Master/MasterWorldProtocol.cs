using Newtonsoft.Json;
using System;

namespace Game.Server.Networking.Master
{
    /// <summary>Contains the transport-only JSON contract used by the Master World Server.</summary>
    internal static class MasterWorldProtocol
    {
        /// <summary>Canonical outcomes for a World Server registration request.</summary>
        internal enum RegistrationResult
        {
            Accepted = 0,
            InvalidWorldServerRunID = 1,
            InvalidWorldName = 2,
            InvalidServerBuildID = 3,
            Unauthorized = 4,
            WorldNotFound = 5,
            WorldAlreadyActive = 6,
            StaleWorldServerRun = 7,
            InvalidReleaseID = 8,
            InvalidProtocolVersion = 9,
            InvalidGameplayContentRevision = 10,
            InvalidGameplayContentManifestHash = 11,
            ReleaseIDMismatch = 12,
            ServerBuildIDMismatch = 13,
            ProtocolVersionMismatch = 14,
            GameplayContentRevisionMismatch = 15,
            GameplayContentManifestHashMismatch = 16,
        }

        /// <summary>Canonical outcomes for a World Server lease request.</summary>
        internal enum LeaseResult
        {
            Accepted = 0,
            InvalidWorldID = 1,
            InvalidWorldServerRunID = 2,
            Unauthorized = 3,
            WorldNotFound = 4,
            StaleWorldServerRun = 5,
        }

        /// <summary>Wire request for registering a World Server with the Master.</summary>
        internal sealed class RegistrationRequest
        {
            [JsonProperty("worldServerRunID")]
            public Guid WorldServerRunID { get; set; }

            [JsonProperty("worldName")]
            public string WorldName { get; set; }

            [JsonProperty("releaseID")]
            public Guid ReleaseID { get; set; }

            [JsonProperty("serverBuildID")]
            public string ServerBuildID { get; set; }

            [JsonProperty("protocolVersion")]
            public int ProtocolVersion { get; set; }

            [JsonProperty("gameplayContentRevision")]
            public long GameplayContentRevision { get; set; }

            [JsonProperty("gameplayContentManifestHash")]
            public string GameplayContentManifestHash { get; set; }
        }

        /// <summary>Client compatibility requirements projected from the accepted Release Manifest.</summary>
        internal sealed class ClientCompatibilityRequirements
        {
            /// <summary>Gets or sets the human-facing client build version.</summary>
            [JsonProperty("clientBuildVersion")]
            public string ClientBuildVersion { get; set; }

            /// <summary>Gets or sets the immutable client source/build identity.</summary>
            [JsonProperty("clientBuildID")]
            public string ClientBuildID { get; set; }

            /// <summary>Gets or sets the network protocol identity.</summary>
            [JsonProperty("protocolVersion")]
            public int ProtocolVersion { get; set; }

            /// <summary>Gets or sets the authoritative gameplay content revision.</summary>
            [JsonProperty("gameplayContentRevision")]
            public long GameplayContentRevision { get; set; }

            /// <summary>Gets or sets the authoritative gameplay-definition manifest hash.</summary>
            [JsonProperty("gameplayContentManifestHash")]
            public string GameplayContentManifestHash { get; set; }

            /// <summary>Gets or sets the Unity build target identity.</summary>
            [JsonProperty("buildTarget")]
            public string BuildTarget { get; set; }

            /// <summary>Gets or sets the Addressables catalog revision.</summary>
            [JsonProperty("catalogRevision")]
            public long CatalogRevision { get; set; }

            /// <summary>Gets or sets the opaque Addressables catalog hash token.</summary>
            [JsonProperty("catalogHash")]
            public string CatalogHash { get; set; }
        }

        /// <summary>Wire response returned by a World Server registration request.</summary>
        internal sealed class RegistrationResponse
        {
            [JsonProperty("result")]
            public RegistrationResult Result { get; set; }

            [JsonProperty("worldID")]
            public Guid? WorldID { get; set; }

            [JsonProperty("heartbeatIntervalSeconds")]
            public int? HeartbeatIntervalSeconds { get; set; }

            [JsonProperty("leaseTimeoutSeconds")]
            public int? LeaseTimeoutSeconds { get; set; }

            /// <summary>Gets or sets the accepted Release Manifest's client compatibility projection.</summary>
            [JsonProperty("clientCompatibility")]
            public ClientCompatibilityRequirements ClientCompatibility { get; set; }
        }

        /// <summary>Wire request for renewing or releasing a World Server lease.</summary>
        internal sealed class LeaseRequest
        {
            [JsonProperty("worldID")]
            public Guid WorldID { get; set; }

            [JsonProperty("worldServerRunID")]
            public Guid WorldServerRunID { get; set; }
        }

        /// <summary>Wire response returned by a World Server lease request.</summary>
        internal sealed class LeaseResponse
        {
            [JsonProperty("result")]
            public LeaseResult Result { get; set; }
        }
    }
}
