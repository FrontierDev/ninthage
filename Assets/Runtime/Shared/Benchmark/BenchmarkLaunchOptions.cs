using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Game.Shared.Benchmark
{
    /// <summary>Identifies the runtime role requested by an explicit benchmark launch.</summary>
    public enum BenchmarkRole
    {
        Server,
        Client
    }

    /// <summary>Validated inputs shared by the benchmark bootstrap and orchestration hooks.</summary>
    public sealed class BenchmarkLaunchOptions
    {
        private const string BenchmarkFlag = "--benchmark";
        private static readonly string[] KnownKeys =
        {
            "benchmark-role",
            "benchmark-profile",
            "benchmark-fixture",
            "benchmark-seed",
            "benchmark-results",
            "benchmark-run-id",
            "benchmark-build-revision",
            "benchmark-client-username",
            "benchmark-client-password"
        };

        private BenchmarkLaunchOptions() { }

        /// <summary>Gets the requested process role.</summary>
        public BenchmarkRole Role { get; private set; }
        /// <summary>Gets the benchmark profile identifier.</summary>
        public string Profile { get; private set; }
        /// <summary>Gets the fixture identifier.</summary>
        public string Fixture { get; private set; }
        /// <summary>Gets the canonical benchmark seed.</summary>
        public int Seed { get; private set; }
        /// <summary>Gets the absolute result directory.</summary>
        public string ResultsDirectory { get; private set; }
        /// <summary>Gets the unique run identity.</summary>
        public string RunId { get; private set; }
        /// <summary>Gets the build revision supplied by the build tool, when present.</summary>
        public string BuildRevision { get; private set; }
        /// <summary>Gets the real account username used by an automated client.</summary>
        public string ClientUsername { get; private set; }
        /// <summary>Gets the real account password used by an automated client.</summary>
        public string ClientPassword { get; private set; }

        /// <summary>
        /// Parses command-line arguments. A missing benchmark flag returns no options and leaves ordinary startup untouched.
        /// </summary>
        public static bool TryParse(string[] args, out BenchmarkLaunchOptions options, out string error)
        {
            options = null;
            error = null;

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var benchmarkRequested = false;

            foreach (var argument in args ?? Array.Empty<string>())
            {
                if (string.Equals(argument, BenchmarkFlag, StringComparison.OrdinalIgnoreCase))
                {
                    if (benchmarkRequested)
                    {
                        error = "The --benchmark flag was supplied more than once.";
                        return false;
                    }

                    benchmarkRequested = true;
                    continue;
                }

                if (!argument.StartsWith("--benchmark-", StringComparison.OrdinalIgnoreCase))
                    continue;

                var separator = argument.IndexOf('=');
                if (separator <= "--".Length || separator == argument.Length - 1)
                {
                    error = $"Benchmark argument '{argument}' must use --name=value syntax.";
                    return false;
                }

                var key = argument.Substring(2, separator - 2);
                var value = argument.Substring(separator + 1);
                if (Array.FindIndex(KnownKeys, knownKey => string.Equals(knownKey, key, StringComparison.OrdinalIgnoreCase)) < 0)
                {
                    error = $"Unknown benchmark argument '--{key}'.";
                    return false;
                }

                if (!values.TryAdd(key, value))
                {
                    error = $"Benchmark argument '--{key}' was supplied more than once.";
                    return false;
                }
            }

            if (!benchmarkRequested)
            {
                if (values.Count > 0)
                {
                    error = "Benchmark arguments require the explicit --benchmark flag.";
                    return false;
                }

                return true;
            }

            if (!TryGet(values, "benchmark-role", out var roleValue, out error) ||
                !TryGet(values, "benchmark-profile", out var profile, out error) ||
                !TryGet(values, "benchmark-fixture", out var fixture, out error) ||
                !TryGet(values, "benchmark-seed", out var seedValue, out error) ||
                !TryGet(values, "benchmark-results", out var results, out error) ||
                !TryGet(values, "benchmark-run-id", out var runId, out error))
                return false;

            BenchmarkRole role;
            if (string.Equals(roleValue, "server", StringComparison.OrdinalIgnoreCase))
                role = BenchmarkRole.Server;
            else if (string.Equals(roleValue, "client", StringComparison.OrdinalIgnoreCase))
                role = BenchmarkRole.Client;
            else
            {
                error = $"Invalid benchmark role '{roleValue}'. Expected 'server' or 'client'.";
                return false;
            }

            if (!IsSafeIdentifier(profile, "profile", out error) ||
                !IsSafeIdentifier(fixture, "fixture", out error) ||
                !IsSafeIdentifier(runId, "run id", out error))
                return false;

            if (values.TryGetValue("benchmark-build-revision", out var buildRevision) &&
                !IsSafeIdentifier(buildRevision, "build revision", out error))
                return false;

            if (!int.TryParse(seedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seed) || seed < 0)
            {
                error = $"Invalid benchmark seed '{seedValue}'. Expected a non-negative integer.";
                return false;
            }

            if (!Path.IsPathRooted(results))
            {
                error = $"Benchmark result directory must be an absolute path: '{results}'.";
                return false;
            }

            string resultsDirectory;
            try
            {
                resultsDirectory = Path.GetFullPath(results);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException || exception is PathTooLongException)
            {
                error = $"Invalid benchmark result directory '{results}': {exception.Message}";
                return false;
            }

            if (role == BenchmarkRole.Client)
            {
                if (!TryGet(values, "benchmark-client-username", out var username, out error) ||
                    !TryGet(values, "benchmark-client-password", out var password, out error))
                    return false;

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
                {
                    error = "Benchmark client credentials must be non-empty and are validated by the normal authentication service.";
                    return false;
                }

                options = new BenchmarkLaunchOptions
                {
                    Role = role,
                    Profile = profile,
                    Fixture = fixture,
                    Seed = seed,
                    ResultsDirectory = resultsDirectory,
                    RunId = runId,
                    BuildRevision = buildRevision,
                    ClientUsername = username,
                    ClientPassword = password
                };
                return true;
            }

            if (values.ContainsKey("benchmark-client-username") || values.ContainsKey("benchmark-client-password"))
            {
                error = "Client credentials are only valid with --benchmark-role=client.";
                return false;
            }

            options = new BenchmarkLaunchOptions
            {
                Role = role,
                Profile = profile,
                Fixture = fixture,
                Seed = seed,
                ResultsDirectory = resultsDirectory,
                RunId = runId,
                BuildRevision = buildRevision
            };
            return true;
        }

        /// <summary>Parses the current process command line.</summary>
        public static bool TryParseCurrentProcess(out BenchmarkLaunchOptions options, out string error)
        {
            return TryParse(Environment.GetCommandLineArgs(), out options, out error);
        }

        /// <summary>Writes immutable launch metadata without logging credentials.</summary>
        public void WriteRunMetadata()
        {
            Directory.CreateDirectory(ResultsDirectory);
            var metadataPath = Path.Combine(ResultsDirectory, "run.json");
            if (File.Exists(metadataPath))
                throw new IOException($"Benchmark run metadata already exists: '{metadataPath}'. Run identities must be unique.");

            var metadata = new BenchmarkRunMetadata
            {
                schema_version = "benchmark-run/v1",
                run_id = RunId,
                profile_id = Profile,
                fixture_id = Fixture,
                seed = Seed,
                role = Role.ToString().ToLowerInvariant(),
                created_utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                build_revision = string.IsNullOrEmpty(BuildRevision) ? Application.buildGUID : BuildRevision,
                unity_version = Application.unityVersion,
                unity_revision = Application.buildGUID,
                platform = Application.platform.ToString(),
                graphics_api = SystemInfo.graphicsDeviceType.ToString(),
                batch_mode = Application.isBatchMode,
                results_directory = ResultsDirectory
            };

            var json = JsonUtility.ToJson(metadata, true);
            using (var stream = new FileStream(metadataPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(json);
            }
        }

        private static bool TryGet(Dictionary<string, string> values, string key, out string value, out string error)
        {
            if (values.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value))
            {
                error = null;
                return true;
            }

            error = $"Missing required benchmark argument '--{key}=...'.";
            return false;
        }

        private static bool IsSafeIdentifier(string value, string name, out string error)
        {
            if (!string.IsNullOrEmpty(value))
            {
                foreach (var character in value)
                {
                    if (char.IsLetterOrDigit(character) || character == '-' || character == '_' || character == '.')
                        continue;

                    error = $"Invalid benchmark {name} '{value}'. Use only letters, digits, '.', '_' or '-'.";
                    return false;
                }

                error = null;
                return true;
            }

            error = $"Benchmark {name} must not be empty.";
            return false;
        }

        [Serializable]
        private sealed class BenchmarkRunMetadata
        {
            public string schema_version;
            public string run_id;
            public string profile_id;
            public string fixture_id;
            public int seed;
            public string role;
            public string created_utc;
            public string build_revision;
            public string unity_version;
            public string unity_revision;
            public string platform;
            public string graphics_api;
            public bool batch_mode;
            public string results_directory;
        }
    }
}
