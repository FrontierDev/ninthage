using Game.Client.UI.Skin;
using System;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Drawers
{
    /// <summary>
    /// Draws the serialized typography values for one <see cref="UI_SkinTextBinding"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(UI_SkinTextBinding))]
    public sealed class UI_SkinTextBindingDrawer : PropertyDrawer
    {
        private const float HORIZONTAL_GAP = 4f;

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
        }

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var textProperty = FindRequiredProperty(property, "_text");
            var fontRoleProperty = FindRequiredProperty(property, "_fontRole");
            var fontSizeProperty = FindRequiredProperty(property, "_fontSize");
            var fontStyleProperty = FindRequiredProperty(property, "_fontStyle");

            EditorGUI.BeginProperty(position, label, property);

            var rowHeight = EditorGUIUtility.singleLineHeight;
            var rowSpacing = EditorGUIUtility.standardVerticalSpacing;
            var textRect = new Rect(position.x, position.y, position.width, rowHeight);
            var controlsY = textRect.yMax + rowSpacing;
            var controlsWidth = position.width - HORIZONTAL_GAP * 2f;
            var roleWidth = controlsWidth * 0.4f;
            var sizeWidth = controlsWidth * 0.25f;
            var styleWidth = controlsWidth - roleWidth - sizeWidth;

            using (new EditorGUI.DisabledScope(true))
                EditorGUI.PropertyField(textRect, textProperty, new GUIContent("Text"));

            var roleRect = new Rect(position.x, controlsY, roleWidth, rowHeight);
            var sizeRect = new Rect(roleRect.xMax + HORIZONTAL_GAP, controlsY, sizeWidth, rowHeight);
            var styleRect = new Rect(sizeRect.xMax + HORIZONTAL_GAP, controlsY, styleWidth, rowHeight);
            EditorGUI.PropertyField(roleRect, fontRoleProperty, new GUIContent("Font Role"));
            EditorGUI.PropertyField(sizeRect, fontSizeProperty, new GUIContent("Font Size"));
            EditorGUI.PropertyField(styleRect, fontStyleProperty, new GUIContent("Font Style"));

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
