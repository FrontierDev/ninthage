using Game.Server.Core;
using Game.Shared;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using Game.Tests.Performance.Fixtures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace Game.Tests.Performance.Harness
{
    /// <summary>Runs the Editor-only simulation-region sizing and isolation probes.</summary>
    internal sealed class Test_SimulationRegionSizingProbe
    {
        internal const string SPEC_REVISION = "simulation-region-sizing/v1";
        private static readonly Guid PROBE_WORLD_ID = new("3d7b1f96-47a1-4f50-9b6f-96f3b8e2d4a1");
        private const string PROBE_MAP_ID = "performance-simulation-region-sizing";

        private readonly List<Test_SimulationRegionProbeInstance> _instances = new();
        private Test_PerformanceRun _run;
        private Test_SimulationRegionSizingEvidence _evidence;

        internal IEnumerator Execute(Test_PerformanceRun run)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _evidence = new Test_SimulationRegionSizingEvidence
            {
                phase = run.Options.SimulationRegionCapacity == 1 ? "A_size_sweep" : "B_concurrency_sweep",
                tileSpan = run.Options.SimulationRegionTileSpan,
                concurrentRegions = run.Options.SimulationRegionCapacity,
                requiredQualifiedRepeats = run.Options.SimulationRegionRepeatCount,
                fixtureVariant = run.Options.WorldFixture.ToString(),
                fixturePath = run.WorldPlan == null ? null : run.WorldPlan.AssetPath,
            };

            try
            {
                Prepare();
            }
            catch (Exception exception)
            {
                RecordFailure(exception);
                throw;
            }

            yield return RunPhase(PerformanceMeasurementPhase.Ramp, Test_PerformanceRun.RAMP_SECONDS, false);
            yield return RunPhase(PerformanceMeasurementPhase.Warmup, Test_PerformanceRun.WARMUP_SECONDS, false);
            yield return RunPhase(PerformanceMeasurementPhase.Measurement, Test_PerformanceRun.MEASUREMENT_SECONDS, true);
            yield return RunPhase(PerformanceMeasurementPhase.Recovery, Test_PerformanceRun.RECOVERY_SECONDS, false);

            var teardownTimer = Stopwatch.StartNew();
            yield return CleanupInstances();
            teardownTimer.Stop();
            _evidence.teardownDurationMilliseconds = teardownTimer.Elapsed.TotalMilliseconds.ToString("R", CultureInfo.InvariantCulture);
            _evidence.cleanup = "passed";
            _run.MarkSimulationRegionSizingCleanupComplete();
            FinalizeEvidence();
        }

        private void Prepare()
        {
            RequireSharedGrid();
            var physicsManager = ServerPhysicsManager.Instance;
            if (physicsManager == null)
            {
                throw new Test_PerformancePrerequisiteException(
                    "Simulation-region sizing is blocked because ServerPhysicsManager is not initialized.");
            }

            var fixture = ResolveFixturePlan();
            var context = ActorSpatialContext.CreatePublic(PROBE_WORLD_ID, PROBE_MAP_ID);
            var cellSizeMetres = GameConfigurationManager.Config.ChunkSize;
            var regionWidth = (double)_run.Options.SimulationRegionTileSpan * cellSizeMetres;
            var anchorPitch = regionWidth * 2d;
            _evidence.cellSizeMetres = cellSizeMetres.ToString(CultureInfo.InvariantCulture);
            _evidence.regionWidthMetres = regionWidth.ToString("R", CultureInfo.InvariantCulture);

            var memoryBefore = Profiler.GetTotalAllocatedMemoryLong();
            var setupTimer = Stopwatch.StartNew();
            for (var index = 0; index < _run.Options.SimulationRegionCapacity; index++)
            {
                var regionCoordinate = GetRegionCoordinate(index);
                var localAnchor = new Vector3((float)(index * anchorPitch), 0f, 0f);
                var key = SimulationRegionKey.FromEnvironmentTile(
                    context,
                    new Vector2Int(
                        regionCoordinate.x * _run.Options.SimulationRegionTileSpan,
                        regionCoordinate.y * _run.Options.SimulationRegionTileSpan),
                    _run.Options.SimulationRegionTileSpan,
                    _run.Options.SimulationRegionTileSpan);
                var descriptor = new SimulationRegionDescriptor(
                    key,
                    _run.Options.SimulationRegionTileSpan,
                    _run.Options.SimulationRegionTileSpan,
                    localAnchor);
                CreateRegionInstance(fixture, descriptor, cellSizeMetres, index);
                ThrowIfResourceCeilingExceeded();
            }
            setupTimer.Stop();

            _evidence.setupDurationMilliseconds = setupTimer.Elapsed.TotalMilliseconds.ToString("R", CultureInfo.InvariantCulture);
            _evidence.memoryBeforeBytes = memoryBefore.ToString(CultureInfo.InvariantCulture);
            _evidence.memoryAfterSetupBytes = Profiler.GetTotalAllocatedMemoryLong().ToString(CultureInfo.InvariantCulture);
            _evidence.memoryDeltaBytes = (Profiler.GetTotalAllocatedMemoryLong() - memoryBefore).ToString(CultureInfo.InvariantCulture);

            foreach (var instance in _instances)
            {
                AddProbeBody(instance);
                SynchronizePhysicsScene(instance);
                VerifyPhysicsIsolation(instance);
            }

            VerifyConversions(physicsManager, context, regionWidth);
            VerifyNavMeshIsolationAndContinuity(regionWidth);
            foreach (var instance in _instances)
            {
                _evidence.instances.Add(new Test_SimulationRegionSizingInstanceEvidence
                {
                    achievedTileCount = instance.Descriptor.TileSpanX * instance.Descriptor.TileSpanZ,
                    achievedObjectCount = instance.ObjectCount,
                    achievedColliderCount = instance.ColliderCount,
                    navMeshDataCount = instance.NavMeshDataCount,
                    navMeshBuildDurationMilliseconds = instance.NavMeshBuildDurationMilliseconds,
                    navMeshRegistrationDurationMilliseconds = instance.NavMeshRegistrationDurationMilliseconds,
                    physicsQueryDurationMilliseconds = instance.PhysicsQueryDurationMilliseconds,
                    navMeshPathQueryDurationMilliseconds = instance.NavMeshPathQueryDurationMilliseconds,
                });
            }
            _evidence.physicsIsolation = "passed";
            _evidence.navMeshCrossRegionIsolation = "passed";
            _evidence.navMeshInternalTileContinuity = _run.Options.SimulationRegionTileSpan == 1
                ? "not_applicable_control"
                : "passed";
            _run.MarkSimulationRegionSizingWorkloadApplied();
        }

        internal void RecordFailure(Exception exception)
        {
            if (exception is Test_PerformancePrerequisiteException)
                _evidence.status = "blocked";
            else
                _evidence.status = "failed";
            _evidence.blockedReason = exception == null ? "Unknown simulation-region sizing failure." : exception.Message;
        }

        internal IEnumerator CleanupAfterFailure()
        {
            if (_instances.Count > 0)
                yield return CleanupInstances();
            FinalizeEvidence();
        }

        private void FinalizeEvidence()
        {
            _evidence.status = _evidence.status == "unavailable" ? "qualified" : _evidence.status;
            Test_PerformanceEvidence.WriteSimulationRegionSizingEvidence(_run.ResultsDirectory, _evidence);
        }

        private void RequireSharedGrid()
        {
            if (!GameConfigurationManager.IsInitialized || GameConfigurationManager.Config == null)
            {
                throw new Test_PerformancePrerequisiteException(
                    "Simulation-region sizing requires initialized shared game configuration.");
            }

            GameConfigurationManager.Config.ValidateWorldGridConfiguration();
            var cellSizeMetres = GameConfigurationManager.Config.ChunkSize;
            if (cellSizeMetres != 256)
            {
                throw new Test_PerformancePrerequisiteException(
                    $"Simulation-region sizing requires the validated 256 m shared grid; found {cellSizeMetres} m.");
            }
        }

        private Test_WorldFixturePlan ResolveFixturePlan()
        {
            var manifest = Test_WorldFixtureLayout.BuildManifest(new Test_WorldFixtureOptions
            {
                Seed = Test_WorldFixtureLayout.DEFAULT_SEED,
                DensityTier = Test_WorldFixtureDensityTier.D1_Default,
                FixtureVersion = Test_WorldFixtureLayout.FIXTURE_VERSION,
            });
            foreach (var plan in manifest.Scenes)
            {
                if (plan.Variant == _run.Options.WorldFixture.ToString())
                {
                    if (plan.Spawns == null || plan.Spawns.Length == 0 || plan.CollisionObjectCount <= 0 || !plan.IsNavigable)
                        break;
                    return plan;
                }
            }

            throw new Test_PerformancePrerequisiteException(
                "Tracked fixture geometry or navigable content is unavailable; the sizing probe will not substitute an empty region.");
        }

        private Test_SimulationRegionProbeInstance CreateRegionInstance(
            Test_WorldFixturePlan fixture,
            SimulationRegionDescriptor descriptor,
            int cellSizeMetres,
            int index)
        {
            var scene = SceneManager.CreateScene(
                $"SimulationRegionSizing_{_run.RunID}_{index}",
                new CreateSceneParameters(LocalPhysicsMode.Physics3D));
            if (!scene.IsValid())
                throw new Test_PerformancePrerequisiteException("Unity could not create an isolated physics scene for the sizing probe.");

            var root = new GameObject("SimulationRegionProbeRoot_" + index);
            SceneManager.MoveGameObjectToScene(root, scene);
            var instance = new Test_SimulationRegionProbeInstance(descriptor, scene, root);
            _instances.Add(instance);
            try
            {
                for (var tileZ = 0; tileZ < descriptor.TileSpanZ; tileZ++)
                {
                    for (var tileX = 0; tileX < descriptor.TileSpanX; tileX++)
                    {
                        foreach (var spawn in fixture.Spawns)
                        {
                            var primitive = GameObject.CreatePrimitive(ToPrimitiveType(spawn.Primitive));
                            primitive.name = $"{spawn.SpawnID.Replace('/', '_')}_{tileX}_{tileZ}";
                            SceneManager.MoveGameObjectToScene(primitive, scene);
                            primitive.transform.SetParent(root.transform, true);
                            primitive.transform.position = descriptor.LocalAnchor + new Vector3(
                                tileX * cellSizeMetres + (float)(spawn.WorldX - fixture.BoundsMinX),
                                (float)spawn.WorldY,
                                tileZ * cellSizeMetres + (float)(spawn.WorldZ - fixture.BoundsMinZ));
                            primitive.transform.localScale = new Vector3(spawn.ScaleX, spawn.ScaleY, spawn.ScaleZ);
                            if (!spawn.HasCollision)
                            {
                                var collider = primitive.GetComponent<Collider>();
                                if (collider != null)
                                    UnityEngine.Object.DestroyImmediate(collider);
                            }
                            instance.ObjectCount++;
                        }
                    }
                }

                instance.ColliderCount = root.GetComponentsInChildren<Collider>(true).Length;
                instance.ExpectedObjectCount = fixture.ObjectCount * descriptor.TileSpanX * descriptor.TileSpanZ;
                instance.ExpectedColliderCount = fixture.CollisionObjectCount * descriptor.TileSpanX * descriptor.TileSpanZ;
                if (instance.ObjectCount != instance.ExpectedObjectCount || instance.ColliderCount != instance.ExpectedColliderCount)
                {
                    throw new InvalidOperationException(
                        $"Fixture content count mismatch for region {index}: objects {instance.ObjectCount}/{instance.ExpectedObjectCount}, colliders {instance.ColliderCount}/{instance.ExpectedColliderCount}.");
                }

                BuildNavMesh(instance, descriptor);
                return instance;
            }
            catch
            {
                instance.DisposeImmediate();
                throw;
            }
        }

        private static void BuildNavMesh(Test_SimulationRegionProbeInstance instance, SimulationRegionDescriptor descriptor)
        {
            var settings = NavMesh.GetSettingsByID(0);
            if (settings.agentTypeID < 0)
                throw new Test_PerformancePrerequisiteException("BLOCKED_NAVMESH_ISOLATION: no valid Unity NavMesh build settings are available.");

            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(
                instance.Root.transform,
                ~0,
                NavMeshCollectGeometry.PhysicsColliders,
                0,
                new List<NavMeshBuildMarkup>(),
                sources);
            if (sources.Count == 0)
                throw new Test_PerformancePrerequisiteException("BLOCKED_NAVMESH_ISOLATION: tracked fixture produced no NavMesh build sources.");

            var bounds = new Bounds(
                descriptor.LocalAnchor + new Vector3((float)(descriptor.WidthMetres * 0.5d), 0f, (float)(descriptor.DepthMetres * 0.5d)),
                new Vector3((float)descriptor.WidthMetres, 64f, (float)descriptor.DepthMetres));
            var timer = Stopwatch.StartNew();
            var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            timer.Stop();
            if (data == null)
                throw new Test_PerformancePrerequisiteException("BLOCKED_NAVMESH_ISOLATION: Unity could not build test-owned NavMesh data for the tracked fixture.");

            var registrationTimer = Stopwatch.StartNew();
            var instanceData = NavMesh.AddNavMeshData(data);
            registrationTimer.Stop();
            if (!instanceData.valid)
                throw new Test_PerformancePrerequisiteException("BLOCKED_NAVMESH_ISOLATION: Unity rejected test-owned NavMesh data registration.");

            instance.NavMeshData = data;
            instance.NavMeshInstance = instanceData;
            instance.NavMeshBuildDurationMilliseconds = timer.Elapsed.TotalMilliseconds.ToString("R", CultureInfo.InvariantCulture);
            instance.NavMeshRegistrationDurationMilliseconds = registrationTimer.Elapsed.TotalMilliseconds.ToString("R", CultureInfo.InvariantCulture);
            instance.NavMeshDataCount = 1;
        }

        private void AddProbeBody(Test_SimulationRegionProbeInstance instance)
        {
            var probe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            probe.name = "SimulationRegionKnownProbe";
            SceneManager.MoveGameObjectToScene(probe, instance.Scene);
            probe.transform.SetParent(instance.Root.transform, true);
            probe.transform.position = instance.Descriptor.LocalAnchor + new Vector3(
                (float)(instance.Descriptor.WidthMetres * 0.5d),
                1f,
                (float)(instance.Descriptor.DepthMetres * 0.5d));
            probe.transform.localScale = Vector3.one;
            instance.ProbeBody = probe;
        }

        private static void SynchronizePhysicsScene(Test_SimulationRegionProbeInstance instance)
        {
            if (!instance.PhysicsScene.IsValid())
                throw new InvalidOperationException("Simulation-region probe physics scene is invalid before physics synchronization.");

            instance.PhysicsScene.RunSimulationStages(
                0f,
                SimulationStage.PrepareSimulation,
                SimulationOption.SyncTransforms);
        }

        private void VerifyPhysicsIsolation(Test_SimulationRegionProbeInstance instance)
        {
            var expectedCollider = instance.ProbeBody.GetComponent<Collider>();
            if (expectedCollider == null)
                throw new InvalidOperationException("Simulation-region known physics probe has no Collider.");

            var query = instance.ProbeBody.transform.position;
            var colliders = new Collider[64];
            var timer = Stopwatch.StartNew();
            var overlapCount = instance.PhysicsScene.OverlapBox(
                query,
                Vector3.one,
                colliders,
                Quaternion.identity,
                ~0,
                QueryTriggerInteraction.Ignore);
            var raycastHit = instance.PhysicsScene.Raycast(
                query + Vector3.up * 8f,
                Vector3.down,
                out var hit,
                16f,
                ~0,
                QueryTriggerInteraction.Ignore);
            timer.Stop();
            if (overlapCount <= 0)
            {
                throw new InvalidOperationException(
                    $"Physics isolation overlap returned no colliders for region anchor {instance.Descriptor.LocalAnchor}.");
            }

            var expectedColliderObserved = false;
            for (var index = 0; index < overlapCount; index++)
            {
                var collider = colliders[index];
                if (collider == null)
                {
                    throw new InvalidOperationException(
                        $"Physics isolation overlap returned a null collider for region anchor {instance.Descriptor.LocalAnchor}.");
                }

                if (!collider.transform.IsChildOf(instance.Root.transform))
                {
                    throw new InvalidOperationException(
                        $"Physics isolation overlap observed geometry outside the owning region at anchor {instance.Descriptor.LocalAnchor}.");
                }

                if (collider == expectedCollider)
                    expectedColliderObserved = true;
            }

            if (!expectedColliderObserved)
            {
                throw new InvalidOperationException(
                    $"Physics isolation overlap did not observe the known probe collider for region anchor {instance.Descriptor.LocalAnchor}.");
            }

            if (!raycastHit || hit.collider == null)
            {
                throw new InvalidOperationException(
                    $"Physics isolation raycast did not hit region geometry for region anchor {instance.Descriptor.LocalAnchor}.");
            }

            if (!hit.collider.transform.IsChildOf(instance.Root.transform))
            {
                throw new InvalidOperationException(
                    $"Physics isolation raycast crossed into geometry outside the owning region at anchor {instance.Descriptor.LocalAnchor}.");
            }

            instance.PhysicsQueryDurationMilliseconds = timer.Elapsed.TotalMilliseconds.ToString("R", CultureInfo.InvariantCulture);
        }

        private void VerifyConversions(
            ServerPhysicsManager physicsManager,
            ActorSpatialContext context,
            double regionWidth)
        {
            var maxErrorX = 0d;
            var maxErrorY = 0d;
            var maxErrorZ = 0d;
            foreach (var instance in _instances)
            {
                VerifyDescriptorConversion(physicsManager, instance.Descriptor, regionWidth, ref maxErrorX, ref maxErrorY, ref maxErrorZ);
                VerifyBoundaryRejection(physicsManager, instance.Descriptor, regionWidth);
            }

            if (_instances.Count == 1)
            {
                var negativeDescriptor = new SimulationRegionDescriptor(
                    SimulationRegionKey.FromEnvironmentTile(
                        context,
                        new Vector2Int(-_run.Options.SimulationRegionTileSpan, -_run.Options.SimulationRegionTileSpan),
                        _run.Options.SimulationRegionTileSpan,
                        _run.Options.SimulationRegionTileSpan),
                    _run.Options.SimulationRegionTileSpan,
                    _run.Options.SimulationRegionTileSpan,
                    Vector3.zero);
                VerifyDescriptorConversion(physicsManager, negativeDescriptor, regionWidth, ref maxErrorX, ref maxErrorY, ref maxErrorZ);
                VerifyBoundaryRejection(physicsManager, negativeDescriptor, regionWidth);
            }

            _evidence.maxRoundTripErrorXMetres = maxErrorX.ToString("R", CultureInfo.InvariantCulture);
            _evidence.maxRoundTripErrorYMetres = maxErrorY.ToString("R", CultureInfo.InvariantCulture);
            _evidence.maxRoundTripErrorZMetres = maxErrorZ.ToString("R", CultureInfo.InvariantCulture);
        }

        private static void VerifyDescriptorConversion(
            ServerPhysicsManager physicsManager,
            SimulationRegionDescriptor descriptor,
            double regionWidth,
            ref double maxErrorX,
            ref double maxErrorY,
            ref double maxErrorZ)
        {
            var points = new[]
            {
                new WorldPosition(descriptor.WorldOrigin.x, 0d, descriptor.WorldOrigin.z),
                new WorldPosition(descriptor.WorldOrigin.x + regionWidth * 0.5d, 7d, descriptor.WorldOrigin.z + regionWidth * 0.5d),
                new WorldPosition(descriptor.WorldOrigin.x + regionWidth - 0.001d, 3d, descriptor.WorldOrigin.z + regionWidth - 0.001d),
            };
            foreach (var point in points)
            {
                var local = physicsManager.ToRegionLocal(descriptor, point);
                var reconstructed = physicsManager.ToWorld(descriptor, local);
                maxErrorX = Math.Max(maxErrorX, Math.Abs(reconstructed.x - point.x));
                maxErrorY = Math.Max(maxErrorY, Math.Abs(reconstructed.y - point.y));
                maxErrorZ = Math.Max(maxErrorZ, Math.Abs(reconstructed.z - point.z));
            }
        }

        private static void VerifyBoundaryRejection(
            ServerPhysicsManager physicsManager,
            SimulationRegionDescriptor descriptor,
            double regionWidth)
        {
            var rejectedWorldX = false;
            var rejectedWorldZ = false;
            try
            {
                physicsManager.ToRegionLocal(
                    descriptor,
                    new WorldPosition(descriptor.WorldOrigin.x + regionWidth, 0d, descriptor.WorldOrigin.z + 1d));
            }
            catch (ArgumentOutOfRangeException)
            {
                rejectedWorldX = true;
            }
            try
            {
                physicsManager.ToRegionLocal(
                    descriptor,
                    new WorldPosition(descriptor.WorldOrigin.x + 1d, 0d, descriptor.WorldOrigin.z + regionWidth));
            }
            catch (ArgumentOutOfRangeException)
            {
                rejectedWorldZ = true;
            }

            var rejectedLocalX = false;
            var rejectedLocalZ = false;
            try
            {
                physicsManager.ToWorld(
                    descriptor,
                    descriptor.LocalAnchor + new Vector3((float)regionWidth, 0f, 1f));
            }
            catch (ArgumentOutOfRangeException)
            {
                rejectedLocalX = true;
            }
            try
            {
                physicsManager.ToWorld(
                    descriptor,
                    descriptor.LocalAnchor + new Vector3(1f, 0f, (float)regionWidth));
            }
            catch (ArgumentOutOfRangeException)
            {
                rejectedLocalZ = true;
            }

            if (!rejectedWorldX || !rejectedWorldZ || !rejectedLocalX || !rejectedLocalZ)
                throw new InvalidOperationException("Simulation-region conversion did not enforce half-open positive-edge ownership.");
        }

        private void VerifyNavMeshIsolationAndContinuity(double regionWidth)
        {
            foreach (var instance in _instances)
            {
                var centre = instance.Descriptor.LocalAnchor + new Vector3(
                    (float)(regionWidth * 0.5d),
                    1f,
                    (float)(regionWidth * 0.5d));
                if (!NavMesh.SamplePosition(centre, out var sample, 8f, NavMesh.AllAreas))
                    throw new InvalidOperationException("NavMesh isolation probe could not resolve navigation in the owning region.");
                if (!IsInsideLocalFootprint(instance.Descriptor, sample.position))
                    throw new InvalidOperationException("NavMesh query resolved onto another concurrent region's representation.");

                if (_run.Options.SimulationRegionTileSpan == 1)
                    continue;

                var pathStart = instance.Descriptor.LocalAnchor + new Vector3(
                    (float)(regionWidth * 0.25d),
                    1f,
                    (float)(regionWidth * 0.5d));
                var pathEnd = instance.Descriptor.LocalAnchor + new Vector3(
                    (float)(regionWidth * 0.75d),
                    1f,
                    (float)(regionWidth * 0.5d));
                var pathTimer = Stopwatch.StartNew();
                var path = new NavMeshPath();
                var complete = NavMesh.SamplePosition(pathStart, out var startHit, 8f, NavMesh.AllAreas)
                    && NavMesh.SamplePosition(pathEnd, out var endHit, 8f, NavMesh.AllAreas)
                    && NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, path)
                    && path.status == NavMeshPathStatus.PathComplete;
                pathTimer.Stop();
                if (!complete)
                    throw new InvalidOperationException("NavMesh path could not cross an internal 256 m fixture-tile boundary within one region.");
                instance.NavMeshPathQueryDurationMilliseconds = pathTimer.Elapsed.TotalMilliseconds.ToString("R", CultureInfo.InvariantCulture);
            }
        }

        private static bool IsInsideLocalFootprint(SimulationRegionDescriptor descriptor, Vector3 position)
        {
            return position.x >= descriptor.LocalAnchor.x
                && position.x < descriptor.LocalAnchor.x + descriptor.WidthMetres
                && position.z >= descriptor.LocalAnchor.z
                && position.z < descriptor.LocalAnchor.z + descriptor.DepthMetres;
        }

        private IEnumerator RunPhase(PerformanceMeasurementPhase phase, float durationSeconds, bool recordSamples)
        {
            _run.BeginPhase(phase);
            var started = Time.realtimeSinceStartup;
            var nextSample = 0f;
            while (Time.realtimeSinceStartup - started < durationSeconds)
            {
                if (_run.StopRequested)
                    throw new Test_PerformanceUserStopException("The user requested the simulation-region sizing run to stop.");
                ThrowIfResourceCeilingExceeded();
                var elapsed = Time.realtimeSinceStartup - started;
                if (recordSamples && elapsed >= nextSample)
                {
                    _run.RecordMeasurementSample();
                    nextSample += Test_PerformanceRun.SAMPLE_INTERVAL_SECONDS;
                }
                _run.RecordMeasurementFrame();
                yield return null;
            }
            _run.MarkPhaseComplete(phase);
        }

        private static void ThrowIfResourceCeilingExceeded()
        {
            var allocatedBytes = Profiler.GetTotalAllocatedMemoryLong();
            if (allocatedBytes > Test_PerformanceRun.MAX_TOTAL_ALLOCATED_BYTES)
            {
                throw new Test_PerformanceResourceCeilingException(
                    $"Simulation-region sizing exceeded the allocated-memory ceiling: {allocatedBytes} bytes > {Test_PerformanceRun.MAX_TOTAL_ALLOCATED_BYTES} bytes.");
            }
        }

        private IEnumerator CleanupInstances()
        {
            if (_instances.Count == 0)
                yield break;

            foreach (var instance in _instances)
                instance.DisposeImmediate();
            yield return null;
            foreach (var instance in _instances)
            {
                if (!instance.Scene.IsValid() || !instance.Scene.isLoaded)
                    continue;
                var operation = SceneManager.UnloadSceneAsync(instance.Scene);
                if (operation == null)
                    throw new InvalidOperationException("Unity did not create a scene unload operation for a sizing probe region.");
                while (!operation.isDone)
                    yield return null;
            }
            _instances.Clear();
        }

        private static Vector2Int GetRegionCoordinate(int index)
        {
            if (index == 0)
                return Vector2Int.zero;
            if (index == 1)
                return new Vector2Int(-1, -1);
            return new Vector2Int(index - 1, 0);
        }

        private static PrimitiveType ToPrimitiveType(string primitive)
        {
            switch (primitive)
            {
                case "Cube": return PrimitiveType.Cube;
                case "Cylinder": return PrimitiveType.Cylinder;
                case "Sphere": return PrimitiveType.Sphere;
                case "Capsule": return PrimitiveType.Capsule;
                case "Plane": return PrimitiveType.Plane;
                default: throw new InvalidOperationException("Tracked fixture contains unsupported primitive geometry: " + primitive);
            }
        }
    }

    [Serializable]
    internal sealed class Test_SimulationRegionSizingEvidence
    {
        public string status = "unavailable";
        public string phase;
        public int tileSpan;
        public int concurrentRegions;
        public int requiredQualifiedRepeats;
        public string fixtureVariant;
        public string fixturePath;
        public string cellSizeMetres = "unavailable";
        public string regionWidthMetres = "unavailable";
        public string setupDurationMilliseconds = "unavailable";
        public string teardownDurationMilliseconds = "unavailable";
        public string memoryBeforeBytes = "unavailable";
        public string memoryAfterSetupBytes = "unavailable";
        public string memoryDeltaBytes = "unavailable";
        public string maxRoundTripErrorXMetres = "unavailable";
        public string maxRoundTripErrorYMetres = "unavailable";
        public string maxRoundTripErrorZMetres = "unavailable";
        public string physicsIsolation = "unavailable";
        public string navMeshInternalTileContinuity = "unavailable";
        public string navMeshCrossRegionIsolation = "unavailable";
        public string cleanup = "unavailable";
        public string blockedReason;
        public readonly List<Test_SimulationRegionSizingInstanceEvidence> instances = new();
    }

    [Serializable]
    internal sealed class Test_SimulationRegionSizingInstanceEvidence
    {
        public int achievedTileCount;
        public int achievedObjectCount;
        public int achievedColliderCount;
        public int navMeshDataCount;
        public string navMeshBuildDurationMilliseconds = "unavailable";
        public string navMeshRegistrationDurationMilliseconds = "unavailable";
        public string physicsQueryDurationMilliseconds = "unavailable";
        public string navMeshPathQueryDurationMilliseconds = "unavailable";
    }

    internal sealed class Test_SimulationRegionProbeInstance
    {
        internal Test_SimulationRegionProbeInstance(SimulationRegionDescriptor descriptor, Scene scene, GameObject root)
        {
            Descriptor = descriptor;
            Scene = scene;
            PhysicsScene = scene.GetPhysicsScene();
            Root = root;
        }

        internal SimulationRegionDescriptor Descriptor { get; }
        internal Scene Scene { get; }
        internal PhysicsScene PhysicsScene { get; }
        internal GameObject Root { get; }
        internal GameObject ProbeBody { get; set; }
        internal NavMeshData NavMeshData { get; set; }
        internal NavMeshDataInstance NavMeshInstance { get; set; }
        internal int ObjectCount { get; set; }
        internal int ColliderCount { get; set; }
        internal int ExpectedObjectCount { get; set; }
        internal int ExpectedColliderCount { get; set; }
        internal int NavMeshDataCount { get; set; }
        internal string NavMeshBuildDurationMilliseconds { get; set; } = "unavailable";
        internal string NavMeshRegistrationDurationMilliseconds { get; set; } = "unavailable";
        internal string PhysicsQueryDurationMilliseconds { get; set; } = "unavailable";
        internal string NavMeshPathQueryDurationMilliseconds { get; set; } = "unavailable";

        internal void DisposeImmediate()
        {
            if (NavMeshInstance.valid)
                NavMeshInstance.Remove();
            NavMeshInstance = default;
            NavMeshData = null;
            if (Root != null)
                UnityEngine.Object.DestroyImmediate(Root);
        }
    }
}
