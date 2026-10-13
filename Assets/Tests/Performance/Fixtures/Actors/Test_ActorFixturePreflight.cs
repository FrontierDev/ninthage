using Game.Server.Core;
using Game.Shared.Data;
using Game.Shared.Runtime.Actors;
using PurrNet;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Tests.Performance.Fixtures.Actors
{
    /// <summary>Validates actor fixture prerequisites without substituting a simpler workload.</summary>
    public static class Test_ActorFixturePreflight
    {
        /// <summary>Validates the ECS world required for a data-only fixture.</summary>
        public static void ValidateDataOnly(Test_ActorFixtureProfile profile)
        {
            ValidateProfile(profile, Test_ActorFixtureRepresentation.ecs_data_only);
            Require(ServerECSManager.Initialized, "Server ECS is not initialized for the data-only fixture.");
            Require(ServerECSManager.DefaultWorld != null && ServerECSManager.DefaultWorld.IsCreated,
                "Server ECS default world is unavailable for the data-only fixture.");
        }

        /// <summary>
        /// Validates the loaded server and authored assets required for full-fidelity actors.
        /// </summary>
        public static GameObject ValidateFull(
            Test_ActorFixtureProfile profile,
            int connectedRealPlayers,
            SceneID sceneID)
        {
            ValidateProfile(profile, Test_ActorFixtureRepresentation.networked_gameobject_ecs);
            ValidateRequiredAssets();

            Require(NetworkManager.main != null && NetworkManager.main.isServer,
                "A running PurrNet server is required for the full actor fixture.");
            Require(connectedRealPlayers == profile.population.players.requested,
                $"Full actor fixture '{profile.variant_id}' requires {profile.population.players.requested} real player connection(s); received {connectedRealPlayers}.");
            Require(NetworkManager.main.playerCount == connectedRealPlayers,
                $"The PurrNet server reports {NetworkManager.main.playerCount} connected player(s), not the supplied real-player count {connectedRealPlayers}.");
            Require(ServerECSManager.Initialized, "Server ECS is not initialized for the full actor fixture.");
            Require(ServerSpawnManager.IsInitialized, "ServerSpawnManager is not initialized for the full actor fixture.");
            Require(ServerActorManager.IsInitialized, "ServerActorManager is not initialized for the full actor fixture.");
            Require(ServerStatManager.IsInitialized, "ServerStatManager is not initialized for the full actor fixture.");
            Require(ServerWorldManager.IsInitialized && ServerWorldManager.Instance.IsGlobalActorsSceneLoaded,
                "The networked global_actors scene is not loaded for the full actor fixture.");
            Require(sceneID != default, "The full actor fixture requires a non-default networked scene identifier.");
            Require(sceneID == ServerWorldManager.Instance.GlobalActorsSceneID,
                "Full actor fixture actors must be spawned into the loaded global_actors scene.");
            Require(NetworkManager.main.sceneModule.TryGetSceneState(sceneID, out _),
                "The supplied global_actors scene identifier is not registered with PurrNet.");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Test_ActorFixtureLayout.NPC_PREFAB_PATH);
            Require(prefab != null, $"Required NPC prefab is missing: {Test_ActorFixtureLayout.NPC_PREFAB_PATH}.");
            Require(prefab.GetComponent<NetworkIdentity>() != null,
                $"NPC prefab '{Test_ActorFixtureLayout.NPC_PREFAB_PATH}' has no NetworkIdentity.");
            Require(prefab.GetComponent<NPCActor>() != null,
                $"NPC prefab '{Test_ActorFixtureLayout.NPC_PREFAB_PATH}' has no NPCActor.");
            Require(prefab.GetComponent<NPCStatProfile>() != null,
                $"NPC prefab '{Test_ActorFixtureLayout.NPC_PREFAB_PATH}' has no NPCStatProfile.");
            Require(prefab.GetComponent<ActorSpellcaster>() != null,
                $"NPC prefab '{Test_ActorFixtureLayout.NPC_PREFAB_PATH}' has no ActorSpellcaster.");
            return prefab;
        }

        /// <summary>Validates every authored input used by the scenario schedule.</summary>
        public static void ValidateRequiredAssets()
        {
            ValidateAssetManifest();
            RequireFile(Test_ActorFixtureLayout.NPC_PREFAB_PATH);
            RequireFile(Test_ActorFixtureLayout.PLAYER_PREFAB_PATH);
            RequireFile(Test_ActorFixtureLayout.GLOBAL_ACTOR_SCENE_PATH);

            RequireAsset<SpellDefinition>("Assets/Data/SpellDefinitions/Spell_auto_attack.asset", "auto_attack");
            RequireAsset<SpellDefinition>("Assets/Data/SpellDefinitions/Spell_fireball.asset", "fireball");
            RequireAsset<SpellDefinition>("Assets/Data/SpellDefinitions/Spell_cleave.asset", "cleave");
            RequireAsset<AuraDefinition>("Assets/Data/AuraDefinitions/Aura_burning.asset", "burning");
            RequireAsset<AuraDefinition>("Assets/Data/AuraDefinitions/Aura_bleed.asset", "bleed");
            RequireAsset<ClassDefinition>("Assets/Data/ClassDefinitions/Class_fighter.asset", "fighter");
            RequireAsset<ClassDefinition>("Assets/Data/ClassDefinitions/Class_ranger.asset", "ranger");
            RequireAsset<ClassDefinition>("Assets/Data/ClassDefinitions/Class_sorcerer.asset", "sorcerer");
        }

        private static void ValidateAssetManifest()
        {
            RequireFile(Test_ActorFixtureLayout.ASSET_MANIFEST_PATH);
            var manifest = JsonUtility.FromJson<Test_ActorAssetManifest>(
                File.ReadAllText(Test_ActorFixtureLayout.ASSET_MANIFEST_PATH));
            Require(manifest != null && manifest.assets != null && manifest.assets.Length > 0,
                $"Actor asset manifest is empty or invalid: {Test_ActorFixtureLayout.ASSET_MANIFEST_PATH}.");

            foreach (var asset in manifest.assets)
            {
                RequireFile(asset.path);
                var importedGUID = AssetDatabase.AssetPathToGUID(asset.path);
                Require(importedGUID == asset.guid,
                    $"Actor asset manifest GUID mismatch for '{asset.path}': '{asset.guid}' versus '{importedGUID}'.");
            }

            using (var sha256 = SHA256.Create())
            {
                var digest = sha256.ComputeHash(File.ReadAllBytes(Test_ActorFixtureLayout.ASSET_MANIFEST_PATH));
                var builder = new StringBuilder(digest.Length * 2);
                foreach (var value in digest)
                    builder.Append(value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
                Require(builder.ToString() == Test_ActorFixtureLayout.ASSET_MANIFEST_SHA256,
                    "Actor asset manifest content hash does not match the catalog provenance.");
            }
        }

        private static void ValidateProfile(Test_ActorFixtureProfile profile, Test_ActorFixtureRepresentation expectedRepresentation)
        {
            Require(profile != null, "Actor fixture profile is null.");
            Test_ActorFixtureLayout.ValidatePopulation(profile);
            Require(profile.representation == expectedRepresentation.ToString(),
                $"Fixture profile '{profile.variant_id}' is '{profile.representation}', expected '{expectedRepresentation}'.");
        }

        private static void RequireFile(string path)
        {
            Require(File.Exists(path), $"Required fixture input is missing: {path}.");
            Require(!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)),
                $"Required fixture input is not imported by Unity: {path}.");
        }

        private static void RequireAsset<T>(string path, string definitionID) where T : UnityEngine.Object
        {
            RequireFile(path);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Require(asset != null, $"Required definition asset could not be loaded: {path}.");
            if (asset is DataDefinition definition)
                Require(definition.DefinitionID == definitionID,
                    $"Definition asset '{path}' has ID '{definition.DefinitionID}', expected '{definitionID}'.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new Test_ActorFixturePreconditionException(message);
        }
    }

    /// <summary>Identifies an unavailable prerequisite instead of silently downgrading a workload.</summary>
    public sealed class Test_ActorFixturePreconditionException : InvalidOperationException
    {
        /// <summary>Initializes a fixture prerequisite failure.</summary>
        public Test_ActorFixturePreconditionException(string message) : base(message)
        {
        }
    }
}
