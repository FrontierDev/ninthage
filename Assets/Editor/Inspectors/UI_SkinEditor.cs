using Game.Client.UI.Skin;
using Game.Editor.UI;
using System;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Inspectors
{
    /// <summary>
    /// Provides the edit-mode authoring surface for a hierarchy-local <see cref="UI_Skin"/>.
    /// </summary>
    [CustomEditor(typeof(UI_Skin))]
    public sealed class UI_SkinEditor : UnityEditor.Editor
    {
        private SerializedProperty _configurationProperty;
        private SerializedProperty _textBindingsProperty;
        private SerializedProperty _rectTransformBindingsProperty;
        private string _errorMessage;

        private void OnEnable()
        {
            _configurationProperty = FindRequiredProperty("_configuration");
            _textBindingsProperty = FindRequiredProperty("_textBindings");
            _rectTransformBindingsProperty = FindRequiredProperty("_rectTransformBindings");
        }

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_configurationProperty, new GUIContent("UI Configuration"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Text Bindings", EditorStyles.boldLabel);
            DrawTextBindings();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rect Transform Bindings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_rectTransformBindingsProperty, true);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                ApplySkinWithUndo();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh Text Bindings"))
                RefreshTextBindings();
            if (GUILayout.Button("Apply Skin"))
                ApplySkinWithUndo();

            if (!string.IsNullOrEmpty(_errorMessage))
                EditorGUILayout.HelpBox(_errorMessage, MessageType.Error);
        }

        private void DrawTextBindings()
        {
            for (var index = 0; index < _textBindingsProperty.arraySize; index++)
            {
                var bindingProperty = _textBindingsProperty.GetArrayElementAtIndex(index);
                EditorGUILayout.PropertyField(bindingProperty, GUIContent.none, true);
            }
        }

        private void RefreshTextBindings()
        {
            var skin = GetSkin();
            Undo.RecordObject(skin, "Refresh UI Skin Text Bindings");
            skin.RefreshTextBindings();
            EditorUtility.SetDirty(skin);
            PrefabUtility.RecordPrefabInstancePropertyModifications(skin);
            serializedObject.Update();
            ApplySkinWithUndo();
        }

        private void ApplySkinWithUndo()
        {
            var skin = GetSkin();
            UI_SkinEditorUtility.TryApplyWithUndo(skin, "Apply UI Skin", out _errorMessage);
        }

        private UI_Skin GetSkin()
        {
            var skin = target as UI_Skin;
            if (skin == null)
                throw new InvalidOperationException("UI_SkinEditor target is not a UI_Skin.");

            return skin;
        }

        private SerializedProperty FindRequiredProperty(string propertyName)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException(
                    $"UI_SkinEditor expected serialized property '{propertyName}' on UI_Skin.");

            return property;
        }
    }
}
