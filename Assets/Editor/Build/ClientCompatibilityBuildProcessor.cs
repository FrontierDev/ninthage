using Game.Shared.Networking;
using Game.Shared.Utility;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Game.Editor.Build
{
    /// <summary>
    /// Stamps validated client compatibility metadata into supported Windows Player builds.
    /// </summary>
    internal sealed class ClientCompatibilityBuildProcessor :
        IPreprocessBuildWithReport,
        IPostprocessBuildWithReport
    {
        private const string PLAYER_DATA_DIRECTORY_SUFFIX = "_Data";
        private const string STREAMING_ASSETS_DIRECTORY_NAME = "StreamingAssets";
        private const string ADDRESSABLES_DIRECTORY_NAME = "aa";
        private const string CATALOG_HASH_FILE_NAME = "catalog.hash";
        private const string CATALOG_BINARY_FILE_NAME = "catalog.bin";
        private const string RUNTIME_SETTINGS_FILE_NAME = "settings.json";

        /// <summary>
        /// Gets the order in which this build callback runs.
        /// </summary>
        public int callbackOrder => 0;

        /// <summary>
        /// Validates the selected Release Manifest before a supported Player build starts.
        /// </summary>
        /// <param name="report">The Unity build report.</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            if (!IsSupportedGraphicalPlayer(report))
                return;

            var manifestPath = GetConfiguredManifestPath();
            var identity = LoadConfiguredIdentity(manifestPath);
            if (!string.Equals(identity.ClientBuildVersion, PlayerSettings.bundleVersion, StringComparison.Ordinal))
            {
                throw new BuildFailedException(
                    $"Release Manifest clientBuildVersion '{identity.ClientBuildVersion}' does not match "
                    + $"PlayerSettings.bundleVersion '{PlayerSettings.bundleVersion}'.");
            }
        }

        /// <summary>
        /// Writes the selected Release Manifest projection into a successful Player build.
        /// </summary>
        /// <param name="report">The Unity build report.</param>
        public void OnPostprocessBuild(BuildReport report)
        {
            if (!IsSupportedGraphicalPlayer(report))
                return;

            var manifestPath = GetConfiguredManifestPath();
            var identity = LoadConfiguredIdentity(manifestPath);
            var playerDataDirectory = GetPlayerDataDirectory(report.summary.outputPath);
            var streamingAssetsDirectory = Path.Combine(
                playerDataDirectory,
                STREAMING_ASSETS_DIRECTORY_NAME);
            var metadataPath = Path.Combine(
                streamingAssetsDirectory,
                ClientCompatibilityIdentity.METADATA_FILE_NAME);
            var sourceBranchMetadataPath = Path.Combine(
                streamingAssetsDirectory,
                ClientSourceBranch.METADATA_FILE_NAME);

            try
            {
                ValidatePackagedLocalCatalog(streamingAssetsDirectory, identity);
                // Existing Player installations are never mutated; rebuilding is required to remove obsolete metadata.
                Directory.CreateDirectory(streamingAssetsDirectory);
                string sourceBranch;
                try
                {
                    sourceBranch = ClientSourceBranch.LoadCurrent();
                }
                catch (Exception exception)
                {
                    throw new BuildFailedException(
                        $"Could not determine the client source branch for '{sourceBranchMetadataPath}': {exception.Message}");
                }

                var metadataJson = JsonConvert.SerializeObject(identity, Formatting.Indented);
                File.WriteAllText(metadataPath, metadataJson, Encoding.UTF8);

                try
                {
                    File.WriteAllText(
                        sourceBranchMetadataPath,
                        sourceBranch + "\n",
                        Encoding.UTF8);
                }
                catch (Exception exception)
                {
                    throw new BuildFailedException(
                        $"Could not write client source branch metadata to '{sourceBranchMetadataPath}': {exception.Message}");
                }
            }
            catch (Exception exception) when (
                exception is IOException
                || exception is UnauthorizedAccessException
                || exception is ArgumentException
                || exception is NotSupportedException)
            {
                throw new BuildFailedException(
                    $"Could not write Player compatibility metadata to '{metadataPath}': {exception.Message}");
            }
            catch (JsonException exception)
            {
                throw new BuildFailedException(
                    $"Could not serialize client compatibility metadata: {exception.Message}");
            }
        }

        private static void ValidatePackagedLocalCatalog(
            string streamingAssetsDirectory,
            ClientCompatibilityIdentity identity)
        {
            var addressablesDirectory = Path.Combine(streamingAssetsDirectory, ADDRESSABLES_DIRECTORY_NAME);
            if (!Directory.Exists(addressablesDirectory))
            {
                throw new BuildFailedException(
                    $"The graphical Player does not contain the local Addressables directory '{addressablesDirectory}'.");
            }

            var catalogHashPaths = Directory.GetFiles(
                addressablesDirectory,
                CATALOG_HASH_FILE_NAME,
                SearchOption.AllDirectories);
            if (catalogHashPaths.Length != 1)
            {
                throw new BuildFailedException(
                    $"The graphical Player must contain exactly one packaged '{CATALOG_HASH_FILE_NAME}' file; "
                    + $"found {catalogHashPaths.Length}.");
            }

            var catalogHashPath = catalogHashPaths[0];
            var catalogDirectory = Path.GetDirectoryName(catalogHashPath);
            var catalogPath = Path.Combine(catalogDirectory, CATALOG_BINARY_FILE_NAME);
            var runtimeSettingsPath = Path.Combine(catalogDirectory, RUNTIME_SETTINGS_FILE_NAME);
            if (!File.Exists(catalogPath) || !File.Exists(runtimeSettingsPath))
            {
                throw new BuildFailedException(
                    $"The packaged local Addressables catalog beside '{catalogHashPath}' is incomplete.");
            }

            string packagedCatalogHash;
            try
            {
                packagedCatalogHash = File.ReadAllText(catalogHashPath).Trim();
            }
            catch (Exception exception) when (
                exception is IOException
                || exception is UnauthorizedAccessException
                || exception is ArgumentException)
            {
                throw new BuildFailedException(
                    $"Could not read the packaged local Addressables catalog hash '{catalogHashPath}': {exception.Message}");
            }

            if (!string.Equals(packagedCatalogHash, identity.CatalogHash, StringComparison.Ordinal))
            {
                throw new BuildFailedException(
                    $"Packaged local Addressables catalog hash '{packagedCatalogHash}' does not match "
                    + $"the prepared Release Manifest hash '{identity.CatalogHash}'. Re-run Development Release preparation.");
            }

            try
            {
                var catalogText = Encoding.UTF8.GetString(File.ReadAllBytes(catalogPath))
                    + Environment.NewLine
                    + File.ReadAllText(runtimeSettingsPath);
                if (ContainsRemoteLocation(catalogText))
                {
                    throw new BuildFailedException(
                        "The graphical Player's packaged Addressables catalog contains a remote or CCD bundle path.");
                }
            }
            catch (BuildFailedException)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is IOException
                || exception is UnauthorizedAccessException
                || exception is ArgumentException)
            {
                throw new BuildFailedException(
                    $"Could not inspect the packaged local Addressables catalog: {exception.Message}");
            }
        }

        private static bool ContainsRemoteLocation(string value)
        {
            return value.IndexOf("http://", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("https://", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("unity3dusercontent.com", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("release_by_badge", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static ClientCompatibilityIdentity LoadConfiguredIdentity(string manifestPath)
        {
            try
            {
                return ClientCompatibilityIdentity.LoadFromReleaseManifest(
                    manifestPath,
                    "StandaloneWindows64");
            }
            catch (ArgumentException exception)
            {
                throw new BuildFailedException(
                    $"The configured Release Manifest is invalid: {exception.Message}");
            }
            catch (InvalidOperationException exception)
            {
                throw new BuildFailedException(
                    $"The configured Release Manifest could not be used: {exception.Message}");
            }
        }

        private static string GetConfiguredManifestPath()
        {
            return Environment.GetEnvironmentVariable(
                ClientCompatibilityIdentity.RELEASE_MANIFEST_PATH_ENVIRONMENT_VARIABLE);
        }

        private static string GetPlayerDataDirectory(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new BuildFailedException(
                    "The successful Windows Player build report did not provide an output path.");
            }

            string fullOutputPath;
            try
            {
                fullOutputPath = Path.GetFullPath(outputPath);
            }
            catch (Exception exception) when (
                exception is ArgumentException
                || exception is NotSupportedException
                || exception is PathTooLongException)
            {
                throw new BuildFailedException(
                    $"The Windows Player output path '{outputPath}' is invalid: {exception.Message}");
            }

            var outputDirectory = Path.GetDirectoryName(fullOutputPath);
            var outputName = Path.GetFileNameWithoutExtension(fullOutputPath);
            if (string.IsNullOrWhiteSpace(outputDirectory) || string.IsNullOrWhiteSpace(outputName))
            {
                throw new BuildFailedException(
                    $"The Windows Player output path '{outputPath}' does not identify a player executable.");
            }

            return Path.Combine(outputDirectory, outputName + PLAYER_DATA_DIRECTORY_SUFFIX);
        }

        private static bool IsSupportedGraphicalPlayer(BuildReport report)
        {
            return report.summary.platform == BuildTarget.StandaloneWindows64
                && report.summary.GetSubtarget<StandaloneBuildSubtarget>() == StandaloneBuildSubtarget.Player;
        }
    }
}
