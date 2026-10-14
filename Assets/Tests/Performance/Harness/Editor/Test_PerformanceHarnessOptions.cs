using Game.Tests.Performance.Fixtures;
using Game.Tests.Performance.Fixtures.Actors;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Tests.Performance.Harness
{
    /// <summary>Describes the workload representation selected in the Editor runner.</summary>
    public enum Test_PerformanceWorkloadMode
    {
        EcsDataOnlyMinimal,
        EcsDataOnlyActivity,
        FullNetworkedActors,
        StandaloneHeadlessServer,
        NetworkedClients,
        GraphicalClient,
    }

    /// <summary>Bounded Editor configuration for one test-only performance run.</summary>
    [Serializable]
    public sealed class Test_PerformanceHarnessOptions
    {
        public string ProfileID = Test_ActorFixtureLayout.PROFILE_ID;
        public Test_WorldFixtureVariant WorldFixture = Test_WorldFixtureVariant.Outdoor;
        public Test_WorldFixtureDensityTier DensityTier = Test_WorldFixtureDensityTier.D1_Default;
        public Test_PerformanceWorkloadMode WorkloadMode = Test_PerformanceWorkloadMode.EcsDataOnlyMinimal;
        public int ActorCount = Test_ActorFixtureLayout.NPC_COUNT;
        public int Seed = Test_ActorFixtureLayout.DEFAULT_SEED;
        public Test_GraphicalScenario GraphicalScenario = Test_GraphicalScenario.Outdoor;
        public Test_GraphicalActorDensity ActorDensity = Test_GraphicalActorDensity.None;
        public float WarmupSeconds = Test_GraphicalFixtureLayout.DEFAULT_WARMUP_SECONDS;
        public float MeasurementSeconds = Test_GraphicalFixtureLayout.DEFAULT_MEASUREMENT_SECONDS;

        public Test_PerformanceHarnessOptions Clone()
        {
            return new Test_PerformanceHarnessOptions
            {
                ProfileID = ProfileID,
                WorldFixture = WorldFixture,
                DensityTier = DensityTier,
                WorkloadMode = WorkloadMode,
                ActorCount = ActorCount,
                Seed = Seed,
                GraphicalScenario = GraphicalScenario,
                ActorDensity = ActorDensity,
                WarmupSeconds = WarmupSeconds,
                MeasurementSeconds = MeasurementSeconds,
            };
        }

        public bool IsGraphicalClient => WorkloadMode == Test_PerformanceWorkloadMode.GraphicalClient;

        public string EffectiveProfileID => IsGraphicalClient ? Test_GraphicalFixtureLayout.PROFILE_ID : ProfileID;

        public bool IsServerOnlyDataMode => WorkloadMode == Test_PerformanceWorkloadMode.EcsDataOnlyMinimal
            || WorkloadMode == Test_PerformanceWorkloadMode.EcsDataOnlyActivity;

        public string ActorVariantID
        {
            get
            {
                if (IsGraphicalClient)
                    return Test_GraphicalFixtureLayout.PROFILE_ID + "-I-GEOMETRY";
                return WorkloadMode == Test_PerformanceWorkloadMode.EcsDataOnlyMinimal
                    || WorkloadMode == Test_PerformanceWorkloadMode.EcsDataOnlyActivity
                    ? "S04-I-DATA"
                    : WorkloadMode == Test_PerformanceWorkloadMode.NetworkedClients
                        ? "S04-C-WAKE"
                        : "S04-I-FULL";
            }
        }

        /// <summary>Validates the tracked fixture and explicitly reports unsupported execution paths.</summary>
        public bool TryValidate(out string diagnostic, out Test_WorldFixturePlan worldPlan, out Test_ActorFixtureProfile actorProfile)
        {
            diagnostic = null;
            worldPlan = null;
            actorProfile = null;

            if (IsGraphicalClient)
            {
                if (!Test_GraphicalFixturePreflight.TryValidate(this, out var graphicalPlan, out diagnostic))
                    return false;
                worldPlan = FindWorldPlan(graphicalPlan, graphicalPlan.FixtureVariants[0]);
                return true;
            }

            if (ProfileID != Test_ActorFixtureLayout.PROFILE_ID)
                return Fail("Only the tracked profile P1-S0-04 is available to this runner.", out diagnostic);
            if (Seed != Test_ActorFixtureLayout.DEFAULT_SEED)
                return Fail("The tracked world scenes are generated for seed 424242; regenerate a test fixture before selecting another seed.", out diagnostic);
            if (ActorCount != Test_ActorFixtureLayout.NPC_COUNT)
                return Fail("The selected actor fixture has a fixed population of 100 NPCs; no partial or substituted population is supported.", out diagnostic);
            if (DensityTier != Test_WorldFixtureDensityTier.D1_Default)
                return Fail("The tracked world scenes are available only at density tier D1_Default.", out diagnostic);

            if (!File.Exists(Test_ActorFixtureLayout.CATALOG_PATH)
                || !File.Exists(Test_ActorFixtureLayout.SCENARIO_PATH)
                || !File.Exists(Test_ActorFixtureLayout.ASSET_MANIFEST_PATH))
            {
                return Fail("One or more tracked actor fixture inputs are missing; the runner will not substitute generated metadata.", out diagnostic);
            }

            var manifestPath = "Assets/Tests/Performance/Fixtures/Worlds/world-fixture-manifest.json";
            if (!File.Exists(manifestPath))
                return Fail($"World fixture manifest is missing: {manifestPath}", out diagnostic);

            var manifest = JsonUtility.FromJson<Test_WorldFixtureManifest>(File.ReadAllText(manifestPath));
            if (manifest == null || manifest.Scenes == null)
                return Fail($"World fixture manifest is malformed: {manifestPath}", out diagnostic);

            foreach (var plan in manifest.Scenes)
            {
                if (plan.Variant != WorldFixture.ToString())
                    continue;

                worldPlan = plan;
                break;
            }

            if (worldPlan == null)
                return Fail($"World fixture variant '{WorldFixture}' is absent from {manifestPath}.", out diagnostic);
            if (worldPlan.Status != "Ready")
                return Fail($"World fixture variant '{WorldFixture}' is not ready: {worldPlan.BlockedReason}", out diagnostic);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(worldPlan.AssetPath) == null || !File.Exists(worldPlan.AssetPath))
                return Fail($"World fixture scene is missing or not imported: {worldPlan.AssetPath}", out diagnostic);

            var importedGUID = AssetDatabase.AssetPathToGUID(worldPlan.AssetPath);
            if (!string.Equals(importedGUID, worldPlan.MetaGUID, StringComparison.Ordinal))
                return Fail($"World fixture scene identity mismatch for {worldPlan.AssetPath}: expected {worldPlan.MetaGUID}, found {importedGUID}.", out diagnostic);

            var catalog = Test_ActorFixtureLayout.BuildCatalog(Seed);
            foreach (var profile in catalog.profiles)
            {
                if (profile.variant_id != ActorVariantID)
                    continue;

                actorProfile = profile;
                break;
            }

            if (actorProfile == null)
                return Fail($"Actor fixture variant '{ActorVariantID}' is absent from the deterministic catalog.", out diagnostic);

            Test_ActorFixtureLayout.ValidatePopulation(actorProfile);
            if (!IsServerOnlyDataMode)
                return Fail(GetUnsupportedModeDiagnostic(), out diagnostic);

            return true;
        }

        public string GetUnsupportedModeDiagnostic()
        {
            switch (WorkloadMode)
            {
                case Test_PerformanceWorkloadMode.FullNetworkedActors:
                    return "Full networked actors require a real authenticated client through the ordinary login path; this runner does not launch or synthesize clients.";
                case Test_PerformanceWorkloadMode.StandaloneHeadlessServer:
                    return "Standalone headless execution is blocked: no public or test-owned standalone launch hook is available to this runner.";
                case Test_PerformanceWorkloadMode.NetworkedClients:
                    return "Networked-client execution is blocked: no supported real-client driver is available; server-only execution remains independent.";
                case Test_PerformanceWorkloadMode.GraphicalClient:
                    return "Graphical execution is blocked until the selected rendered-client fixture passes graphical preflight.";
                default:
                    return "The selected workload mode is not supported by this test-only runner.";
            }
        }

        private static bool Fail(string message, out string diagnostic)
        {
            diagnostic = message;
            return false;
        }

        private static Test_WorldFixturePlan FindWorldPlan(Test_GraphicalScenarioPlan plan, string variant)
        {
            var manifest = Test_WorldFixtureLayout.BuildManifest(new Test_WorldFixtureOptions
            {
                Seed = plan.Seed,
                DensityTier = (Test_WorldFixtureDensityTier)Enum.Parse(typeof(Test_WorldFixtureDensityTier), plan.ObjectDensityTier),
                FixtureVersion = Test_WorldFixtureLayout.FIXTURE_VERSION,
            });
            foreach (var worldPlan in manifest.Scenes)
            {
                if (worldPlan.Variant == variant)
                    return worldPlan;
            }

            return null;
        }
    }

    /// <summary>Small Editor window that owns no runtime or build configuration.</summary>
    public sealed class Test_PerformanceHarnessWindow : EditorWindow
    {
        private Test_PerformanceHarnessOptions _options = new();
        private Vector2 _scrollPosition;

        [MenuItem("Tools/Performance/Test Workload Runner")]
        private static void Open()
        {
            var window = GetWindow<Test_PerformanceHarnessWindow>();
            window.titleContent = new GUIContent("Performance Workload");
            window.minSize = new Vector2(390f, 360f);
            window.Show();
        }

        private void OnEnable()
        {
            Test_PerformanceHarnessController.StateChanged += Repaint;
        }

        private void OnDisable()
        {
            Test_PerformanceHarnessController.StateChanged -= Repaint;
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            EditorGUILayout.LabelField("Test-only performance workload", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Runs one selected test-only workload through the ordinary project startup path. Graphical runs use the rendered client and never synthesize authenticated actors or replace missing streamed scenes.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(Test_PerformanceHarnessController.IsRunning))
            {
                EditorGUILayout.LabelField("Profile", _options.EffectiveProfileID);
                _options.WorldFixture = (Test_WorldFixtureVariant)EditorGUILayout.EnumPopup("World fixture", _options.WorldFixture);
                _options.WorkloadMode = (Test_PerformanceWorkloadMode)EditorGUILayout.EnumPopup("Workload mode", _options.WorkloadMode);
                _options.DensityTier = (Test_WorldFixtureDensityTier)EditorGUILayout.EnumPopup("Density tier", _options.DensityTier);
                if (!_options.IsGraphicalClient)
                    _options.ActorCount = EditorGUILayout.IntField("Actor count", _options.ActorCount);
                _options.Seed = EditorGUILayout.IntField("Seed", _options.Seed);

                if (_options.IsGraphicalClient)
                {
                    _options.GraphicalScenario = (Test_GraphicalScenario)EditorGUILayout.EnumPopup("Graphical scenario", _options.GraphicalScenario);
                    _options.ActorDensity = (Test_GraphicalActorDensity)EditorGUILayout.EnumPopup("Actor density", _options.ActorDensity);
                    _options.WarmupSeconds = EditorGUILayout.FloatField("Warmup seconds", _options.WarmupSeconds);
                    _options.MeasurementSeconds = EditorGUILayout.FloatField("Measurement seconds", _options.MeasurementSeconds);
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Execution", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "Phase timing",
                _options.IsGraphicalClient
                    ? $"2 s ramp / {_options.WarmupSeconds:0.##} s warmup / {_options.MeasurementSeconds:0.##} s measurement / 1 s recovery"
                    : "2 s ramp / 2 s warmup / 5 s measurement / 1 s recovery");
            EditorGUILayout.LabelField(
                "Population",
                _options.IsGraphicalClient ? "World objects from the selected tracked scene; actors require a real supported client session." : "Fixture-defined 100 NPCs; selected count must remain 100");

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(Test_PerformanceHarnessController.IsRunning))
                {
                    if (GUILayout.Button("Run", GUILayout.Height(28f)))
                        Test_PerformanceHarnessController.Start(_options.Clone());
                }

                using (new EditorGUI.DisabledScope(!Test_PerformanceHarnessController.IsRunning))
                {
                    if (GUILayout.Button("Stop", GUILayout.Height(28f)))
                        Test_PerformanceHarnessController.RequestStop();
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Status", Test_PerformanceHarnessController.Status ?? "Idle");
            if (!string.IsNullOrEmpty(Test_PerformanceHarnessController.LastReportPath))
            {
                EditorGUILayout.LabelField("Report", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(Test_PerformanceHarnessController.LastReportPath, EditorStyles.textField, GUILayout.Height(34f));
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
