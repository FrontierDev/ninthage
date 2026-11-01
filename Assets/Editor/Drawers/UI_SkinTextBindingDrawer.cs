using Game.Client.UI.Skin;
using System;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Drawers
{
    /// <summary>
    /// Draws the serialized semantic text-style mapping for one <see cref="UI_SkinTextBinding"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(UI_SkinTextBinding))]
    public sealed class UI_SkinTextBindingDrawer : PropertyDrawer
    {
        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
        }

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var textProperty = FindRequiredProperty(property, "_text");
            var textStyleProperty = FindRequiredProperty(property, "_textStyle");

            EditorGUI.BeginProperty(position, label, property);

            var rowHeight = EditorGUIUtility.singleLineHeight;
            var rowSpacing = EditorGUIUtility.standardVerticalSpacing;
            var textRect = new Rect(position.x, position.y, position.width, rowHeight);
            var controlsY = textRect.yMax + rowSpacing;
            var textStyleRect = new Rect(position.x, controlsY, position.width, rowHeight);

            using (new EditorGUI.DisabledScope(true))
                EditorGUI.PropertyField(textRect, textProperty, new GUIContent("Text"));

            EditorGUI.PropertyField(textStyleRect, textStyleProperty, new GUIContent("Text Style"));

            EditorGUI.EndProperty();
        }

        private static SerializedProperty FindRequiredProperty(SerializedProperty property, string propertyName)
        {
            var childProperty = property.FindPropertyRelative(propertyName);
            if (childProperty == null)
                throw new InvalidOperationException(
                    $"UI_SkinTextBindingDrawer expected serialized property '{propertyName}'.");

            return childProperty;
        }
    }
}
