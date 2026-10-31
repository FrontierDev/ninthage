using System;

namespace Game.Shared.Networking
{
    /// <summary>
    /// Payload sent from client to server during authentication.
    /// Contains username/password credentials and the client's compatibility identity.
    /// </summary>
    [Serializable]
    public class AuthenticationPayload
    {
        /// <summary>Gets or sets the username credential.</summary>
        public string Username { get; set; }

        /// <summary>Gets or sets the password credential.</summary>
        public string Password { get; set; }

        /// <summary>Gets or sets the human-facing client build version.</summary>
        public string ClientBuildVersion { get; set; }

        /// <summary>Gets or sets the immutable client source/build identity.</summary>
        public string ClientBuildID { get; set; }

        /// <summary>Gets or sets the network protocol identity.</summary>
        public int ProtocolVersion { get; set; }

        /// <summary>Gets or sets the authoritative gameplay content revision.</summary>
        public long GameplayContentRevision { get; set; }

        /// <summary>Gets or sets the authoritative gameplay-definition manifest hash.</summary>
        public string GameplayContentManifestHash { get; set; }

        /// <summary>Gets or sets the Unity build target identity.</summary>
        public string BuildTarget { get; set; }

        /// <summary>Gets or sets the Addressables catalog revision.</summary>
        public long CatalogRevision { get; set; }

        /// <summary>Gets or sets the opaque Addressables catalog hash token.</summary>
        public string CatalogHash { get; set; }

        /// <summary>Initializes credentials and the immutable client compatibility projection.</summary>
        /// <param name="username">The username credential.</param>
        /// <param name="password">The password credential.</param>
        /// <param name="compatibilityIdentity">The validated client compatibility identity.</param>
        public AuthenticationPayload(
            string username,
            string password,
            ClientCompatibilityIdentity compatibilityIdentity)
        {
            if (compatibilityIdentity == null)
                throw new ArgumentNullException(nameof(compatibilityIdentity));

            Username = username;
            Password = password;
            ClientBuildVersion = compatibilityIdentity.ClientBuildVersion;
            ClientBuildID = compatibilityIdentity.ClientBuildID;
            ProtocolVersion = compatibilityIdentity.ProtocolVersion;
            GameplayContentRevision = compatibilityIdentity.GameplayContentRevision;
            GameplayContentManifestHash = compatibilityIdentity.GameplayContentManifestHash;
            BuildTarget = compatibilityIdentity.BuildTarget;
            CatalogRevision = compatibilityIdentity.CatalogRevision;
            CatalogHash = compatibilityIdentity.CatalogHash;
        }

        /// <summary>Initializes an empty payload for serialization.</summary>
        public AuthenticationPayload()
        {
            Username = string.Empty;
            Password = string.Empty;
            ClientBuildVersion = string.Empty;
            ClientBuildID = string.Empty;
            ProtocolVersion = 0;
            GameplayContentRevision = 0;
            GameplayContentManifestHash = string.Empty;
            BuildTarget = string.Empty;
            CatalogRevision = 0;
            CatalogHash = string.Empty;
        }
    }
}
