using Game.Shared.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using UnityEngine;

namespace Game.Shared.Content
{
    /// <summary>
    /// Identifies the immutable Addressables delivery location selected by a Release Manifest.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AddressablesReleaseLocation
    {
        private const string ADDRESSABLES_CATALOGS_PROPERTY_NAME = "addressablesCatalogs";
        private const string BUILD_TARGET_PROPERTY_NAME = "buildTarget";
        private const string REMOTE_LOAD_PATH_PROPERTY_NAME = "remoteLoadPath";
        private static readonly string[] CATALOG_PROPERTY_NAMES =
        {
            BUILD_TARGET_PROPERTY_NAME,
            REMOTE_LOAD_PATH_PROPERTY_NAME
        };
        private static readonly string[] METADATA_PROPERTY_NAMES =
        {
            REMOTE_LOAD_PATH_PROPERTY_NAME
        };
        private static readonly JsonSerializerSettings METADATA_JSON_SETTINGS =
            new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error
            };
        private static readonly JsonLoadSettings JSON_LOAD_SETTINGS =
            new JsonLoadSettings
            {
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
            };

        /// <summary>
        /// Gets the filename written into a graphical player's StreamingAssets directory.
        /// </summary>
        public const string METADATA_FILE_NAME = "ninthage-addressables-release.json";

        /// <summary>
        /// Gets the runtime property name reserved for the validated Addressables remote load path.
        /// </summary>
        public const string REMOTE_LOAD_PATH_RUNTIME_PROPERTY =
            "NINTHAGE_ADDRESSABLES_REMOTE_LOAD_PATH";

        /// <summary>
        /// Initializes a validated immutable Addressables release location.
        /// </summary>
        /// <param name="remoteLoadPath">The absolute HTTP or HTTPS delivery URL.</param>
        public AddressablesReleaseLocation(string remoteLoadPath)
        {
            if (string.IsNullOrWhiteSpace(remoteLoadPath))
            {
                throw new ArgumentException(
                    "RemoteLoadPath must be a nonblank absolute HTTP or HTTPS URI.",
                    nameof(remoteLoadPath));
            }

            if (!Uri.TryCreate(remoteLoadPath, UriKind.Absolute, out var uri))
            {
                throw new ArgumentException(
                    "RemoteLoadPath must be an absolute URI.",
                    nameof(remoteLoadPath));
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "RemoteLoadPath must use the HTTP or HTTPS scheme.",
                    nameof(remoteLoadPath));
            }

            if (uri.AbsolutePath.IndexOf("/release_by_badge/", StringComparison.Ordinal) >= 0)
            {
                throw new ArgumentException(
                    "RemoteLoadPath must not resolve through a movable CCD release badge.",
                    nameof(remoteLoadPath));
            }

