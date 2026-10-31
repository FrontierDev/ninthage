using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
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
        private static readonly string[] CLIENT_METADATA_PROPERTY_NAMES =
        {
            "clientBuildVersion",
            "clientBuildID",
            "protocolVersion",
            "gameplayContentRevision",
            "gameplayContentManifestHash",
            "buildTarget",
            "catalogRevision",
            "catalogHash"
        };
        private static readonly string[] RELEASE_MANIFEST_PROPERTY_NAMES =
        {
            "clientBuildVersion",
            "clientBuildID",
            "protocolVersion",
            "gameplayContentRevision",
            "gameplayContentManifestHash",
            "addressablesCatalogs"
        };
        private static readonly string[] CATALOG_PROPERTY_NAMES =
        {
            "buildTarget",
            "catalogRevision",
            "catalogHash"
        };
        private static readonly JsonSerializerSettings METADATA_JSON_SETTINGS =
            new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error
            };
        private static readonly JsonSerializerSettings RELEASE_MANIFEST_JSON_SETTINGS =
            new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
        private static readonly JsonLoadSettings JSON_LOAD_SETTINGS =
            new JsonLoadSettings
            {
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
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
                var manifestPath = Environment.GetEnvironmentVariable(
                    RELEASE_MANIFEST_PATH_ENVIRONMENT_VARIABLE);
                identity = LoadFromReleaseManifest(manifestPath, SUPPORTED_BUILD_TARGET);
            }
            else
            {
                var streamingAssetsPath = Application.streamingAssetsPath;
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

                var metadataObject = ReadJsonObjectFile(
                    metadataPath,
                    "client compatibility metadata");
                RequireExactPropertySet(
                    metadataObject,
                    CLIENT_METADATA_PROPERTY_NAMES,
                    "client compatibility metadata");
                var metadata = ConvertJsonObject<MetadataProjection>(
                    metadataObject,
                    METADATA_JSON_SETTINGS,
                    metadataPath,
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

            var manifestObject = ReadJsonObjectFile(
                manifestPath,
                "Release Manifest");
            RequireCanonicalPropertyNames(
                manifestObject,
                RELEASE_MANIFEST_PROPERTY_NAMES,
                "Release Manifest");
            ValidateCatalogPropertyNames(manifestObject, manifestPath);
            var manifest = ConvertJsonObject<ReleaseManifestProjection>(
                manifestObject,
                RELEASE_MANIFEST_JSON_SETTINGS,
                manifestPath,
                "Release Manifest");

            var catalogs = manifest.AddressablesCatalogs;
            if (catalogs == null)
                throw new InvalidOperationException("Release Manifest addressablesCatalogs is required.");

            CatalogProjection matchingCatalog = null;
            var matchingCatalogCount = 0;
            foreach (var catalog in catalogs)
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

        private static JObject ReadJsonObjectFile(string path, string description)
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
                return JObject.Parse(json, JSON_LOAD_SETTINGS);
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException(
                    $"The {description} file '{path}' contains invalid or incompatible JSON.",
                    exception);
            }
        }

        private static T ConvertJsonObject<T>(
            JObject jsonObject,
            JsonSerializerSettings settings,
            string path,
            string description)
        {
            try
            {
                var value = jsonObject.ToObject<T>(JsonSerializer.Create(settings));
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

        private static void RequireExactPropertySet(
            JObject jsonObject,
            string[] expectedPropertyNames,
            string description)
        {
            var propertyCount = 0;
            foreach (var property in jsonObject.Properties())
            {
                propertyCount++;
                if (!ContainsOrdinal(expectedPropertyNames, property.Name))
                {
                    throw new InvalidOperationException(
                        $"The {description} contains unexpected property '{property.Name}'.");
                }
            }

            if (propertyCount != expectedPropertyNames.Length)
            {
                throw new InvalidOperationException(
                    $"The {description} must contain exactly {expectedPropertyNames.Length} properties.");
            }

            RequireCanonicalPropertyNames(jsonObject, expectedPropertyNames, description);
        }

        private static void RequireCanonicalPropertyNames(
            JObject jsonObject,
            string[] requiredPropertyNames,
            string description)
        {
            foreach (var requiredPropertyName in requiredPropertyNames)
            {
                var hasCanonicalProperty = false;
                foreach (var property in jsonObject.Properties())
                {
                    if (string.Equals(property.Name, requiredPropertyName, StringComparison.Ordinal))
                    {
                        hasCanonicalProperty = true;
                        continue;
                    }

                    if (string.Equals(property.Name, requiredPropertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"The {description} property '{property.Name}' must be named '{requiredPropertyName}'.");
                    }
                }

                if (!hasCanonicalProperty)
                {
                    throw new InvalidOperationException(
                        $"The {description} requires property '{requiredPropertyName}'.");
                }
            }
        }

        private static void ValidateCatalogPropertyNames(JObject manifestObject, string manifestPath)
        {
            var catalogsProperty = GetProperty(manifestObject, "addressablesCatalogs");
            if (!(catalogsProperty.Value is JArray catalogs))
                return;

            foreach (var catalogToken in catalogs)
            {
                if (!(catalogToken is JObject catalogObject))
                    continue;

                RequireCanonicalPropertyNames(
                    catalogObject,
                    CATALOG_PROPERTY_NAMES,
                    $"Release Manifest file '{manifestPath}' catalog entry");
            }
        }

        private static JProperty GetProperty(JObject jsonObject, string propertyName)
        {
            foreach (var property in jsonObject.Properties())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.Ordinal))
                    return property;
            }

            throw new InvalidOperationException($"Required JSON property '{propertyName}' was not found.");
        }

        private static bool ContainsOrdinal(string[] propertyNames, string propertyName)
        {
            foreach (var candidate in propertyNames)
            {
                if (string.Equals(candidate, propertyName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static bool IsClientBuildVersion(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            var separatorCount = 0;
            var componentLength = 0;
            foreach (var character in value)
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

            foreach (var character in value)
            {
                var isDigit = character >= '0' && character <= '9';
                var isLowercaseHexLetter = character >= 'a' && character <= 'f';
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
