using Game.Server.Core;
using Game.Server.Systems;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using PurrNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Tests.Performance.Fixtures.Actors
{
    /// <summary>
    /// Owns one test population and its teardown ledger. Data-only entities never enter
    /// the networked actor lifecycle; full actors are submitted through ServerSpawnManager.
    /// </summary>
    public sealed class Test_ActorFixtureRuntime : IDisposable
    {
        private readonly Test_ActorFixtureProfile _profile;
        private readonly List<Entity> _dataOnlyEntities = new();
        private readonly List<Test_ActorFixturePlacement> _dataOnlyPlacements = new();
        private readonly List<Actor> _fullActors = new();
        private readonly Queue<Test_ActorFixturePlacement> _pendingFullPlacements = new();
        private readonly Dictionary<Actor, Test_ActorFixturePlacement> _fullActorPlacements = new();
        private readonly HashSet<Guid> _fullActorIDs = new();
        private int _baselineActorEntityCount;
        private bool _subscribed;
        private bool _disposed;
        private Actor _realPlayerTarget;

        /// <summary>Creates a population controller for one catalog profile.</summary>
        public Test_ActorFixtureRuntime(Test_ActorFixtureProfile profile)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            Test_ActorFixtureLayout.ValidatePopulation(profile);
        }

        /// <summary>Gets the profile being executed.</summary>
        public Test_ActorFixtureProfile Profile => _profile;

        /// <summary>Gets the number of data-only ECS entities created by this fixture.</summary>
        public int DataOnlyEntityCount => _dataOnlyEntities.Count;

        /// <summary>Gets the tracked data-only ECS entities in deterministic placement order.</summary>
        public IReadOnlyList<Entity> DataOnlyEntities => _dataOnlyEntities;

        /// <summary>Gets the placements corresponding to <see cref="DataOnlyEntities"/>.</summary>
        public IReadOnlyList<Test_ActorFixturePlacement> DataOnlyPlacements => _dataOnlyPlacements;

        /// <summary>Gets the number of full actors observed through the server spawn event.</summary>
        public int SpawnedFullActorCount => _fullActors.Count;

        /// <summary>Gets the number of despawn events observed for this fixture.</summary>
        public int DespawnedFullActorCount { get; private set; }

        /// <summary>Gets the actors whose server lifecycle completed.</summary>
        public IReadOnlyList<Actor> FullActors => _fullActors;

        /// <summary>Gets the most recent cleanup report produced by this fixture.</summary>
        public Test_ActorFixtureCleanupReport LastCleanupReport { get; private set; }

        /// <summary>Creates data-only ECS state with no network identity or player owner.</summary>
        public void StartDataOnly()
        {
            StartDataOnly(_profile.population.accounting.ecs_data_only_npcs);
            if (_dataOnlyEntities.Count != _profile.population.accounting.ecs_data_only_npcs)
                throw new InvalidOperationException(
                    $"Data-only fixture created {_dataOnlyEntities.Count} ECS entities; expected {_profile.population.accounting.ecs_data_only_npcs}.");
        }

        /// <summary>Prepares and creates an exact initial data-only population for an incremental ramp.</summary>
        public void StartDataOnly(int initialCount)
        {
            ThrowIfDisposed();
            Test_ActorFixturePreflight.ValidateDataOnly(_profile);
            RequireRepresentation(Test_ActorFixtureRepresentation.ecs_data_only);
            CaptureBaselineEntityCount();

            var maximumCount = _profile.population.accounting.ecs_data_only_npcs;
            if (initialCount < 0 || initialCount > maximumCount)
                throw new ArgumentOutOfRangeException(nameof(initialCount));

            SpawnDataOnly(initialCount);
        }

        /// <summary>Spawns additional data-only entities from the canonical placement list.</summary>
        public void SpawnDataOnly(int count)
        {
            ThrowIfDisposed();
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            var placements = Test_ActorFixtureLayout.BuildPlacements(_profile);
            if (_dataOnlyPlacements.Count + count > placements.Count)
                throw new InvalidOperationException(
                    $"Data-only fixture cannot spawn {_dataOnlyPlacements.Count + count} entities; expected at most {placements.Count}.");

            for (var index = 0; index < count; index++)
            {
                var placement = placements[_dataOnlyPlacements.Count];
                _dataOnlyPlacements.Add(placement);
                _dataOnlyEntities.Add(CreateDataOnlyEntity(placement));
            }
        }

        /// <summary>Despawns the newest tracked data-only entities through the ECS world.</summary>
        public void DespawnDataOnly(int count)
        {
            ThrowIfDisposed();
            if (count < 0 || count > _dataOnlyEntities.Count)
                throw new ArgumentOutOfRangeException(nameof(count));

            var entityManager = ServerECSManager.DefaultWorld.EntityManager;
            for (var index = 0; index < count; index++)
            {
                var lastIndex = _dataOnlyEntities.Count - 1;
                var entity = _dataOnlyEntities[lastIndex];
                if (entityManager.Exists(entity))
                    entityManager.DestroyEntity(entity);
                _dataOnlyEntities.RemoveAt(lastIndex);
                _dataOnlyPlacements.RemoveAt(lastIndex);
            }

            if (_dataOnlyEntities.Count != _dataOnlyPlacements.Count)
                throw new InvalidOperationException("Data-only fixture entity and placement ledgers diverged.");
        }

        /// <summary>
        /// Submits all full NPC placements to the project's authoritative spawn queue.
        /// A real player actor is supplied separately for target-based activity; it is never synthesized here.
        /// </summary>
        public void StartFullActors(SceneID sceneID, Actor realPlayerTarget)
        {
            StartFullActors(sceneID, realPlayerTarget, Test_ActorFixtureLayout.BuildPlacements(_profile));
        }

        /// <summary>Spawns an exact caller-built full-actor placement list through the authoritative spawn queue.</summary>
        public void StartFullActors(
            SceneID sceneID,
            Actor realPlayerTarget,
            IReadOnlyList<Test_ActorFixturePlacement> placements)
        {
            ThrowIfDisposed();
            if (placements == null)
                throw new ArgumentNullException(nameof(placements));
            if (placements.Count != _profile.population.npcs.requested)
                throw new InvalidOperationException(
                    $"Full actor fixture received {placements.Count} placements; expected {_profile.population.npcs.requested}.");
            var connectedRealPlayers = NetworkManager.main == null ? -1 : NetworkManager.main.playerCount;
            var prefab = Test_ActorFixturePreflight.ValidateFull(
                _profile,
                connectedRealPlayers,
                sceneID);
            RequireRepresentation(Test_ActorFixtureRepresentation.networked_gameobject_ecs);
            Require(realPlayerTarget != null && realPlayerTarget.IsPlayer && realPlayerTarget.Owner != null,
                "A real owned PlayerActor is required for the full actor fixture; synthetic targets are not supported.");

            _realPlayerTarget = realPlayerTarget;
            CaptureBaselineEntityCount();
            SubscribeToLifecycle();

            foreach (var placement in placements)
            {
                _pendingFullPlacements.Enqueue(placement);
                var worldPosition = new WorldPosition(
                    placement.world_x,
                    placement.world_y,
                    placement.world_z);
                var sourceChunk = new WorldChunkKey(
                    ServerWorldManager.Instance.CurrentRuntimeContext,
                    worldPosition.Chunk);

                ServerSpawnManager.Instance.SpawnAt(
                    sourceChunk,
                    worldPosition,
                    prefab,
                    sceneID,
                    default);
            }
        }

        /// <summary>Provides a coroutine-friendly wait that fails instead of downgrading on a timeout.</summary>
        public IEnumerator WaitForFullSpawn(float timeoutSeconds)
        {
            if (timeoutSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));

            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (_pendingFullPlacements.Count > 0)
            {
                if (Time.realtimeSinceStartup >= deadline)
                    throw new TimeoutException(
                        $"Timed out waiting for full actor spawn events: {SpawnedFullActorCount}/{_profile.population.npcs.requested} observed.");
                yield return null;
            }

            if (SpawnedFullActorCount != _profile.population.npcs.requested)
                throw new InvalidOperationException(
                    $"Full actor fixture observed {SpawnedFullActorCount} actors; expected {_profile.population.npcs.requested}.");

            foreach (var actor in _fullActors)
            {
                var npc = actor as NPCActor;
                Require(npc != null, $"Spawned Stage 1.4 actor '{actor?.name}' is not the ordinary NPCActor representation.");
                Require(actor.GetComponent<ActorMotor>() != null,
                    $"Spawned Stage 1.4 NPC '{actor.ID}' is missing the ordinary ActorMotor component.");
                var agent = actor.GetComponent<NavMeshAgent>();
                Require(agent != null,
                    $"Spawned Stage 1.4 NPC '{actor.ID}' is missing the ordinary NavMeshAgent movement component.");
                Require(actor.TryGetAcceptedPositionSnapshot(out var snapshot) && snapshot.Position.IsFinite,
                    $"Spawned Stage 1.4 NPC '{actor.ID}' has no finite accepted authoritative position.");
            }
        }

        /// <summary>Assigns a real player target and starts the legal server-side engaged schedule.</summary>
        public void StartEngagementForGroup(string groupID, int count)
        {
            ThrowIfDisposed();
            RequireRepresentation(Test_ActorFixtureRepresentation.networked_gameobject_ecs);
            Require(_realPlayerTarget != null, "A real player target must be supplied before engagement starts.");
            Require(count > 0, "Engagement count must be positive.");

            var started = 0;
            for (var index = 0; index < _fullActors.Count && started < count; index++)
            {
                var actor = _fullActors[index];
                if (!_fullActorPlacements.TryGetValue(actor, out var placement) || placement.group_id != groupID)
                    continue;

                var spellcaster = actor.GetComponent<ActorSpellcaster>();
                Require(spellcaster != null, $"Actor '{actor.name}' has no ActorSpellcaster component.");
                spellcaster.LearnSpell("cleave");
                actor.SetTargetFromServer(_realPlayerTarget);
                actor.Server_StartAutoAttack();
                started++;
            }

            if (started != count)
                throw new InvalidOperationException(
                    $"Could not start engagement for group '{groupID}': requested {count}, available {started}.");
        }

        /// <summary>Requests one supported server-only ability cast for each actor in a group.</summary>
        public void RequestAbilitySchedule(string groupID, string abilityDefinitionID)
        {
            ThrowIfDisposed();
            Require(_realPlayerTarget != null, "A real player target must be supplied before ability activity starts.");
            Require(abilityDefinitionID == "auto_attack" || abilityDefinitionID == "fireball" || abilityDefinitionID == "cleave",
                $"Ability '{abilityDefinitionID}' is not part of the actor fixture schedule.");

            foreach (var actor in _fullActors)
            {
                if (!_fullActorPlacements.TryGetValue(actor, out var placement) || placement.group_id != groupID)
                    continue;

                actor.SetTargetFromServer(_realPlayerTarget);
                var spellcaster = actor.GetComponent<ActorSpellcaster>();
                Require(spellcaster != null, $"Actor '{actor.name}' has no ActorSpellcaster component.");
                spellcaster.LearnSpell(abilityDefinitionID);
                if (abilityDefinitionID != "auto_attack")
                    spellcaster.ServerOnly_RequestSpellCast(abilityDefinitionID);
            }
        }

        /// <summary>
        /// Despawns full actors through PurrNet and removes only this fixture's tracked ECS state.
        /// </summary>
        public Test_ActorFixtureCleanupReport Cleanup()
        {
            if (_disposed)
            {
                LastCleanupReport = BuildCleanupReport();
                return LastCleanupReport;
            }

            foreach (var actor in _fullActors.ToArray())
            {
                if (actor == null)
                    continue;

                var identity = actor.GetComponent<NetworkIdentity>();
                if (identity == null)
                    throw new InvalidOperationException($"Full fixture actor '{actor.name}' lost its NetworkIdentity before teardown.");
                identity.Despawn();
            }

            DestroyTrackedDataOnlyEntities();
            DestroyTrackedFullActorEntities();
            if (_fullActors.Count == 0)
                UnsubscribeFromLifecycle();
            _disposed = true;
            LastCleanupReport = BuildCleanupReport();
            return LastCleanupReport;
        }

        /// <summary>Waits for deferred network destruction before asserting teardown state.</summary>
        public IEnumerator WaitForCleanup(float timeoutSeconds)
        {
            if (!_disposed)
                throw new InvalidOperationException("Cleanup must be called before waiting for actor fixture teardown.");
            if (timeoutSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));

            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (_fullActors.Count > 0)
            {
                if (Time.realtimeSinceStartup >= deadline)
                    throw new TimeoutException(
                        $"Timed out waiting for full actor despawn events: {DespawnedFullActorCount}/{_profile.population.npcs.requested} observed.");
                yield return null;
            }

            UnsubscribeFromLifecycle();
            AssertCleanupComplete();
        }

        /// <summary>Disposes fixture state and performs the same explicit cleanup as the test teardown.</summary>
        public void Dispose()
        {
            Cleanup();
        }

        /// <summary>Throws if tracked actor and ECS state did not return to the captured baseline.</summary>
        public void AssertCleanupComplete()
        {
            var report = BuildCleanupReport();
            LastCleanupReport = report;
            if (report.remainingTrackedActors != 0
                || report.remainingFixtureEntities != 0
                || report.currentActorEntities != report.baselineActorEntities
                || report.despawnEvents != report.expectedFullActors)
            {
                throw new InvalidOperationException(
                    $"Actor fixture cleanup incomplete: trackedActors={report.remainingTrackedActors}, fixtureEntities={report.remainingFixtureEntities}, actorEntities={report.currentActorEntities}/{report.baselineActorEntities}, despawnEvents={report.despawnEvents}/{report.expectedFullActors}.");
            }
        }

        private Entity CreateDataOnlyEntity(Test_ActorFixturePlacement placement)
        {
            var entityManager = ServerECSManager.DefaultWorld.EntityManager;
            var entity = entityManager.CreateEntity();
            var position = new double3(placement.world_x, placement.world_y, placement.world_z);
            entityManager.AddComponentData(entity, new ActorComponent
            {
                ActorID = StableActorID(placement.actor_fixture_id),
                ActorName = new FixedString128Bytes(placement.actor_fixture_id),
                Owner = default,
                CurrentScene = default,
                WorldPositionD = position,
            });
            entityManager.AddComponentData(entity, new MotionStateComponent
            {
                VerticalVelocity = 0f,
                IsGrounded = true,
                TimeSinceLastGround = 0f,
            });
            entityManager.AddComponentData(entity, new GroundDetectionComponent
            {
                groundDetectionRadius = 0.5f,
                lastGroundCheck = new float3(0f, -1f, 0f),
            });
            entityManager.AddComponentData(entity, new SpellCastComponent());
            entityManager.SetComponentEnabled<SpellCastComponent>(entity, false);
            entityManager.AddBuffer<CooldownElement>(entity);
            entityManager.SetComponentEnabled<CooldownElement>(entity, false);
            entityManager.AddBuffer<AuraElement>(entity);
            entityManager.SetComponentEnabled<AuraElement>(entity, false);
            entityManager.AddBuffer<TickerElement>(entity);
            entityManager.SetComponentEnabled<TickerElement>(entity, false);
            return entity;
        }

        private void OnServerActorSpawned(Actor actor)
        {
            if (actor == null || _pendingFullPlacements.Count == 0)
                return;

            var npc = actor.GetComponent<NPCActor>();
            if (npc == null || npc.NPCID != "6e50fb82-1724-4040-afec-88fabc925ae0")
                return;

            var placement = _pendingFullPlacements.Dequeue();
            npc.level.value = placement.level;
            var spellcaster = actor.GetComponent<ActorSpellcaster>();
            Require(spellcaster != null, $"Spawned actor '{actor.name}' has no ActorSpellcaster component.");
            if (placement.group_id == "npc-engaged")
                spellcaster.LearnSpell("cleave");

            _fullActors.Add(actor);
            _fullActorPlacements[actor] = placement;
            _fullActorIDs.Add(actor.ID);
        }

        private void OnServerActorDespawned(Actor actor)
        {
            if (actor == null || !_fullActorPlacements.Remove(actor))
                return;

            _fullActors.Remove(actor);
            DespawnedFullActorCount++;
        }

        private void SubscribeToLifecycle()
        {
            if (_subscribed)
                return;
            ActorEndpoint.onServerActorSpawned += OnServerActorSpawned;
            ActorEndpoint.onServerActorDespawned += OnServerActorDespawned;
            _subscribed = true;
        }

        private void UnsubscribeFromLifecycle()
        {
            if (!_subscribed)
                return;
            ActorEndpoint.onServerActorSpawned -= OnServerActorSpawned;
            ActorEndpoint.onServerActorDespawned -= OnServerActorDespawned;
            _subscribed = false;
        }

        private void CaptureBaselineEntityCount()
        {
            var query = ServerECSManager.DefaultWorld.EntityManager.CreateEntityQuery(typeof(ActorComponent));
            _baselineActorEntityCount = query.CalculateEntityCount();
            query.Dispose();
        }

        private void DestroyTrackedDataOnlyEntities()
        {
            var entityManager = ServerECSManager.DefaultWorld.EntityManager;
            foreach (var entity in _dataOnlyEntities)
            {
                if (entityManager.Exists(entity))
                    entityManager.DestroyEntity(entity);
            }
            _dataOnlyEntities.Clear();
            _dataOnlyPlacements.Clear();
        }

        private void DestroyTrackedFullActorEntities()
        {
            var entityManager = ServerECSManager.DefaultWorld.EntityManager;
            var query = entityManager.CreateEntityQuery(typeof(ActorComponent));
            var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var component = entityManager.GetComponentData<ActorComponent>(entity);
                if (_fullActorIDs.Contains(component.ActorID))
                    entityManager.DestroyEntity(entity);
            }
            entities.Dispose();
            query.Dispose();
        }

        private Test_ActorFixtureCleanupReport BuildCleanupReport()
        {
            var remainingFixtureEntities = 0;
            var currentActorEntities = _baselineActorEntityCount;
            if (ServerECSManager.DefaultWorld != null && ServerECSManager.DefaultWorld.IsCreated)
            {
                var entityManager = ServerECSManager.DefaultWorld.EntityManager;
                var query = entityManager.CreateEntityQuery(typeof(ActorComponent));
                currentActorEntities = query.CalculateEntityCount();
                var entities = query.ToEntityArray(Allocator.Temp);
                foreach (var entity in entities)
                {
                    if (_fullActorIDs.Contains(entityManager.GetComponentData<ActorComponent>(entity).ActorID))
                        remainingFixtureEntities++;
                }
                entities.Dispose();
                query.Dispose();
            }

            return new Test_ActorFixtureCleanupReport
            {
                expectedFullActors = _profile.population.accounting.full_gameobject_purrnet_npcs,
                despawnEvents = DespawnedFullActorCount,
                remainingTrackedActors = _fullActors.Count,
                remainingFixtureEntities = remainingFixtureEntities,
                currentActorEntities = currentActorEntities,
                baselineActorEntities = _baselineActorEntityCount,
            };
        }

        private static Guid StableActorID(string value)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes("ninthage-actor-fixture\n" + value));
                var guidBytes = new byte[16];
                Array.Copy(bytes, guidBytes, guidBytes.Length);
                return new Guid(guidBytes);
            }
        }

        private void RequireRepresentation(Test_ActorFixtureRepresentation representation)
        {
            if (_profile.representation != representation.ToString())
                throw new InvalidOperationException(
                    $"Profile '{_profile.variant_id}' cannot execute as '{representation}'; its manifest representation is '{_profile.representation}'.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new Test_ActorFixturePreconditionException(message);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Test_ActorFixtureRuntime));
        }
    }

    /// <summary>Reports actor and ECS state observed during fixture teardown.</summary>
    [Serializable]
    public sealed class Test_ActorFixtureCleanupReport
    {
        public int expectedFullActors;
        public int despawnEvents;
        public int remainingTrackedActors;
        public int remainingFixtureEntities;
        public int currentActorEntities;
        public int baselineActorEntities;
    }
}
