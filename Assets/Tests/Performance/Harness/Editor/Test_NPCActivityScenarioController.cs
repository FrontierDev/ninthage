using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using Game.Tests.Performance.Fixtures.Actors;
using PurrNet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Game.Tests.Performance.Harness
{
    /// <summary>Captures the real player actors used by a Stage 1.4 scenario.</summary>
    internal sealed class Test_NPCActivityPlayerCapture
    {
        public IReadOnlyList<PlayerActor> Players { get; set; }
        public PlayerActor Anchor { get; set; }
        public ActorSpatialContext Context { get; set; }
        public WorldPosition AnchorPosition { get; set; }
    }

    /// <summary>Evidence fields specific to one Stage 1.4 NPC activity run.</summary>
    [Serializable]
    internal sealed class Test_NPCActivityScenarioEvidence
    {
        public string npc_activity_scenario;
        public int requested_npc_count;
        public int achieved_full_npc_count;
        public int required_real_player_count;
        public int achieved_real_player_count;
        public string anchor_actor_id;
        public string anchor_initial_context;
        public double[] anchor_initial_position;
        public string authoritative_world_id;
        public string authoritative_map_id;
        public string authoritative_instance_id;
        public string[] ordinary_runtime_chunks;
        public double[] mass_wake_cluster_centre;
        public bool mass_wake_reached;
        public string context_transition_source;
        public string context_transition_destination;
    }

    /// <summary>
    /// Owns Stage 1.4 scenario timing and observations. It deliberately contains no player movement or NPC
    /// activity mutation; those transitions remain authoritative in the runtime systems under test.
    /// </summary>
    internal sealed class Test_NPCActivityScenarioController
    {
        public const float MASS_WAKE_ACTION_SECONDS = 150f;
        private readonly Test_NPCActivityScenario _scenario;
        private readonly IReadOnlyList<PlayerActor> _players;
        private readonly PlayerActor _anchor;
        private readonly ActorSpatialContext _initialContext;
        private readonly WorldPosition _anchorInitialPosition;
        private readonly WorldPosition _massWakeClusterCentre;
        private readonly IReadOnlyList<Test_ActorFixturePlacement> _placements;
        private readonly int _requestedNPCCount;
        private readonly float _passiveAggroRange;
        private bool _massWakeArmed;
        private bool _massWakeReached;
        private bool _contextTransitionRequested;
        private bool _contextTransitionObserved;
        private WorldPosition _massWakeArmedPosition;
        private ActorSpatialContext _contextTransitionSource;
        private ActorSpatialContext _contextTransitionDestination;

        public Test_NPCActivityScenarioController(
            Test_NPCActivityScenario scenario,
            Test_NPCActivityPlayerCapture playerCapture,
            Test_ActorFixtureProfile profile,
            IReadOnlyList<Test_ActorFixturePlacement> placements,
            float passiveAggroRange)
        {
            if (playerCapture == null)
                throw new ArgumentNullException(nameof(playerCapture));
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));
            if (placements == null)
                throw new ArgumentNullException(nameof(placements));

            _scenario = scenario;
            _players = playerCapture.Players ?? throw new ArgumentException("The Stage 1.4 player capture has no players.", nameof(playerCapture));
            _anchor = playerCapture.Anchor ?? throw new ArgumentException("The Stage 1.4 player capture has no anchor player.", nameof(playerCapture));
            _initialContext = playerCapture.Context;
            _anchorInitialPosition = playerCapture.AnchorPosition;
            _massWakeClusterCentre = new WorldPosition(_anchorInitialPosition.x + 140d, _anchorInitialPosition.y, _anchorInitialPosition.z);
            _placements = placements;
            _requestedNPCCount = profile.population.npcs.requested;
            _passiveAggroRange = passiveAggroRange;
            ValidatePlacements(placements, profile.population.npcs.requested);
        }

        public Test_NPCActivityScenario Scenario => _scenario;
        public PlayerActor Anchor => _anchor;
        public WorldPosition AnchorInitialPosition => _anchorInitialPosition;
        public ActorSpatialContext InitialContext => _initialContext;
        public WorldPosition MassWakeClusterCentre => _massWakeClusterCentre;
        public bool MassWakeArmed => _massWakeArmed;
        public bool MassWakeReached => _massWakeReached;
        public bool ContextTransitionRequested => _contextTransitionRequested;
        public bool ContextTransitionObserved => _contextTransitionObserved;
        public ActorSpatialContext ContextTransitionSource => _contextTransitionSource;
        public ActorSpatialContext ContextTransitionDestination => _contextTransitionDestination;

        public static bool TryCaptureRealPlayers(
            int requiredCount,
            bool requireDenseCluster,
            out Test_NPCActivityPlayerCapture capture,
            out string diagnostic)
        {
            capture = null;
            diagnostic = null;
            if (requiredCount <= 0)
            {
                diagnostic = "Stage 1.4 requires a positive real-player count.";
                return false;
            }
            if (NetworkManager.main == null)
            {
                diagnostic = "PurrNet NetworkManager is unavailable while capturing Stage 1.4 real players.";
                return false;
            }
            if (NetworkManager.main.playerCount != requiredCount)
            {
                diagnostic = $"Stage 1.4 requires exactly {requiredCount} real player connection(s); PurrNet reports {NetworkManager.main.playerCount}.";
                return false;
            }
            if (ChunkSubscriptions.PLAYER_ACTORS.Count != requiredCount)
            {
                diagnostic = $"Stage 1.4 requires exactly {requiredCount} registered PlayerActor entries; the player registry contains {ChunkSubscriptions.PLAYER_ACTORS.Count}.";
                return false;
            }

            var players = new List<PlayerActor>(requiredCount);
            ActorSpatialContext context = default;
            WorldPosition anchorPosition = default;
            var hasContext = false;
            foreach (var entry in ChunkSubscriptions.PLAYER_ACTORS)
            {
                if (!(entry.Value is PlayerActor player))
                {
                    diagnostic = $"Player registry entry '{entry.Key}' is not a PlayerActor; synthetic or base Actor entries are not accepted.";
                    return false;
                }
                if (player.Owner == null || !player.Owner.Value.Equals(entry.Key))
                {
                    diagnostic = $"PlayerActor '{player.ID}' does not have an owner matching registry key '{entry.Key}'.";
                    return false;
                }
                if (player.isDead)
                {
                    diagnostic = $"PlayerActor '{player.ID}' is dead and cannot be used as a Stage 1.4 real player.";
                    return false;
                }
                if (!player.SpatialContext.IsValid)
                {
                    diagnostic = $"PlayerActor '{player.ID}' has no valid authoritative spatial context.";
                    return false;
                }
                if (!player.TryGetAcceptedPositionSnapshot(out var snapshot))
                {
                    diagnostic = $"PlayerActor '{player.ID}' has no accepted position snapshot.";
                    return false;
                }
                if (!hasContext)
                {
                    context = player.SpatialContext;
                    hasContext = true;
                    anchorPosition = snapshot.Position;
                }
                else if (!context.IsCompatibleWith(player.SpatialContext))
                {
                    diagnostic = $"Stage 1.4 real players are not in compatible spatial contexts: '{context}' and '{player.SpatialContext}'.";
                    return false;
                }
                players.Add(player);
            }

            players.Sort((left, right) => left.ID.CompareTo(right.ID));
            var anchor = players[0];
            if (!anchor.TryGetAcceptedPositionSnapshot(out var anchorSnapshot))
            {
                diagnostic = $"Anchor PlayerActor '{anchor.ID}' has no accepted position snapshot after deterministic sorting.";
                return false;
            }
            anchorPosition = anchorSnapshot.Position;
            if (requireDenseCluster)
            {
                foreach (var player in players)
                {
                    if (!player.TryGetAcceptedPositionSnapshot(out var playerSnapshot)
                        || HorizontalDistance(anchorPosition, playerSnapshot.Position) > 20d)
                    {
                        diagnostic = $"DensePlayerCluster requires all {requiredCount} real players within 20m horizontal distance of anchor '{anchor.ID}'.";
                        return false;
                    }
                }
            }

            capture = new Test_NPCActivityPlayerCapture
            {
                Players = players,
                Anchor = anchor,
                Context = context,
                AnchorPosition = anchorPosition,
            };
            return true;
        }

        public void ObserveMeasurement(float elapsedSeconds)
        {
            if (elapsedSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            ValidatePlayerReferences();

            if (_scenario == Test_NPCActivityScenario.MassWake)
            {
                if (!_massWakeArmed && elapsedSeconds >= MASS_WAKE_ACTION_SECONDS)
                {
                    if (!_anchor.TryGetAcceptedPositionSnapshot(out var armedSnapshot))
                        throw new InvalidOperationException($"Anchor PlayerActor '{_anchor.ID}' lost its accepted position at MassWake arm time.");
                    _massWakeArmedPosition = armedSnapshot.Position;
                    _massWakeArmed = true;
                    UnityEngine.Debug.Log($"Stage 1.4 MassWake destination armed at ({_massWakeClusterCentre.x.ToString("R", CultureInfo.InvariantCulture)}, {_massWakeClusterCentre.y.ToString("R", CultureInfo.InvariantCulture)}, {_massWakeClusterCentre.z.ToString("R", CultureInfo.InvariantCulture)}); the real player must move there.");
                }
                if (_massWakeArmed && !_massWakeReached
                    && HorizontalDistance(_massWakeArmedPosition, _massWakeClusterCentre) > 40d
                    && _anchor.TryGetAcceptedPositionSnapshot(out var currentPosition)
                    && HorizontalDistance(currentPosition.Position, _massWakeClusterCentre) <= 40d)
                {
                    _massWakeReached = true;
                }
            }
            else if (_scenario == Test_NPCActivityScenario.CrossInstance)
            {
                if (!_contextTransitionRequested && elapsedSeconds >= MASS_WAKE_ACTION_SECONDS)
                {
                    if (ActorEndpoint.onServerInstanceValidationToggleRequested == null)
                        throw new Test_PerformancePrerequisiteException("CrossInstance requires ActorEndpoint.onServerInstanceValidationToggleRequested to be registered by ordinary server initialization.");
                    if (_anchor.Owner == null)
                        throw new InvalidOperationException($"Anchor PlayerActor '{_anchor.ID}' lost its real owner before the context transition.");
                    _contextTransitionSource = _anchor.SpatialContext;
                    _contextTransitionRequested = true;
                    ActorEndpoint.onServerInstanceValidationToggleRequested.Invoke(_anchor.Owner.Value);
                }
                if (_contextTransitionRequested && !_contextTransitionObserved)
                {
                    if (_anchor.SpatialContext.IsValid && !_anchor.SpatialContext.IsCompatibleWith(_contextTransitionSource))
                    {
                        _contextTransitionDestination = _anchor.SpatialContext;
                        _contextTransitionObserved = true;
                    }
                }
            }
        }

        public void ValidateMeasurementComplete()
        {
            if (_scenario == Test_NPCActivityScenario.MassWake && !_massWakeReached)
                throw new Test_NPCActivityWorkloadNotAchievedException("WORKLOAD_NOT_ACHIEVED: MassWake did not observe the real anchor player reach the <=40m cluster during measurement.");
            if (_scenario == Test_NPCActivityScenario.CrossInstance && !_contextTransitionObserved)
                throw new Test_NPCActivityWorkloadNotAchievedException("WORKLOAD_NOT_ACHIEVED: CrossInstance did not observe the anchor PlayerActor enter a different spatial context during measurement.");
        }

        public void ValidatePlayerReferencesForSentinel()
        {
            ValidatePlayerReferences();
        }

        public Test_NPCActivityScenarioEvidence BuildEvidence(int achievedFullNPCCount)
        {
            return new Test_NPCActivityScenarioEvidence
            {
                npc_activity_scenario = _scenario.ToString(),
                requested_npc_count = _requestedNPCCount,
                achieved_full_npc_count = achievedFullNPCCount,
                required_real_player_count = Test_ActorFixtureLayout.GetStage14NPCActivityRequiredPlayerCount(_scenario),
                achieved_real_player_count = _players.Count,
                anchor_actor_id = _anchor.ID.ToString(),
                anchor_initial_context = FormatContext(_initialContext),
                anchor_initial_position = new[] { _anchorInitialPosition.x, _anchorInitialPosition.y, _anchorInitialPosition.z },
                authoritative_world_id = _initialContext.WorldID.ToString(),
                authoritative_map_id = _initialContext.MapID,
                authoritative_instance_id = _initialContext.InstanceID == Guid.Empty ? null : _initialContext.InstanceID.ToString(),
                ordinary_runtime_chunks = BuildRequiredChunkEvidence(),
                mass_wake_cluster_centre = _scenario == Test_NPCActivityScenario.MassWake
                    ? new[] { _massWakeClusterCentre.x, _massWakeClusterCentre.y, _massWakeClusterCentre.z }
                    : null,
                mass_wake_reached = _scenario == Test_NPCActivityScenario.MassWake && _massWakeReached,
                context_transition_source = _scenario == Test_NPCActivityScenario.CrossInstance && _contextTransitionRequested
                    ? FormatContext(_contextTransitionSource)
                    : null,
                context_transition_destination = _scenario == Test_NPCActivityScenario.CrossInstance && _contextTransitionObserved
                    ? FormatContext(_contextTransitionDestination)
                    : null,
            };
        }

        private void ValidatePlayerReferences()
        {
            foreach (var player in _players)
            {
                if (player == null || player.isDead || player.Owner == null)
                    throw new InvalidOperationException("A captured Stage 1.4 PlayerActor was destroyed, became dead, or lost its owner during measurement.");
                if (!ChunkSubscriptions.PLAYER_ACTORS.TryGetValue(player.Owner.Value, out var registeredActor)
                    || !ReferenceEquals(registeredActor, player))
                    throw new InvalidOperationException($"Captured Stage 1.4 PlayerActor '{player.ID}' is no longer the registered actor for its owner.");
                if (!player.TryGetAcceptedPositionSnapshot(out _))
                    throw new InvalidOperationException($"Captured Stage 1.4 PlayerActor '{player.ID}' lost its accepted position snapshot during measurement.");
                if (_scenario != Test_NPCActivityScenario.CrossInstance
                    && !player.SpatialContext.IsCompatibleWith(_initialContext))
                    throw new InvalidOperationException($"Captured Stage 1.4 PlayerActor '{player.ID}' changed spatial context unexpectedly.");
            }
        }

        private void ValidatePlacements(IReadOnlyList<Test_ActorFixturePlacement> placements, int expectedCount)
        {
            if (placements.Count != expectedCount)
                throw new InvalidOperationException($"Stage 1.4 placement count {placements.Count} does not equal requested NPC count {expectedCount}.");
            var engagedCount = placements.Count(placement => placement.group_id == "npc-engaged");
            if (engagedCount != 1)
                throw new InvalidOperationException($"Stage 1.4 requires exactly one engaged NPC placement; received {engagedCount}.");

            foreach (var placement in placements)
            {
                var distance = HorizontalDistance(_anchorInitialPosition, new WorldPosition(placement.world_x, placement.world_y, placement.world_z));
                if (placement.group_id == "npc-engaged")
                {
                    if (distance > 40d)
                        throw new InvalidOperationException("The engaged Stage 1.4 NPC placement is outside the full-entry radius.");
                    continue;
                }

                if (_scenario == Test_NPCActivityScenario.MostlyDormant)
                {
                    if (distance <= 96d)
                        throw new InvalidOperationException("MostlyDormant passive NPC placement must be beyond 96m from the anchor.");
                }
                else if (_scenario == Test_NPCActivityScenario.MassWake)
                {
                    if (HorizontalDistance(_massWakeClusterCentre, new WorldPosition(placement.world_x, placement.world_y, placement.world_z)) > 8d
                        || distance <= 96d)
                        throw new InvalidOperationException("MassWake passive NPC placement must be a compact <=8m cluster beyond 96m from the anchor.");
                }
                else if (distance <= _passiveAggroRange + 2d || distance >= 40d)
                {
                    throw new InvalidOperationException("Stage 1.4 passive NPC placement must be outside aggro plus 2m and inside the 40m full-entry radius.");
                }
            }
        }

        private static double HorizontalDistance(WorldPosition first, WorldPosition second)
        {
            var x = first.x - second.x;
            var z = first.z - second.z;
            return Math.Sqrt(x * x + z * z);
        }

        private string[] BuildRequiredChunkEvidence()
        {
            var chunks = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var placement in _placements)
            {
                var chunk = new WorldPosition(placement.world_x, placement.world_y, placement.world_z).Chunk;
                chunks.Add(
                    chunk.x.ToString(CultureInfo.InvariantCulture)
                    + ","
                    + chunk.y.ToString(CultureInfo.InvariantCulture));
            }

            return chunks.ToArray();
        }

        private static string FormatContext(ActorSpatialContext context)
        {
            return $"{context.WorldID:D}/{context.MapID}/{context.InstanceID:D}";
        }
    }

    /// <summary>Signals that a scenario ran but did not achieve its required real-player workload outcome.</summary>
    internal sealed class Test_NPCActivityWorkloadNotAchievedException : InvalidOperationException
    {
        public Test_NPCActivityWorkloadNotAchievedException(string message) : base(message) { }
    }
}
