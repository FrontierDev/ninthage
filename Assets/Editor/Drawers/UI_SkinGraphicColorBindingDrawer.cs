using Game.Client.UI.Skin;
using System;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Drawers
{
    /// <summary>
    /// Draws one serialized semantic colour mapping for a UI graphic.
    /// </summary>
    [CustomPropertyDrawer(typeof(UI_SkinGraphicColorBinding))]
    public sealed class UI_SkinGraphicColorBindingDrawer : PropertyDrawer
    {
        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
        }

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var targetProperty = FindRequiredProperty(property, "_target");
            var colorProperty = FindRequiredProperty(property, "_color");

            EditorGUI.BeginProperty(position, label, property);

            var rowHeight = EditorGUIUtility.singleLineHeight;
            var rowSpacing = EditorGUIUtility.standardVerticalSpacing;
            var targetRect = new Rect(position.x, position.y, position.width, rowHeight);
            var colorRect = new Rect(position.x, targetRect.yMax + rowSpacing, position.width, rowHeight);

            EditorGUI.PropertyField(targetRect, targetProperty, new GUIContent("Graphic"));
            EditorGUI.PropertyField(colorRect, colorProperty, new GUIContent("Color Token"));

            EditorGUI.EndProperty();
        }

        private static SerializedProperty FindRequiredProperty(SerializedProperty property, string propertyName)
        {
            var childProperty = property.FindPropertyRelative(propertyName);
            if (childProperty == null)
                throw new InvalidOperationException(
                    $"UI_SkinGraphicColorBindingDrawer expected serialized property '{propertyName}'.");

            return childProperty;
        }
    }
}
