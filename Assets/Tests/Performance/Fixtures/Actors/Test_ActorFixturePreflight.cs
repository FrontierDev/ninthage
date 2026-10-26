using Game.Server.Core;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using PurrNet;
using System;
using System.Collections.Generic;
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
            var behavior = prefab.GetComponent<NPCBehavior>();
            Require(behavior != null, $"NPC prefab '{Test_ActorFixtureLayout.NPC_PREFAB_PATH}' has no NPCBehavior.");
            Require(!float.IsNaN(behavior.AggroRange) && !float.IsInfinity(behavior.AggroRange) && behavior.AggroRange > 0f,
                $"NPC prefab '{Test_ActorFixtureLayout.NPC_PREFAB_PATH}' has no positive finite NPCBehavior.AggroRange.");
            Require(prefab.GetComponent<ActorSpellcaster>() != null,
                $"NPC prefab '{Test_ActorFixtureLayout.NPC_PREFAB_PATH}' has no ActorSpellcaster.");
            return prefab;
        }

        /// <summary>Returns the authored passive aggro range after full-actor asset validation.</summary>
        public static float GetNPCPassiveAggroRange(GameObject prefab)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab));
            var behavior = prefab.GetComponent<NPCBehavior>();
            Require(behavior != null, $"NPC prefab '{prefab.name}' has no NPCBehavior.");
            Require(!float.IsNaN(behavior.AggroRange) && !float.IsInfinity(behavior.AggroRange) && behavior.AggroRange > 0f,
                $"NPC prefab '{prefab.name}' has no positive finite NPCBehavior.AggroRange.");
            return behavior.AggroRange;
        }

        /// <summary>Resolves the configured runtime ground layer for an authoritative server-world query.</summary>
        public static int ResolveGroundLayerMask(string authorityIdentity, string configuredLayerName, WorldPosition testedPosition)
        {
            Require(!string.IsNullOrWhiteSpace(authorityIdentity), "The ground preflight authority identity is empty.");
            if (string.IsNullOrWhiteSpace(configuredLayerName))
                throw new Test_ActorFixturePreconditionException(
                    $"Authoritative ground preflight failed for authority '{authorityIdentity}': configured ground layer '<empty>' is missing at tested position {FormatPosition(testedPosition)}.");

            var layerIndex = LayerMask.NameToLayer(configuredLayerName);
            if (layerIndex < 0)
                throw new Test_ActorFixturePreconditionException(
                    $"Authoritative ground preflight failed for authority '{authorityIdentity}': configured ground layer '{configuredLayerName}' is missing at tested position {FormatPosition(testedPosition)}.");

            return LayerMask.GetMask(configuredLayerName);
        }

        /// <summary>
        /// Resolves every Stage 1.4 placement against the same authoritative ground query used by runtime movement.
        /// </summary>
        public static IReadOnlyList<Test_ActorFixturePlacement> ResolveAuthoritativeGround(
            IReadOnlyList<Test_ActorFixturePlacement> placements,
            Test_NPCActivityScenario scenario)
        {
            Require(placements != null && placements.Count > 0,
                "Authoritative ground preflight received no Stage 1.4 placements.");
            Require(GameConfigurationManager.IsInitialized && GameConfigurationManager.Config != null,
                "Authoritative ground preflight cannot read the initialized game configuration.");
            Require(ServerPhysicsManager.IsInitialized && ServerPhysicsManager.Instance != null,
                "Authoritative ground preflight requires an initialized ServerPhysicsManager.");
            Require(placements[0] != null,
                $"Authoritative ground preflight received a null placement for scenario '{scenario}'.");

            var configuration = GameConfigurationManager.Config;
            var firstPosition = new WorldPosition(placements[0].world_x, placements[0].world_y, placements[0].world_z);
            int groundLayerMask;
            try
            {
                groundLayerMask = ResolveGroundLayerMask(
                    "ordinary server world",
                    configuration.GroundDetectionMaskName,
                    firstPosition);
            }
            catch (Test_ActorFixturePreconditionException exception)
            {
                throw new Test_ActorFixturePreconditionException(
                    $"Authoritative ground preflight failed for ordinary server world, configured ground layer '{configuration.GroundDetectionMaskName}', scenario '{scenario}', placement '{placements[0].actor_fixture_id}' at tested position {FormatPosition(firstPosition)}: {exception.Message}");
            }
            var groundDetectionRadius = configuration.GroundDetectionRadius;
            Require(!float.IsNaN(groundDetectionRadius)
                && !float.IsInfinity(groundDetectionRadius)
                && groundDetectionRadius >= 0f,
                $"Authoritative ground preflight failed for ordinary server world, configured ground layer '{configuration.GroundDetectionMaskName}', scenario '{scenario}', placement '{placements[0].actor_fixture_id}' at tested position {FormatPosition(firstPosition)}: invalid configured ground detection radius '{groundDetectionRadius:R}'.");
            var maxDistance = groundDetectionRadius * 2f + 10f;
            Require(!float.IsNaN(maxDistance)
                && !float.IsInfinity(maxDistance)
                && maxDistance > 0f,
                $"Authoritative ground preflight failed for ordinary server world, configured ground layer '{configuration.GroundDetectionMaskName}', scenario '{scenario}', placement '{placements[0].actor_fixture_id}' at tested position {FormatPosition(firstPosition)}: configured ground query distance is not finite.");

            foreach (var placement in placements)
            {
                Require(placement != null,
                    $"Authoritative ground preflight received a null placement for scenario '{scenario}'.");
                var testedPosition = new WorldPosition(placement.world_x, placement.world_y, placement.world_z);
                Require(testedPosition.IsFinite,
                    $"Authoritative ground preflight failed for ordinary server world, configured ground layer '{configuration.GroundDetectionMaskName}', scenario '{scenario}', placement '{placement.actor_fixture_id}' at tested position {FormatPosition(testedPosition)}: tested position is not finite.");

                var rayOrigin = new WorldPosition(
                    testedPosition.x,
                    testedPosition.y + groundDetectionRadius,
                    testedPosition.z);
                Require(rayOrigin.IsFinite,
                    $"Authoritative ground preflight failed for ordinary server world, configured ground layer '{configuration.GroundDetectionMaskName}', scenario '{scenario}', placement '{placement.actor_fixture_id}' at tested position {FormatPosition(testedPosition)}: ground query origin is not finite.");
                var hitGround = ServerPhysicsManager.Instance.RaycastGround(
                    rayOrigin,
                    maxDistance,
                    groundLayerMask,
                    out var hitWorldPosition,
                    out var hitDistance);
                if (!hitGround)
                {
                    throw new Test_ActorFixturePreconditionException(
                        $"Authoritative ground preflight failed for ordinary server world, configured ground layer '{configuration.GroundDetectionMaskName}', scenario '{scenario}', placement '{placement.actor_fixture_id}' at tested position {FormatPosition(testedPosition)}: no ground hit was returned.");
                }

                Require(hitWorldPosition.IsFinite
                    && !float.IsNaN(hitDistance)
                    && !float.IsInfinity(hitDistance)
                    && hitDistance >= 0f
                    && hitDistance <= maxDistance,
                    $"Authoritative ground preflight failed for ordinary server world, configured ground layer '{configuration.GroundDetectionMaskName}', scenario '{scenario}', placement '{placement.actor_fixture_id}' at tested position {FormatPosition(testedPosition)}: returned an invalid hit distance or position.");

                placement.world_y = hitWorldPosition.y;
                var resolvedPosition = new WorldPosition(placement.world_x, placement.world_y, placement.world_z);
                Require(resolvedPosition.IsFinite,
                    $"Authoritative ground preflight failed for ordinary server world, configured ground layer '{configuration.GroundDetectionMaskName}', scenario '{scenario}', placement '{placement.actor_fixture_id}' at tested position {FormatPosition(testedPosition)}: resolved placement is not finite.");
            }

            return placements;
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

        private static string FormatPosition(WorldPosition position)
        {
            return $"({position.x:R}, {position.y:R}, {position.z:R})";
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
