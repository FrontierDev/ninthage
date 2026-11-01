using Game.Shared.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build;
using UnityEngine;

namespace Game.Editor.Build
{
    /// <summary>
    /// Contains the result of one Development Release preparation attempt.
    /// </summary>
    internal sealed class DevelopmentReleasePreparationResult
    {
        private DevelopmentReleasePreparationResult(
            bool succeeded,
            string message,
            string manifestPath,
            string releaseID,
            long catalogRevision,
            string catalogHash,
            string contentStateSHA256,
            string unityGitSHA)
        {
            Succeeded = succeeded;
            Message = message;
            ManifestPath = manifestPath;
            ReleaseID = releaseID;
            CatalogRevision = catalogRevision;
            CatalogHash = catalogHash;
            ContentStateSHA256 = contentStateSHA256;
            UnityGitSHA = unityGitSHA;
        }

        /// <summary>Gets whether the preparation completed and was published.</summary>
        public bool Succeeded { get; }

        /// <summary>Gets the user-facing preparation result.</summary>
        public string Message { get; }

        /// <summary>Gets the active Development manifest path.</summary>
        public string ManifestPath { get; }

        /// <summary>Gets the prepared release ID.</summary>
        public string ReleaseID { get; }

        /// <summary>Gets the prepared catalog revision.</summary>
        public long CatalogRevision { get; }

        /// <summary>Gets the prepared local catalog hash.</summary>
        public string CatalogHash { get; }

        /// <summary>Gets the SHA-256 of the prepared content-state file.</summary>
        public string ContentStateSHA256 { get; }

        /// <summary>Gets the committed Unity source SHA used for both build identities.</summary>
        public string UnityGitSHA { get; }

        internal static DevelopmentReleasePreparationResult Success(
            string message,
            string manifestPath,
            string releaseID,
            long catalogRevision,
            string catalogHash,
            string contentStateSHA256,
            string unityGitSHA)
        {
            return new DevelopmentReleasePreparationResult(
                true,
                message,
                manifestPath,
                releaseID,
                catalogRevision,
                catalogHash,
                contentStateSHA256,
                unityGitSHA);
        }

        internal static DevelopmentReleasePreparationResult Failure(string message, string manifestPath)
        {
            return new DevelopmentReleasePreparationResult(
                false,
                message,
                manifestPath,
                string.Empty,
                0,
                string.Empty,
                string.Empty,
                string.Empty);
        }
    }

    /// <summary>
    /// Builds local Addressables content and prepares the Development Release Manifest.
    /// </summary>
    internal static class DevelopmentReleaseBuilder
    {
        internal const string MASTER_REPOSITORY_EDITOR_PREFS_KEY =
            "NinthAge.DevelopmentRelease.MasterRepositoryPath";

        private const string DEVELOPMENT_MANIFEST_FILE_NAME = "development.json";
        private const string SUPPORTED_BUILD_TARGET = "StandaloneWindows64";
        private const string REQUIRED_PROFILE_NAME = "Local";
        private const string ENVIRONMENT_NAME = "development";
        private const string CATALOG_HASH_FILE_NAME = "catalog.hash";
        private const string CATALOG_BINARY_FILE_NAME = "catalog.bin";
        private const string SETTINGS_FILE_NAME = "settings.json";
        private const string CONTENT_STATE_FILE_NAME = "addressables_content_state.bin";

        private static readonly string[] MANIFEST_V1_PROPERTIES =
        {
            "releaseID",
            "environment",
            "clientBuildVersion",
            "clientBuildID",
            "serverBuildID",
            "protocolVersion",
            "gameplayContentRevision",
            "gameplayContentManifestHash",
            "addressablesCatalogs",
            "databaseSchemaVersion"
        };

        private static readonly string[] MANIFEST_V2_PROPERTIES =
        {
            "manifestVersion",
            "releaseID",
            "environment",
            "clientBuildVersion",
            "clientBuildID",
            "serverBuildID",
            "protocolVersion",
            "gameplayContentRevision",
            "gameplayContentManifestHash",
            "addressablesCatalogs",
            "databaseSchemaVersion",
            "createdAtUTC"
        };

        private static readonly string[] CATALOG_V1_PROPERTIES =
        {
            "buildTarget",
            "catalogRevision",
            "catalogHash",
            "remoteLoadPath"
        };

        private static readonly string[] CATALOG_V2_PROPERTIES =
        {
            "buildTarget",
            "catalogRevision",
            "catalogHash",
            "contentStateSHA256"
        };

        /// <summary>
        /// Prepares one local Development Release Manifest from a fresh Addressables build.
        /// </summary>
        /// <param name="masterRepositoryPath">The explicitly selected Master repository root.</param>
        /// <returns>The single preparation result for this invocation.</returns>
        internal static DevelopmentReleasePreparationResult Prepare(string masterRepositoryPath)
        {
            string manifestPath = TryGetManifestPath(masterRepositoryPath);
            try
            {
                ValidateEditorState();
                var settings = ValidateAddressablesSettings();
                var manifest = LoadDevelopmentManifest(masterRepositoryPath);
                string unityGitSHA = ValidateUnityRepositoryGitState();
                ValidatePlayerVersion();

                var carriedForward = ReadCarriedForwardValues(manifest.Manifest);
                var artifacts = BuildAndReadAddressables(settings);
                var preparedManifest = BuildManifest(manifest, carriedForward, artifacts, unityGitSHA);
                var publication = PublishManifest(manifest, preparedManifest, artifacts, unityGitSHA);

                Environment.SetEnvironmentVariable(
                    ClientCompatibilityIdentity.RELEASE_MANIFEST_PATH_ENVIRONMENT_VARIABLE,
                    publication.ManifestPath,
                    EnvironmentVariableTarget.Process);

                return DevelopmentReleasePreparationResult.Success(
                    publication.Message,
                    publication.ManifestPath,
                    publication.ReleaseID,
                    publication.CatalogRevision,
                    artifacts.CatalogHash,
                    artifacts.ContentStateSHA256,
                    unityGitSHA);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                return DevelopmentReleasePreparationResult.Failure(
                    exception.Message,
                    manifestPath);
            }
        }

