using System;
using System.IO;
using System.Text;
using Game.Shared.Networking;
using Newtonsoft.Json;
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

            ClientCompatibilityIdentity identity = LoadConfiguredIdentity();
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
            if (!IsSupportedGraphicalPlayer(report) || report.summary.result != BuildResult.Succeeded)
                return;

            ClientCompatibilityIdentity identity = LoadConfiguredIdentity();
            string playerDataDirectory = GetPlayerDataDirectory(report.summary.outputPath);
            string streamingAssetsDirectory = Path.Combine(
                playerDataDirectory,
                STREAMING_ASSETS_DIRECTORY_NAME);
            string metadataPath = Path.Combine(
                streamingAssetsDirectory,
                ClientCompatibilityIdentity.METADATA_FILE_NAME);

            try
            {
                Directory.CreateDirectory(streamingAssetsDirectory);
                string metadataJson = JsonConvert.SerializeObject(identity, Formatting.Indented);
                File.WriteAllText(metadataPath, metadataJson, Encoding.UTF8);
            }
            catch (Exception exception) when (
                exception is IOException
                || exception is UnauthorizedAccessException
                || exception is ArgumentException
                || exception is NotSupportedException)
            {
                throw new BuildFailedException(
                    $"Could not write client compatibility metadata to '{metadataPath}': {exception.Message}");
            }
            catch (JsonException exception)
            {
                throw new BuildFailedException(
                    $"Could not serialize client compatibility metadata: {exception.Message}");
            }
        }

        private static ClientCompatibilityIdentity LoadConfiguredIdentity()
        {
            string manifestPath = Environment.GetEnvironmentVariable(
                ClientCompatibilityIdentity.RELEASE_MANIFEST_PATH_ENVIRONMENT_VARIABLE);

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

            string outputDirectory = Path.GetDirectoryName(fullOutputPath);
            string outputName = Path.GetFileNameWithoutExtension(fullOutputPath);
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
