using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Debug = Game.Shared.FormattedDebug;
using PurrNet;
using PurrNet.Modules;
using System.Collections;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Game.Shared;
using System;
using Unity.Collections;
using Unity.Entities.UniversalDelegates;
using System.Linq;
using Game.Shared.Data;

namespace Game.Server
{
    public sealed class ServerSpawnManager : MonoBehaviour
    {
        private static ServerSpawnManager _instance;
        public static ServerSpawnManager Instance => _instance;
        private static bool _initialized;
        public static bool IsInitialized => _initialized;
        private NetworkManager networkManager;

        // Managed prefab list indexed by SpawnPointComponent.PrefabIndex
        private readonly List<GameObject> prefabRegistry = new();
        // SceneIndex → Scene for PurrNet Instantiate
        private readonly Dictionary<int, Scene> sceneRegistry = new();
        private int nextSceneIndex;

        private Dictionary<Guid, Actor> spawnPointActorMap = new Dictionary<Guid, Actor>();

        private EntityManager entityManager;
        private EntityQuery spawnCommandQuery;

        public void Awake()
        {
            if (_instance != null)
            {
                Debug.Error("ServerSpawnManager is already initialized!");
                return;
            }
            _instance = this;

            networkManager = NetworkManager.main;
            networkManager.sceneModule.onPostSceneLoaded += OnPostSceneLoaded;

            Game.Shared.Networking.ActorService.onServerActorDespawned += OnServerActorDespawned;

            var world = ECS.ServerECSManager.DefaultWorld;
            entityManager = world.EntityManager;
            spawnCommandQuery = entityManager.CreateEntityQuery(typeof(ECS.SpawnCommand));

            _initialized = true;
        }

        private void OnPostSceneLoaded(SceneID sceneID, bool asServer)
        {
            if (!asServer) return;
            if (!networkManager.sceneModule.TryGetSceneState(sceneID, out var state)) return;

            var globalSceneId = ServerWorldManager.Instance.GlobalActorsSceneId;
            if (!networkManager.sceneModule.TryGetSceneState(globalSceneId, out var globalState))
                return;
            int sceneIndex = RegisterScene(globalState.scene);

            // Find spawn points ONLY in the specific scene that was loaded
            ActorSpawnPoint[] actorSpawnPoints = state.scene.GetRootGameObjects()
                .SelectMany(go => go.GetComponentsInChildren<ActorSpawnPoint>())
                .ToArray();

            Debug.Log($"Found {actorSpawnPoints.Length} spawn points in '{state.scene.name}'.");

            foreach (ActorSpawnPoint spawnPoint in actorSpawnPoints)
            {
                Guid spawnPointGuid = spawnPoint.Guid;
                if (spawnPointActorMap.ContainsKey(spawnPointGuid))
                {
                    Debug.Warning($"Spawn point '{spawnPoint.name}' with GUID {spawnPointGuid} already has an actor spawned. Skipping duplicate.");
                    continue;
                }

                int prefabIndex = RegisterPrefab(spawnPoint.Actor.gameObject);

                var entity = entityManager.CreateEntity();
                entityManager.AddComponentData(entity, new ECS.SpawnPointComponent
                {
                    SpawnPointGuid = spawnPointGuid,
                    SceneName = state.scene.name,
                    Position = spawnPoint.transform.position,
                    PrefabIndex = prefabIndex,
                    SceneIndex = sceneIndex,
                    Pending = true
                });

                // Tell the spawn system that this spawn point has an actor associated with it.
                spawnPointActorMap[spawnPointGuid] = spawnPoint.Actor;
            }
        }

        private void OnServerActorDespawned(Actor actor)
        {
            // If this actor was spawned from a spawn point, mark that spawn point as pending again so it can respawn.
            if (actor.SpawnPointGuid != default && spawnPointActorMap.ContainsKey(actor.SpawnPointGuid))
                spawnPointActorMap.Remove(actor.SpawnPointGuid);
        }

        public void SpawnPlayer(PlayerID playerId)
        {
            StartCoroutine(SpawnPlayerCoroutine(playerId));
        }

