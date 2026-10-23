using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Networking;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Performance;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using PurrNet;
using PurrNet.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace Game.Server.Core
{
    /// <summary>Coordinates authoritative actor spawning and scene placement.</summary>
    public sealed class ServerSpawnManager : MonoBehaviour
    {
        private static readonly PerformanceMetricDefinition UPDATE_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_SPAWN_UPDATE_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "spawn_manager",
            "server_update");
        private static readonly PerformanceMetricDefinition SPAWN_COMMAND_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_SPAWN_COMMAND_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "spawn_manager",
            "server_update");
        private static readonly PerformanceMetricDefinition SPAWN_SUCCESS_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_SPAWN_SUCCESS_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "spawn_manager",
            "lifetime");
        private static readonly PerformanceMetricDefinition SPAWN_FAILURE_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_SPAWN_FAILURE_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "spawn_manager",
            "lifetime");
        private static readonly PerformanceMetricDefinition SCENE_LOAD_DURATION_METRIC = new(
            PerformanceMetricIds.SERVER_SPAWN_SCENE_LOAD_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "spawn_manager",
            "scene_operation");
        private static readonly PerformanceMetricDefinition DESPAWN_EVENT_COUNT_METRIC = new(
            PerformanceMetricIds.SERVER_ACTOR_DESPAWN_EVENT_COUNT,
            PerformanceMetricUnit.Count,
            PerformanceMetricSide.Server,
            "spawn_manager",
            "lifetime");
        private static readonly PerformanceMarker UPDATE_MARKER = new(UPDATE_DURATION_METRIC, "Server.SpawnManager.Update");
        private static readonly PerformanceMarker SCENE_LOAD_MARKER = new(SCENE_LOAD_DURATION_METRIC, "Server.SpawnManager.OnPostSceneLoaded");
        private static ServerSpawnManager _instance;
        /// <summary>Gets the active server spawn manager.</summary>
        public static ServerSpawnManager Instance => _instance;
        private static bool _initialized;
        /// <summary>Gets whether the spawn manager is initialized.</summary>
        public static bool IsInitialized => _initialized;

        internal static void RegisterPerformanceMetrics()
        {
            PerformanceTelemetry.RegisterMetric(UPDATE_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(SPAWN_COMMAND_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(SPAWN_SUCCESS_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(SPAWN_FAILURE_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(SCENE_LOAD_DURATION_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
            PerformanceTelemetry.RegisterMetric(DESPAWN_EVENT_COUNT_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
        }

        private NetworkManager _networkManager;

        // Managed prefab list indexed by SpawnPointComponent.PrefabIndex
        private readonly List<GameObject> _prefabRegistry = new();
        // SceneIndex → Scene for PurrNet Instantiate
        private readonly Dictionary<int, Scene> _sceneRegistry = new();
        private int _nextSceneIndex;

        private Dictionary<Guid, Actor> _spawnPointActorMap = new();

        private EntityManager _entityManager;
        private EntityQuery _spawnCommandQuery;

        /// <summary>Initializes server spawn manager state and subscriptions.</summary>
        public void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("ServerSpawnManager is already initialized!");
                return;
            }

            _instance = this;

            _networkManager = NetworkManager.main;
            _networkManager.sceneModule.onPostSceneLoaded += OnPostSceneLoaded;

            Game.Shared.Networking.ActorEndpoint.onServerActorDespawned += OnServerActorDespawned;

            var world = ServerECSManager.DefaultWorld;
            _entityManager = world.EntityManager;
            _spawnCommandQuery = _entityManager.CreateEntityQuery(typeof(Systems.SpawnCommand));

            _initialized = true;
        }

        private void OnDestroy()
        {
            if (_networkManager != null && _networkManager.sceneModule != null)
                _networkManager.sceneModule.onPostSceneLoaded -= OnPostSceneLoaded;

            Game.Shared.Networking.ActorEndpoint.onServerActorDespawned -= OnServerActorDespawned;

            _instance = null;
            _initialized = false;
        }

        private void OnPostSceneLoaded(SceneID sceneID, bool asServer)
        {
            if (!asServer) return;
            if (!_networkManager.sceneModule.TryGetSceneState(sceneID, out var state)) return;
            using var markerScope = SCENE_LOAD_MARKER.Begin();

            var globalSceneID = ServerWorldManager.Instance.GlobalActorsSceneID;
            if (!_networkManager.sceneModule.TryGetSceneState(globalSceneID, out var globalState))
                return;
            int sceneIndex = RegisterScene(globalState.scene);

            // Find spawn points ONLY in the specific scene that was loaded
            ActorSpawnPoint[] actorSpawnPoints = state.scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<ActorSpawnPoint>())
                .ToArray();

            Debug.Log($"Found {actorSpawnPoints.Length} spawn points in '{state.scene.name}'.");
            if (actorSpawnPoints.Length == 0)
                return;

            WorldChunkKey sourceChunk = ResolveLoadedChunkKey(sceneID);
            if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "Server spawn-point registration requires an initialized server physics manager.");
            }
            if (!ServerPhysicsManager.Instance.TryGetEnvironmentRegionDescriptor(sourceChunk, out var region))
            {
                throw new InvalidOperationException(
                    $"Loaded source chunk {sourceChunk.ChunkCoord} has no active physical region binding.");
            }

            foreach (ActorSpawnPoint spawnPoint in actorSpawnPoints)
            {
                Guid spawnPointGUID = spawnPoint.GUID;
                if (_spawnPointActorMap.ContainsKey(spawnPointGUID))
                {
                    Debug.Warning($"Spawn point '{spawnPoint.name}' with GUID {spawnPointGUID} already has an actor spawned. Skipping duplicate.");
                    continue;
                }

                int prefabIndex = RegisterPrefab(spawnPoint.Actor.gameObject);
                WorldPosition spawnWorldPosition = ServerPhysicsManager.Instance.ToWorld(
                    region,
                    spawnPoint.transform.position);

                var entity = _entityManager.CreateEntity();
                _entityManager.AddComponentData(entity, new Systems.SpawnPointComponent
                {
                    SpawnPointGUID = spawnPointGUID,
                    SceneName = state.scene.name,
                    WorldPositionD = new double3(
                        spawnWorldPosition.x,
                        spawnWorldPosition.y,
                        spawnWorldPosition.z),
                    ChunkCoord = new int2(sourceChunk.ChunkCoord.x, sourceChunk.ChunkCoord.y),
                    PrefabIndex = prefabIndex,
                    SceneIndex = sceneIndex,
                    Pending = true
                });

                // Tell the spawn system that this spawn point has an actor associated with it.
                _spawnPointActorMap[spawnPointGUID] = spawnPoint.Actor;
            }
        }

        private WorldChunkKey ResolveLoadedChunkKey(SceneID sceneID)
        {
            if (!ServerWorldManager.IsInitialized || ServerWorldManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "Resolving a loaded source chunk requires an initialized server world manager.");
            }

            WorldChunkKey resolvedChunk = default;
            bool found = false;
            foreach (var loadedChunk in ServerWorldManager.Instance.LoadedChunks)
            {
                if (loadedChunk.Value != sceneID)
                    continue;
                if (found)
                {
                    throw new InvalidOperationException(
                        $"SceneID {sceneID} is mapped to multiple loaded world chunks.");
                }

                resolvedChunk = loadedChunk.Key;
                found = true;
            }

            if (!found)
            {
                throw new InvalidOperationException(
                    $"SceneID {sceneID} is not mapped to a loaded world chunk.");
            }

            return resolvedChunk;
        }

        private void OnServerActorDespawned(Actor actor)
        {
            // If this actor was spawned from a spawn point, mark that spawn point as pending again so it can respawn.
            if (actor.SpawnPointGUID != default && _spawnPointActorMap.ContainsKey(actor.SpawnPointGUID))
                _spawnPointActorMap.Remove(actor.SpawnPointGUID);
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(DESPAWN_EVENT_COUNT_METRIC, 1);
        }

        /// <summary>Starts spawning the actor associated with a connected player.</summary>
        /// <param name="playerID">The player to spawn.</param>
        public void SpawnPlayer(PlayerID playerID)
        {
            StartCoroutine(SpawnPlayerCoroutine(playerID));
        }

        private IEnumerator SpawnPlayerCoroutine(PlayerID playerID)
        {
            var spawnScene = new Vector2Int(0, 0);

            // Actively request the spawn chunk to load instead of passively waiting.
            bool chunkLoadCompleted = false;
            bool chunkLoadSucceeded = false;
            try
            {
                ServerWorldManager.Instance.EnsureChunkLoaded(
                    ServerWorldManager.Instance.CurrentRuntimeContext,
                    spawnScene,
                    (succeeded, _) =>
                    {
                        chunkLoadSucceeded = succeeded;
                        chunkLoadCompleted = true;
                    });
            }
            catch (Exception exception)
            {
                Debug.Error($"Failed to start loading the player spawn chunk: {exception.Message}");
                yield break;
            }

            yield return new WaitUntil(() => chunkLoadCompleted);
            if (!chunkLoadSucceeded)
            {
                Debug.Error($"Failed to load the player spawn chunk for player {playerID}.");
                yield break;
            }

            // Load the player prefab from Addressables
            var handle = Addressables.LoadAssetAsync<GameObject>("PlayerPrefab"); // your addressable key
            yield return handle;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.Error("Failed to load player prefab addressable.");
                yield break;
            }

            var runtimeContext = ServerWorldManager.Instance.CurrentRuntimeContext;
            var sourceChunk = new WorldChunkKey(runtimeContext, spawnScene);
            var spawnWorldPosition = WorldPosition.FromChunkLocal(
                spawnScene,
                new Vector3(0f, 5f, 0f));
            GameObject prefab = handle.Result;

            // Spawn the actor into the persistent global_actors scene, not the chunk scene.
            var globalSceneID = ServerWorldManager.Instance.GlobalActorsSceneID;
            SpawnAt(
                sourceChunk,
                spawnWorldPosition,
                prefab,
                globalSceneID,
                playerID);

            // Wait for the actor to be spawned and linked to the session (happens on next Update tick).
            var session = ServerConnectionManager.Instance.GetSessionData(playerID);
            if (session == null) yield break;
            yield return new WaitUntil(() => session.PlayerActor != null);

            // Look up the character data from the account and assign it to the PlayerActor.
            var accountData = ServerConnectionManager.Instance.GetAccountData(playerID);
            var characterData = accountData?.Characters.Find(c => c.GUID == session.CharacterID);
            if (characterData == null)
            {
                Debug.Error($"Character data not found for player {playerID}, character GUID {session.CharacterID}.");
                yield break;
            }

            var playerActor = session.PlayerActor as Game.Shared.Runtime.Actors.PlayerActor;
            if (playerActor == null)
            {
                Debug.Error($"PlayerPrefab is missing a PlayerActor component. " +
                            $"Ensure the prefab has PlayerActor (not just Actor).");
                yield break;
            }

            // Check that the player has all known spells for their class.
            var classDef = Shared.Data.ClassDefinitionLibrary.Instance.GetDefinition(characterData.ClassID);
            if (classDef == null)
            {
                Debug.Error($"Class definition '{characterData.ClassID}' not found for player {playerID}.");
                yield break;
            }
            else
            {
                foreach (var classSpell in classDef.ClassSpellList)
                {
                    if (!characterData.KnownSpellIDs.Contains(classSpell.spell.DefinitionID))
                    {
                        Debug.Log($"Adding missing spell '{classSpell.spell.DisplayName}' to player {playerID} character data.");
                        characterData.KnownSpellIDs.Add(classSpell.spell.DefinitionID);
                    }
                }
            }

            characterData.PVPFactionID = RaceDefinitionLibrary.Instance.GetDefinition(characterData.RaceID)?.DefaultPVPFaction?.DefinitionID;

            for (int i = characterData.SavedInventory.Count - 1; i >= 0; i--)
            {
                var entry = characterData.SavedInventory[i];
                if (Shared.Data.ItemDefinitionLibrary.Instance.GetDefinition(entry.ItemID) == null)
                {
                    Debug.Warning($"Removing invalid inventory entry at slot {entry.SlotIndex} with unknown item '{entry.ItemID}' for player {playerID}.");
                    characterData.SavedInventory.RemoveAt(i);
                }
                else if (string.IsNullOrEmpty(entry.ItemGUID))
                {
                    entry.ItemGUID = System.Guid.NewGuid().ToString("N");
                    characterData.SavedInventory[i] = entry;
                    Debug.Log($"Assigned GUID to inventory entry at slot {entry.SlotIndex} for player {playerID}.");
                }
            }

            for (int i = characterData.SavedEquipment.Count - 1; i >= 0; i--)
            {
                var entry = characterData.SavedEquipment[i];
                var def = Shared.Data.ItemDefinitionLibrary.Instance.GetDefinition(entry.ItemID);
                if (def == null || (def.ItemType != Shared.Data.ItemType.Weapon && def.ItemType != Shared.Data.ItemType.Armor))
                {
                    Debug.Warning($"Removing invalid equipment entry at slot {entry.SlotIndex} with unknown item '{entry.ItemID}' for player {playerID}.");
                    characterData.SavedEquipment.RemoveAt(i);
                }
                else if (string.IsNullOrEmpty(entry.ItemGUID))
                {
                    entry.ItemGUID = System.Guid.NewGuid().ToString("N");
                    characterData.SavedEquipment[i] = entry;
                    Debug.Log($"Assigned GUID to equipment entry at slot {entry.SlotIndex} for player {playerID}.");
                }
            }

            // Ensure player has all required weapon skills for their class
            foreach (var weaponType in classDef.WeaponTypes)
            {
                if (!characterData.SavedWeaponSkills.Any(ws => ws.WeaponType == (int)weaponType))
                {
                    characterData.SavedWeaponSkills.Add(new Shared.Persistence.SavedWeaponSkill
                    {
                        WeaponType = (int)weaponType,
                        Level = 1,
                        Experience = 0
                    });
                    Debug.Log($"Added weapon skill for {weaponType} to player {playerID}.");
                }
            }

            // Remove weapon skills no longer valid for this class
            characterData.SavedWeaponSkills.RemoveAll(ws =>
            {
                var weaponType = (Shared.Data.ItemWeaponType)ws.WeaponType;
                bool isInvalid = !classDef.WeaponTypes.Contains(weaponType);
                if (isInvalid)
                    Debug.Warning($"Removing invalid weapon skill {weaponType} from player {playerID}.");
                return isInvalid;
            });

            foreach (var startingRep in RaceDefinitionLibrary.Instance.GetDefinition(characterData.RaceID).StartingReputations)
            {
                if (characterData.SavedReputations.Any(rep => rep.FactionID == startingRep.Faction.DefinitionID))
                    continue;

                characterData.SavedReputations.Add(new Shared.Persistence.SavedReputationEntry
                {
                    FactionID = startingRep.Faction.DefinitionID,
                    Reputation = startingRep.Reputation
                });
                Debug.Log($"Set initial reputation with faction '{startingRep.Faction.DisplayName}' to {startingRep.Reputation} for player {playerID}.");
            }

            try
            {
                playerActor.SetCharacterData(characterData);
            }
            catch (Exception e)
            {
                Debug.Error($"Error setting character data for player {playerID}: {e}");
            }


            var json = JsonUtility.ToJson(characterData);
            Debug.Log($"Character JSON length: {json.Length}");

            ServerStatManager.Instance.InitializePlayerStats(playerActor);
            ServerStatManager.Instance.RegisterActor(playerActor);

            // Stat initialization sets all stats to 0 before filling to max,
            // which transiently triggers the death check. Reset the flag.
            playerActor.isDead = false;

            // Trigger initial 3x3 chunk interest around the spawn location.
            Game.Shared.Networking.MovementEndpoint.onInterestRequest?.Invoke(playerID, spawnScene);

            // Send the finalized character data to the client now, after all server-side
            // modifications are complete. This must happen before AddPlayerToScene so
            // the client has _activeCharacter set before actor ownership is replicated.
            Game.Shared.Networking.AccountEndpoint.Client_EnterWorld(playerID, characterData, $"World_{spawnScene.x}_{spawnScene.y}");

            // Add the player to global_actors AFTER ChunkSubscriptions is populated.
            // This avoids a race where PurrNet's Spawn() and OnPlayerLoadedScene both
            // send spawn packets before the visibility rule has data to work with.
            _networkManager.scenePlayersModule.AddPlayerToScene(playerID, globalSceneID);
        }

        /// <summary>Queues an actor prefab at an absolute world position in a networked scene.</summary>
        /// <param name="sourceChunk">The verified environment tile supplying the spawn position and context.</param>
        /// <param name="worldPosition">The finite absolute world position for the spawned actor.</param>
        /// <param name="prefab">The prefab to spawn.</param>
        /// <param name="sceneID">The networked scene identifier.</param>
        /// <param name="owner">The player that owns the spawned actor.</param>
        public void SpawnAt(
            WorldChunkKey sourceChunk,
            WorldPosition worldPosition,
            GameObject prefab,
            SceneID sceneID,
            PlayerID owner)
        {
            if (!sourceChunk.Context.IsValid)
                throw new ArgumentException("The source chunk must contain a valid context.", nameof(sourceChunk));
            if (!worldPosition.IsFinite)
                throw new ArgumentException("The spawn world position must be finite.", nameof(worldPosition));
            if (!ServerWorldManager.IsInitialized || ServerWorldManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "Queueing a spawn requires an initialized server world manager.");
            }
            if (!sourceChunk.Context.IsCompatibleWith(ServerWorldManager.Instance.CurrentRuntimeContext))
            {
                throw new InvalidOperationException(
                    "The source chunk context must match the current server runtime context.");
            }

            string sceneName = $"World_{sourceChunk.ChunkCoord.x}_{sourceChunk.ChunkCoord.y}";
            if (!_networkManager.sceneModule.TryGetSceneState(sceneID, out var state))
            {
                Debug.Error($"Cannot queue spawn for scene '{sceneName}' because network scene '{sceneID}' is not available.");
                return;
            }
            if (!state.scene.IsValid() || !state.scene.isLoaded)
            {
                Debug.Error($"Cannot queue spawn for scene '{sceneName}' because scene '{state.scene.name}' is not loaded.");
                return;
            }
            if (prefab == null)
            {
                Debug.Error($"Cannot queue spawn for scene '{sceneName}' because the prefab is null.");
                return;
            }

            if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
            {
                throw new InvalidOperationException(
                    "Queueing a spawn requires an initialized server physics manager.");
            }
            if (!ServerPhysicsManager.Instance.TryGetEnvironmentRegionDescriptor(sourceChunk, out var region))
            {
                throw new InvalidOperationException(
                    $"Source chunk {sourceChunk.ChunkCoord} has no active physical region binding.");
            }
            ServerPhysicsManager.Instance.ToRegionLocal(region, worldPosition);

            int prefabIndex = RegisterPrefab(prefab);
            int sceneIndex = RegisterScene(state.scene);

            var entity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(entity, new Systems.SpawnCommand
            {
                WorldPositionD = new double3(worldPosition.x, worldPosition.y, worldPosition.z),
                PrefabIndex = prefabIndex,
                SceneIndex = sceneIndex,
                SceneName = sceneName,
                Owner = owner,
                ChunkCoord = new int2(sourceChunk.ChunkCoord.x, sourceChunk.ChunkCoord.y)
            });
        }

        /// <summary>Commits the ECS spawn-command snapshot at the authoritative tick boundary.</summary>
        internal void CommitSpawnCommands()
        {
            // The authoritative simulation owns this snapshot and invokes it after all ECS systems finish.
            using var markerScope = UPDATE_MARKER.Begin();
            bool telemetryEnabled = PerformanceTelemetry.IsEnabled;
            if (_spawnCommandQuery.IsEmpty)
            {
                if (telemetryEnabled)
                    PerformanceTelemetry.TryRecord(SPAWN_COMMAND_COUNT_METRIC, 0);
                return;
            }

            NativeArray<Entity> entities = _spawnCommandQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            try
            {
                NativeArray<Systems.SpawnCommand> commands = default;
                try
                {
                    commands = _spawnCommandQuery.ToComponentDataArray<Systems.SpawnCommand>(Unity.Collections.Allocator.Temp);

                    if (telemetryEnabled)
                        PerformanceTelemetry.TryRecord(SPAWN_COMMAND_COUNT_METRIC, commands.Length);

                    for (int i = 0; i < commands.Length; i++)
                    {
                        var cmd = commands[i];
                        string prefabName = $"registry index {cmd.PrefabIndex}";
                        string sceneName = cmd.SceneName.ToString();
                        string sceneNameForLog = $"registry index {cmd.SceneIndex}";
                        GameObject prefab = null;
                        GameObject spawnedObject = null;
                        Actor spawnedActor = null;
                        NetworkIdentity registeredIdentity = null;
                        Actor spawnPointReservation = null;
                        bool hasSpawnPointReservation = _spawnPointActorMap.TryGetValue(
                            cmd.SpawnPointGUID,
                            out spawnPointReservation);
                        bool playerActorRegistered = false;
                        bool hadPreviousPlayerActor = false;
                        Actor previousPlayerActor = null;
                        PlayerSessionData session = null;
                        Actor previousSessionActor = null;
                        bool sessionActorRegistered = false;
                        bool npcStatsRegistered = false;
                        bool actorSpawnEventStarted = false;
                        bool spawnSucceeded = false;

                        try
                        {
                            if (cmd.PrefabIndex < 0 || cmd.PrefabIndex >= _prefabRegistry.Count)
                                throw new InvalidOperationException($"Prefab registry index {cmd.PrefabIndex} is invalid.");

                            prefab = _prefabRegistry[cmd.PrefabIndex];
                            if (prefab == null)
                                throw new InvalidOperationException($"Prefab registry entry {cmd.PrefabIndex} is null or has been destroyed.");
                            prefabName = prefab.name;

                            if (!_sceneRegistry.TryGetValue(cmd.SceneIndex, out var scene))
                                throw new InvalidOperationException($"Scene registry index {cmd.SceneIndex} is invalid.");
                            if (!scene.IsValid() || !scene.isLoaded)
                                throw new InvalidOperationException($"Scene registry entry {cmd.SceneIndex} is no longer loaded.");
                            sceneNameForLog = scene.name;

                            if (!ServerWorldManager.IsInitialized || ServerWorldManager.Instance == null)
                                throw new InvalidOperationException("The server world manager is not initialized.");

                            ActorSpatialContext runtimeContext = ServerWorldManager.Instance.CurrentRuntimeContext;
                            if (!runtimeContext.IsValid)
                                throw new InvalidOperationException("The server runtime spatial context is not initialized.");

                            var sourceChunk = new WorldChunkKey(
                                runtimeContext,
                                new Vector2Int(cmd.ChunkCoord.x, cmd.ChunkCoord.y));
                            if (!ServerPhysicsManager.IsInitialized || ServerPhysicsManager.Instance == null)
                                throw new InvalidOperationException("The server physics manager is not initialized.");
                            if (!ServerPhysicsManager.Instance.TryGetEnvironmentRegionDescriptor(sourceChunk, out var region))
                            {
                                throw new InvalidOperationException(
                                    $"Source chunk {sourceChunk.ChunkCoord} has no active physical region binding.");
                            }
                            if (!ServerActorManager.IsInitialized || ServerActorManager.Instance == null)
                                throw new InvalidOperationException("Consuming a spawn command requires an initialized server actor manager.");

                            var spawnWorldPosition = new WorldPosition(
                                cmd.WorldPositionD.x,
                                cmd.WorldPositionD.y,
                                cmd.WorldPositionD.z);
                            if (!spawnWorldPosition.IsFinite)
                                throw new InvalidOperationException("The spawn command world position must be finite.");

                            var regionLocalPosition = ServerPhysicsManager.Instance.ToRegionLocal(
                                region,
                                spawnWorldPosition);

                            // Use PurrNet's Instantiate proxy — handles scene placement,
                            // hierarchy registration, spawn, and replication in one call.
                            spawnedObject = PurrNet.UnityProxy.Instantiate(prefab, regionLocalPosition, Quaternion.identity, scene);
                            if (spawnedObject == null)
                                throw new InvalidOperationException("PurrNet returned a null spawned object.");
                            Debug.Log($"Spawned '{prefabName}' at {regionLocalPosition} in scene '{sceneNameForLog}'.");

                            var actor = spawnedObject.GetComponent<Game.Shared.Runtime.Actors.Actor>();
                            if (actor == null)
                                throw new InvalidOperationException($"Prefab '{prefabName}' is missing an Actor component. Add it to the prefab in the editor.");
                            spawnedActor = actor;

                            actor.CurrentChunk = sceneName;
                            actor.SetAuthoritativeSpatialContext(runtimeContext);
                            actor.Initialize(actor.ID, cmd.Owner != default ? $"Player_{cmd.Owner}" : prefabName);
                            actor.SetSpawnPointGUID(cmd.SpawnPointGUID);

                            // Register the root identity for targeted visibility re-evaluation.
                            NetworkIdentity identity = spawnedObject.GetComponent<NetworkIdentity>();
                            if (identity != null)
                            {
                                Game.Shared.Networking.ChunkSubscriptions.RegisterIdentity(sceneName, identity);
                                registeredIdentity = identity;
                            }

                            // Set ownership and register player actor cache.
                            if (identity != null && cmd.Owner != default)
                            {
                                identity.GiveOwnership(cmd.Owner);
                                actor.SetOwner(cmd.Owner);
                                hadPreviousPlayerActor = Game.Shared.Networking.ChunkSubscriptions.PLAYER_ACTORS.TryGetValue(
                                    cmd.Owner,
                                    out previousPlayerActor);
                                Game.Shared.Networking.ChunkSubscriptions.RegisterPlayerActor(cmd.Owner, actor);
                                playerActorRegistered = true;
                                spawnedObject.name = $"Player{cmd.Owner}";

                                // Add a disabled NavMeshAgent for server-driven movement (charge spells, etc.).
                                if (spawnedObject.GetComponent<UnityEngine.AI.NavMeshAgent>() == null)
                                {
                                    var playerAgent = spawnedObject.AddComponent<UnityEngine.AI.NavMeshAgent>();
                                    playerAgent.radius = 0.25f;
                                    playerAgent.height = 2f;
                                    playerAgent.enabled = false;
                                }
                            }

                            // Initialize NPC stats (NPCs have no owner, so this must be outside the owner block).
                            if (!actor.IsPlayer)
                            {
                                ServerStatManager.Instance.InitializeNPCStats(actor);
                                ServerStatManager.Instance.RegisterActor(actor);
                                npcStatsRegistered = true;

                                // Teach NPCs the auto-attack spell so they can swing.
                                var autoAttack = GameConfigurationManager.Config.AutoAttackSpell;
                                if (autoAttack != null)
                                    spawnedObject.GetComponent<ActorSpellcaster>()?.LearnSpell(autoAttack.DefinitionID);
                            }

                            // Fire spawn event after Initialize + GiveOwnership so ECS entity is created with accurate data.
                            actorSpawnEventStarted = true;
                            ServerActorManager.Instance.RegisterSpawnedActor(actor, spawnWorldPosition);
                            Game.Shared.Networking.ActorEndpoint.onServerActorSpawned?.Invoke(actor);

                            // Link the actor to the player's session data for disconnect cleanup.
                            if (cmd.Owner != default)
                            {
                                session = ServerConnectionManager.Instance.GetSessionData(cmd.Owner);
                                if (session != null)
                                {
                                    previousSessionActor = session.PlayerActor;
                                    session.SetPlayerActor(actor);
                                    sessionActorRegistered = true;
                                }
                            }

                            spawnSucceeded = true;
                        }
                        catch (Exception ex)
                        {
                            Debug.Error($"Failed to consume spawn command for prefab '{prefabName}' in scene '{sceneNameForLog}' (spawn point {cmd.SpawnPointGUID}): {ex.Message}\n{ex.StackTrace}");
                        }
                        finally
                        {
                            if (!spawnSucceeded)
                            {
                                try
                                {
                                    if (sessionActorRegistered && session != null && session.PlayerActor == spawnedActor)
                                        session.SetPlayerActor(previousSessionActor);
                                }
                                catch (Exception cleanupException)
                                {
                                    Debug.Error($"Failed to restore the player session after rejecting spawn point {cmd.SpawnPointGUID}: {cleanupException.Message}\n{cleanupException.StackTrace}");
                                }

                                try
                                {
                                    if (playerActorRegistered
                                        && Game.Shared.Networking.ChunkSubscriptions.PLAYER_ACTORS.TryGetValue(cmd.Owner, out var currentPlayerActor)
                                        && currentPlayerActor == spawnedActor)
                                    {
                                        if (hadPreviousPlayerActor)
                                            Game.Shared.Networking.ChunkSubscriptions.PLAYER_ACTORS[cmd.Owner] = previousPlayerActor;
                                        else
                                            Game.Shared.Networking.ChunkSubscriptions.UnregisterPlayerActor(cmd.Owner);
                                    }
                                }
                                catch (Exception cleanupException)
                                {
                                    Debug.Error($"Failed to restore player actor subscriptions after rejecting spawn point {cmd.SpawnPointGUID}: {cleanupException.Message}\n{cleanupException.StackTrace}");
                                }

                                try
                                {
                                    if (npcStatsRegistered && ServerStatManager.IsInitialized)
                                        ServerStatManager.Instance.UnregisterActor(spawnedActor);
                                }
                                catch (Exception cleanupException)
                                {
                                    Debug.Error($"Failed to unregister NPC stats after rejecting spawn point {cmd.SpawnPointGUID}: {cleanupException.Message}\n{cleanupException.StackTrace}");
                                }

                                try
                                {
                                    if (actorSpawnEventStarted && spawnedActor != null && ServerActorManager.IsInitialized)
                                        ServerActorManager.Instance.RollbackActorSpawn(spawnedActor);
                                }
                                catch (Exception cleanupException)
                                {
                                    Debug.Error($"Failed to roll back ECS actor state after rejecting spawn point {cmd.SpawnPointGUID}: {cleanupException.Message}\n{cleanupException.StackTrace}");
                                }

                                try
                                {
                                    if (hasSpawnPointReservation
                                        && _spawnPointActorMap.TryGetValue(cmd.SpawnPointGUID, out var currentReservation)
                                        && ReferenceEquals(currentReservation, spawnPointReservation))
                                    {
                                        _spawnPointActorMap.Remove(cmd.SpawnPointGUID);
                                    }
                                }
                                catch (Exception cleanupException)
                                {
                                    Debug.Error($"Failed to release the spawn-point reservation for {cmd.SpawnPointGUID}: {cleanupException.Message}\n{cleanupException.StackTrace}");
                                }

                                try
                                {
                                    if (registeredIdentity != null)
                                        Game.Shared.Networking.ChunkSubscriptions.UnregisterIdentity(sceneName, registeredIdentity);
                                }
                                catch (Exception cleanupException)
                                {
                                    Debug.Error($"Failed to unregister the rejected spawn identity for {cmd.SpawnPointGUID}: {cleanupException.Message}\n{cleanupException.StackTrace}");
                                }

                                if (spawnedObject != null)
                                {
                                    try
                                    {
                                        PurrNet.UnityProxy.Destroy(spawnedObject);
                                    }
                                    catch (Exception cleanupException)
                                    {
                                        Debug.Error($"Failed to clean up rejected spawn object for prefab '{prefabName}' in scene '{sceneNameForLog}': {cleanupException.Message}\n{cleanupException.StackTrace}");
                                    }
                                }
                            }

                            // Always destroy the entity so it doesn't retry.
                            if (_entityManager.Exists(entities[i]))
                                _entityManager.DestroyEntity(entities[i]);

                            if (telemetryEnabled)
                            {
                                PerformanceTelemetry.TryRecord(
                                    spawnSucceeded ? SPAWN_SUCCESS_COUNT_METRIC : SPAWN_FAILURE_COUNT_METRIC,
                                    1);
                            }
                        }
                    }

                }
                finally
                {
                    if (commands.IsCreated)
                        commands.Dispose();
                }
            }
            finally
            {
                if (entities.IsCreated)
                    entities.Dispose();
            }
        }
        private int RegisterPrefab(GameObject scenePrefab)
        {
            // Resolve to PurrNet's own reference so Spawn() identity check passes.
            // Addressable scenes deserialize a different object instance than what
            // NetworkPrefabs holds, so we match by name.
            foreach (var data in _networkManager.prefabProvider.allPrefabs)
            {
                if (data.prefab != null && data.prefab.name == scenePrefab.name)
                {
                    int idx = _prefabRegistry.IndexOf(data.prefab);
                    if (idx >= 0) return idx;
                    _prefabRegistry.Add(data.prefab);
                    return _prefabRegistry.Count - 1;
                }
            }

            Debug.Error($"Prefab '{scenePrefab.name}' not found in PurrNet's NetworkPrefabs!");
            _prefabRegistry.Add(scenePrefab);
            return _prefabRegistry.Count - 1;
        }

        private int RegisterScene(Scene scene)
        {
            foreach (var kvp in _sceneRegistry)
                if (kvp.Value == scene) return kvp.Key;
            int idx = _nextSceneIndex++;
            _sceneRegistry[idx] = scene;
            return idx;
        }
    }
}
