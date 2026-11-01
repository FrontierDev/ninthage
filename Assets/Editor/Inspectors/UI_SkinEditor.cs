using Game.Client.UI.Skin;
using System;
using System.Collections.Generic;
using TMPro;
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
        private SerializedProperty _headingFontProperty;
        private SerializedProperty _bodyFontProperty;
        private SerializedProperty _numericFontProperty;
        private SerializedProperty _textBindingsProperty;
        private string _errorMessage;

        private void OnEnable()
        {
            _headingFontProperty = FindRequiredProperty("_headingFont");
            _bodyFontProperty = FindRequiredProperty("_bodyFont");
            _numericFontProperty = FindRequiredProperty("_numericFont");
            _textBindingsProperty = FindRequiredProperty("_textBindings");
        }

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Fonts", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_headingFontProperty, new GUIContent("Heading Font"));
            EditorGUILayout.PropertyField(_bodyFontProperty, new GUIContent("Body Font"));
            EditorGUILayout.PropertyField(_numericFontProperty, new GUIContent("Numeric Font"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Text Bindings", EditorStyles.boldLabel);
            DrawTextBindings();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                ApplyTextBindingsWithUndo();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh Text Bindings"))
                RefreshTextBindings();
            if (GUILayout.Button("Apply Text Styles"))
                ApplyTextBindingsWithUndo();

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
            ApplyTextBindingsWithUndo();
        }

        private void ApplyTextBindingsWithUndo()
        {
            var skin = GetSkin();
            var textObjects = new List<TMP_Text>();
            var seenTexts = new HashSet<TMP_Text>();
            if (skin.TextBindings != null)
            {
                foreach (var binding in skin.TextBindings)
                {
                    if (binding?.Text != null && seenTexts.Add(binding.Text))
                        textObjects.Add(binding.Text);
                }
            }

            Undo.RecordObjects(textObjects.ToArray(), "Apply UI Skin");
            try
            {
                skin.ApplyTextBindings();
            }
            catch (InvalidOperationException exception)
            {
                _errorMessage = exception.Message;
                return;
            }

            foreach (var text in textObjects)
            {
                EditorUtility.SetDirty(text);
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }

            _errorMessage = null;
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
