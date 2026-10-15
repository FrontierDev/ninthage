using Game.Tests.Performance.Fixtures;
using Game.Tests.Performance.Fixtures.Actors;
using System;
using System.Collections.Generic;
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
        private Vector2 _configurationScrollPosition;
        private Vector2 _resultScrollPosition;
        private Test_PerformanceLastRunView _lastRunView;
        private string _queueDiagnostic;
        private List<Test_PerformanceCampaignArchive> _campaignArchives = new();
        private Test_PerformanceCampaignComparisonReport _campaignComparison;
        private Test_PerformanceCampaignLinkedQualificationReport _linkedQualification;
        private string _campaignComparisonDiagnostic;
        private string _baselineCampaignID;
        private string _candidateCampaignID;

        private const string BASELINE_CAMPAIGN_PREF = "NinthAge.Performance.Compare.Baseline";
        private const string CANDIDATE_CAMPAIGN_PREF = "NinthAge.Performance.Compare.Candidate";

        [MenuItem("Tools/Performance/Test Workload Runner")]
        private static void Open()
        {
            var window = GetWindow<Test_PerformanceHarnessWindow>();
            window.titleContent = new GUIContent("Performance Workload");
            window.minSize = new Vector2(860f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            Test_PerformanceHarnessController.StateChanged += OnControllerStateChanged;
            Test_PerformanceJobQueue.StateChanged += OnQueueStateChanged;
            RefreshLastRun();
            RefreshCampaignArchives();
        }

        private void OnDisable()
        {
            Test_PerformanceHarnessController.StateChanged -= OnControllerStateChanged;
            Test_PerformanceJobQueue.StateChanged -= OnQueueStateChanged;
        }

        private void OnGUI()
        {
            if (_lastRunView == null)
                RefreshLastRun();

            var columnWidth = Mathf.Max(320f, (position.width - 24f) * 0.5f);
            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(columnWidth), GUILayout.ExpandHeight(true)))
                {
                    _configurationScrollPosition = EditorGUILayout.BeginScrollView(
                        _configurationScrollPosition,
                        GUILayout.ExpandWidth(true),
                        GUILayout.ExpandHeight(true));
                    DrawConfigurationPane();
                    DrawQueueSection();
                    EditorGUILayout.EndScrollView();
                }

                GUILayout.Space(8f);
                GUILayout.Box(GUIContent.none, GUILayout.Width(1f), GUILayout.ExpandHeight(true));
                GUILayout.Space(8f);

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(columnWidth), GUILayout.ExpandHeight(true)))
                {
                    _resultScrollPosition = EditorGUILayout.BeginScrollView(
                        _resultScrollPosition,
                        GUILayout.ExpandWidth(true),
                        GUILayout.ExpandHeight(true));
                    DrawLastRunPane();
                    DrawCampaignComparisonPane();
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private void OnControllerStateChanged()
        {
            if (!Test_PerformanceHarnessController.IsRunning)
                RefreshLastRun();
            Repaint();
        }

        private void OnQueueStateChanged()
        {
            _campaignComparison = null;
            Repaint();
        }

        private void RefreshLastRun()
        {
            _lastRunView = Test_PerformanceEvidence.ReadLatestFinalizedResult(Test_PerformanceHarnessController.ActiveResultDirectory);
        }

        private void DrawConfigurationPane()
        {
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
                using (new EditorGUI.DisabledScope(Test_PerformanceHarnessController.IsRunning || Test_PerformanceJobQueue.IsRunning))
                {
                    if (GUILayout.Button("Run", GUILayout.Height(28f)))
                        Test_PerformanceHarnessController.Start(_options.Clone());
                }

                using (new EditorGUI.DisabledScope(!Test_PerformanceHarnessController.IsRunning))
                {
                    if (GUILayout.Button("Stop", GUILayout.Height(28f)))
                    {
                        if (Test_PerformanceJobQueue.IsRunning)
                            Test_PerformanceJobQueue.StopCurrentAndCancelRemaining();
                        else
                            Test_PerformanceHarnessController.RequestStop();
                    }
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Current Run", EditorStyles.boldLabel);
            if (Test_PerformanceHarnessController.IsRunning)
            {
                EditorGUILayout.HelpBox("A workload is active. Last Run remains the previous finalized attempt until this run finalizes.", MessageType.Info);
                DrawResultField("Run ID", Test_PerformanceHarnessController.ActiveRunID);
                DrawResultField("Status", Test_PerformanceHarnessController.Status ?? "Running");
                DrawSelectableValue("Result path", Test_PerformanceHarnessController.ActiveResultDirectory);
            }
            else
            {
                DrawResultField("Status", Test_PerformanceHarnessController.Status ?? "Idle");
                if (!string.IsNullOrEmpty(Test_PerformanceHarnessController.LastReportPath))
                    DrawSelectableValue("Latest report path", Test_PerformanceHarnessController.LastReportPath);
            }
        }

        private void DrawQueueSection()
        {
            string diagnostic;
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Queue / campaign", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"Queue real Editor Play Mode runs sequentially. Limits: {Test_PerformanceJobQueue.MAX_JOBS} jobs, {Test_PerformanceJobQueue.MAX_REPEAT_COUNT} repeats per job, {Test_PerformanceJobQueue.MAX_TOTAL_ATTEMPTS} total attempts. No duration estimate is inferred.",
                MessageType.Info);

            if (!string.IsNullOrEmpty(_queueDiagnostic))
                EditorGUILayout.HelpBox(_queueDiagnostic, MessageType.Error);
            if (!string.IsNullOrEmpty(Test_PerformanceJobQueue.QueueDiagnostic)
                && !string.Equals(_queueDiagnostic, Test_PerformanceJobQueue.QueueDiagnostic, StringComparison.Ordinal))
            {
                EditorGUILayout.HelpBox(Test_PerformanceJobQueue.QueueDiagnostic, MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(!Test_PerformanceJobQueue.CanEdit || Test_PerformanceHarnessController.IsRunning))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add current configuration"))
                        ShowQueueAction(Test_PerformanceJobQueue.AddCurrentConfiguration(_options, out diagnostic), diagnostic);
                    if (GUILayout.Button("Supported baseline preset"))
                        ShowQueueAction(Test_PerformanceJobQueue.LoadSupportedBaselinePreset(out diagnostic), diagnostic);
                }
                var policyIndex = Test_PerformanceJobQueue.StopPolicy == Test_PerformanceCampaignStopPolicy.CONTINUE_AFTER_FAILURE ? 1 : 0;
                var selectedPolicy = EditorGUILayout.Popup("Failure policy", policyIndex, new[] { "Stop on failure (safe default)", "Continue after failure" });
                if (selectedPolicy != policyIndex)
                {
                    var policy = selectedPolicy == 1
                        ? Test_PerformanceCampaignStopPolicy.CONTINUE_AFTER_FAILURE
                        : Test_PerformanceCampaignStopPolicy.STOP_ON_FAILURE;
                    ShowQueueAction(Test_PerformanceJobQueue.SetStopPolicy(policy, out diagnostic), diagnostic);
                }
            }

            if (!Test_PerformanceJobQueue.HasCampaign)
                return;

            EditorGUILayout.LabelField("Campaign", EditorStyles.miniBoldLabel);
            DrawResultField("ID", Test_PerformanceJobQueue.CampaignID);
            DrawResultField("State", Test_PerformanceJobQueue.CampaignState);
            DrawResultField("Source", Test_PerformanceJobQueue.SourceSummary);
            DrawWrappedField("Provenance", Test_PerformanceJobQueue.SourceDetails);
            DrawResultField("Progress", Test_PerformanceJobQueue.ProgressText);
            DrawWrappedField(
                "Counts",
                $"queued {Test_PerformanceJobQueue.QueuedCount}; running {Test_PerformanceJobQueue.RunningCount}; completed {Test_PerformanceJobQueue.CompletedCount}; failed {Test_PerformanceJobQueue.FailedCount}; blocked {Test_PerformanceJobQueue.BlockedCount}; unverified {Test_PerformanceJobQueue.UnverifiedCount}; skipped {Test_PerformanceJobQueue.SkippedCount}; cancelled {Test_PerformanceJobQueue.CancelledCount}");
            if (!string.IsNullOrEmpty(Test_PerformanceJobQueue.PauseReason))
                DrawWrappedField("Pause reason", Test_PerformanceJobQueue.PauseReason);
            if (Test_PerformanceJobQueue.CampaignState == Test_PerformanceCampaignState.COMPLETED
                || Test_PerformanceJobQueue.CampaignState == Test_PerformanceCampaignState.CANCELLED)
            {
                if (GUILayout.Button("New campaign"))
                    ShowQueueAction(Test_PerformanceJobQueue.CreateNewDraft(out diagnostic), diagnostic);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var canStart = Test_PerformanceJobQueue.CampaignState == Test_PerformanceCampaignState.DRAFT
                    || Test_PerformanceJobQueue.CampaignState == Test_PerformanceCampaignState.PAUSED;
                using (new EditorGUI.DisabledScope(!canStart || Test_PerformanceHarnessController.IsRunning))
                {
                    if (GUILayout.Button(Test_PerformanceJobQueue.CampaignState == Test_PerformanceCampaignState.PAUSED ? "Resume queue" : "Start queue"))
                        ShowQueueAction(Test_PerformanceJobQueue.StartOrResume(out diagnostic), diagnostic);
                }
                using (new EditorGUI.DisabledScope(!Test_PerformanceJobQueue.IsRunning))
                {
                    if (GUILayout.Button("Stop after current"))
                        Test_PerformanceJobQueue.StopAfterCurrent();
                    if (GUILayout.Button("Stop + cancel remaining"))
                        Test_PerformanceJobQueue.StopCurrentAndCancelRemaining();
                }
                using (new EditorGUI.DisabledScope(Test_PerformanceJobQueue.CampaignState != Test_PerformanceCampaignState.PAUSED))
                {
                    if (GUILayout.Button("Cancel campaign"))
                        ShowQueueAction(Test_PerformanceJobQueue.CancelPausedCampaign(out diagnostic), diagnostic);
                }
            }
            using (new EditorGUI.DisabledScope(!Test_PerformanceJobQueue.CanEdit))
            {
                if (GUILayout.Button("Clear draft"))
                    ShowQueueAction(Test_PerformanceJobQueue.ClearDraft(out diagnostic), diagnostic);
            }

            var currentAttempt = Test_PerformanceJobQueue.CurrentAttempt;
            if (currentAttempt != null)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Current attempt", EditorStyles.miniBoldLabel);
                DrawResultField("Queue index", Test_PerformanceJobQueue.CurrentAttemptIndex);
                DrawResultField("Job", Test_PerformanceJobQueue.CurrentJobDisplayName);
                DrawResultField("Repeat", currentAttempt.repeat_index.ToString());
                DrawResultField("Run ID", currentAttempt.run_id);
                if (currentAttempt.source != null)
                    DrawWrappedField("Attempt source", currentAttempt.source.git_branch + " @ " + currentAttempt.source.commit_sha + "; captured " + currentAttempt.source.captured_utc + "; dirty " + currentAttempt.source.dirty + " (informational)");
                DrawSelectableValue("Report path", currentAttempt.report_path);
            }

            EditorGUILayout.Space(4f);
            for (var index = 0; index < Test_PerformanceJobQueue.Jobs.Count; index++)
                DrawQueueJob(index, Test_PerformanceJobQueue.Jobs[index]);

            var summaryLines = Test_PerformanceJobQueue.BuildScenarioSummaryLines();
            if (summaryLines.Count > 0)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Campaign summary", EditorStyles.miniBoldLabel);
                foreach (var line in summaryLines)
                    EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
            }

            if (Test_PerformanceJobQueue.ExcludedScenarios.Count > 0)
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Excluded scenarios", EditorStyles.miniBoldLabel);
                foreach (var excluded in Test_PerformanceJobQueue.ExcludedScenarios)
                    EditorGUILayout.LabelField(excluded, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawQueueJob(int index, Test_PerformanceCampaignJob job)
        {
            string diagnostic;
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField($"{index + 1}. {job.DisplayName}", EditorStyles.miniBoldLabel);
                var options = job.options;
                if (options != null)
                {
                    DrawWrappedField(
                        "Snapshot",
                        $"profile {options.EffectiveProfileID}; mode {options.WorkloadMode}; world {options.WorldFixture}; scenario {options.GraphicalScenario}; density {options.DensityTier}/{options.ActorDensity}; actors {options.ActorCount}; seed {options.Seed}; warmup {options.WarmupSeconds:0.##} s; measurement {options.MeasurementSeconds:0.##} s");
                }
                DrawResultField("Validation", job.validation_state);
                if (!string.IsNullOrEmpty(job.validation_diagnostic))
                    DrawWrappedField("Diagnostic", job.validation_diagnostic);

                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!Test_PerformanceJobQueue.CanEdit))
                    {
                        var repeatCount = EditorGUILayout.IntField("Repeats / estimated attempts", job.repeat_count);
                        if (repeatCount != job.repeat_count)
                            ShowQueueAction(Test_PerformanceJobQueue.SetRepeatCount(job.job_id, repeatCount, out diagnostic), diagnostic);
                        if (GUILayout.Button("Up", GUILayout.Width(32f)))
                            ShowQueueAction(Test_PerformanceJobQueue.MoveJob(job.job_id, -1, out diagnostic), diagnostic);
                        if (GUILayout.Button("Down", GUILayout.Width(44f)))
                            ShowQueueAction(Test_PerformanceJobQueue.MoveJob(job.job_id, 1, out diagnostic), diagnostic);
                        if (GUILayout.Button("Duplicate", GUILayout.Width(68f)))
                            ShowQueueAction(Test_PerformanceJobQueue.DuplicateJob(job.job_id, out diagnostic), diagnostic);
                        if (GUILayout.Button("Remove", GUILayout.Width(56f)))
                            ShowQueueAction(Test_PerformanceJobQueue.RemoveJob(job.job_id, out diagnostic), diagnostic);
                    }
                    using (new EditorGUI.DisabledScope(Test_PerformanceJobQueue.CanEdit || (Test_PerformanceJobQueue.CampaignState != Test_PerformanceCampaignState.COMPLETED && Test_PerformanceJobQueue.CampaignState != Test_PerformanceCampaignState.CANCELLED && Test_PerformanceJobQueue.CampaignState != Test_PerformanceCampaignState.PAUSED)))
                    {
                        if (GUILayout.Button("Make-up 1", GUILayout.Width(70f)))
                            ShowQueueAction(Test_PerformanceJobQueue.CreateMakeupCampaign(job.job_id, 1, out diagnostic), diagnostic);
                    }
                }

                foreach (var attempt in Test_PerformanceJobQueue.Attempts)
                {
                    if (attempt.job_id != job.job_id)
                        continue;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"Attempt {attempt.repeat_index}: {attempt.state}", GUILayout.Width(145f));
                        EditorGUILayout.LabelField(string.IsNullOrEmpty(attempt.run_id) ? "no run ID" : attempt.run_id, EditorStyles.wordWrappedMiniLabel);
                        using (new EditorGUI.DisabledScope(!Test_PerformanceJobQueue.CanOpenReport(attempt)))
                        {
                            if (GUILayout.Button("Report", GUILayout.Width(52f)))
                                Test_PerformanceJobQueue.OpenReport(attempt);
                        }
                        using (new EditorGUI.DisabledScope(!Test_PerformanceJobQueue.CanOpenFolder(attempt)))
                        {
                            if (GUILayout.Button("Folder", GUILayout.Width(52f)))
                                Test_PerformanceJobQueue.OpenFolder(attempt);
                        }
                        using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(attempt.run_id)))
                        {
                            if (GUILayout.Button("Copy ID", GUILayout.Width(52f)))
                                GUIUtility.systemCopyBuffer = attempt.run_id;
                        }
                    }
                    if (!string.IsNullOrEmpty(attempt.result_path))
                        DrawSelectableValue("Result path", attempt.result_path);
                    if (attempt.source != null)
                        DrawWrappedField("Attempt source", (attempt.source.git_branch ?? attempt.source.git_branch_status ?? "unknown") + " @ " + (attempt.source.commit_sha ?? "unknown") + "; captured " + (attempt.source.captured_utc ?? "unknown") + "; dirty " + attempt.source.dirty + " (informational)");
                    if (!string.IsNullOrEmpty(attempt.diagnostic))
                        DrawWrappedField("Attempt diagnostic", attempt.diagnostic);
                }
            }
        }

        private void ShowQueueAction(bool success, string diagnostic)
        {
            _queueDiagnostic = success ? null : diagnostic;
            Repaint();
        }

        private void DrawLastRunPane()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Last Run", EditorStyles.boldLabel);
                if (GUILayout.Button("Refresh", GUILayout.Width(72f)))
                    RefreshLastRun();
            }

            if (_lastRunView == null || !_lastRunView.HasAttempt)
            {
                EditorGUILayout.HelpBox("No finalized local performance attempt was found.", MessageType.Info);
                return;
            }

            if (!_lastRunView.IsVerified)
            {
                EditorGUILayout.HelpBox("Latest finalized attempt is unverified. Older results are intentionally not shown as a substitute.\n\n" + _lastRunView.ErrorMessage, MessageType.Error);
                DrawResultField("Run ID", _lastRunView.RunID);
                DrawResultField("Result path", _lastRunView.ResultDirectory);
                DrawResultActions();
                return;
            }

            EditorGUILayout.HelpBox(_lastRunView.EvidenceScope, MessageType.Info);
            DrawIdentitySection();
            DrawPopulationSection();
            DrawMeasurementSection();
            DrawMetricSection();
            DrawResultActions();
        }

        private void DrawCampaignComparisonPane()
        {
            EditorGUILayout.Space(16f);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Compare Campaigns", EditorStyles.boldLabel);
                if (GUILayout.Button("Refresh", GUILayout.Width(72f)))
                    RefreshCampaignArchives();
            }
            EditorGUILayout.HelpBox(
                "Comparison is on-demand and read-only. It uses finalized structured evidence, measurement-phase observations, a 95% retention policy and at least 5 qualified run-level observations for every required metric/statistic by default. No pooled samples or budget verdicts are produced.",
                MessageType.Info);

            if (!string.IsNullOrEmpty(_campaignComparisonDiagnostic))
                EditorGUILayout.HelpBox(_campaignComparisonDiagnostic, MessageType.Error);
            if (_campaignArchives.Count == 0)
            {
                EditorGUILayout.HelpBox("No archived campaigns were found.", MessageType.Info);
                return;
            }

            var baselineIndex = FindCampaignIndex(_baselineCampaignID);
            var candidateIndex = FindCampaignIndex(_candidateCampaignID);
            var baselineOptions = BuildCampaignLabels();
            var candidateOptions = BuildCampaignLabels();
            var nextBaselineIndex = EditorGUILayout.Popup("Baseline campaign", Math.Max(0, baselineIndex), baselineOptions);
            var nextCandidateIndex = EditorGUILayout.Popup("Candidate campaign", Math.Max(0, candidateIndex), candidateOptions);
            if (nextBaselineIndex != baselineIndex)
                SetComparisonSelection(true, _campaignArchives[nextBaselineIndex].CampaignID);
            if (nextCandidateIndex != candidateIndex)
                SetComparisonSelection(false, _campaignArchives[nextCandidateIndex].CampaignID);

            var baseline = FindCampaign(_baselineCampaignID);
            var candidate = FindCampaign(_candidateCampaignID);
            if (baseline != null)
            {
                DrawCampaignDetails("Baseline provenance", baseline);
            }
            if (candidate != null)
            {
                DrawCampaignDetails("Candidate provenance", candidate);
            }

            using (new EditorGUI.DisabledScope(baseline == null || candidate == null))
            {
                if (GUILayout.Button("Compare selected campaigns"))
                    _campaignComparison = Test_PerformanceCampaignComparator.Compare(_campaignArchives, _baselineCampaignID, _candidateCampaignID);
            }
            var qualificationCampaign = candidate ?? baseline;
            using (new EditorGUI.DisabledScope(qualificationCampaign == null))
            {
                if (GUILayout.Button("Evaluate linked make-up cohort"))
                    _linkedQualification = Test_PerformanceCampaignComparator.BuildLinkedQualificationCohort(_campaignArchives, qualificationCampaign.CampaignID);
            }
            if (_campaignComparison != null)
                DrawCampaignComparisonReport(_campaignComparison);
            if (_linkedQualification != null)
                DrawLinkedQualificationReport(_linkedQualification);
        }

        private void RefreshCampaignArchives()
        {
            _campaignArchives = Test_PerformanceCampaignComparator.LoadArchives(out _campaignComparisonDiagnostic);
            _baselineCampaignID = EditorPrefs.GetString(BASELINE_CAMPAIGN_PREF, null);
            _candidateCampaignID = EditorPrefs.GetString(CANDIDATE_CAMPAIGN_PREF, null);
            if (FindCampaign(_candidateCampaignID) == null)
                _candidateCampaignID = FindCompletedCampaign(0);
            if (FindCampaign(_baselineCampaignID) == null || _baselineCampaignID == _candidateCampaignID)
                _baselineCampaignID = FindCompletedCampaign(1);
            _campaignComparison = null;
            _linkedQualification = null;
            Repaint();
        }

        private string[] BuildCampaignLabels()
        {
            var labels = new string[_campaignArchives.Count];
            for (var index = 0; index < _campaignArchives.Count; index++)
                labels[index] = _campaignArchives[index].CampaignID;
            return labels;
        }

        private int FindCampaignIndex(string campaignID)
        {
            for (var index = 0; index < _campaignArchives.Count; index++)
            {
                if (_campaignArchives[index].CampaignID == campaignID)
                    return index;
            }
            return 0;
        }

        private string FindCompletedCampaign(int completedIndex)
        {
            var seen = 0;
            foreach (var archive in _campaignArchives)
            {
                if (!archive.IsCompleted)
                    continue;
                if (seen++ == completedIndex)
                    return archive.CampaignID;
            }
            return _campaignArchives.Count == 0 ? null : _campaignArchives[Math.Min(completedIndex, _campaignArchives.Count - 1)].CampaignID;
        }

        private Test_PerformanceCampaignArchive FindCampaign(string campaignID)
        {
            foreach (var archive in _campaignArchives)
            {
                if (archive.CampaignID == campaignID)
                    return archive;
            }
            return null;
        }

        private void SetComparisonSelection(bool baseline, string campaignID)
        {
            if (baseline)
            {
                _baselineCampaignID = campaignID;
                EditorPrefs.SetString(BASELINE_CAMPAIGN_PREF, campaignID ?? string.Empty);
            }
            else
            {
                _candidateCampaignID = campaignID;
                EditorPrefs.SetString(CANDIDATE_CAMPAIGN_PREF, campaignID ?? string.Empty);
            }
            _campaignComparison = null;
            _linkedQualification = null;
        }

        private static void DrawCampaignDetails(string title, Test_PerformanceCampaignArchive archive)
        {
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            DrawResultField("Campaign", archive.CampaignID);
            DrawResultField("State", archive.Manifest?.state);
            DrawWrappedField("Provenance", archive.SourceDisplay);
            DrawWrappedField("Environment", archive.EnvironmentDisplay);
            DrawWrappedField("Dates", archive.DateDisplay);
            DrawWrappedField("Runs", archive.DisplayLabel);
            if (!string.IsNullOrEmpty(archive.Diagnostic))
                DrawWrappedField("Manifest diagnostic", archive.Diagnostic);
        }

        private static void DrawCampaignComparisonReport(Test_PerformanceCampaignComparisonReport report)
        {
            EditorGUILayout.Space(6f);
            var messageType = report.Status == Test_PerformanceCampaignComparisonStatus.COMPARABLE
                ? MessageType.Info
                : report.Status == Test_PerformanceCampaignComparisonStatus.PROVISIONAL ? MessageType.Warning : MessageType.Error;
            EditorGUILayout.HelpBox(report.Status + "\n" + report.Summary, messageType);
            foreach (var reason in report.Reasons)
                EditorGUILayout.LabelField(reason, EditorStyles.wordWrappedMiniLabel);
            foreach (var group in report.Groups)
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField(group.Classification + ": " + group.Label, EditorStyles.miniBoldLabel);
                    DrawResultField("Valid repeats", group.BaselineValidCount + " baseline / " + group.CandidateValidCount + " candidate");
                    DrawResultField("Qualified execution cohort", group.BaselineQualifiedAttemptCount + " baseline / " + group.CandidateQualifiedAttemptCount + " candidate");
                    if (!string.IsNullOrEmpty(group.Reason))
                        DrawWrappedField("Group caveat", group.Reason);
                    if (!string.IsNullOrEmpty(group.ContextDiagnostic))
                        DrawWrappedField("Context caveat", group.ContextDiagnostic);
                    DrawCampaignAttempts("Baseline attempts", group.BaselineAttempts, report.Baseline.Directory);
                    DrawCampaignAttempts("Candidate attempts", group.CandidateAttempts, report.Candidate.Directory);
                    foreach (var metric in group.Metrics)
                    {
                        EditorGUILayout.LabelField(metric.MetricID + " [" + metric.Unit + "] / " + metric.Source + " / " + metric.Scope, EditorStyles.miniBoldLabel);
                        DrawResultField("Sample policy", metric.SamplePolicy ?? "unknown");
                        if (metric.IsRequired)
                            DrawResultField("Required statistics", metric.RequiredStatistics);
                        DrawWrappedField("Semantics", metric.Semantics + "; " + metric.Direction);
                        DrawWrappedField("Baseline", metric.Baseline.Describe() + " (" + metric.BaselineQualified + "/" + metric.BaselineAvailable + " qualified/available)");
                        DrawWrappedField("Candidate", metric.Candidate.Describe() + " (" + metric.CandidateQualified + "/" + metric.CandidateAvailable + " qualified/available)");
                        if (metric.IsRequired)
                            DrawResultField("Qualified N p50/p95/p99", metric.BaselineP50Qualified + "/" + metric.BaselineP95Qualified + "/" + metric.BaselineP99Qualified + " baseline; " + metric.CandidateP50Qualified + "/" + metric.CandidateP95Qualified + "/" + metric.CandidateP99Qualified + " candidate");
                        DrawResultField("Delta median p50", FormatDelta(metric.DeltaP50, metric.DeltaP50Percent));
                        DrawResultField("Delta median p95", FormatDelta(metric.DeltaP95, metric.DeltaP95Percent));
                        DrawResultField("Delta median p99", FormatDelta(metric.DeltaP99, metric.DeltaP99Percent));
                        if (!string.IsNullOrEmpty(metric.Caveat))
                            DrawWrappedField("Metric caveat", metric.Caveat);
                    }
                }
            }
        }

        private static void DrawCampaignAttempts(string title, List<Test_PerformanceCampaignAttemptView> attempts, string campaignDirectory)
        {
            EditorGUILayout.LabelField(title + " (" + attempts.Count + ")", EditorStyles.miniBoldLabel);
            foreach (var attempt in attempts)
            {
                var line = (attempt.CampaignID ?? "unknown campaign") + " / " + (attempt.RunID ?? attempt.AttemptID ?? "no run ID") + " / " + (attempt.State ?? "unknown")
                    + " / " + (attempt.ResultStatus ?? "no finalized status") + " / " + (attempt.StopReason ?? "no stop reason")
                    + " / execution " + (attempt.IsValid ? "complete" : "not qualified")
                    + " / performance " + (attempt.PerformanceQualified ? "qualified" : "provisional")
                    + " / focus " + (attempt.FocusContext ?? "unknown")
                    + " / rendering " + (attempt.RenderingQualification ?? "unknown")
                    + " / queue dispatch-settle " + (attempt.DispatchToQueueSettledSeconds < 0d ? "unknown" : attempt.DispatchToQueueSettledSeconds.ToString("0.###")) + " s";
                EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
                if (!string.IsNullOrEmpty(attempt.Diagnostic))
                    DrawWrappedField("Reason", attempt.Diagnostic);
                foreach (var metric in attempt.Metrics)
                {
                    EditorGUILayout.LabelField(
                        "  " + metric.MetricID + ": retained " + metric.RetainedSampleCount + "/" + metric.AttemptedSampleCount
                        + "; coverage " + metric.MeasurementCoverage.ToString("P0")
                        + (metric.HasP50 ? "; qualified" : "; excluded: unavailable/coverage"),
                        EditorStyles.wordWrappedMiniLabel);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(attempt.ResultPath)))
                    {
                        if (GUILayout.Button("Reveal result", GUILayout.Width(100f)))
                            Test_PerformanceCampaignComparator.OpenEvidence(attempt.ResultPath, campaignDirectory);
                    }
                    using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(attempt.ReportPath)))
                    {
                        if (GUILayout.Button("Reveal report", GUILayout.Width(100f)))
                            Test_PerformanceCampaignComparator.OpenEvidence(attempt.ReportPath, campaignDirectory);
                    }
                }
            }
        }

        private static void DrawLinkedQualificationReport(Test_PerformanceCampaignLinkedQualificationReport report)
        {
            EditorGUILayout.Space(6f);
            var messageType = report.Status == Test_PerformanceCampaignComparisonStatus.COMPARABLE
                ? MessageType.Info
                : report.Status == Test_PerformanceCampaignComparisonStatus.PROVISIONAL ? MessageType.Warning : MessageType.Error;
            EditorGUILayout.HelpBox("Linked qualification cohort: " + report.Status + "\n" + report.Summary, messageType);
            foreach (var reason in report.Reasons)
                EditorGUILayout.LabelField(reason, EditorStyles.wordWrappedMiniLabel);
            foreach (var group in report.Groups)
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField(group.Classification + ": " + group.Label, EditorStyles.miniBoldLabel);
                    DrawResultField("Contributing runs", group.BaselineQualifiedAttemptCount + " performance-qualified / " + group.BaselineValidCount + " execution-valid / " + group.BaselineAttempts.Count + " inventory");
                    if (!string.IsNullOrEmpty(group.Reason))
                        DrawWrappedField("Cohort caveat", group.Reason);
                    if (!string.IsNullOrEmpty(group.ContextDiagnostic))
                        DrawWrappedField("Context caveat", group.ContextDiagnostic);
                    DrawCampaignAttempts("Cohort attempts", group.BaselineAttempts, null);
                    foreach (var metric in group.Metrics)
                    {
                        EditorGUILayout.LabelField(metric.MetricID + " [" + metric.Unit + "]", EditorStyles.miniBoldLabel);
                        DrawResultField("Required statistics", metric.RequiredStatistics ?? "descriptive p50");
                        DrawWrappedField("Cohort", metric.Baseline.Describe() + " (" + metric.BaselineQualified + "/" + metric.BaselineAvailable + " qualified/available)");
                        if (!string.IsNullOrEmpty(metric.Caveat))
                            DrawWrappedField("Metric caveat", metric.Caveat);
                    }
                }
            }
        }

        private static string FormatDelta(double? delta, double? percent)
        {
            if (!delta.HasValue)
                return "unavailable (zero baseline or missing qualified values)";
            return delta.Value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture) + " / "
                + (percent.HasValue ? percent.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "%" : "n/a (zero baseline)");
        }

        private void DrawIdentitySection()
        {
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            DrawResultField("Timestamp", _lastRunView.Timestamp);
            DrawResultField("Run ID", _lastRunView.RunID);
            DrawResultField("Workload / variant", _lastRunView.Workload);
            DrawResultField("Profile", _lastRunView.Profile);
            DrawResultField("Scenario", _lastRunView.Scenario);
            DrawResultField("Seed", _lastRunView.Seed);
            DrawResultField("Branch", _lastRunView.Branch);
            DrawResultField("Commit", _lastRunView.Commit);
            DrawResultField("Source captured", _lastRunView.SourceCapturedUtc);
            DrawResultField("Working tree", _lastRunView.DirtyState);
            DrawResultField("Duration", _lastRunView.Duration);
            DrawWrappedField("Measurement", _lastRunView.MeasurementSummary);
            DrawWrappedField("Editor orchestration", _lastRunView.OrchestrationSummary);
            DrawResultField("Status", _lastRunView.Status);
            DrawResultField("Stop reason", _lastRunView.StopReason);
            DrawWrappedField("Stop message", _lastRunView.StopMessage);
            DrawResultField("Correctness", _lastRunView.Correctness);
            DrawResultField("Cleanup", _lastRunView.Cleanup);
        }

        private void DrawPopulationSection()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Population / coverage", EditorStyles.boldLabel);
            DrawWrappedField("Requested vs achieved", _lastRunView.PopulationSummary);
            DrawWrappedField("Activity / churn", _lastRunView.ActivitySummary);
            DrawWrappedField("Route coverage", _lastRunView.RouteSummary);
            DrawWrappedField("PurrNet / headless", _lastRunView.PurrNetStatus);
            DrawWrappedField("Focus context", _lastRunView.FocusSummary);
            DrawWrappedField("Rendering qualification", _lastRunView.RenderingSummary);
            DrawWrappedField("Archive integrity", _lastRunView.ArchiveIntegritySummary);
        }

        private void DrawMeasurementSection()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Measurement phases", EditorStyles.boldLabel);
            foreach (var phase in _lastRunView.Phases)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(phase.Name, GUILayout.Width(110f));
                    EditorGUILayout.LabelField(phase.State);
                }
            }
        }

        private void DrawMetricSection()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Measurement metrics", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Values are read from summary.json measurement-phase entries. p99 reports insufficient_samples when the summary did not qualify that percentile; absent values are never displayed as zero.", MessageType.None);
            foreach (var group in _lastRunView.MetricGroups)
            {
                EditorGUILayout.LabelField(group.Title, EditorStyles.miniBoldLabel);
                foreach (var metric in group.Metrics)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(metric.Label, GUILayout.MinWidth(145f), GUILayout.MaxWidth(185f));
                        EditorGUILayout.LabelField(metric.DisplayValue, EditorStyles.wordWrappedMiniLabel);
                    }
                }
                EditorGUILayout.Space(4f);
            }
        }

        private void DrawResultActions()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Artifacts", EditorStyles.boldLabel);
            DrawSelectableValue("Result path", _lastRunView.ResultDirectory);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!_lastRunView.CanOpenReport))
                {
                    if (GUILayout.Button("Open report"))
                        EditorUtility.OpenWithDefaultApp(_lastRunView.ReportPath);
                }
                using (new EditorGUI.DisabledScope(!_lastRunView.CanOpenFolder))
                {
                    if (GUILayout.Button("Open result folder"))
                        EditorUtility.RevealInFinder(_lastRunView.ResultDirectory);
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!_lastRunView.CanCopyRunID))
                {
                    if (GUILayout.Button("Copy run ID"))
                        GUIUtility.systemCopyBuffer = _lastRunView.RunID;
                }
                using (new EditorGUI.DisabledScope(!_lastRunView.CanCopyResultPath))
                {
                    if (GUILayout.Button("Copy result path"))
                        GUIUtility.systemCopyBuffer = _lastRunView.ResultDirectory;
                }
            }
        }

        private static void DrawResultField(string label, string value)
        {
            EditorGUILayout.LabelField(label, string.IsNullOrEmpty(value) ? "unavailable" : value);
        }

        private static void DrawWrappedField(string label, string value)
        {
            EditorGUILayout.LabelField(label, string.IsNullOrEmpty(value) ? "unavailable" : value, EditorStyles.wordWrappedLabel);
        }

        private static void DrawSelectableValue(string label, string value)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(value ?? "unavailable", EditorStyles.textField, GUILayout.MinHeight(34f));
        }
    }
}
