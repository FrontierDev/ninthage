using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor.Benchmark
{
    /// <summary>Validates and builds the project-owned benchmark configurations.</summary>
    public static class BenchmarkBuildTools
    {
        private const string DevelopmentConfigPath = "Assets/Settings/Build Profiles/Windows Benchmark Development.asset";
        private const string MeasurementConfigPath = "Assets/Settings/Build Profiles/Windows Benchmark Measurement.asset";
        private const string ExpectedUnityVersion = "6000.3.10f1";
        private const string ExpectedUnityRevision = "e35f0c77bd8e";

        [MenuItem("Tools/Ninth Age/Benchmark/Validate Build Configurations")]
        public static void ValidateBuildConfigurations()
        {
            ValidateConfiguration(DevelopmentConfigPath);
            ValidateConfiguration(MeasurementConfigPath);
            Debug.Log("Benchmark build configurations are valid.");
        }

        [MenuItem("Tools/Ninth Age/Benchmark/Build Development")]
        public static void BuildDevelopment()
        {
            Build(DevelopmentConfigPath);
        }

        [MenuItem("Tools/Ninth Age/Benchmark/Build Measurement")]
        public static void BuildMeasurement()
        {
            Build(MeasurementConfigPath);
        }

        private static void Build(string configurationPath)
        {
            var configuration = ValidateConfiguration(configurationPath);
            var buildDirectory = Path.GetFullPath(Path.Combine("Builds", "Benchmarks", configuration.configurationId));
            Directory.CreateDirectory(buildDirectory);
            var executablePath = Path.Combine(buildDirectory, "NinthAge.exe");

            var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.Standalone;
            var oldBackend = PlayerSettings.GetScriptingBackend(namedBuildTarget);
            var oldStripping = PlayerSettings.GetManagedStrippingLevel(namedBuildTarget);
            var oldArchitecture = PlayerSettings.GetArchitecture(namedBuildTarget);
            var oldDefines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
            var oldGraphicsApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);
            var oldUseDefaultGraphicsApis = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64);

            try
            {
                PlayerSettings.SetScriptingBackend(namedBuildTarget, configuration.scriptingBackend == "IL2CPP" ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x);
                PlayerSettings.SetManagedStrippingLevel(namedBuildTarget, configuration.managedStripping == "Medium" ? ManagedStrippingLevel.Medium : ManagedStrippingLevel.Minimal);
                PlayerSettings.SetArchitecture(namedBuildTarget, configuration.architectureValue);
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, configuration.benchmarkDefine);
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { ParseGraphicsApi(configuration.graphicsApi) });

                var options = BuildOptions.CompressWithLz4HC;
                if (configuration.developmentBuild) options |= BuildOptions.Development;
                if (configuration.connectProfiler) options |= BuildOptions.ConnectWithProfiler;
                if (configuration.deepProfiling) options |= BuildOptions.EnableDeepProfilingSupport;
                if (configuration.allowDebugging) options |= BuildOptions.AllowDebugging;

                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                    locationPathName = executablePath,
                    target = BuildTarget.StandaloneWindows64,
                    options = options
                });

                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException($"Benchmark build '{configuration.configurationId}' failed with result {report.summary.result}.");

                WriteBuildMetadata(buildDirectory, configuration);
                Debug.Log($"Built benchmark configuration '{configuration.configurationId}' at '{executablePath}'.");
            }
            finally
            {
                PlayerSettings.SetScriptingBackend(namedBuildTarget, oldBackend);
                PlayerSettings.SetManagedStrippingLevel(namedBuildTarget, oldStripping);
                PlayerSettings.SetArchitecture(namedBuildTarget, oldArchitecture);
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, oldDefines);
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, oldUseDefaultGraphicsApis);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, oldGraphicsApis);
            }
        }

        private static BenchmarkBuildConfiguration ValidateConfiguration(string configurationPath)
        {
            var configuration = AssetDatabase.LoadAssetAtPath<BenchmarkBuildConfiguration>(configurationPath);
            if (configuration == null)
                throw new InvalidOperationException($"Benchmark build configuration is missing: '{configurationPath}'.");

            var validationError = configuration.Validate();
            if (validationError != null)
                throw new InvalidOperationException($"Invalid benchmark build configuration '{configurationPath}': {validationError}");

            if (!File.Exists("ProjectSettings/ProjectVersion.txt"))
                throw new InvalidOperationException("ProjectSettings/ProjectVersion.txt is missing.");

            var projectVersion = File.ReadAllText("ProjectSettings/ProjectVersion.txt");
            if (!projectVersion.Contains($"m_EditorVersion: {ExpectedUnityVersion}") ||
                !projectVersion.Contains($"m_EditorVersionWithRevision: {ExpectedUnityVersion} ({ExpectedUnityRevision})"))
                throw new InvalidOperationException("The benchmark build requires the pinned Unity editor version 6000.3.10f1 (e35f0c77bd8e).");

            ValidateFixtureManifest(configuration.fixtureManifestPath);
            ValidateProductionSceneList();
            return configuration;
        }

        private static void ValidateFixtureManifest(string manifestPath)
        {
            if (!File.Exists(manifestPath))
                throw new InvalidOperationException($"Benchmark fixture manifest is missing: '{manifestPath}'.");

            var manifest = File.ReadAllText(manifestPath);
            var paths = Regex.Matches(manifest, "\\\"AssetPath\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
            if (paths.Count == 0)
                throw new InvalidOperationException($"Benchmark fixture manifest contains no AssetPath entries: '{manifestPath}'.");

            foreach (Match match in paths)
            {
                var assetPath = match.Groups[1].Value;
                if (!assetPath.StartsWith("Assets/Tests/Performance/Fixtures/", StringComparison.Ordinal) ||
                    !assetPath.EndsWith(".unity", StringComparison.Ordinal))
                    throw new InvalidOperationException($"Fixture scene is outside the tracked benchmark fixture root: '{assetPath}'.");

                if (!File.Exists(assetPath) || !File.Exists(assetPath + ".meta"))
                    throw new InvalidOperationException($"Fixture scene or stable meta file is missing: '{assetPath}'.");
            }
        }

        private static void ValidateProductionSceneList()
        {
            var requiredPaths = new HashSet<string>(StringComparer.Ordinal)
            {
                "Assets/Scenes/Bootstrapper.unity",
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/CharacterCreation.unity",
                "Assets/Scenes/CharacterSelection.unity",
                "Assets/Scenes/global_world.unity",
                "Assets/Scenes/global_actors.unity"
            };

            var configuredPaths = new HashSet<string>(EditorBuildSettings.scenes.Select(scene => scene.path), StringComparer.Ordinal);
            if (!requiredPaths.SetEquals(configuredPaths))
                throw new InvalidOperationException("The benchmark build requires the existing production EditorBuildSettings scene list unchanged.");
        }

        private static GraphicsDeviceType ParseGraphicsApi(string value)
        {
            if (!Enum.TryParse(value, true, out GraphicsDeviceType graphicsApi) || graphicsApi == GraphicsDeviceType.Null)
                throw new InvalidOperationException($"Unsupported benchmark graphics API '{value}'.");

            return graphicsApi;
        }

        private static void WriteBuildMetadata(string buildDirectory, BenchmarkBuildConfiguration configuration)
        {
            var metadata = new BenchmarkBuildMetadata
            {
                schema_version = "benchmark-build/v1",
                configuration_id = configuration.configurationId,
                build_revision = configuration.buildRevision,
                unity_version = Application.unityVersion,
                platform = configuration.platform,
                architecture = configuration.architecture,
                scripting_backend = configuration.scriptingBackend,
                managed_stripping = configuration.managedStripping,
                development_build = configuration.developmentBuild,
                connect_profiler = configuration.connectProfiler,
                deep_profiling = configuration.deepProfiling,
                allow_debugging = configuration.allowDebugging,
                graphics_api = configuration.graphicsApi,
                fixture_manifest_path = configuration.fixtureManifestPath
            };

            File.WriteAllText(Path.Combine(buildDirectory, "benchmark-build.json"), JsonUtility.ToJson(metadata, true));
        }

        [Serializable]
        private sealed class BenchmarkBuildMetadata
        {
            public string schema_version;
            public string configuration_id;
            public string build_revision;
            public string unity_version;
            public string platform;
            public string architecture;
            public string scripting_backend;
            public string managed_stripping;
            public bool development_build;
            public bool connect_profiler;
            public bool deep_profiling;
            public bool allow_debugging;
            public string graphics_api;
            public string fixture_manifest_path;
        }
    }
}
