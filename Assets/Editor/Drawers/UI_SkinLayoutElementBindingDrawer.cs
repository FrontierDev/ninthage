using Game.Client.UI.Skin;
using System;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Drawers
{
    /// <summary>
    /// Draws one serialized LayoutElement metric mapping.
    /// </summary>
    [CustomPropertyDrawer(typeof(UI_SkinLayoutElementBinding))]
    public sealed class UI_SkinLayoutElementBindingDrawer : PropertyDrawer
    {
        private const float TOGGLE_WIDTH = 100f;

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 5f + EditorGUIUtility.standardVerticalSpacing * 4f;
        }

        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var targetProperty = FindRequiredProperty(property, "_target");
            var applyMinWidthProperty = FindRequiredProperty(property, "_applyMinWidth");
            var minWidthMetricProperty = FindRequiredProperty(property, "_minWidthMetric");
            var applyMinHeightProperty = FindRequiredProperty(property, "_applyMinHeight");
            var minHeightMetricProperty = FindRequiredProperty(property, "_minHeightMetric");
            var applyPreferredWidthProperty = FindRequiredProperty(property, "_applyPreferredWidth");
            var preferredWidthMetricProperty = FindRequiredProperty(property, "_preferredWidthMetric");
            var applyPreferredHeightProperty = FindRequiredProperty(property, "_applyPreferredHeight");
            var preferredHeightMetricProperty = FindRequiredProperty(property, "_preferredHeightMetric");

            EditorGUI.BeginProperty(position, label, property);

            var rowHeight = EditorGUIUtility.singleLineHeight;
            var rowSpacing = EditorGUIUtility.standardVerticalSpacing;
            var targetRect = new Rect(position.x, position.y, position.width, rowHeight);
            var minWidthY = targetRect.yMax + rowSpacing;
            var minHeightY = minWidthY + rowHeight + rowSpacing;
            var preferredWidthY = minHeightY + rowHeight + rowSpacing;
            var preferredHeightY = preferredWidthY + rowHeight + rowSpacing;

            EditorGUI.PropertyField(targetRect, targetProperty, new GUIContent("Target"));
            DrawToggleAndMetric(
                position,
                minWidthY,
                applyMinWidthProperty,
                minWidthMetricProperty,
                "Apply Min Width",
                "Min Width Metric");
            DrawToggleAndMetric(
                position,
                minHeightY,
                applyMinHeightProperty,
                minHeightMetricProperty,
                "Apply Min Height",
                "Min Height Metric");
            DrawToggleAndMetric(
                position,
                preferredWidthY,
                applyPreferredWidthProperty,
                preferredWidthMetricProperty,
                "Apply Preferred Width",
                "Preferred Width Metric");
            DrawToggleAndMetric(
                position,
                preferredHeightY,
                applyPreferredHeightProperty,
                preferredHeightMetricProperty,
                "Apply Preferred Height",
                "Preferred Height Metric");

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
                    $"UI_SkinLayoutElementBindingDrawer expected serialized property '{propertyName}'.");

            return childProperty;
        }
    }
}
