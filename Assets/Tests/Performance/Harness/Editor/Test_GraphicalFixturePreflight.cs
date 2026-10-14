using Game.Tests.Performance.Fixtures;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Tests.Performance.Harness
{
    /// <summary>Checks graphical fixture inputs before a client Play Mode run starts.</summary>
    internal static class Test_GraphicalFixturePreflight
    {
        internal const string CLIENT_BOOTSTRAP_SCENE_PATH = "Assets/Scenes/Bootstrapper.unity";

        internal static bool TryValidate(
            Test_PerformanceHarnessOptions options,
            out Test_GraphicalScenarioPlan plan,
            out string diagnostic)
        {
            plan = null;
            diagnostic = null;
            if (options == null)
                return Fail("Graphical fixture options are missing.", out diagnostic);

            plan = Test_GraphicalFixtureLayout.BuildPlan(
                options.GraphicalScenario,
                options.DensityTier,
                options.ActorDensity,
                options.Seed);
            try
            {
                Test_GraphicalFixtureLayout.ValidatePlan(plan);
            }
            catch (Exception exception)
            {
                return Fail(exception.Message, out diagnostic);
            }

            if (!plan.IsReady)
                return Fail(plan.BlockedReason, out diagnostic);
            if (options.WarmupSeconds <= 0f)
                return Fail("Warmup duration must be greater than zero.", out diagnostic);
            if (options.MeasurementSeconds <= 0f)
                return Fail("Measurement duration must be greater than zero.", out diagnostic);
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return Fail(
                    "Graphical execution is blocked because Unity reports no graphics device; GPU and rendered-frame metrics would be unavailable rather than zero.",
                    out diagnostic);
            }
            var rendererIdentity = Test_PerformanceEvidence.BuildRendererIdentity(GraphicsSettings.currentRenderPipeline);
            if (!rendererIdentity.EndsWith("/ForwardPlus", StringComparison.Ordinal))
            {
                return Fail(
                    "Graphical execution is blocked because the active render pipeline is not the supported URP Forward+ renderer: " + rendererIdentity,
                    out diagnostic);
            }
            if (options.ActorDensity != Test_GraphicalActorDensity.None)
            {
                return Fail(
                    "Visible actor density is blocked: the current test-only workflow has no supported real client session and must not synthesize networked actors or nameplates.",
                    out diagnostic);
            }

            if (plan.RequiresWorldStreaming)
            {
                return Fail(
                    "Adjacent-tile scenario is blocked: the tracked Test_WorldFixture scenes are not Addressable World_X_Y inputs for the normal ClientWorldManager/PurrNet streaming path. The runner will not substitute direct additive loads.",
                    out diagnostic);
            }

            if (!File.Exists(CLIENT_BOOTSTRAP_SCENE_PATH)
                || AssetDatabase.LoadAssetAtPath<SceneAsset>(CLIENT_BOOTSTRAP_SCENE_PATH) == null)
            {
                return Fail(
                    "The ordinary graphical client bootstrap scene is missing or not imported: " + CLIENT_BOOTSTRAP_SCENE_PATH,
                    out diagnostic);
            }

            for (var index = 0; index < plan.FixtureScenePaths.Length; index++)
            {
                var scenePath = plan.FixtureScenePaths[index];
                if (!File.Exists(scenePath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                {
                    return Fail(
                        $"Graphical fixture scene '{scenePath}' is missing or not imported; no substitute scene is permitted.",
                        out diagnostic);
                }
            }

            return true;
        }

        private static bool Fail(string message, out string diagnostic)
        {
            diagnostic = message;
            return false;
        }
    }
}