        private static void ValidateEditorState()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Preflight: Unity must not be in Play Mode.");

            if (EditorApplication.isCompiling)
                throw new InvalidOperationException("Preflight: Unity is compiling; try again after compilation finishes.");

            if (BuildPipeline.isBuildingPlayer)
                throw new InvalidOperationException("Preflight: a Player build is already running.");

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
            {
                throw new InvalidOperationException(
                    $"Preflight: active build target must be {SUPPORTED_BUILD_TARGET}; "
                    + $"found {EditorUserBuildSettings.activeBuildTarget}.");
            }

            if (EditorUserBuildSettings.standaloneBuildSubtarget != StandaloneBuildSubtarget.Player)
            {
                throw new InvalidOperationException(
                    "Preflight: active Standalone subtarget must be the graphical Player subtarget.");
            }
        }

        private static AddressableAssetSettings ValidateAddressablesSettings()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                throw new InvalidOperationException("Preflight: Addressable Asset Settings are not configured.");

            string profileName = settings.profileSettings.GetProfileName(settings.activeProfileId);
            if (!string.Equals(profileName, REQUIRED_PROFILE_NAME, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Preflight: the active Addressables profile must be '{REQUIRED_PROFILE_NAME}'; "
                    + $"found '{profileName}'.");
            }

            if (settings.BuildRemoteCatalog)
                throw new InvalidOperationException("Preflight: BuildRemoteCatalog must be disabled for a local release.");

            if (!settings.DisableCatalogUpdateOnStartup)
            {
                throw new InvalidOperationException(
                    "Preflight: automatic remote catalog updates must be disabled for a local release.");
            }

            if (!(settings.ActivePlayerDataBuilder is BuildScriptPackedMode))
            {
                throw new InvalidOperationException(
                    "Preflight: the active Addressables Player Data Builder must be Packed Mode.");
            }

            foreach (var group in settings.groups)
            {
                if (group == null)
                    continue;

                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null || !schema.IncludeInBuild)
                    continue;

                string buildPath = schema.BuildPath?.GetValue(settings);
                string loadPath = schema.LoadPath?.GetValue(settings);
                if (IsRemotePath(buildPath) || IsRemotePath(loadPath))
                {
                    throw new InvalidOperationException(
                        $"Preflight: Addressables group '{group.Name}' has a remote build or load path.");
                }

