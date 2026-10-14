using Game.Tests.Performance.Fixtures;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Tests.Performance.Harness
{
    /// <summary>Moves the ordinary graphical client's camera through a deterministic fixture route.</summary>
    internal sealed class Test_GraphicalFixtureRuntime : IDisposable
    {
        private readonly Test_GraphicalScenarioPlan _plan;
        private Camera _camera;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private bool _disposed;

        public Test_GraphicalFixtureRuntime(Test_GraphicalScenarioPlan plan)
        {
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
            Test_GraphicalFixtureLayout.ValidatePlan(plan);
        }

        public Test_GraphicalScenarioPlan Plan => _plan;

        public int ActualObjectCount { get; private set; }

        public int VisibleObjectCount { get; private set; }

        public int ActualActorCount { get; private set; }

        public int RouteWaypointsReached { get; private set; }

        public float RouteDistanceTravelled { get; private set; }

        public bool RouteComplete => RouteWaypointsReached == _plan.Waypoints.Length;

        public bool CleanupComplete { get; private set; }

        public void AttachCamera(Camera camera)
        {
            _camera = camera ?? throw new Test_PerformancePrerequisiteException(
                "The ordinary graphical client has no MainCamera; the route cannot be executed without creating a substitute camera.");
            _startPosition = _camera.transform.position;
            _startRotation = _camera.transform.rotation;
        }

        public void ValidateLoadedScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                throw new Test_PerformancePrerequisiteException("The selected graphical fixture scene did not finish loading.");
            if (scene.path != _plan.FixtureScenePaths[0])
                throw new Test_PerformancePrerequisiteException(
                    $"Loaded graphical fixture identity mismatch: expected '{_plan.FixtureScenePaths[0]}', found '{scene.path}'.");

            var fixtureRoot = FindFixtureRoot(scene);
            var metadata = fixtureRoot == null ? null : fixtureRoot.GetComponent<Test_WorldFixtureMetadata>();
            if (fixtureRoot == null)
                throw new Test_PerformancePrerequisiteException(
                    "The selected graphical fixture has no FixtureRoot; its object density cannot be trusted.");
            if (metadata == null)
                throw new Test_PerformancePrerequisiteException(
                    "The selected graphical fixture has no Test_WorldFixtureMetadata; its object count cannot be trusted.");
            if (metadata.Seed != _plan.Seed || metadata.Variant != _plan.FixtureVariants[0])
            {
                throw new Test_PerformancePrerequisiteException(
                    $"Loaded graphical fixture metadata does not match the selected scenario: expected seed {_plan.Seed}/{_plan.FixtureVariants[0]}, found {metadata.Seed}/{metadata.Variant}.");
            }

            ActualObjectCount = fixtureRoot.transform.childCount;
            if (ActualObjectCount != _plan.RequestedObjectCount)
            {
                throw new Test_PerformancePrerequisiteException(
                    $"Graphical object density was not achieved: requested {_plan.RequestedObjectCount}, observed {ActualObjectCount} scene objects. The fixture metadata claimed {metadata.ObjectCount}; no representative or empty substitute is accepted.");
            }

            VisibleObjectCount = CountVisibleRenderers(scene);
            ActualActorCount = 0;
            if (_plan.RequestedActorCount != ActualActorCount)
            {
                throw new Test_PerformancePrerequisiteException(
                    $"Graphical actor density was not achieved: requested {_plan.RequestedActorCount}, observed {ActualActorCount}.");
            }
        }

        public void Advance(float elapsedSeconds, float durationSeconds)
        {
            if (_camera == null)
                throw new Test_PerformancePrerequisiteException("The graphical route has no attached MainCamera.");
            if (durationSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(durationSeconds));

            var normalized = Mathf.Clamp01(elapsedSeconds / durationSeconds);
            var routePosition = normalized * (_plan.Waypoints.Length - 1);
            var segment = _plan.Waypoints.Length == 1
                ? 0
                : Mathf.Min(Mathf.FloorToInt(routePosition), _plan.Waypoints.Length - 2);
            var segmentT = _plan.Waypoints.Length == 1 ? 0f : routePosition - segment;
            var from = _plan.Waypoints[segment];
            var to = _plan.Waypoints[Mathf.Min(segment + 1, _plan.Waypoints.Length - 1)];
            var position = Vector3.Lerp(ToVector3(from), ToVector3(to), segmentT);
            var heading = Mathf.LerpAngle(from.HeadingDegrees, to.HeadingDegrees, segmentT);
            _camera.transform.SetPositionAndRotation(position, Quaternion.Euler(18f, heading, 0f));

            var reached = Mathf.Min(_plan.Waypoints.Length, Mathf.FloorToInt(routePosition) + 1);
            if (normalized >= 1f)
                reached = _plan.Waypoints.Length;
            RouteWaypointsReached = Mathf.Max(RouteWaypointsReached, reached);
            RouteDistanceTravelled = CalculateTravelledDistance(normalized);
        }

        public void RecordMetrics()
        {
            Test_PerformanceHarnessMetrics.RecordGraphical(this);
        }

        public void RestoreCamera()
        {
            if (_camera != null)
                _camera.transform.SetPositionAndRotation(_startPosition, _startRotation);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            if (_camera == null)
                throw new Test_PerformancePrerequisiteException(
                    "The ordinary MainCamera was destroyed before graphical cleanup; camera state could not be restored.");
            RestoreCamera();
            _disposed = true;
            CleanupComplete = true;
        }

        private static GameObject FindFixtureRoot(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "FixtureRoot")
                    return root;
            }

            return null;
        }

        private static int CountVisibleRenderers(Scene scene)
        {
            var count = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.enabled && renderer.gameObject.activeInHierarchy)
                        count++;
                }
            }

            return count;
        }

        private float CalculateTravelledDistance(float normalized)
        {
            return _plan.RouteDistanceMeters * Mathf.Clamp01(normalized);
        }

        private static Vector3 ToVector3(Test_GraphicalRouteWaypoint waypoint)
        {
            return new Vector3(waypoint.WorldX, waypoint.WorldY, waypoint.WorldZ);
        }
    }

    /// <summary>Marks a graphical prerequisite failure so the run is blocked instead of downgraded.</summary>
    internal sealed class Test_PerformancePrerequisiteException : InvalidOperationException
    {
        public Test_PerformancePrerequisiteException(string message)
            : base(message)
        {
        }
    }
}
