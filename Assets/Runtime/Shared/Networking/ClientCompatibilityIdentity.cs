using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Shared.Networking
{
    /// <summary>
    /// Identifies the exact client build, protocol, gameplay content, and Addressables catalog.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class ClientCompatibilityIdentity
    {
        private const string SUPPORTED_BUILD_TARGET = "StandaloneWindows64";
        private static readonly JsonSerializerSettings METADATA_JSON_SETTINGS =
            new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error
            };

        /// <summary>
        /// Gets the environment variable that selects the explicit Release Manifest for Editor use and builds.
        /// </summary>
        public const string RELEASE_MANIFEST_PATH_ENVIRONMENT_VARIABLE =
            "NINTHAGE_RELEASE_MANIFEST_PATH";

        /// <summary>
        /// Gets the filename written into a graphical player's StreamingAssets directory.
        /// </summary>
        public const string METADATA_FILE_NAME = "ninthage-client-compatibility.json";

        /// <summary>Initializes a validated client compatibility identity.</summary>
        /// <param name="clientBuildVersion">The exact MAJOR.MINOR.PATCH client version.</param>
        /// <param name="clientBuildID">The 40-character lowercase hexadecimal client build identity.</param>
        /// <param name="protocolVersion">The positive network protocol identity.</param>
        /// <param name="gameplayContentRevision">The positive gameplay content revision.</param>
        /// <param name="gameplayContentManifestHash">The 64-character lowercase hexadecimal gameplay manifest hash.</param>
        /// <param name="buildTarget">The supported Unity build target.</param>
        /// <param name="catalogRevision">The positive Addressables catalog revision.</param>
        /// <param name="catalogHash">The opaque Addressables catalog hash token.</param>
        public ClientCompatibilityIdentity(
            string clientBuildVersion,
            string clientBuildID,
            int protocolVersion,
            long gameplayContentRevision,
            string gameplayContentManifestHash,
            string buildTarget,
            long catalogRevision,
            string catalogHash)
        {
            if (!IsClientBuildVersion(clientBuildVersion))
            {
                throw new ArgumentException(
                    "ClientBuildVersion must use the exact MAJOR.MINOR.PATCH format.",
                    nameof(clientBuildVersion));
            }

            if (!IsLowercaseHex(clientBuildID, 40))
            {
                throw new ArgumentException(
                    "ClientBuildID must be exactly 40 lowercase hexadecimal characters.",
                    nameof(clientBuildID));
            }

            if (protocolVersion <= 0)
                throw new ArgumentOutOfRangeException(nameof(protocolVersion), "ProtocolVersion must be positive.");

            if (gameplayContentRevision <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(gameplayContentRevision),
                    "GameplayContentRevision must be positive.");
            }

            if (!IsLowercaseHex(gameplayContentManifestHash, 64))
            {
                throw new ArgumentException(
                    "GameplayContentManifestHash must be exactly 64 lowercase hexadecimal characters.",
                    nameof(gameplayContentManifestHash));
            }

            if (!string.Equals(buildTarget, SUPPORTED_BUILD_TARGET, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"BuildTarget must be exactly '{SUPPORTED_BUILD_TARGET}'.",
                    nameof(buildTarget));
            }

            if (catalogRevision <= 0)
                throw new ArgumentOutOfRangeException(nameof(catalogRevision), "CatalogRevision must be positive.");

            if (string.IsNullOrWhiteSpace(catalogHash))
            {
                throw new ArgumentException(
                    "CatalogHash must be a nonblank opaque token.",
                    nameof(catalogHash));
            }

            ClientBuildVersion = clientBuildVersion;
            ClientBuildID = clientBuildID;
            ProtocolVersion = protocolVersion;
            GameplayContentRevision = gameplayContentRevision;
            GameplayContentManifestHash = gameplayContentManifestHash;
            BuildTarget = buildTarget;
            CatalogRevision = catalogRevision;
            CatalogHash = catalogHash;
        }

        /// <summary>Gets the human-facing client build version.</summary>
        [JsonProperty("clientBuildVersion")]
        public string ClientBuildVersion { get; }

        /// <summary>Gets the immutable client source/build identity.</summary>
        [JsonProperty("clientBuildID")]
        public string ClientBuildID { get; }

        /// <summary>Gets the network protocol identity.</summary>
        [JsonProperty("protocolVersion")]
        public int ProtocolVersion { get; }

        /// <summary>Gets the authoritative gameplay content revision.</summary>
        [JsonProperty("gameplayContentRevision")]
        public long GameplayContentRevision { get; }

        /// <summary>Gets the authoritative gameplay-definition manifest hash.</summary>
        [JsonProperty("gameplayContentManifestHash")]
        public string GameplayContentManifestHash { get; }

        /// <summary>Gets the Unity build target identity.</summary>
        [JsonProperty("buildTarget")]
        public string BuildTarget { get; }

        /// <summary>Gets the Addressables catalog revision.</summary>
        [JsonProperty("catalogRevision")]
        public long CatalogRevision { get; }

        /// <summary>Gets the opaque Addressables catalog hash token.</summary>
        [JsonProperty("catalogHash")]
        public string CatalogHash { get; }

        /// <summary>
        /// Loads the current client identity from built-player metadata or the explicit Editor manifest.
        /// </summary>
        /// <returns>The validated current client identity.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the current identity cannot be loaded or does not match the application version.</exception>
        public static ClientCompatibilityIdentity LoadCurrent()
        {
            ClientCompatibilityIdentity identity;
            if (Application.isEditor)
            {
                string manifestPath = Environment.GetEnvironmentVariable(
                    RELEASE_MANIFEST_PATH_ENVIRONMENT_VARIABLE);
                identity = LoadFromReleaseManifest(manifestPath, SUPPORTED_BUILD_TARGET);
            }
            else
            {
                string streamingAssetsPath = Application.streamingAssetsPath;
                if (string.IsNullOrWhiteSpace(streamingAssetsPath))
                {
                    throw new InvalidOperationException(
                        "Application.streamingAssetsPath is required to load client compatibility metadata.");
                }

                string metadataPath;
                try
                {
                    metadataPath = Path.Combine(streamingAssetsPath, METADATA_FILE_NAME);
                }
                catch (ArgumentException exception)
                {
                    throw new InvalidOperationException(
                        "Application.streamingAssetsPath is not a valid metadata directory.",
                        exception);
                }

                MetadataProjection metadata = ReadJsonFile<MetadataProjection>(
                    metadataPath,
                    METADATA_JSON_SETTINGS,
                    "client compatibility metadata");
                identity = CreateIdentity(metadata);
            }

            if (!string.Equals(identity.ClientBuildVersion, Application.version, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Client compatibility version '{identity.ClientBuildVersion}' does not match "
                    + $"Application.version '{Application.version}'.");
            }

            return identity;
        }

        /// <summary>
        /// Projects the client compatibility identity from one explicit Release Manifest.
        /// </summary>
        /// <param name="manifestPath">The existing Release Manifest JSON path.</param>
        /// <param name="buildTarget">The required Unity build target.</param>
        /// <returns>The validated client identity projected from the selected catalog.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the manifest cannot be read or is incomplete.</exception>
        /// <exception cref="ArgumentException">Thrown when the requested build target or manifest data is invalid.</exception>
        public static ClientCompatibilityIdentity LoadFromReleaseManifest(
            string manifestPath,
            string buildTarget)
        {
            if (!string.Equals(buildTarget, SUPPORTED_BUILD_TARGET, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"BuildTarget must be exactly '{SUPPORTED_BUILD_TARGET}'.",
                    nameof(buildTarget));
            }

            ReleaseManifestProjection manifest = ReadJsonFile<ReleaseManifestProjection>(
                manifestPath,
                new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore
                },
                "Release Manifest");

            CatalogProjection[] catalogs = manifest.AddressablesCatalogs;
            if (catalogs == null)
                throw new InvalidOperationException("Release Manifest addressablesCatalogs is required.");

            CatalogProjection matchingCatalog = null;
            int matchingCatalogCount = 0;
            foreach (CatalogProjection catalog in catalogs)
            {
                if (catalog == null || !string.Equals(catalog.BuildTarget, buildTarget, StringComparison.Ordinal))
                    continue;

                matchingCatalog = catalog;
                matchingCatalogCount++;
            }

            if (matchingCatalogCount != 1)
            {
                throw new InvalidOperationException(
                    $"Release Manifest must contain exactly one '{buildTarget}' Addressables catalog; "
                    + $"found {matchingCatalogCount}.");
            }

            return new ClientCompatibilityIdentity(
                manifest.ClientBuildVersion,
                manifest.ClientBuildID,
                manifest.ProtocolVersion,
                manifest.GameplayContentRevision,
                manifest.GameplayContentManifestHash,
                matchingCatalog.BuildTarget,
                matchingCatalog.CatalogRevision,
                matchingCatalog.CatalogHash);
        }

        private static ClientCompatibilityIdentity CreateIdentity(MetadataProjection metadata)
        {
            return new ClientCompatibilityIdentity(
                metadata.ClientBuildVersion,
                metadata.ClientBuildID,
                metadata.ProtocolVersion,
                metadata.GameplayContentRevision,
                metadata.GameplayContentManifestHash,
                metadata.BuildTarget,
                metadata.CatalogRevision,
                metadata.CatalogHash);
        }

        private static T ReadJsonFile<T>(
            string path,
            JsonSerializerSettings settings,
            string description)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException($"An explicit {description} path is required.");

            if (!File.Exists(path))
                throw new InvalidOperationException($"The {description} file '{path}' does not exist.");

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception exception) when (
                exception is IOException || exception is UnauthorizedAccessException)
            {
                throw new InvalidOperationException(
                    $"The {description} file '{path}' could not be read.",
                    exception);
            }

            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException($"The {description} file '{path}' is empty.");

            try
            {
                T value = JsonConvert.DeserializeObject<T>(json, settings);
                if (value == null)
                    throw new InvalidOperationException($"The {description} file '{path}' contains null JSON.");

                return value;
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException(
                    $"The {description} file '{path}' contains invalid or incompatible JSON.",
                    exception);
            }
        }

        private static bool IsClientBuildVersion(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            int separatorCount = 0;
            int componentLength = 0;
            foreach (char character in value)
            {
                if (character == '.')
                {
                    if (componentLength == 0)
                        return false;

                    separatorCount++;
                    componentLength = 0;
                    continue;
                }

                if (character < '0' || character > '9')
                    return false;

                componentLength++;
            }

            return separatorCount == 2 && componentLength > 0;
        }

        private static bool IsLowercaseHex(string value, int expectedLength)
        {
            if (value == null || value.Length != expectedLength)
                return false;

            foreach (char character in value)
            {
                bool isDigit = character >= '0' && character <= '9';
                bool isLowercaseHexLetter = character >= 'a' && character <= 'f';
                if (!isDigit && !isLowercaseHexLetter)
                    return false;
            }

            return true;
        }

        private sealed class MetadataProjection
        {
            [JsonProperty("clientBuildVersion", Required = Required.Always)]
            public string ClientBuildVersion { get; set; }

            [JsonProperty("clientBuildID", Required = Required.Always)]
            public string ClientBuildID { get; set; }

            [JsonProperty("protocolVersion", Required = Required.Always)]
            public int ProtocolVersion { get; set; }

            [JsonProperty("gameplayContentRevision", Required = Required.Always)]
            public long GameplayContentRevision { get; set; }

            [JsonProperty("gameplayContentManifestHash", Required = Required.Always)]
            public string GameplayContentManifestHash { get; set; }

            [JsonProperty("buildTarget", Required = Required.Always)]
            public string BuildTarget { get; set; }

            [JsonProperty("catalogRevision", Required = Required.Always)]
            public long CatalogRevision { get; set; }

            [JsonProperty("catalogHash", Required = Required.Always)]
            public string CatalogHash { get; set; }
        }

        private sealed class ReleaseManifestProjection
        {
            [JsonProperty("clientBuildVersion", Required = Required.Always)]
            public string ClientBuildVersion { get; set; }

            [JsonProperty("clientBuildID", Required = Required.Always)]
            public string ClientBuildID { get; set; }

            [JsonProperty("protocolVersion", Required = Required.Always)]
            public int ProtocolVersion { get; set; }

            [JsonProperty("gameplayContentRevision", Required = Required.Always)]
            public long GameplayContentRevision { get; set; }

            [JsonProperty("gameplayContentManifestHash", Required = Required.Always)]
            public string GameplayContentManifestHash { get; set; }

            [JsonProperty("addressablesCatalogs", Required = Required.Always)]
            public CatalogProjection[] AddressablesCatalogs { get; set; }
        }

        private sealed class CatalogProjection
        {
            [JsonProperty("buildTarget", Required = Required.Always)]
            public string BuildTarget { get; set; }

            [JsonProperty("catalogRevision", Required = Required.Always)]
            public long CatalogRevision { get; set; }

            [JsonProperty("catalogHash", Required = Required.Always)]
            public string CatalogHash { get; set; }
        }
    }
}
