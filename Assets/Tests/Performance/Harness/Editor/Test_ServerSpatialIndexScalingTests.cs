using Game.Server.Core;
using Game.Shared;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

namespace Game.Tests.Performance.Harness
{
    /// <summary>Provides deterministic evidence that spatial work remains local as distant population grows.</summary>
    public sealed class Test_ServerSpatialIndexScalingTests
    {
        private const int LOCAL_ACTOR_COUNT = 32;
        private const int SAMPLE_COUNT = 5;
        private const int OPERATIONS_PER_SAMPLE = 2000;
        private const string TEST_IDENTIFIER = "Test_ServerSpatialIndexScalingTests.LocalSpatialQueryCostIsIndependentOfDistantPopulation";
        private const string TIMING_QUALIFICATION =
            "Timings are descriptive evidence only; no Step1.2 timing budget is defined.";

        private static readonly Guid _benchmarkWorldID = Guid.Parse("b6a7d8e9-f001-4234-8567-89abcdef0123");
        private static readonly int[] _totalActorCounts = { 128, 1024, 8192 };
        private static readonly WorldPosition _queryCenter = new WorldPosition(0d, 0d, 0d);

        [Test]
        public void LocalSpatialQueryCostIsIndependentOfDistantPopulation()
        {
            if (!GameConfigurationManager.IsInitialized)
                GameConfigurationManager.Initialize();

            Assert.That(GameConfigurationManager.Config, Is.Not.Null);
            GameConfigurationManager.Config.ValidateSpatialConfiguration();

            double cellWidth = GameConfigurationManager.Config.SpatialIndexCellWidth;
            double queryRadius = GameConfigurationManager.Config.ActorVisibilityEntryRadius;
            var context = ActorSpatialContext.CreatePublic(_benchmarkWorldID, "stage12-spatial-scaling");
            var tierEvidence = new SpatialScalingTierEvidence[_totalActorCounts.Length];

            for (int tierIndex = 0; tierIndex < _totalActorCounts.Length; tierIndex++)
            {
                tierEvidence[tierIndex] = RunTier(
                    _totalActorCounts[tierIndex],
                    cellWidth,
                    queryRadius,
                    context);
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            Test_PerformanceRunMetadata metadata = Test_PerformanceRunMetadata.Capture(projectRoot);
            var evidence = new SpatialScalingEvidence
            {
                schema = "ninth-age.performance-evidence.v1",
                test_identifier = TEST_IDENTIFIER,
                captured_utc = metadata.CapturedUtc,
                repository = metadata.Repository,
                git_branch = metadata.GitBranch,
                git_branch_status = metadata.GitBranchStatus,
                git_branch_diagnostic = metadata.GitBranchDiagnostic,
                git_commit = metadata.Commit,
                git_dirty = metadata.Dirty,
                git_diff_sha256 = metadata.DiffSha256,
                packages_sha256 = metadata.PackagesSha256,
                packages_lock_sha256 = metadata.PackagesLockSha256,
                purrnet_sha256 = metadata.PurrNetSha256,
                unity_version = Application.unityVersion,
                host_id = metadata.HostID,
                process_id = metadata.ProcessID,
                hardware = metadata.Hardware,
                memory_bytes = metadata.MemoryBytes,
                cell_width = cellWidth,
                query_radius = queryRadius,
                samples_per_tier = SAMPLE_COUNT,
                query_operations_per_sample = OPERATIONS_PER_SAMPLE,
                move_operations_per_sample = OPERATIONS_PER_SAMPLE,
                timing_unit = "nanoseconds_per_operation",
                timing_qualification = TIMING_QUALIFICATION,
                tiers = tierEvidence,
            };

            WriteEvidence(projectRoot, evidence);
        }

        private static SpatialScalingTierEvidence RunTier(
            int totalActorCount,
            double cellWidth,
            double queryRadius,
            ActorSpatialContext context)
        {
            int distantActorCount = totalActorCount - LOCAL_ACTOR_COUNT;
            var spatialIndex = new ServerSpatialIndex(cellWidth);
            var acceptedPositions = new Dictionary<Guid, WorldPosition>(totalActorCount);
            var localActorIDs = new HashSet<Guid>();

            for (int localIndex = 0; localIndex < LOCAL_ACTOR_COUNT; localIndex++)
            {
                Guid actorID = CreateActorID(localIndex + 1);
                WorldPosition position = CreateLocalPosition(localIndex, queryRadius);
                localActorIDs.Add(actorID);
                acceptedPositions.Add(actorID, position);
                spatialIndex.Insert(actorID, context, position);
            }

            double distantSpacing = 2d * cellWidth;
            double distantStart = Math.Max(10000d, queryRadius + distantSpacing);
            for (int distantIndex = 0; distantIndex < distantActorCount; distantIndex++)
            {
                Guid actorID = CreateActorID(1000000 + distantIndex);
                WorldPosition position = new WorldPosition(
                    distantStart + distantIndex * distantSpacing,
                    0d,
                    0d);
                acceptedPositions.Add(actorID, position);
                spatialIndex.Insert(actorID, context, position);
            }

            Func<Guid, WorldPosition> acceptedPositionResolver =
                actorID => ResolveAcceptedPosition(acceptedPositions, actorID);
            var resultBuffer = new List<Guid>(LOCAL_ACTOR_COUNT);

            int localSubjectIndex = 0;
            Guid localSubjectID = CreateActorID(localSubjectIndex + 1);
            WorldPosition canonicalPosition = acceptedPositions[localSubjectID];
            WorldPosition movePositionA = new WorldPosition(cellWidth * 0.25d, 0d, 0d);
            WorldPosition movePositionB = new WorldPosition(cellWidth * 1.25d, 0d, 0d);

            double[] queryNanosecondsPerOperation = MeasureQueries(
                spatialIndex,
                context,
                queryRadius,
                resultBuffer,
                acceptedPositionResolver,
                localActorIDs);
            double[] moveNanosecondsPerOperation = MeasureMoves(
                spatialIndex,
                context,
                acceptedPositions,
                localSubjectID,
                canonicalPosition,
                movePositionA,
                movePositionB);

            QueryAndAssert(
                spatialIndex,
                context,
                queryRadius,
                resultBuffer,
                acceptedPositionResolver,
                localActorIDs);

            return new SpatialScalingTierEvidence
            {
                total_actors = totalActorCount,
                local_actor_count = LOCAL_ACTOR_COUNT,
                distant_actor_count = distantActorCount,
                returned_count = LOCAL_ACTOR_COUNT,
                examined_candidate_count = LOCAL_ACTOR_COUNT,
                query_nanoseconds_per_operation = queryNanosecondsPerOperation,
                move_nanoseconds_per_operation = moveNanosecondsPerOperation,
            };
        }

        private static double[] MeasureQueries(
            ServerSpatialIndex spatialIndex,
            ActorSpatialContext context,
            double queryRadius,
            List<Guid> resultBuffer,
            Func<Guid, WorldPosition> acceptedPositionResolver,
            HashSet<Guid> localActorIDs)
        {
            QueryAndAssert(
                spatialIndex,
                context,
                queryRadius,
                resultBuffer,
                acceptedPositionResolver,
                localActorIDs);

            var samples = new double[SAMPLE_COUNT];
            for (int sampleIndex = 0; sampleIndex < SAMPLE_COUNT; sampleIndex++)
            {
                long startTimestamp = Stopwatch.GetTimestamp();
                int returnedCount = 0;
                int examinedCandidateCount = 0;
                for (int operationIndex = 0; operationIndex < OPERATIONS_PER_SAMPLE; operationIndex++)
                {
                    resultBuffer.Clear();
                    returnedCount = spatialIndex.QueryInRange(
                        context,
                        _queryCenter,
                        queryRadius,
                        resultBuffer,
                        acceptedPositionResolver,
                        out examinedCandidateCount,
                        includeVerticalDistance: true);
                }

                long elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
                samples[sampleIndex] = NanosecondsPerOperation(elapsedTicks, OPERATIONS_PER_SAMPLE);
                AssertQueryResult(returnedCount, examinedCandidateCount, resultBuffer, localActorIDs);
            }

            return samples;
        }

        private static double[] MeasureMoves(
            ServerSpatialIndex spatialIndex,
            ActorSpatialContext context,
            Dictionary<Guid, WorldPosition> acceptedPositions,
            Guid localSubjectID,
            WorldPosition canonicalPosition,
            WorldPosition movePositionA,
            WorldPosition movePositionB)
        {
            spatialIndex.Move(localSubjectID, context, movePositionA);
            acceptedPositions[localSubjectID] = movePositionA;

            var samples = new double[SAMPLE_COUNT];
            bool moveToA = false;
            for (int sampleIndex = 0; sampleIndex < SAMPLE_COUNT; sampleIndex++)
            {
                long startTimestamp = Stopwatch.GetTimestamp();
                for (int operationIndex = 0; operationIndex < OPERATIONS_PER_SAMPLE; operationIndex++)
                {
                    WorldPosition nextPosition = moveToA ? movePositionA : movePositionB;
                    spatialIndex.Move(localSubjectID, context, nextPosition);
                    acceptedPositions[localSubjectID] = nextPosition;
                    moveToA = !moveToA;
                }

                long elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
                samples[sampleIndex] = NanosecondsPerOperation(elapsedTicks, OPERATIONS_PER_SAMPLE);
            }

            spatialIndex.Move(localSubjectID, context, canonicalPosition);
            acceptedPositions[localSubjectID] = canonicalPosition;
            return samples;
        }

        private static void QueryAndAssert(
            ServerSpatialIndex spatialIndex,
            ActorSpatialContext context,
            double queryRadius,
            List<Guid> resultBuffer,
            Func<Guid, WorldPosition> acceptedPositionResolver,
            HashSet<Guid> localActorIDs)
        {
            resultBuffer.Clear();
            int returnedCount = spatialIndex.QueryInRange(
                context,
                _queryCenter,
                queryRadius,
                resultBuffer,
                acceptedPositionResolver,
                out int examinedCandidateCount,
                includeVerticalDistance: true);
            AssertQueryResult(returnedCount, examinedCandidateCount, resultBuffer, localActorIDs);
        }

        private static void AssertQueryResult(
            int returnedCount,
            int examinedCandidateCount,
            List<Guid> resultBuffer,
            HashSet<Guid> localActorIDs)
        {
            Assert.That(returnedCount, Is.EqualTo(LOCAL_ACTOR_COUNT));
            Assert.That(examinedCandidateCount, Is.EqualTo(LOCAL_ACTOR_COUNT));
            Assert.That(resultBuffer.Count, Is.EqualTo(LOCAL_ACTOR_COUNT));
            Assert.That(localActorIDs.SetEquals(resultBuffer), Is.True);
        }

        private static WorldPosition ResolveAcceptedPosition(
            Dictionary<Guid, WorldPosition> acceptedPositions,
            Guid actorID)
        {
            if (!acceptedPositions.TryGetValue(actorID, out WorldPosition position))
            {
                throw new KeyNotFoundException($"No accepted position exists for actor '{actorID}'.");
            }

            return position;
        }

        private static WorldPosition CreateLocalPosition(int localIndex, double queryRadius)
        {
            int column = localIndex % 8;
            int row = localIndex / 8;
            double localRadius = queryRadius * 0.25d;
            double x = (column - 3.5d) / 3.5d * localRadius;
            double z = (row - 1.5d) / 1.5d * localRadius;
            return new WorldPosition(x, 0d, z);
        }

        private static Guid CreateActorID(int ordinal)
        {
            return Guid.Parse($"00000000-0000-0000-0000-{ordinal:X12}");
        }

        private static double NanosecondsPerOperation(long elapsedTicks, int operationCount)
        {
            return elapsedTicks * (1000000000d / Stopwatch.Frequency) / operationCount;
        }

        private static void WriteEvidence(string projectRoot, SpatialScalingEvidence evidence)
        {
            string resultRoot = Path.GetFullPath(Path.Combine(projectRoot, Test_PerformanceRun.RESULT_ROOT));
            Directory.CreateDirectory(resultRoot);

            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'", System.Globalization.CultureInfo.InvariantCulture);
            string baseName = $"stage12-spatial-scaling-{timestamp}-{evidence.process_id}";
            string evidencePath = Path.Combine(resultRoot, baseName + ".json");
            int collisionIndex = 1;
            while (File.Exists(evidencePath))
            {
                evidencePath = Path.Combine(resultRoot, $"{baseName}-{collisionIndex:D2}.json");
                collisionIndex++;
            }

            string temporaryPath = evidencePath + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(evidence, true), new UTF8Encoding(false));
                File.Move(temporaryPath, evidencePath);
            }
            catch
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
                throw;
            }
        }

        [Serializable]
        private sealed class SpatialScalingEvidence
        {
            public string schema;
            public string test_identifier;
            public string captured_utc;
            public string repository;
            public string git_branch;
            public string git_branch_status;
            public string git_branch_diagnostic;
            public string git_commit;
            public bool git_dirty;
            public string git_diff_sha256;
            public string packages_sha256;
            public string packages_lock_sha256;
            public string purrnet_sha256;
            public string unity_version;
            public string host_id;
            public int process_id;
            public string hardware;
            public long memory_bytes;
            public double cell_width;
            public double query_radius;
            public int samples_per_tier;
            public int query_operations_per_sample;
            public int move_operations_per_sample;
            public string timing_unit;
            public string timing_qualification;
            public SpatialScalingTierEvidence[] tiers;
        }

        [Serializable]
        private sealed class SpatialScalingTierEvidence
        {
            public int total_actors;
            public int local_actor_count;
            public int distant_actor_count;
            public int returned_count;
            public int examined_candidate_count;
            public double[] query_nanoseconds_per_operation;
            public double[] move_nanoseconds_per_operation;
        }
    }
}
