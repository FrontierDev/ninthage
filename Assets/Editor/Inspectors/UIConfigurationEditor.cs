using Game.Client.UI.Configuration;
using Game.Client.UI.Skin;
using Game.Editor.UI;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Inspectors
{
    /// <summary>
    /// Applies edited UI configuration values to matching loaded UI skins.
    /// </summary>
    [CustomEditor(typeof(UIConfiguration))]
    public sealed class UIConfigurationEditor : UnityEditor.Editor
    {
        private string _errorMessage;

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            DrawConfigurationProperties();
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                ApplyToLoadedSkins();
            }

            if (!string.IsNullOrEmpty(_errorMessage))
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
        }

        private void DrawConfigurationProperties()
        {
            var property = serializedObject.GetIterator();
            var enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.propertyPath == "m_Script")
                    continue;

                EditorGUILayout.PropertyField(property, true);
            }
        }

        private void ApplyToLoadedSkins()
        {
            var configuration = GetConfiguration();
            var skins = FindMatchingSkins(configuration);
            var undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply UI Configuration");
            var failures = new List<string>();

            try
            {
                foreach (var skin in skins)
                {
                    if (!UI_SkinEditorUtility.TryApplyWithUndo(skin, "Apply UI Skin", out var errorMessage))
                        failures.Add($"{GetSceneHierarchyPath(skin)}: {errorMessage}");
                }
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }

            _errorMessage = failures.Count == 0 ? null : string.Join("\n", failures);
        }

        private static List<UI_Skin> FindMatchingSkins(UIConfiguration configuration)
        {
            var matchingSkins = new List<UI_Skin>();
            foreach (var skin in UnityEngine.Object.FindObjectsByType<UI_Skin>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (skin.Configuration == configuration && !EditorUtility.IsPersistent(skin))
                    matchingSkins.Add(skin);
            }

            matchingSkins.Sort(CompareSkins);
            return matchingSkins;
        }

        private static int CompareSkins(UI_Skin left, UI_Skin right)
        {
            var sceneComparison = string.CompareOrdinal(
                left.gameObject.scene.path,
                right.gameObject.scene.path);
            return sceneComparison != 0
                ? sceneComparison
                : string.CompareOrdinal(GetHierarchyPath(left.transform), GetHierarchyPath(right.transform));
        }

        private static string GetSceneHierarchyPath(UI_Skin skin)
        {
            var scenePath = skin.gameObject.scene.path;
            if (string.IsNullOrEmpty(scenePath))
                scenePath = skin.gameObject.scene.name;

            return $"{scenePath}/{GetHierarchyPath(skin.transform)}";
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var pathParts = new List<string>();
            var current = transform;
            while (current != null)
            {
                pathParts.Insert(0, current.name);
                current = current.parent;
            }

            return string.Join("/", pathParts);
        }

        private UIConfiguration GetConfiguration()
        {
            var configuration = target as UIConfiguration;
            if (configuration == null)
                throw new InvalidOperationException("UIConfigurationEditor target is not a UIConfiguration.");

            return configuration;
        }
    }
}
