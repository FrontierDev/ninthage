using Game.Shared.Runtime.Actors;
using PurrNet;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor.Networking
{
    /// <summary>
    /// Validates the authored component ordering required by actor network identities.
    /// </summary>
    public sealed class ActorNetworkIdentityValidator : IPreprocessBuildWithReport
    {
        private const string REQUIRED_ORDERING =
            "dedicated NetworkIdentity before NetworkTransform, Actor, or any other NetworkIdentity subclass";

        /// <summary>
        /// Gets the order in which this build validation callback runs.
        /// </summary>
        public int callbackOrder => 0;

        /// <summary>
        /// Scans all prefab assets for invalid actor network identity ordering.
        /// </summary>
        public static IReadOnlyList<string> ValidateActorPrefabs()
        {
            var violations = new List<string>();
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");

            foreach (var prefabGuid in prefabGuids)
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    violations.Add(
                        $"{prefabPath}: prefab could not be loaded as a GameObject; " +
                        $"actual first identity type: unavailable. Required ordering: {REQUIRED_ORDERING}.");
                    continue;
                }

                var actors = prefab.GetComponentsInChildren<Actor>(true);
                var validatedActorObjects = new HashSet<GameObject>();
                foreach (var actor in actors)
                {
                    if (!validatedActorObjects.Add(actor.gameObject))
                        continue;

                    ValidateActorGameObject(prefabPath, actor.gameObject, violations);
                }
            }

            violations.Sort(StringComparer.Ordinal);
            return violations;
        }

        /// <summary>
        /// Runs actor network identity validation from the Unity Editor menu.
        /// </summary>
        [MenuItem("Tools/Ninth Age/Validation/Validate Actor Network Identities")]
        public static void ValidateActorNetworkIdentities()
        {
            var violations = ValidateActorPrefabs();
            if (violations.Count == 0)
            {
                Debug.Log("Actor NetworkIdentity validation passed: no violations found.");
                return;
            }

            Debug.LogError(
                $"Actor NetworkIdentity validation found {violations.Count} violation(s):" +
                Environment.NewLine + string.Join(Environment.NewLine, violations));
        }

        /// <summary>
        /// Blocks builds when an actor prefab violates the authored identity ordering.
        /// </summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            var violations = ValidateActorPrefabs();
            if (violations.Count == 0)
                return;

            throw new BuildFailedException(
                "Actor NetworkIdentity validation failed. Fix the following prefab violations before building:" +
                Environment.NewLine + string.Join(Environment.NewLine, violations));
        }

        private static void ValidateActorGameObject(
            string prefabPath,
            GameObject actorGameObject,
            ICollection<string> violations)
        {
            var components = actorGameObject.GetComponents<Component>();
            NetworkIdentity firstIdentity = null;
            foreach (var component in components)
            {
                if (component is NetworkIdentity identity)
                {
                    firstIdentity = identity;
                    break;
                }
            }

            if (firstIdentity != null && firstIdentity.GetType() == typeof(NetworkIdentity))
                return;

            var firstIdentityType = firstIdentity == null ? "none" : firstIdentity.GetType().FullName;
            violations.Add(
                $"{prefabPath}: Actor GameObject '{GetHierarchyPath(actorGameObject.transform)}' has " +
                $"actual first identity type '{firstIdentityType}'. Required ordering: {REQUIRED_ORDERING}.");
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }

            return path;
        }
    }
}