        [ToDo("Implement proper spawn point selection logic rather than hardcoding to a specific scene and position.Lots of placeholder code in this method that not be in production.")]
        private IEnumerator SpawnPlayerCoroutine(PlayerID playerId)
        {
            Vector2Int spawnScene = new Vector2Int(0, 0);

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

            Vector3 spawnPosition = new Vector3(0, 5, 0);
            GameObject prefab = handle.Result;

            // Spawn the actor into the persistent global_actors scene, not the chunk scene.
            var globalSceneId = ServerWorldManager.Instance.GlobalActorsSceneId;
            SpawnAt("World_0_0", spawnPosition, prefab, globalSceneId, playerId);

            // Wait for the actor to be spawned and linked to the session (happens on next Update tick).
            var session = ServerConnectionManager.Instance.GetSessionData(playerId);
            if (session == null) yield break;
            yield return new WaitUntil(() => session.PlayerActor != null);

            // Look up the character data from the account and assign it to the PlayerActor.
            var accountData = ServerConnectionManager.Instance.GetAccountData(playerId);
            var characterData = accountData?.Characters.Find(c => c.Guid == session.CharacterId);
            if (characterData == null)
            {
                Debug.Error($"Character data not found for player {playerId}, character GUID {session.CharacterId}.");
                yield break;
            }

            var playerActor = session.PlayerActor as Game.Shared.PlayerActor;
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
                Debug.Error($"Class definition '{characterData.ClassID}' not found for player {playerId}.");
                yield break;
            }
            else
            {
                foreach (var classSpell in classDef.ClassSpellList)
                {
                    if (!characterData.KnownSpellIDs.Contains(classSpell.spell.DefinitionId))
                    {
                        Debug.Log($"Adding missing spell '{classSpell.spell.DisplayName}' to player {playerId} character data.");
                        characterData.KnownSpellIDs.Add(classSpell.spell.DefinitionId);
                    }
                }
            }

            characterData.Level = UnityEngine.Random.Range(11, 60);
            characterData.PvPFactionID = RaceDefinitionLibrary.Instance.GetDefinition(characterData.RaceID)?.DefaultPVPFaction?.DefinitionId;

            characterData.SavedInventory.Clear();
            characterData.SavedEquipment.Clear();
            characterData.SavedTalents.Clear();
            characterData.SavedWeaponSkills.Clear();
            characterData.SavedReputations.Clear();
            characterData.SavedQuests.Clear();

            // Add test quests.
            characterData.SavedQuests = new List<Shared.Persistence.SavedQuestEntry>
            {
                new() { QuestID = "test", Progress = new int[] { 0 }, IsCompleted = false },
            };

            // Add the test talents.
            characterData.SavedTalents = new List<Shared.Persistence.SavedTalentEntry>
            {
                new() { TalentID = "test", Rank = 3 },
            };

            Debug.Log($"Talents for player {playerId}: {string.Join(", ", characterData.SavedTalents.Select(t => $"{t.TalentID} (Rank {t.Rank})"))}");

            // If the player's inventory is null (can happen if they had an old save without inventory data), initialize it with an empty inventory.
            bool alwaysRefresh = true;
            if (characterData.SavedInventory.Count < 5 || alwaysRefresh)
            {
                Debug.Log($"Initializing empty inventory for player {playerId} character.");
                characterData.SavedInventory = new List<Shared.Persistence.SavedInventoryEntry>
                {
                    new() {
                        SlotIndex = 0,
                        ItemId = "test_item",
                        StackSize = 1,
                        Modifications = new List<string>()
                    },
                    new() {
                        SlotIndex = 1,
                        ItemId = "6nGzh6jdMl",
                        StackSize = 1,
                        Modifications = new List<string>()
                    },
                    new() {
                        SlotIndex = 2,
                        ItemId = "IF76hX6mCs",
                        StackSize = 1,
                        Modifications = new List<string>()
                    },
                    new() {
                        SlotIndex = 3,
                        ItemId = "test_2h",
                        StackSize = 1,
                        Modifications = new List<string>()
                    },
                };
            }
            else
            {
                for (int i = characterData.SavedInventory.Count - 1; i >= 0; i--)
                {
                    var entry = characterData.SavedInventory[i];
                    if (Shared.Data.ItemDefinitionLibrary.Instance.GetDefinition(entry.ItemId) == null)
                    {
                        Debug.Warning($"Removing invalid inventory entry at slot {entry.SlotIndex} with unknown item '{entry.ItemId}' for player {playerId}.");
                        characterData.SavedInventory.RemoveAt(i);
                    }
                    else if (string.IsNullOrEmpty(entry.ItemGuid))
                    {
                        entry.ItemGuid = System.Guid.NewGuid().ToString("N");
                        characterData.SavedInventory[i] = entry;
                        Debug.Log($"Assigned GUID to inventory entry at slot {entry.SlotIndex} for player {playerId}.");
                    }
                }
            }

