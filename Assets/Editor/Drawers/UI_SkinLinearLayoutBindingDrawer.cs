using Game.Client.UI.Skin;
using System;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Drawers
{
    /// <summary>
    /// Draws one serialized linear layout metric mapping.
    /// </summary>
    [CustomPropertyDrawer(typeof(UI_SkinLinearLayoutBinding))]
    public sealed class UI_SkinLinearLayoutBindingDrawer : PropertyDrawer
    {
        private const float TOGGLE_WIDTH = 100f;

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 3f + EditorGUIUtility.standardVerticalSpacing * 2f;
        }

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var targetProperty = FindRequiredProperty(property, "_target");
            var applySpacingProperty = FindRequiredProperty(property, "_applySpacing");
            var spacingMetricProperty = FindRequiredProperty(property, "_spacingMetric");
            var applyPaddingProperty = FindRequiredProperty(property, "_applyPadding");
            var paddingMetricProperty = FindRequiredProperty(property, "_paddingMetric");

            EditorGUI.BeginProperty(position, label, property);

            var rowHeight = EditorGUIUtility.singleLineHeight;
            var rowSpacing = EditorGUIUtility.standardVerticalSpacing;
            var targetRect = new Rect(position.x, position.y, position.width, rowHeight);
            var spacingY = targetRect.yMax + rowSpacing;
            var paddingY = spacingY + rowHeight + rowSpacing;
            var spacingToggleRect = new Rect(position.x, spacingY, TOGGLE_WIDTH, rowHeight);
            var spacingMetricRect = new Rect(
                spacingToggleRect.xMax + rowSpacing,
                spacingY,
                position.width - TOGGLE_WIDTH - rowSpacing,
                rowHeight);
            var paddingToggleRect = new Rect(position.x, paddingY, TOGGLE_WIDTH, rowHeight);
            var paddingMetricRect = new Rect(
                paddingToggleRect.xMax + rowSpacing,
                paddingY,
                position.width - TOGGLE_WIDTH - rowSpacing,
                rowHeight);

            EditorGUI.PropertyField(targetRect, targetProperty, new GUIContent("Target"));
            EditorGUI.PropertyField(spacingToggleRect, applySpacingProperty, new GUIContent("Apply Spacing"));
            using (new EditorGUI.DisabledScope(!applySpacingProperty.boolValue))
                EditorGUI.PropertyField(spacingMetricRect, spacingMetricProperty, new GUIContent("Spacing Metric"));

            EditorGUI.PropertyField(paddingToggleRect, applyPaddingProperty, new GUIContent("Apply Padding"));
            using (new EditorGUI.DisabledScope(!applyPaddingProperty.boolValue))
                EditorGUI.PropertyField(paddingMetricRect, paddingMetricProperty, new GUIContent("Padding Metric"));

            EditorGUI.EndProperty();
        }

        private static SerializedProperty FindRequiredProperty(SerializedProperty property, string propertyName)
        {
            var childProperty = property.FindPropertyRelative(propertyName);
            if (childProperty == null)
                throw new InvalidOperationException(
                    $"UI_SkinLinearLayoutBindingDrawer expected serialized property '{propertyName}'.");

            return childProperty;
        }
    }
}
