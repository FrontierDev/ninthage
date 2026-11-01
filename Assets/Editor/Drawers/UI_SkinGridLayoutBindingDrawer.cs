using Game.Client.UI.Skin;
using System;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Drawers
{
    /// <summary>
    /// Draws one serialized grid layout metric mapping.
    /// </summary>
    [CustomPropertyDrawer(typeof(UI_SkinGridLayoutBinding))]
    public sealed class UI_SkinGridLayoutBindingDrawer : PropertyDrawer
    {
        private const float TOGGLE_WIDTH = 100f;

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 6f + EditorGUIUtility.standardVerticalSpacing * 5f;
        }

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var targetProperty = FindRequiredProperty(property, "_target");
            var applyCellWidthProperty = FindRequiredProperty(property, "_applyCellWidth");
            var cellWidthMetricProperty = FindRequiredProperty(property, "_cellWidthMetric");
            var applyCellHeightProperty = FindRequiredProperty(property, "_applyCellHeight");
            var cellHeightMetricProperty = FindRequiredProperty(property, "_cellHeightMetric");
            var applySpacingXProperty = FindRequiredProperty(property, "_applySpacingX");
            var spacingXMetricProperty = FindRequiredProperty(property, "_spacingXMetric");
            var applySpacingYProperty = FindRequiredProperty(property, "_applySpacingY");
            var spacingYMetricProperty = FindRequiredProperty(property, "_spacingYMetric");
            var applyPaddingProperty = FindRequiredProperty(property, "_applyPadding");
            var paddingMetricProperty = FindRequiredProperty(property, "_paddingMetric");

            EditorGUI.BeginProperty(position, label, property);

            var rowHeight = EditorGUIUtility.singleLineHeight;
            var rowSpacing = EditorGUIUtility.standardVerticalSpacing;
            var targetRect = new Rect(position.x, position.y, position.width, rowHeight);
            var cellWidthY = targetRect.yMax + rowSpacing;
            var cellHeightY = cellWidthY + rowHeight + rowSpacing;
            var spacingXY = cellHeightY + rowHeight + rowSpacing;
            var spacingYY = spacingXY + rowHeight + rowSpacing;
            var paddingY = spacingYY + rowHeight + rowSpacing;

            EditorGUI.PropertyField(targetRect, targetProperty, new GUIContent("Target"));
            DrawToggleAndMetric(
                position,
                cellWidthY,
                applyCellWidthProperty,
                cellWidthMetricProperty,
                "Apply Cell Width",
                "Cell Width Metric");
            DrawToggleAndMetric(
                position,
                cellHeightY,
                applyCellHeightProperty,
                cellHeightMetricProperty,
                "Apply Cell Height",
                "Cell Height Metric");
            DrawToggleAndMetric(
                position,
                spacingXY,
                applySpacingXProperty,
                spacingXMetricProperty,
                "Apply Spacing X",
                "Spacing X Metric");
            DrawToggleAndMetric(
                position,
                spacingYY,
                applySpacingYProperty,
                spacingYMetricProperty,
                "Apply Spacing Y",
                "Spacing Y Metric");
            DrawToggleAndMetric(
                position,
                paddingY,
                applyPaddingProperty,
                paddingMetricProperty,
                "Apply Padding",
                "Padding Metric");

            EditorGUI.EndProperty();
        }

        private static void DrawToggleAndMetric(
            Rect position,
            float y,
            SerializedProperty applyProperty,
            SerializedProperty metricProperty,
            string applyLabel,
            string metricLabel)
        {
            var rowHeight = EditorGUIUtility.singleLineHeight;
            var rowSpacing = EditorGUIUtility.standardVerticalSpacing;
            var toggleRect = new Rect(position.x, y, TOGGLE_WIDTH, rowHeight);
            var metricRect = new Rect(
                toggleRect.xMax + rowSpacing,
                y,
                position.width - TOGGLE_WIDTH - rowSpacing,
                rowHeight);

            EditorGUI.PropertyField(toggleRect, applyProperty, new GUIContent(applyLabel));
            using (new EditorGUI.DisabledScope(!applyProperty.boolValue))
                EditorGUI.PropertyField(metricRect, metricProperty, new GUIContent(metricLabel));
        }

        private static SerializedProperty FindRequiredProperty(SerializedProperty property, string propertyName)
        {
            var childProperty = property.FindPropertyRelative(propertyName);
            if (childProperty == null)
                throw new InvalidOperationException(
                    $"UI_SkinGridLayoutBindingDrawer expected serialized property '{propertyName}'.");

            return childProperty;
        }
    }
}
