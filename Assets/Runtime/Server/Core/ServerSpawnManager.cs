using Debug = Game.Shared.Utility.FormattedDebug;
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

            foreach (ActorSpawnPoint spawnPoint in actorSpawnPoints)
            {
                Guid spawnPointGUID = spawnPoint.GUID;
                if (_spawnPointActorMap.ContainsKey(spawnPointGUID))
                {
                    Debug.Warning($"Spawn point '{spawnPoint.name}' with GUID {spawnPointGUID} already has an actor spawned. Skipping duplicate.");
                    continue;
                }

                int prefabIndex = RegisterPrefab(spawnPoint.Actor.gameObject);

                var entity = _entityManager.CreateEntity();
                _entityManager.AddComponentData(entity, new Systems.SpawnPointComponent
                {
                    SpawnPointGUID = spawnPointGUID,
                    SceneName = state.scene.name,
                    Position = spawnPoint.transform.position,
                    PrefabIndex = prefabIndex,
                    SceneIndex = sceneIndex,
                    Pending = true
                });

                // Tell the spawn system that this spawn point has an actor associated with it.
                _spawnPointActorMap[spawnPointGUID] = spawnPoint.Actor;
            }
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
            bool chunkReady = false;
            ServerWorldManager.Instance.EnsureChunkLoaded(null, spawnScene, (_) => chunkReady = true);
            yield return new WaitUntil(() => chunkReady);

            // Load the player prefab from Addressables
            var handle = Addressables.LoadAssetAsync<GameObject>("PlayerPrefab"); // your addressable key
            yield return handle;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.Error("Failed to load player prefab addressable.");
                yield break;
            }

            var spawnPosition = new Vector3(0, 5, 0);
            GameObject prefab = handle.Result;

            // Spawn the actor into the persistent global_actors scene, not the chunk scene.
            var globalSceneID = ServerWorldManager.Instance.GlobalActorsSceneID;
            SpawnAt("World_0_0", spawnPosition, prefab, globalSceneID, playerID);

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

        /// <summary>Spawns an actor prefab at a position in a networked scene.</summary>
        /// <param name="sceneName">The scene name used for the spawn.</param>
        /// <param name="position">The world position for the spawned actor.</param>
        /// <param name="prefab">The prefab to spawn.</param>
        /// <param name="sceneID">The networked scene identifier.</param>
        /// <param name="owner">The player that owns the spawned actor.</param>
        public void SpawnAt(string sceneName, Vector3 position, GameObject prefab, SceneID sceneID, PlayerID owner)
        {
            if (!_networkManager.sceneModule.TryGetSceneState(sceneID, out var state)) return;

            int prefabIndex = RegisterPrefab(prefab);
            int sceneIndex = RegisterScene(state.scene);

            var chunkCoord = ServerWorldManager.Instance.ParseChunkCoordinate(sceneName);
            var entity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(entity, new Systems.SpawnCommand
            {
                Position = position,
                PrefabIndex = prefabIndex,
                SceneIndex = sceneIndex,
                SceneName = sceneName,
                Owner = owner,
                ChunkCoord = new Unity.Mathematics.int2(chunkCoord.x, chunkCoord.y)
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

            var entities = _spawnCommandQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var commands = _spawnCommandQuery.ToComponentDataArray<Systems.SpawnCommand>(Unity.Collections.Allocator.Temp);

            if (telemetryEnabled)
                PerformanceTelemetry.TryRecord(SPAWN_COMMAND_COUNT_METRIC, commands.Length);

            for (int i = 0; i < commands.Length; i++)
            {
                var cmd = commands[i];
                var prefab = _prefabRegistry[cmd.PrefabIndex];
                var scene = _sceneRegistry[cmd.SceneIndex];
                var sceneName = cmd.SceneName.ToString();
                var spawnPointGUID = cmd.SpawnPointGUID;
                bool spawnSucceeded = false;

                try
                {
                    // Use PurrNet's Instantiate proxy — handles scene placement,
                    // hierarchy registration, spawn, and replication in one call.
                    var go = PurrNet.UnityProxy.Instantiate(prefab, cmd.Position, Quaternion.identity, scene);
                    Debug.Log($"Spawned '{prefab.name}' at {cmd.Position} in scene '{scene.name}'.");

                    var actor = go.GetComponent<Game.Shared.Runtime.Actors.Actor>();
                    if (actor == null)
                    {
                        Debug.Error($"Prefab '{prefab.name}' is missing an Actor component. Add it to the prefab in the editor.");
                        continue;
                    }
                    else
                    {
                        actor.CurrentChunk = sceneName;
                        actor.WorldPos = Game.Shared.Utility.WorldPosition.FromChunkLocal(
                            new Vector2Int(cmd.ChunkCoord.x, cmd.ChunkCoord.y),
                            new Vector3(cmd.Position.x, cmd.Position.y, cmd.Position.z));
                        actor.Initialize(actor.ID, cmd.Owner != default ? $"Player_{cmd.Owner}" : prefab.name);
                        actor.SetSpawnPointGUID(spawnPointGUID);

                        // Register the root identity for targeted visibility re-evaluation.
                        NetworkIdentity identity = go.GetComponent<NetworkIdentity>();
                        if (identity != null)
                            Game.Shared.Networking.ChunkSubscriptions.RegisterIdentity(sceneName, identity);

                        // Set ownership and register player actor cache.
                        if (identity != null && cmd.Owner != default)
                        {
                            identity.GiveOwnership(cmd.Owner);
                            actor.SetOwner(cmd.Owner);
                            Game.Shared.Networking.ChunkSubscriptions.RegisterPlayerActor(cmd.Owner, actor);
                            go.name = $"Player{cmd.Owner}";

                            // Add a disabled NavMeshAgent for server-driven movement (charge spells, etc.).
                            if (go.GetComponent<UnityEngine.AI.NavMeshAgent>() == null)
                            {
                                var playerAgent = go.AddComponent<UnityEngine.AI.NavMeshAgent>();
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

                            // Teach NPCs the auto-attack spell so they can swing.
                            var autoAttack = GameConfigurationManager.Config.AutoAttackSpell;
                            if (autoAttack != null)
                                go.GetComponent<ActorSpellcaster>()?.LearnSpell(autoAttack.DefinitionID);
                        }

                        // Fire spawn event after Initialize + GiveOwnership so ECS entity is created with accurate data.
                        Game.Shared.Networking.ActorEndpoint.onServerActorSpawned?.Invoke(actor);

                        // Link the actor to the player's session data for disconnect cleanup.
                        if (cmd.Owner != default)
                        {
                            var session = ServerConnectionManager.Instance.GetSessionData(cmd.Owner);
                            if (session != null)
                                session.SetPlayerActor(actor);
                        }

                        spawnSucceeded = true;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.Error($"Failed to spawn prefab '{prefab.name}': {ex.Message}\n{ex.StackTrace}");
                }
                finally
                {
                    if (telemetryEnabled)
                    {
                        PerformanceTelemetry.TryRecord(
                            spawnSucceeded ? SPAWN_SUCCESS_COUNT_METRIC : SPAWN_FAILURE_COUNT_METRIC,
                            1);
                    }

                    // Always destroy the entity so it doesn't retry
                    _entityManager.DestroyEntity(entities[i]);
                }
            }

            entities.Dispose();
            commands.Dispose();
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
