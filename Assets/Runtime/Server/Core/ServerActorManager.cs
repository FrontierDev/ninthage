using Actor = Game.Shared.Runtime.Actors.Actor;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Runtime;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using PurrNet;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Server
{
    /// <summary>
    /// The server actor manager handles all server-side actors, whether they are NPCs or PCs.
    /// Player movement is client-authoritative (via NetworkTransform owner-auth).
    /// The server periodically validates player positions for cheat detection.
    /// </summary>
    public sealed class ServerActorManager : MonoBehaviour
    {
        private static ServerActorManager _instance;
        /// <summary>Gets the active server actor manager.</summary>
        public static ServerActorManager Instance => _instance;
        private static bool _initialized = false;
        /// <summary>Gets whether the actor manager is initialized.</summary>
        public static bool IsInitialized => _initialized;

        // ECS Management.
        private EntityManager _entityManager;
        private EntityQuery _actorComponentQuery;

        // Maps actor ID -> Actor MonoBehaviour so LateUpdate can push ECS WorldPositionD -> transform.
        private readonly Dictionary<Guid, Actor> _actorObjects = new();

        /// <summary>Finds the server actor associated with an actor identifier.</summary>
        /// <param name="actorID">The actor identifier to find.</param>
        /// <param name="actor">Receives the actor when found.</param>
        /// <returns><see langword="true"/> when an actor was found.</returns>
        public bool TryGetActor(Guid actorID, out Actor actor) => _actorObjects.TryGetValue(actorID, out actor);

        // Player tracking for validation.
        private readonly Dictionary<PlayerID, Actor> _playerActors = new();
        private readonly Dictionary<PlayerID, Vector3> _lastValidatedPositions = new();
        private readonly Dictionary<PlayerID, float> _validationTimers = new();
        private readonly Dictionary<PlayerID, int> _violationCounters = new();

        // NPC aggro tracking.
        private struct NPCAggroState
        {
            public Actor NPC;
            public NPCBehavior Behavior;
            public NavMeshAgent Agent;
            public Actor PendingTarget;
            public float AggroTimer;
        }
        private readonly Dictionary<Guid, NPCAggroState> _npcAggroStates = new();

        // Generous speed cap: moveSpeed(5) * sqrt(2) diagonal * 2 headroom for latency bursts.
        private const float MAX_ALLOWED_SPEED = 15f;
        // Require sustained violations before correcting to tolerate network jitter.
        private const int VIOLATION_THRESHOLD = 3;
        private const float VALIDATION_INTERVAL = 0.5f;

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("ServerActorManager is already initialized!");
                return;
            }
            _instance = this;

            ActorEndpoint.onServerActorSpawned += OnActorSpawned;
            ActorEndpoint.onServerActorOwnerChanged += OnActorOwnerChanged;
            ActorEndpoint.onServerActorInterestChanged += OnActorInterestChanged;

            var world = ECS.ServerECSManager.DefaultWorld;
            _entityManager = world.EntityManager;
            _actorComponentQuery = _entityManager.CreateEntityQuery(typeof(ECS.ActorComponent));

            _initialized = true;
        }

        private void OnDestroy()
        {
            ActorEndpoint.onServerActorSpawned -= OnActorSpawned;
            ActorEndpoint.onServerActorOwnerChanged -= OnActorOwnerChanged;
            ActorEndpoint.onServerActorInterestChanged -= OnActorInterestChanged;

            _actorObjects.Clear();
            _playerActors.Clear();
            _lastValidatedPositions.Clear();
            _validationTimers.Clear();
            _violationCounters.Clear();
            _npcAggroStates.Clear();
            _instance = null;
            _initialized = false;
        }

        private void Update()
        {
            if (!_initialized) return;
            UpdateNPCAggro();
            ValidatePlayerPositions();
            SyncPlayerECS();
        }

        private void LateUpdate()
        {
            if (!_initialized) return;

            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var component = _entityManager.GetComponentData<ECS.ActorComponent>(entity);
                if (component.Owner != default) continue;

                if (_actorObjects.TryGetValue(component.ActorID, out var actorObj) && actorObj != null)
                {
                    var agent = actorObj.GetComponent<NavMeshAgent>();
                    if (agent != null && agent.enabled)
                    {
                        // NavMeshAgent owns movement — read transform into ECS.
                        var pos = actorObj.transform.position;
                        component.WorldPositionD = new Unity.Mathematics.double3(pos.x, pos.y, pos.z);
                        _entityManager.SetComponentData(entity, component);
                    }
                    else
                    {
                        // ECS gravity owns movement — write ECS to transform.
                        actorObj.transform.position = new Vector3(
                            (float)component.WorldPositionD.x,
                            (float)component.WorldPositionD.y,
                            (float)component.WorldPositionD.z
                        );
                    }
                }
            }
            entities.Dispose();
        }

        private void OnActorSpawned(Actor actor)
        {
            var wp = actor.WorldPos;
            var entity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(entity, new ECS.ActorComponent
            {
                ActorID = actor.ID,
                ActorName = actor.Name,
                Owner = actor.Owner ?? default,
                CurrentScene = default,
                WorldPositionD = new Unity.Mathematics.double3(wp.x, wp.y, wp.z)
            });

            if (actor.Owner == null)
            {
                bool hasAgent = actor.GetComponent<NavMeshAgent>() != null;
                _entityManager.AddComponentData(entity, new ECS.MotionStateComponent
                {
                    VerticalVelocity = 0f,
                    IsGrounded = true,
                    TimeSinceLastGround = 0f,
                    UsesNavMeshAgent = hasAgent
                });
                _entityManager.AddComponentData(entity, new ECS.GroundDetectionComponent
                {
                    groundDetectionRadius = GameConfigurationManager.Config.GroundDetectionRadius,
                    lastGroundCheck = new Unity.Mathematics.float3(0, -1, 0)
                });
            }

            _actorObjects[actor.ID] = actor;

            if (actor.Owner != null)
            {
                var owner = actor.Owner.Value;
                _playerActors[owner] = actor;
                _lastValidatedPositions[owner] = actor.transform.position;
                _validationTimers[owner] = 0f;
            }

            // Add the spellcasting component on the actor.
            _entityManager.AddComponentData(entity, new ECS.SpellCastComponent());
            _entityManager.SetComponentEnabled<ECS.SpellCastComponent>(entity, false);

            // Add cooldown buffer for per-spell cooldown tracking.
            _entityManager.AddBuffer<ECS.CooldownElement>(entity);

            // Add aura buffer for active aura tracking.
            _entityManager.AddBuffer<ECS.AuraElement>(entity);

            // Add ticker buffer for generic time-based callbacks.
            _entityManager.AddBuffer<ECS.TickerElement>(entity);

            // Track NPCs with aggro behavior for proximity-based targeting.
            if (actor.Owner == null && actor.isDead == false)
            {
                var behavior = actor.GetComponent<NPCBehavior>();
                if (behavior != null)
                {
                    if (behavior.AggroRange > 0)
                    {
                        _npcAggroStates[actor.ID] = new NPCAggroState
                        {
                            NPC = actor,
                            Behavior = behavior,
                            Agent = actor.GetComponent<NavMeshAgent>(),
                            PendingTarget = null,
                            AggroTimer = 0f
                        };
                    }
                }
            }

            Debug.Log("Actor spawned on server: " + actor.Name + " (ID: " + actor.ID + ")");
        }

        private void OnActorOwnerChanged(Actor actor)
        {
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var component = _entityManager.GetComponentData<ECS.ActorComponent>(entity);
                if (component.ActorID == actor.ID)
                {
                    var previousOwner = component.Owner;
                    component.Owner = actor.Owner ?? default;
                    _entityManager.SetComponentData(entity, component);

                    if (actor.Owner == null && previousOwner != default)
                    {
                        _playerActors.Remove(previousOwner);
                        _lastValidatedPositions.Remove(previousOwner);
                        _validationTimers.Remove(previousOwner);
                        _violationCounters.Remove(previousOwner);
                    }

                    Debug.Log("Actor owner changed on server: " + actor.Name + " (ID: " + actor.ID + ")");
                    break;
                }
            }
            entities.Dispose();
        }

        private void OnActorInterestChanged(Actor actor, SceneID sceneID)
        {
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var component = _entityManager.GetComponentData<ECS.ActorComponent>(entity);
                if (component.ActorID == actor.ID)
                {
                    component.CurrentScene = sceneID;
                    _entityManager.SetComponentData(entity, component);

                    string sceneName = "unknown";
                    if (NetworkManager.main.sceneModule.TryGetSceneState(sceneID, out var state))
                        sceneName = state.scene.name;

                    Debug.Log("Actor interest changed on server: " + actor.Name + " (ID: " + actor.ID + ") to logical scene " + sceneName);
                    break;
                }
            }
            entities.Dispose();
        }

        /// <summary>
        /// Scans for nearby players and starts aggro timers on NPCs.
        /// Once the delay expires the NPC acquires a target and begins auto-attacking.
        /// </summary>
        [ToDo("NPC aggro should be checked from the player, not the NPC, to avoid iterating over all NPCs every frame. This is a temporary implementation.")]
        private void UpdateNPCAggro()
        {
            float dt = Time.deltaTime;
            var keys = new List<Guid>(_npcAggroStates.Keys);

            foreach (var id in keys)
            {
                var state = _npcAggroStates[id];
                if (state.NPC == null || state.NPC.isDead) continue;

                // Already locked on a target — nothing to do.
                if (state.NPC.Target != null) continue;

                // Find the nearest player within aggro range via physics query.
                float aggroRange = state.Behavior.AggroRange;
                Vector3 npcPos = state.NPC.transform.position;

                Actor closest = null;
                float closestDistSqr = float.MaxValue;

                var colliders = Physics.OverlapSphere(npcPos, aggroRange);
                foreach (var col in colliders)
                {
                    var player = col.GetComponentInParent<PlayerActor>();
                    if (player == null || player.isDead) continue;

                    float distSqr = (player.transform.position - npcPos).sqrMagnitude;
                    if (distSqr < closestDistSqr)
                    {
                        closest = player;
                        closestDistSqr = distSqr;
                    }
                }

                if (closest == null)
                {
                    // No player in range — reset any pending aggro.
                    if (state.PendingTarget != null)
                    {
                        state.PendingTarget = null;
                        state.AggroTimer = 0f;
                        _npcAggroStates[id] = state;
                    }
                    continue;
                }

                // Player detected — start or continue aggro timer.
                if (state.PendingTarget != closest)
                {
                    state.PendingTarget = closest;
                    state.AggroTimer = 0f;
                }

                state.AggroTimer += dt;

                if (state.AggroTimer >= state.Behavior.AggroDelay)
                {
                    Debug.Log($"NPC '{state.NPC.Name}' aggroed player '{closest.Name}'.");
                    state.NPC.SetTargetFromServer(closest);
                    ActorEndpoint.onServerStartAutoAttack?.Invoke(state.NPC);
                    state.PendingTarget = null;
                    state.AggroTimer = 0f;
                }

                _npcAggroStates[id] = state;
            }
        }

        /// <summary>
        /// Periodically checks that player positions are legal.
        /// NetworkTransform (owner-auth) pushes the client position to
        /// the server actor's transform automatically; we just read it.
        /// </summary>
        private void ValidatePlayerPositions()
        {
            foreach (var kvp in _playerActors)
            {
                var playerID = kvp.Key;
                var actor = kvp.Value;
                if (actor == null) continue;

                if (!_validationTimers.ContainsKey(playerID)) continue;
                _validationTimers[playerID] += Time.deltaTime;
                if (_validationTimers[playerID] < VALIDATION_INTERVAL) continue;

                float elapsed = _validationTimers[playerID];
                _validationTimers[playerID] = 0f;

                var currentPos = actor.transform.position;
                var lastPos = _lastValidatedPositions[playerID];

                // Speed check: horizontal distance / elapsed time.
                var horizontalDelta = new Vector3(currentPos.x - lastPos.x, 0f, currentPos.z - lastPos.z);
                float speed = horizontalDelta.magnitude / elapsed;

                // Always advance the last-known position to prevent cascading
                // false positives when network updates arrive in bursts.
                _lastValidatedPositions[playerID] = currentPos;

                if (speed > MAX_ALLOWED_SPEED)
                {
                    _violationCounters[playerID] = _violationCounters.GetValueOrDefault(playerID) + 1;

                    if (_violationCounters[playerID] >= VIOLATION_THRESHOLD)
                    {
                        Debug.Warning("Player " + playerID + " exceeded max speed (" + speed.ToString("F1") + " m/s) " + _violationCounters[playerID] + " times. Correcting.");
                        var worldPos = new WorldPosition(lastPos.x, lastPos.y, lastPos.z);
                        var (chunk, localPos) = ServerPositionManager.Instance.ToChunkLocal(worldPos);
                        MovementEndpoint.Client_ServerCorrection(playerID, chunk, localPos);

                        // Reset after correction — give the snap time to take effect.
                        _lastValidatedPositions[playerID] = lastPos;
                        _violationCounters[playerID] = 0;
                    }
                }
                else
                {
                    _violationCounters[playerID] = 0;
                }
            }
        }

        /// <summary>
        /// Pushes the client-authoritative transform position (received via
        /// NetworkTransform) into ECS so game systems have up-to-date data.
        /// </summary>
        private void SyncPlayerECS()
        {
            var entities = _actorComponentQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var component = _entityManager.GetComponentData<ECS.ActorComponent>(entity);
                if (component.Owner == default) continue;

                if (_playerActors.TryGetValue(component.Owner, out var actor) && actor != null)
                {
                    var pos = actor.transform.position;
                    component.WorldPositionD = new Unity.Mathematics.double3(pos.x, pos.y, pos.z);
                    _entityManager.SetComponentData(entity, component);
                }
            }
            entities.Dispose();
        }
    }
}