            for (int i = characterData.SavedEquipment.Count - 1; i >= 0; i--)
            {
                var entry = characterData.SavedEquipment[i];
                var def = Shared.Data.ItemDefinitionLibrary.Instance.GetDefinition(entry.ItemId);
                if (def == null || (def.ItemType != Shared.Data.ItemType.Weapon && def.ItemType != Shared.Data.ItemType.Armor))
                {
                    Debug.Warning($"Removing invalid equipment entry at slot {entry.SlotIndex} with unknown item '{entry.ItemId}' for player {playerId}.");
                    characterData.SavedEquipment.RemoveAt(i);
                }
                else if (string.IsNullOrEmpty(entry.ItemGuid))
                {
                    entry.ItemGuid = System.Guid.NewGuid().ToString("N");
                    characterData.SavedEquipment[i] = entry;
                    Debug.Log($"Assigned GUID to equipment entry at slot {entry.SlotIndex} for player {playerId}.");
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
                    Debug.Log($"Added weapon skill for {weaponType} to player {playerId}.");
                }
            }

            // Remove weapon skills no longer valid for this class
            characterData.SavedWeaponSkills.RemoveAll(ws =>
            {
                var weaponType = (Shared.Data.ItemWeaponType)ws.WeaponType;
                bool isInvalid = !classDef.WeaponTypes.Contains(weaponType);
                if (isInvalid)
                    Debug.Warning($"Removing invalid weapon skill {weaponType} from player {playerId}.");
                return isInvalid;
            });

            // For now, just set all the reputations to their default values for the player's race.
            foreach (var startingRep in RaceDefinitionLibrary.Instance.GetDefinition(characterData.RaceID).StartingReputations)
            {
                characterData.SavedReputations.Add(new Shared.Persistence.SavedReputationEntry
                {
                    FactionID = startingRep.Faction.DefinitionId,
                    Reputation = startingRep.Reputation
                });
                Debug.Log($"Set initial reputation with faction '{startingRep.Faction.DisplayName}' to {startingRep.Reputation} for player {playerId}.");
            }

            try
            {
                playerActor.SetCharacterData(characterData);
            }
            catch (Exception e)
            {
                Debug.Error($"Error setting character data for player {playerId}: {e}");
            }


            var json = JsonUtility.ToJson(characterData);
            Debug.Log($"Character JSON length: {json.Length}");

            ServerStatManager.Instance.InitializePlayerStats(playerActor);
            ServerStatManager.Instance.RegisterActor(playerActor);

            // Stat initialization sets all stats to 0 before filling to max,
            // which transiently triggers the death check. Reset the flag.
            playerActor.isDead = false;

            // Trigger initial 3x3 chunk interest around the spawn location.
            Game.Shared.Networking.MovementService.onInterestRequest?.Invoke(playerId, spawnScene);

            // Send the finalized character data to the client now, after all server-side
            // modifications are complete. This must happen before AddPlayerToScene so
            // the client has _activeCharacter set before actor ownership is replicated.
            Game.Shared.Networking.AccountService.Client_EnterWorld(playerId, characterData, $"World_{spawnScene.x}_{spawnScene.y}");

            // Add the player to global_actors AFTER ChunkSubscriptions is populated.
            // This avoids a race where PurrNet's Spawn() and OnPlayerLoadedScene both
            // send spawn packets before the visibility rule has data to work with.
            networkManager.scenePlayersModule.AddPlayerToScene(playerId, globalSceneId);
        }

