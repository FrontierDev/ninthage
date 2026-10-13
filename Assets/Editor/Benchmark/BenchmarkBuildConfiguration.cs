using UnityEngine;

namespace Game.Editor.Benchmark
{
    /// <summary>Project-owned, serialized build contract for one benchmark variant.</summary>
    public sealed class BenchmarkBuildConfiguration : ScriptableObject
    {
        public string configurationId;
        public string buildRevision;
        public string platform;
        public string architecture;
        public int architectureValue;
        public string scriptingBackend;
        public string managedStripping;
        public bool developmentBuild;
        public bool connectProfiler;
        public bool deepProfiling;
        public bool allowDebugging;
        public string graphicsApi;
        public string fixtureManifestPath;
        public string benchmarkDefine;

        /// <summary>Validates the configuration without changing editor or project settings.</summary>
        public string Validate()
        {
            if (string.IsNullOrWhiteSpace(configurationId)) return "configurationId is required.";
            if (string.IsNullOrWhiteSpace(buildRevision)) return "buildRevision is required.";
            if (platform != "StandaloneWindows64") return "platform must be StandaloneWindows64.";
            if (architecture != "x86_64" || architectureValue != 0) return "architecture must be the supported x86_64 Windows configuration (value 0).";
            if (scriptingBackend != "Mono" && scriptingBackend != "IL2CPP") return "scriptingBackend must be Mono or IL2CPP.";
            if (managedStripping != "Minimal" && managedStripping != "Medium") return "managedStripping must be Minimal or Medium.";
            if (string.IsNullOrWhiteSpace(graphicsApi)) return "graphicsApi is required.";
            if (string.IsNullOrWhiteSpace(fixtureManifestPath)) return "fixtureManifestPath is required.";
            if (benchmarkDefine != "NINTHAGE_BENCHMARK_BUILD") return "benchmarkDefine must be NINTHAGE_BENCHMARK_BUILD.";
            if (!developmentBuild && (connectProfiler || deepProfiling || allowDebugging))
                return "release-like measurement builds cannot enable development, profiler or debugging flags.";
            if (developmentBuild && !connectProfiler)
                return "development benchmark builds must explicitly enable profiler connection.";

            return null;
        }
    }
}