            RemoteLoadPath = remoteLoadPath;
        }

        /// <summary>Gets the exact validated Addressables remote load path.</summary>
        [JsonProperty(REMOTE_LOAD_PATH_PROPERTY_NAME)]
        public string RemoteLoadPath { get; }

        /// <summary>
        /// Loads the current Addressables release location from built-player metadata or the explicit Editor manifest.
        /// </summary>
        /// <returns>The validated current Addressables release location.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the current location cannot be loaded.</exception>
        public static AddressablesReleaseLocation LoadCurrent()
        {
            if (Application.isEditor)
            {
                var manifestPath = Environment.GetEnvironmentVariable(
                    ClientCompatibilityIdentity.RELEASE_MANIFEST_PATH_ENVIRONMENT_VARIABLE);
                var identity = ClientCompatibilityIdentity.LoadCurrent();
                return LoadFromReleaseManifest(manifestPath, identity.BuildTarget);
            }

            var streamingAssetsPath = Application.streamingAssetsPath;
            if (string.IsNullOrWhiteSpace(streamingAssetsPath))
            {
                throw new InvalidOperationException(
                    "Application.streamingAssetsPath is required to load Addressables release metadata.");
            }

            string metadataPath;
            try
            {
                metadataPath = Path.Combine(streamingAssetsPath, METADATA_FILE_NAME);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException(
                    "Application.streamingAssetsPath is not a valid Addressables release metadata directory.",
                    exception);
            }

            var metadataObject = ReadJsonObjectFile(metadataPath, "Addressables release metadata");
            RequireExactPropertySet(metadataObject, METADATA_PROPERTY_NAMES, "Addressables release metadata");
            GetRequiredStringProperty(
                metadataObject,
                REMOTE_LOAD_PATH_PROPERTY_NAME,
                "Addressables release metadata");
            var metadata = ConvertJsonObject<MetadataProjection>(
                metadataObject,
                METADATA_JSON_SETTINGS,
                metadataPath,
                "Addressables release metadata");
            return new AddressablesReleaseLocation(metadata.RemoteLoadPath);
        }

        /// <summary>
        /// Projects the Addressables release location from one explicit Release Manifest.
        /// </summary>
        /// <param name="manifestPath">The existing Release Manifest JSON path.</param>
        /// <param name="buildTarget">The required Unity build target.</param>
        /// <returns>The validated location for the selected build target.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the manifest is missing, malformed, incomplete or ambiguous.</exception>
        /// <exception cref="ArgumentException">Thrown when the requested build target or location is invalid.</exception>
        public static AddressablesReleaseLocation LoadFromReleaseManifest(
            string manifestPath,
            string buildTarget)
        {
            if (string.IsNullOrWhiteSpace(manifestPath))
                throw new InvalidOperationException("An explicit Release Manifest path is required.");

            if (!File.Exists(manifestPath))
            {
                throw new InvalidOperationException(
                    $"The Release Manifest file '{manifestPath}' does not exist.");
            }

            var identity = ClientCompatibilityIdentity.LoadFromReleaseManifest(manifestPath, buildTarget);
            var manifestObject = ReadJsonObjectFile(manifestPath, "Release Manifest");
            var catalogs = GetRequiredArrayProperty(
                manifestObject,
                ADDRESSABLES_CATALOGS_PROPERTY_NAME,
                "Release Manifest");

            string matchingRemoteLoadPath = null;
            var matchingCatalogCount = 0;
            foreach (var catalogToken in catalogs)
            {
                if (!(catalogToken is JObject catalogObject))
                {
                    throw new InvalidOperationException(
                        "Release Manifest addressablesCatalogs entries must be JSON objects.");
                }

                RequireCanonicalPropertyNames(
                    catalogObject,
                    CATALOG_PROPERTY_NAMES,
                    "Release Manifest addressablesCatalogs entry");
                var catalogBuildTarget = GetRequiredStringProperty(
                    catalogObject,
                    BUILD_TARGET_PROPERTY_NAME,
                    "Release Manifest addressablesCatalogs entry");
                var remoteLoadPath = GetRequiredStringProperty(
                    catalogObject,
                    REMOTE_LOAD_PATH_PROPERTY_NAME,
                    "Release Manifest addressablesCatalogs entry");
                if (!string.Equals(catalogBuildTarget, identity.BuildTarget, StringComparison.Ordinal))
                    continue;

                matchingRemoteLoadPath = remoteLoadPath;
                matchingCatalogCount++;
            }

            if (matchingCatalogCount != 1)
            {
                throw new InvalidOperationException(
                    $"Release Manifest must contain exactly one '{identity.BuildTarget}' Addressables catalog; "
                    + $"found {matchingCatalogCount}.");
            }

            return new AddressablesReleaseLocation(matchingRemoteLoadPath);
        }

        private static JObject ReadJsonObjectFile(string path, string description)
        {
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

        private static JArray GetRequiredArrayProperty(
            JObject jsonObject,
            string propertyName,
            string description)
        {
            RequireCanonicalPropertyNames(jsonObject, new[] { propertyName }, description);
            var property = GetProperty(jsonObject, propertyName);
            if (!(property.Value is JArray array))
            {
                throw new InvalidOperationException(
                    $"The {description} property '{propertyName}' must be a JSON array.");
            }

            return array;
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

        private static JProperty GetProperty(JObject jsonObject, string propertyName)
        {
            foreach (var property in jsonObject.Properties())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.Ordinal))
                    return property;
            }

            throw new InvalidOperationException($"Required JSON property '{propertyName}' was not found.");
        }

        private static string GetRequiredStringProperty(
            JObject jsonObject,
            string propertyName,
            string description)
        {
            var value = GetProperty(jsonObject, propertyName).Value;
            if (value.Type != JTokenType.String)
            {
                throw new InvalidOperationException(
                    $"The {description} property '{propertyName}' must be a JSON string.");
            }

            return (string)value;
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

        private sealed class MetadataProjection
        {
            [JsonProperty(REMOTE_LOAD_PATH_PROPERTY_NAME, Required = Required.Always)]
            public string RemoteLoadPath { get; set; }
        }
    }
}