        public void SpawnAt(string sceneName, Vector3 position, GameObject prefab, SceneID sceneID, PlayerID owner)
        {
            if (!networkManager.sceneModule.TryGetSceneState(sceneID, out var state)) return;

            int prefabIndex = RegisterPrefab(prefab);
            int sceneIndex = RegisterScene(state.scene);

            var chunkCoord = ServerWorldManager.Instance.ParseChunkCoordinate(sceneName);
            var entity = entityManager.CreateEntity();
            entityManager.AddComponentData(entity, new ECS.SpawnCommand
            {
                Position = position,
                PrefabIndex = prefabIndex,
                SceneIndex = sceneIndex,
                SceneName = sceneName,
                Owner = owner,
                ChunkCoord = new Unity.Mathematics.int2(chunkCoord.x, chunkCoord.y)
            });
        }

        private void Update()
        {
            // Drain spawn commands from ECS
            if (spawnCommandQuery.IsEmpty) return;

            var entities = spawnCommandQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
            var commands = spawnCommandQuery.ToComponentDataArray<ECS.SpawnCommand>(Unity.Collections.Allocator.Temp);

            for (int i = 0; i < commands.Length; i++)
            {
                var cmd = commands[i];
                var prefab = prefabRegistry[cmd.PrefabIndex];
                var scene = sceneRegistry[cmd.SceneIndex];
                var sceneName = cmd.SceneName.ToString();
                var spawnPointGuid = cmd.SpawnPointGuid;

                try
                {
                    // Use PurrNet's Instantiate proxy — handles scene placement,
                    // hierarchy registration, spawn, and replication in one call.
                    var go = PurrNet.UnityProxy.Instantiate(prefab, cmd.Position, Quaternion.identity, scene);
                    Debug.Log($"Spawned '{prefab.name}' at {cmd.Position} in scene '{scene.name}'.");

                    var actor = go.GetComponent<Game.Shared.Actor>();
                    if (actor == null)
                    {
                        Debug.Error($"Prefab '{prefab.name}' is missing an Actor component. Add it to the prefab in the editor.");
                        continue;
                    }

                    actor.CurrentChunk = sceneName;
                    actor.WorldPos = Game.Shared.Utility.WorldPosition.FromChunkLocal(
                        new Vector2Int(cmd.ChunkCoord.x, cmd.ChunkCoord.y),
                        new Vector3(cmd.Position.x, cmd.Position.y, cmd.Position.z));
                    actor.Initialize(actor.Id, cmd.Owner != default ? $"Player_{cmd.Owner}" : prefab.name);
                    actor.SetSpawnPointGuid(spawnPointGuid);

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
                        ServerStatManager.Instance.InitializeNpcStats(actor);
                        ServerStatManager.Instance.RegisterActor(actor);

                        // Teach NPCs the auto-attack spell so they can swing.
                        var autoAttack = GameConfigurationManager.Config.AutoAttackSpell;
                        if (autoAttack != null)
                            go.GetComponent<ActorSpellcaster>()?.LearnSpell(autoAttack.DefinitionId);
                    }

                    // Fire spawn event after Initialize + GiveOwnership so ECS entity is created with accurate data.
                    Game.Shared.Networking.ActorService.onServerActorSpawned?.Invoke(actor);

                    // Link the actor to the player's session data for disconnect cleanup.
                    if (cmd.Owner != default)
                    {
                        var session = ServerConnectionManager.Instance.GetSessionData(cmd.Owner);
                        if (session != null)
                            session.SetPlayerActor(actor);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.Error($"Failed to spawn prefab '{prefab.name}': {ex.Message}\n{ex.StackTrace}");
                }
                finally
                {
                    // Always destroy the entity so it doesn't retry
                    entityManager.DestroyEntity(entities[i]);
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
            foreach (var data in networkManager.prefabProvider.allPrefabs)
            {
                if (data.prefab != null && data.prefab.name == scenePrefab.name)
                {
                    int idx = prefabRegistry.IndexOf(data.prefab);
                    if (idx >= 0) return idx;
                    prefabRegistry.Add(data.prefab);
                    return prefabRegistry.Count - 1;
                }
            }

            Debug.Error($"Prefab '{scenePrefab.name}' not found in PurrNet's NetworkPrefabs!");
            prefabRegistry.Add(scenePrefab);
            return prefabRegistry.Count - 1;
        }

        private int RegisterScene(Scene scene)
        {
            foreach (var kvp in sceneRegistry)
                if (kvp.Value == scene) return kvp.Key;
            int idx = nextSceneIndex++;
            sceneRegistry[idx] = scene;
            return idx;
        }
    }
}