                if (string.IsNullOrWhiteSpace(buildPath) || string.IsNullOrWhiteSpace(loadPath))
                {
                    throw new InvalidOperationException(
                        $"Preflight: Addressables group '{group.Name}' must have local build and load paths.");
                }
            }

            return settings;
        }

        private static ManifestDocument LoadDevelopmentManifest(string masterRepositoryPath)
        {
            if (string.IsNullOrWhiteSpace(masterRepositoryPath))
                throw new InvalidOperationException("Preflight: choose the Master repository root first.");

            string masterRoot = GetFullPath(masterRepositoryPath, "Master repository root");
            if (!Directory.Exists(masterRoot))
                throw new InvalidOperationException($"Preflight: Master repository '{masterRoot}' does not exist.");

            string contractsPath = Path.Combine(
                masterRoot,
                "src",
                "NinthAge.Contracts",
                "Releases",
                "ReleaseManifest.cs");
            if (!File.Exists(contractsPath))
            {
                throw new InvalidOperationException(
                    $"Preflight: Master repository '{masterRoot}' is missing '{contractsPath}'.");
            }

            string manifestPath = GetManifestPath(masterRoot);
            if (!File.Exists(manifestPath))
            {
                throw new InvalidOperationException(
                    $"Preflight: Development Release Manifest '{manifestPath}' does not exist.");
            }

            JObject manifest = ReadJsonObject(manifestPath, "Development Release Manifest");
            int manifestVersion = GetManifestVersion(manifest);
            ValidateManifest(manifest, manifestVersion, manifestPath);
            return new ManifestDocument(manifestPath, manifest, manifestVersion);
        }

        private static string ValidateUnityRepositoryGitState()
        {
            string projectRoot = GetFullPath(Path.Combine(Application.dataPath, ".."), "Unity repository root");
            string topLevel = RunGit(projectRoot, "rev-parse --show-toplevel").Trim();
            if (!PathsEqual(projectRoot, topLevel))
                throw new InvalidOperationException("Git: the Unity project is not the Git repository root.");

            string commitSHA = RunGit(projectRoot, "rev-parse HEAD").Trim();
            if (!IsLowercaseHex(commitSHA, 40))
                throw new InvalidOperationException("Git: rev-parse HEAD did not return a 40-character lowercase SHA.");

            string branchName = RunGit(projectRoot, "symbolic-ref --quiet --short HEAD").Trim();
            if (string.IsNullOrWhiteSpace(branchName) || string.Equals(branchName, "HEAD", StringComparison.Ordinal))
                throw new InvalidOperationException("Git: detached HEAD is not allowed for a stamped release.");

            string status = RunGit(projectRoot, "status --porcelain=v1 --untracked-files=all");
            foreach (string rawLine in status.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (rawLine.Length < 4)
                    throw new InvalidOperationException("Git: status returned an unreadable worktree entry.");

                string path = rawLine.Substring(3).Trim();
                if (path.IndexOf(" -> ", StringComparison.Ordinal) >= 0)
                    path = path.Substring(path.LastIndexOf(" -> ", StringComparison.Ordinal) + 4);

                path = path.Trim('"').Replace('\\', '/');
                if (!IsIgnoredGeneratedPath(path))
                {
                    throw new InvalidOperationException(
                        $"Git: source worktree is dirty at '{path}'. Commit source changes before preparing a release.");
                }
            }

            return commitSHA;
        }

        private static void ValidatePlayerVersion()
        {
            if (!IsPlayerVersion(PlayerSettings.bundleVersion))
            {
                throw new InvalidOperationException(
                    "Preflight: PlayerSettings.bundleVersion must use MAJOR.MINOR.PATCH.");
            }
        }

        private static CarriedForwardValues ReadCarriedForwardValues(JObject manifest)
        {
            return new CarriedForwardValues(
                ReadRequiredInt(manifest, "protocolVersion", "Development Release Manifest"),
                ReadRequiredLong(manifest, "gameplayContentRevision", "Development Release Manifest"),
                ReadRequiredString(manifest, "gameplayContentManifestHash", "Development Release Manifest"),
                ReadRequiredString(manifest, "databaseSchemaVersion", "Development Release Manifest"));
        }

        private static AddressablesArtifacts BuildAndReadAddressables(AddressableAssetSettings settings)
        {
            AddressablesPlayerBuildResult buildResult;
            try
            {
                AddressableAssetSettings.BuildPlayerContent(out buildResult);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Addressables build: the local content build threw an exception.", exception);
            }

            if (buildResult == null)
                throw new InvalidOperationException("Addressables build: returned a null build result.");

            if (!string.IsNullOrWhiteSpace(buildResult.Error))
            {
                throw new InvalidOperationException(
                    $"Addressables build: {buildResult.Error}");
            }

            if (buildResult.FileRegistry == null)
                throw new InvalidOperationException("Addressables artifacts: the build returned no file registry.");

            var registryPaths = new List<string>();
            foreach (string path in buildResult.FileRegistry.GetFilePaths())
                registryPaths.Add(ResolveArtifactPath(path));

            string settingsPath = ResolveArtifactPath(buildResult.OutputPath);
            RequireRegisteredArtifact(settingsPath, registryPaths, "runtime settings");
            string outputDirectory = Path.GetDirectoryName(settingsPath);
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new InvalidOperationException("Addressables artifacts: runtime settings has no output directory.");

            string catalogHashPath = FindRegisteredSibling(
                registryPaths,
                outputDirectory,
                CATALOG_HASH_FILE_NAME,
                "catalog hash");
            string catalogPath = FindRegisteredSibling(
                registryPaths,
                outputDirectory,
                CATALOG_BINARY_FILE_NAME,
                "local catalog");
            FindRegisteredSibling(registryPaths, outputDirectory, SETTINGS_FILE_NAME, "runtime settings");

            if (buildResult.AssetBundleBuildResults == null || buildResult.AssetBundleBuildResults.Count == 0)
                throw new InvalidOperationException("Addressables artifacts: the build produced no bundles.");

            foreach (var bundle in buildResult.AssetBundleBuildResults)
            {
                if (bundle == null || string.IsNullOrWhiteSpace(bundle.FilePath))
                    throw new InvalidOperationException("Addressables artifacts: a bundle has no output path.");

                string bundlePath = ResolveArtifactPath(bundle.FilePath);
                if (!File.Exists(bundlePath))
                    throw new InvalidOperationException($"Addressables artifacts: bundle '{bundlePath}' is missing.");

                RequireRegisteredArtifact(bundlePath, registryPaths, "bundle");
            }

            string contentStatePath = ResolveContentStatePath(settings, buildResult);
            RequireRegisteredArtifact(contentStatePath, registryPaths, "content-state");

            string catalogHash = File.ReadAllText(catalogHashPath).Trim();
            if (string.IsNullOrWhiteSpace(catalogHash) || ContainsWhitespace(catalogHash))
            {
                throw new InvalidOperationException(
                    $"Addressables artifacts: '{catalogHashPath}' does not contain a valid catalog hash token.");
            }

            ValidateLocalCatalog(catalogPath, settingsPath);
            string contentStateSHA256 = ComputeSHA256(contentStatePath);
            return new AddressablesArtifacts(catalogHash, contentStateSHA256);
        }

        private static JObject BuildManifest(
            ManifestDocument existing,
            CarriedForwardValues carriedForward,
            AddressablesArtifacts artifacts,
            string unityGitSHA)
        {
            JObject oldManifest = existing.Manifest;
            JObject selectedCatalog = FindSelectedCatalog(oldManifest);
            long oldCatalogRevision = ReadRequiredLong(selectedCatalog, "catalogRevision", "catalog entry");
            string oldCatalogHash = ReadRequiredString(selectedCatalog, "catalogHash", "catalog entry");
            string oldContentStateSHA256 = existing.ManifestVersion == 2
                ? ReadRequiredString(selectedCatalog, "contentStateSHA256", "catalog entry")
                : string.Empty;

            string oldClientBuildVersion = ReadRequiredString(oldManifest, "clientBuildVersion", "Development Release Manifest");
            string oldClientBuildID = ReadRequiredString(oldManifest, "clientBuildID", "Development Release Manifest");
            string oldServerBuildID = ReadRequiredString(oldManifest, "serverBuildID", "Development Release Manifest");
            int oldProtocolVersion = ReadRequiredInt(oldManifest, "protocolVersion", "Development Release Manifest");
            long oldGameplayContentRevision = ReadRequiredLong(
                oldManifest,
                "gameplayContentRevision",
                "Development Release Manifest");
            string oldGameplayContentManifestHash = ReadRequiredString(
                oldManifest,
                "gameplayContentManifestHash",
                "Development Release Manifest");
            string oldDatabaseSchemaVersion = ReadRequiredString(
                oldManifest,
                "databaseSchemaVersion",
                "Development Release Manifest");

            bool sameIdentity = string.Equals(oldClientBuildVersion, PlayerSettings.bundleVersion, StringComparison.Ordinal)
                && string.Equals(oldClientBuildID, unityGitSHA, StringComparison.Ordinal)
                && string.Equals(oldServerBuildID, unityGitSHA, StringComparison.Ordinal)
                && oldProtocolVersion == carriedForward.ProtocolVersion
                && oldGameplayContentRevision == carriedForward.GameplayContentRevision
                && string.Equals(
                    oldGameplayContentManifestHash,
                    carriedForward.GameplayContentManifestHash,
                    StringComparison.Ordinal)
                && string.Equals(oldDatabaseSchemaVersion, carriedForward.DatabaseSchemaVersion, StringComparison.Ordinal);
            bool sameArtifacts = existing.ManifestVersion == 2
                && string.Equals(oldCatalogHash, artifacts.CatalogHash, StringComparison.Ordinal)
                && string.Equals(oldContentStateSHA256, artifacts.ContentStateSHA256, StringComparison.Ordinal);
            bool preserveRelease = existing.ManifestVersion == 2 && sameIdentity && sameArtifacts;
            bool incrementCatalogRevision = existing.ManifestVersion == 1 || !sameArtifacts;
            long catalogRevision = incrementCatalogRevision
                ? checked(oldCatalogRevision + 1)
                : oldCatalogRevision;

            string releaseID = preserveRelease
                ? ReadRequiredString(oldManifest, "releaseID", "Development Release Manifest")
                : Guid.NewGuid().ToString("D");
            string createdAtUTC = preserveRelease
                ? ReadRequiredString(oldManifest, "createdAtUTC", "Development Release Manifest")
                : DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);

            var result = new JObject
            {
                ["manifestVersion"] = 2,
                ["releaseID"] = releaseID,
                ["environment"] = ENVIRONMENT_NAME,
                ["clientBuildVersion"] = PlayerSettings.bundleVersion,
                ["clientBuildID"] = unityGitSHA,
                ["serverBuildID"] = unityGitSHA,
                ["protocolVersion"] = carriedForward.ProtocolVersion,
                ["gameplayContentRevision"] = carriedForward.GameplayContentRevision,
                ["gameplayContentManifestHash"] = carriedForward.GameplayContentManifestHash,
                ["addressablesCatalogs"] = BuildCatalogArray(
                    oldManifest,
                    catalogRevision,
                    artifacts),
                ["databaseSchemaVersion"] = carriedForward.DatabaseSchemaVersion,
                ["createdAtUTC"] = createdAtUTC
            };

            ValidateManifest(result, 2, existing.ManifestPath);
            return result;
        }

        private static JArray BuildCatalogArray(
            JObject oldManifest,
            long catalogRevision,
            AddressablesArtifacts artifacts)
        {
            var oldCatalogs = GetRequiredArray(oldManifest, "addressablesCatalogs", "Development Release Manifest");
            var catalogs = new JArray();
            foreach (JToken token in oldCatalogs)
            {
                var oldCatalog = token as JObject;
                if (oldCatalog == null)
                    throw new InvalidOperationException("Manifest: every Addressables catalog entry must be an object.");

                string buildTarget = ReadRequiredString(oldCatalog, "buildTarget", "catalog entry");
                long revision = ReadRequiredLong(oldCatalog, "catalogRevision", "catalog entry");
                string hash = ReadRequiredString(oldCatalog, "catalogHash", "catalog entry");
                string contentStateSHA256;
                if (string.Equals(buildTarget, SUPPORTED_BUILD_TARGET, StringComparison.Ordinal))
                {
                    revision = catalogRevision;
                    hash = artifacts.CatalogHash;
                    contentStateSHA256 = artifacts.ContentStateSHA256;
                }
                else
                {
                    contentStateSHA256 = ReadRequiredString(oldCatalog, "contentStateSHA256", "catalog entry");
                }

                catalogs.Add(new JObject
                {
                    ["buildTarget"] = buildTarget,
                    ["catalogRevision"] = revision,
                    ["catalogHash"] = hash,
                    ["contentStateSHA256"] = contentStateSHA256
                });
            }

            return catalogs;
        }

        private static Publication PublishManifest(
            ManifestDocument existing,
            JObject preparedManifest,
            AddressablesArtifacts artifacts,
            string unityGitSHA)
        {
            var preparedIdentity = ProjectIdentity(preparedManifest);
            bool identical = existing.ManifestVersion == 2
                && JToken.DeepEquals(existing.Manifest, preparedManifest);

            if (!identical)
            {
                string tempPath = Path.Combine(
                    Path.GetDirectoryName(existing.ManifestPath),
                    $".{Path.GetFileName(existing.ManifestPath)}.{Guid.NewGuid():N}.tmp");
                try
                {
                    File.WriteAllText(tempPath, SerializeManifest(preparedManifest), new UTF8Encoding(false));
                    ValidateManifest(ReadJsonObject(tempPath, "staged Development Release Manifest"), 2, tempPath);
                    var stagedIdentity = ClientCompatibilityIdentity.LoadFromReleaseManifest(
                        tempPath,
                        SUPPORTED_BUILD_TARGET);
                    AssertProjectedIdentity(stagedIdentity, preparedIdentity);
                    File.Replace(tempPath, existing.ManifestPath, null);
                }
                finally
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
            }

            var readbackIdentity = ClientCompatibilityIdentity.LoadFromReleaseManifest(
                existing.ManifestPath,
                SUPPORTED_BUILD_TARGET);
            AssertProjectedIdentity(readbackIdentity, preparedIdentity);
            JObject readbackManifest = ReadJsonObject(existing.ManifestPath, "published Development Release Manifest");
            ValidateManifest(readbackManifest, 2, existing.ManifestPath);

            string releaseID = ReadRequiredString(readbackManifest, "releaseID", "Development Release Manifest");
            long catalogRevision = ReadRequiredLong(
                FindSelectedCatalog(readbackManifest),
                "catalogRevision",
                "catalog entry");
            string message =
                $"Prepared Development Release {releaseID} at '{existing.ManifestPath}'. "
                + "Rebuild graphical Player and World Server; restart Master to activate this release.";
            return new Publication(existing.ManifestPath, releaseID, catalogRevision, message);
        }

        private static ClientCompatibilityIdentity ProjectIdentity(JObject manifest)
        {
            JObject selectedCatalog = FindSelectedCatalog(manifest);
            string target = ReadRequiredString(selectedCatalog, "buildTarget", "catalog entry");
            if (!string.Equals(target, SUPPORTED_BUILD_TARGET, StringComparison.Ordinal))
                throw new InvalidOperationException("Manifest: the prepared catalog target is unsupported.");

            return new ClientCompatibilityIdentity(
                ReadRequiredString(manifest, "clientBuildVersion", "Development Release Manifest"),
                ReadRequiredString(manifest, "clientBuildID", "Development Release Manifest"),
                ReadRequiredInt(manifest, "protocolVersion", "Development Release Manifest"),
                ReadRequiredLong(manifest, "gameplayContentRevision", "Development Release Manifest"),
                ReadRequiredString(manifest, "gameplayContentManifestHash", "Development Release Manifest"),
                target,
                ReadRequiredLong(selectedCatalog, "catalogRevision", "catalog entry"),
                ReadRequiredString(selectedCatalog, "catalogHash", "catalog entry"));
        }

        private static void AssertProjectedIdentity(
            ClientCompatibilityIdentity actual,
            ClientCompatibilityIdentity expected)
        {
            if (!string.Equals(actual.ClientBuildVersion, expected.ClientBuildVersion, StringComparison.Ordinal)
                || !string.Equals(actual.ClientBuildID, expected.ClientBuildID, StringComparison.Ordinal)
                || actual.ProtocolVersion != expected.ProtocolVersion
                || actual.GameplayContentRevision != expected.GameplayContentRevision
                || !string.Equals(
                    actual.GameplayContentManifestHash,
                    expected.GameplayContentManifestHash,
                    StringComparison.Ordinal)
                || actual.CatalogRevision != expected.CatalogRevision
                || !string.Equals(actual.CatalogHash, expected.CatalogHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Manifest readback: the eight-field ClientCompatibilityIdentity projection changed unexpectedly.");
            }
        }

        private static void ValidateManifest(JObject manifest, int manifestVersion, string manifestPath)
        {
            if (!string.Equals(ReadRequiredString(manifest, "environment", "Development Release Manifest"), ENVIRONMENT_NAME, StringComparison.Ordinal))
                throw new InvalidOperationException("Manifest: environment must be exactly 'development'.");

            if (manifestVersion == 1)
                RequireLegacyV1PropertySet(manifest);
            else
                RequirePropertySet(manifest, MANIFEST_V2_PROPERTIES, "Development Release Manifest");
            ValidateGUID(ReadRequiredString(manifest, "releaseID", "Development Release Manifest"), "releaseID");
            ValidatePlayerVersionValue(ReadRequiredString(manifest, "clientBuildVersion", "Development Release Manifest"));
            ValidateSHA(ReadRequiredString(manifest, "clientBuildID", "Development Release Manifest"), 40, "clientBuildID");
            ValidateSHA(ReadRequiredString(manifest, "serverBuildID", "Development Release Manifest"), 40, "serverBuildID");
            int protocolVersion = ReadRequiredInt(manifest, "protocolVersion", "Development Release Manifest");
            if (protocolVersion <= 0)
                throw new InvalidOperationException("Manifest: protocolVersion must be positive.");

            long gameplayRevision = ReadRequiredLong(
                manifest,
                "gameplayContentRevision",
                "Development Release Manifest");
            if (gameplayRevision <= 0)
                throw new InvalidOperationException("Manifest: gameplayContentRevision must be positive.");

            ValidateSHA(
                ReadRequiredString(manifest, "gameplayContentManifestHash", "Development Release Manifest"),
                64,
                "gameplayContentManifestHash");
            if (string.IsNullOrWhiteSpace(ReadRequiredString(manifest, "databaseSchemaVersion", "Development Release Manifest")))
                throw new InvalidOperationException("Manifest: databaseSchemaVersion must be nonblank.");

            if (manifestVersion == 2)
            {
                string createdAtUTC = ReadRequiredString(manifest, "createdAtUTC", "Development Release Manifest");
                ValidateUTCTimestamp(createdAtUTC, manifestPath);
            }
            else if (manifest["createdAtUTC"] != null)
            {
                ValidateUTCTimestamp(
                    ReadRequiredString(manifest, "createdAtUTC", "Development Release Manifest"),
                    manifestPath);
            }

            var catalogs = GetRequiredArray(manifest, "addressablesCatalogs", "Development Release Manifest");
            var seenTargets = new HashSet<string>(StringComparer.Ordinal);
            var matchingCount = 0;
            foreach (JToken token in catalogs)
            {
                var catalog = token as JObject;
                if (catalog == null)
                    throw new InvalidOperationException("Manifest: every Addressables catalog entry must be an object.");

                RequirePropertySet(
                    catalog,
                    manifestVersion == 1 ? CATALOG_V1_PROPERTIES : CATALOG_V2_PROPERTIES,
                    "Addressables catalog entry");
                string buildTarget = ReadRequiredString(catalog, "buildTarget", "Addressables catalog entry");
                if (!seenTargets.Add(buildTarget))
                    throw new InvalidOperationException($"Manifest: duplicate Addressables target '{buildTarget}'.");

                long catalogRevision = ReadRequiredLong(catalog, "catalogRevision", "Addressables catalog entry");
                if (catalogRevision <= 0)
                    throw new InvalidOperationException("Manifest: catalogRevision must be positive.");

                if (string.IsNullOrWhiteSpace(ReadRequiredString(catalog, "catalogHash", "Addressables catalog entry")))
                    throw new InvalidOperationException("Manifest: catalogHash must be nonblank.");

                if (string.Equals(buildTarget, SUPPORTED_BUILD_TARGET, StringComparison.Ordinal))
                {
                    matchingCount++;
                    if (manifestVersion == 1
                        && string.IsNullOrWhiteSpace(ReadRequiredString(catalog, "remoteLoadPath", "Addressables catalog entry")))
                    {
                        throw new InvalidOperationException("Manifest: the v1 Windows catalog remoteLoadPath must be nonblank.");
                    }
                }

                if (manifestVersion == 2)
                    ValidateSHA(ReadRequiredString(catalog, "contentStateSHA256", "Addressables catalog entry"), 64, "contentStateSHA256");
            }

            if (matchingCount != 1)
            {
                throw new InvalidOperationException(
                    $"Manifest: exactly one '{SUPPORTED_BUILD_TARGET}' Addressables catalog is required; found {matchingCount}.");
            }
        }

        private static JObject FindSelectedCatalog(JObject manifest)
        {
            var catalogs = GetRequiredArray(manifest, "addressablesCatalogs", "Development Release Manifest");
            JObject selected = null;
            foreach (JToken token in catalogs)
            {
                var catalog = token as JObject;
                if (catalog == null)
                    continue;

                if (!string.Equals(
                    ReadRequiredString(catalog, "buildTarget", "Addressables catalog entry"),
                    SUPPORTED_BUILD_TARGET,
                    StringComparison.Ordinal))
                {
                    continue;
                }

                if (selected != null)
                    throw new InvalidOperationException("Manifest: multiple Windows Addressables catalogs were found.");
                selected = catalog;
            }

            if (selected == null)
                throw new InvalidOperationException("Manifest: the Windows Addressables catalog is missing.");
            return selected;
        }

        private static JObject ReadJsonObject(string path, string description)
        {
            try
            {
                var parsed = JObject.Parse(File.ReadAllText(path), new JsonLoadSettings
                {
                    DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
                });
                return parsed;
            }
            catch (Exception exception) when (
                exception is IOException
                || exception is UnauthorizedAccessException
                || exception is JsonException)
            {
                throw new InvalidOperationException($"Manifest: {description} '{path}' could not be read as JSON.", exception);
            }
        }

        private static void RequirePropertySet(JObject value, string[] requiredProperties, string description)
        {
            var expected = new HashSet<string>(requiredProperties, StringComparer.Ordinal);
            var actual = new HashSet<string>(StringComparer.Ordinal);
            foreach (JProperty property in value.Properties())
            {
                if (!actual.Add(property.Name) || !expected.Contains(property.Name))
                    throw new InvalidOperationException($"Manifest: {description} contains unknown or duplicate property '{property.Name}'.");
            }

            if (actual.Count != expected.Count)
                throw new InvalidOperationException($"Manifest: {description} is missing one or more required properties.");
        }

        private static void RequireLegacyV1PropertySet(JObject manifest)
        {
            var required = new HashSet<string>(MANIFEST_V1_PROPERTIES, StringComparer.Ordinal);
            var allowed = new HashSet<string>(required, StringComparer.Ordinal)
            {
                "manifestVersion",
                "createdAtUTC"
            };
            var actual = new HashSet<string>(StringComparer.Ordinal);
            foreach (JProperty property in manifest.Properties())
            {
                if (!actual.Add(property.Name) || !allowed.Contains(property.Name))
                {
                    throw new InvalidOperationException(
                        $"Manifest: Development Release Manifest contains unknown or duplicate property '{property.Name}'.");
                }
            }

            foreach (string propertyName in required)
            {
                if (!actual.Contains(propertyName))
                    throw new InvalidOperationException("Manifest: Development Release Manifest is missing one or more required properties.");
            }
        }

        private static void ValidateUTCTimestamp(string value, string manifestPath)
        {
            if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out DateTimeOffset parsed)
                || parsed.Offset != TimeSpan.Zero)
            {
                throw new InvalidOperationException(
                    $"Manifest: createdAtUTC in '{manifestPath}' must be a UTC timestamp.");
            }
        }

        private static int GetManifestVersion(JObject manifest)
        {
            JToken version = manifest["manifestVersion"];
            if (version == null)
                return 1;
            if (version.Type != JTokenType.Integer || !int.TryParse(version.ToString(), out int parsed))
                throw new InvalidOperationException("Manifest: manifestVersion must be an integer 1 or 2.");
            if (parsed != 1 && parsed != 2)
                throw new InvalidOperationException($"Manifest: unsupported manifestVersion '{parsed}'.");
            return parsed;
        }

        private static string ReadRequiredString(JObject value, string propertyName, string description)
        {
            JToken token = value[propertyName];
            if (token == null || token.Type != JTokenType.String)
                throw new InvalidOperationException($"Manifest: {description} requires string property '{propertyName}'.");
            return token.Value<string>();
        }

        private static int ReadRequiredInt(JObject value, string propertyName, string description)
        {
            JToken token = value[propertyName];
            if (token == null || token.Type != JTokenType.Integer || !int.TryParse(token.ToString(), out int result))
                throw new InvalidOperationException($"Manifest: {description} requires integer property '{propertyName}'.");
            return result;
        }

        private static long ReadRequiredLong(JObject value, string propertyName, string description)
        {
            JToken token = value[propertyName];
            if (token == null || token.Type != JTokenType.Integer || !long.TryParse(token.ToString(), out long result))
                throw new InvalidOperationException($"Manifest: {description} requires integer property '{propertyName}'.");
            return result;
        }

        private static JArray GetRequiredArray(JObject value, string propertyName, string description)
        {
            JToken token = value[propertyName];
            if (!(token is JArray array))
                throw new InvalidOperationException($"Manifest: {description} requires array property '{propertyName}'.");
            return array;
        }

        private static void ValidateGUID(string value, string propertyName)
        {
            if (!Guid.TryParseExact(value, "D", out Guid parsed)
                || parsed == Guid.Empty
                || !string.Equals(value, parsed.ToString("D"), StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Manifest: {propertyName} must be a canonical lowercase GUID.");
            }
        }

        private static void ValidateSHA(string value, int length, string propertyName)
        {
            if (!IsLowercaseHex(value, length))
                throw new InvalidOperationException($"Manifest: {propertyName} must be {length} lowercase hexadecimal characters.");
        }

        private static void ValidatePlayerVersionValue(string value)
        {
            if (!IsPlayerVersion(value))
                throw new InvalidOperationException("Manifest: clientBuildVersion must use MAJOR.MINOR.PATCH.");
        }

        private static string ResolveContentStatePath(
            AddressableAssetSettings settings,
            AddressablesPlayerBuildResult buildResult)
        {
            string publicPath = ContentUpdateScript.GetContentStateDataPath(false, settings);
            string resultPath = buildResult.ContentStateFilePath;
            if (string.IsNullOrWhiteSpace(publicPath) && string.IsNullOrWhiteSpace(resultPath))
            {
                throw new InvalidOperationException(
                    "Addressables artifacts: no public content-state path was returned for the completed build.");
            }

            string resolvedPublicPath = string.IsNullOrWhiteSpace(publicPath)
                ? string.Empty
                : ResolveArtifactPath(publicPath);
            string resolvedResultPath = string.IsNullOrWhiteSpace(resultPath)
                ? string.Empty
                : ResolveArtifactPath(resultPath);
            if (!string.IsNullOrWhiteSpace(resolvedPublicPath)
                && !string.IsNullOrWhiteSpace(resolvedResultPath)
                && !PathsEqual(resolvedPublicPath, resolvedResultPath))
            {
                throw new InvalidOperationException(
                    "Addressables artifacts: the public content-state path differs from the completed build result.");
            }

            string path = string.IsNullOrWhiteSpace(resolvedResultPath) ? resolvedPublicPath : resolvedResultPath;
            if (!File.Exists(path) || !string.Equals(Path.GetFileName(path), CONTENT_STATE_FILE_NAME, StringComparison.Ordinal))
                throw new InvalidOperationException($"Addressables artifacts: content-state file '{path}' is missing.");
            return path;
        }

        private static void ValidateLocalCatalog(string catalogPath, string settingsPath)
        {
            if (!File.Exists(catalogPath) || !File.Exists(settingsPath))
                throw new InvalidOperationException("Addressables artifacts: local catalog or runtime settings is missing.");

            string combinedText = Encoding.UTF8.GetString(File.ReadAllBytes(catalogPath))
                + Environment.NewLine
                + File.ReadAllText(settingsPath);
            if (ContainsRemoteLocation(combinedText))
            {
                throw new InvalidOperationException(
                    "Addressables artifacts: the completed local catalog contains a remote or CCD bundle location.");
            }
        }

        private static string FindRegisteredSibling(
            List<string> registeredPaths,
            string directory,
            string fileName,
            string description)
        {
            string match = null;
            foreach (string path in registeredPaths)
            {
                if (!PathsEqual(Path.GetDirectoryName(path), directory)
                    || !string.Equals(Path.GetFileName(path), fileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (match != null)
                    throw new InvalidOperationException($"Addressables artifacts: multiple {description} files were returned.");
                match = path;
            }

            if (string.IsNullOrWhiteSpace(match) || !File.Exists(match))
                throw new InvalidOperationException($"Addressables artifacts: the completed build returned no {description} file.");
            return match;
        }

        private static void RequireRegisteredArtifact(string path, List<string> registryPaths, string description)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                throw new InvalidOperationException($"Addressables artifacts: {description} '{path}' is missing.");

            foreach (string registeredPath in registryPaths)
            {
                if (PathsEqual(path, registeredPath))
                    return;
            }

            throw new InvalidOperationException(
                $"Addressables artifacts: {description} '{path}' was not registered by the completed build.");
        }

        private static string ComputeSHA256(string path)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(path);
            byte[] hash = sha256.ComputeHash(stream);
            var builder = new StringBuilder(hash.Length * 2);
            foreach (byte value in hash)
                builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        private static string SerializeManifest(JObject manifest)
        {
            return manifest.ToString(Formatting.Indented) + Environment.NewLine;
        }

        private static string RunGit(string workingDirectory, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            try
            {
                using var process = Process.Start(startInfo);
                if (process == null)
                    throw new InvalidOperationException("Git: the process could not be started.");
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"Git command '{arguments}' failed: {error.Trim()}");
                }
                return output;
            }
            catch (System.ComponentModel.Win32Exception exception)
            {
                throw new InvalidOperationException($"Git: could not run '{arguments}'.", exception);
            }
        }

        private static string ResolveArtifactPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;
            string projectRoot = GetFullPath(Path.Combine(Application.dataPath, ".."), "Unity repository root");
            return Path.IsPathRooted(path)
                ? GetFullPath(path, "Addressables artifact")
                : GetFullPath(Path.Combine(projectRoot, path), "Addressables artifact");
        }

        private static string GetFullPath(string path, string description)
        {
            try
            {
                return Path.GetFullPath(path.Trim());
            }
            catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException)
            {
                throw new InvalidOperationException($"{description} path '{path}' is invalid.", exception);
            }
        }

        private static string TryGetManifestPath(string masterRepositoryPath)
        {
            if (string.IsNullOrWhiteSpace(masterRepositoryPath))
                return string.Empty;
            try
            {
                return GetManifestPath(GetFullPath(masterRepositoryPath, "Master repository root"));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetManifestPath(string masterRoot)
        {
            return Path.Combine(
                masterRoot,
                "src",
                "NinthAge.Master",
                "release-manifests",
                DEVELOPMENT_MANIFEST_FILE_NAME);
        }

        private static bool IsIgnoredGeneratedPath(string path)
        {
            return path.Equals("Assets/AddressableAssetsData/Windows/addressables_content_state.bin", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("Library/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("Assets/StreamingAssets/aa/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("ServerData/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsRemotePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            return ContainsRemoteLocation(path)
                || path.IndexOf("release_by_badge", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsRemoteLocation(string value)
        {
            return value.IndexOf("http://", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("https://", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("unity3dusercontent.com", StringComparison.OrdinalIgnoreCase) >= 0
                || value.IndexOf("release_by_badge", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ContainsWhitespace(string value)
        {
            foreach (char character in value)
            {
                if (char.IsWhiteSpace(character))
                    return true;
            }
            return false;
        }

        private static bool IsPlayerVersion(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            int separators = 0;
            int componentLength = 0;
            foreach (char character in value)
            {
                if (character == '.')
                {
                    if (componentLength == 0)
                        return false;
                    separators++;
                    componentLength = 0;
                    continue;
                }

                if (character < '0' || character > '9')
                    return false;
                componentLength++;
            }

            return separators == 2 && componentLength > 0;
        }

        private static bool IsLowercaseHex(string value, int length)
        {
            if (value == null || value.Length != length)
                return false;

            foreach (char character in value)
            {
                bool digit = character >= '0' && character <= '9';
                bool letter = character >= 'a' && character <= 'f';
                if (!digit && !letter)
                    return false;
            }
            return true;
        }

        private static bool PathsEqual(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return false;
            return string.Equals(
                GetFullPath(left, "path").TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                GetFullPath(right, "path").TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }

        private sealed class ManifestDocument
        {
            internal ManifestDocument(string manifestPath, JObject manifest, int manifestVersion)
            {
                ManifestPath = manifestPath;
                Manifest = manifest;
                ManifestVersion = manifestVersion;
            }

            internal string ManifestPath { get; }
            internal JObject Manifest { get; }
            internal int ManifestVersion { get; }
        }

        private sealed class CarriedForwardValues
        {
            internal CarriedForwardValues(
                int protocolVersion,
                long gameplayContentRevision,
                string gameplayContentManifestHash,
                string databaseSchemaVersion)
            {
                ProtocolVersion = protocolVersion;
                GameplayContentRevision = gameplayContentRevision;
                GameplayContentManifestHash = gameplayContentManifestHash;
                DatabaseSchemaVersion = databaseSchemaVersion;
            }

            internal int ProtocolVersion { get; }
            internal long GameplayContentRevision { get; }
            internal string GameplayContentManifestHash { get; }
            internal string DatabaseSchemaVersion { get; }
        }

        private sealed class AddressablesArtifacts
        {
            internal AddressablesArtifacts(string catalogHash, string contentStateSHA256)
            {
                CatalogHash = catalogHash;
                ContentStateSHA256 = contentStateSHA256;
            }

            internal string CatalogHash { get; }
            internal string ContentStateSHA256 { get; }
        }

        private sealed class Publication
        {
            internal Publication(string manifestPath, string releaseID, long catalogRevision, string message)
            {
                ManifestPath = manifestPath;
                ReleaseID = releaseID;
                CatalogRevision = catalogRevision;
                Message = message;
            }

            internal string ManifestPath { get; }
            internal string ReleaseID { get; }
            internal long CatalogRevision { get; }
            internal string Message { get; }
        }
    }
}
