using System;
using System.Globalization;

namespace Game.Server.Networking.Master
{
    /// <summary>
    /// Contains the immutable release identity declared by one World Server process.
    /// </summary>
    internal sealed class WorldServerReleaseIdentity
    {
        private const string RELEASE_ID_ENVIRONMENT_VARIABLE = "NINTHAGE_RELEASE_ID";
        private const string SERVER_BUILD_ID_ENVIRONMENT_VARIABLE = "NINTHAGE_SERVER_BUILD_ID";
        private const string PROTOCOL_VERSION_ENVIRONMENT_VARIABLE = "NINTHAGE_PROTOCOL_VERSION";
        private const string GAMEPLAY_CONTENT_REVISION_ENVIRONMENT_VARIABLE = "NINTHAGE_GAMEPLAY_CONTENT_REVISION";
        private const string GAMEPLAY_CONTENT_MANIFEST_HASH_ENVIRONMENT_VARIABLE = "NINTHAGE_GAMEPLAY_CONTENT_MANIFEST_HASH";

        private WorldServerReleaseIdentity(
            Guid releaseID,
            string serverBuildID,
            int protocolVersion,
            long gameplayContentRevision,
            string gameplayContentManifestHash)
        {
            ReleaseID = releaseID;
            ServerBuildID = serverBuildID;
            ProtocolVersion = protocolVersion;
            GameplayContentRevision = gameplayContentRevision;
            GameplayContentManifestHash = gameplayContentManifestHash;
        }

        /// <summary>Gets the immutable release ID declared for this World Server process.</summary>
        internal Guid ReleaseID { get; }

        /// <summary>Gets the immutable server build ID declared for this World Server process.</summary>
        internal string ServerBuildID { get; }

        /// <summary>Gets the immutable network protocol version declared for this World Server process.</summary>
        internal int ProtocolVersion { get; }

        /// <summary>Gets the immutable gameplay content revision declared for this World Server process.</summary>
        internal long GameplayContentRevision { get; }

        /// <summary>Gets the immutable gameplay content manifest hash declared for this World Server process.</summary>
        internal string GameplayContentManifestHash { get; }

        /// <summary>
        /// Loads and validates the required release identity declaration for this World Server process.
        /// </summary>
        internal static WorldServerReleaseIdentity LoadFromEnvironment()
        {
            string releaseIDText = ReadRequiredEnvironmentVariable(RELEASE_ID_ENVIRONMENT_VARIABLE);
            if (!Guid.TryParseExact(releaseIDText, "D", out Guid releaseID)
                || releaseID == Guid.Empty
                || !string.Equals(releaseIDText, releaseID.ToString("D"), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{RELEASE_ID_ENVIRONMENT_VARIABLE} must be a nonempty canonical lowercase GUID.");
            }

            string serverBuildID = ReadRequiredEnvironmentVariable(SERVER_BUILD_ID_ENVIRONMENT_VARIABLE);
            if (!IsLowercaseHex(serverBuildID, 40))
            {
                throw new InvalidOperationException(
                    $"{SERVER_BUILD_ID_ENVIRONMENT_VARIABLE} must be exactly 40 lowercase hexadecimal characters.");
            }

            string protocolVersionText = ReadRequiredEnvironmentVariable(PROTOCOL_VERSION_ENVIRONMENT_VARIABLE);
            if (!int.TryParse(
                    protocolVersionText,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int protocolVersion)
                || protocolVersion <= 0)
            {
                throw new InvalidOperationException(
                    $"{PROTOCOL_VERSION_ENVIRONMENT_VARIABLE} must be a positive base-10 integer.");
            }

            string gameplayContentRevisionText =
                ReadRequiredEnvironmentVariable(GAMEPLAY_CONTENT_REVISION_ENVIRONMENT_VARIABLE);
            if (!long.TryParse(
                    gameplayContentRevisionText,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out long gameplayContentRevision)
                || gameplayContentRevision <= 0)
            {
                throw new InvalidOperationException(
                    $"{GAMEPLAY_CONTENT_REVISION_ENVIRONMENT_VARIABLE} must be a positive base-10 integer.");
            }

            string gameplayContentManifestHash =
                ReadRequiredEnvironmentVariable(GAMEPLAY_CONTENT_MANIFEST_HASH_ENVIRONMENT_VARIABLE);
            if (!IsLowercaseHex(gameplayContentManifestHash, 64))
            {
                throw new InvalidOperationException(
                    $"{GAMEPLAY_CONTENT_MANIFEST_HASH_ENVIRONMENT_VARIABLE} must be exactly 64 lowercase hexadecimal characters.");
            }

            return new WorldServerReleaseIdentity(
                releaseID,
                serverBuildID,
                protocolVersion,
                gameplayContentRevision,
                gameplayContentManifestHash);
        }

        private static string ReadRequiredEnvironmentVariable(string variableName)
        {
            string value = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"{variableName} is required.");

            return value;
        }

        private static bool IsLowercaseHex(string value, int expectedLength)
        {
            if (value.Length != expectedLength)
                return false;

            foreach (char character in value)
            {
                bool isDigit = character >= '0' && character <= '9';
                bool isLowercaseLetter = character >= 'a' && character <= 'f';
                if (!isDigit && !isLowercaseLetter)
                    return false;
            }

            return true;
        }
    }
}
