using Game.Client.UI.Skin;
using System;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Drawers
{
    /// <summary>
    /// Draws one serialized RectTransform metric mapping.
    /// </summary>
    [CustomPropertyDrawer(typeof(UI_SkinRectTransformBinding))]
    public sealed class UI_SkinRectTransformBindingDrawer : PropertyDrawer
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
            var applyWidthProperty = FindRequiredProperty(property, "_applyWidth");
            var widthMetricProperty = FindRequiredProperty(property, "_widthMetric");
            var applyHeightProperty = FindRequiredProperty(property, "_applyHeight");
            var heightMetricProperty = FindRequiredProperty(property, "_heightMetric");

            EditorGUI.BeginProperty(position, label, property);

            var rowHeight = EditorGUIUtility.singleLineHeight;
            var rowSpacing = EditorGUIUtility.standardVerticalSpacing;
            var targetRect = new Rect(position.x, position.y, position.width, rowHeight);
            var widthY = targetRect.yMax + rowSpacing;
            var heightY = widthY + rowHeight + rowSpacing;
            var widthToggleRect = new Rect(position.x, widthY, TOGGLE_WIDTH, rowHeight);
            var widthMetricRect = new Rect(
                widthToggleRect.xMax + rowSpacing,
                widthY,
                position.width - TOGGLE_WIDTH - rowSpacing,
                rowHeight);
            var heightToggleRect = new Rect(position.x, heightY, TOGGLE_WIDTH, rowHeight);
            var heightMetricRect = new Rect(
                heightToggleRect.xMax + rowSpacing,
                heightY,
                position.width - TOGGLE_WIDTH - rowSpacing,
                rowHeight);

            EditorGUI.PropertyField(targetRect, targetProperty, new GUIContent("Target"));
            EditorGUI.PropertyField(widthToggleRect, applyWidthProperty, new GUIContent("Apply Width"));
            using (new EditorGUI.DisabledScope(!applyWidthProperty.boolValue))
                EditorGUI.PropertyField(widthMetricRect, widthMetricProperty, new GUIContent("Width Metric"));

            EditorGUI.PropertyField(heightToggleRect, applyHeightProperty, new GUIContent("Apply Height"));
            using (new EditorGUI.DisabledScope(!applyHeightProperty.boolValue))
                EditorGUI.PropertyField(heightMetricRect, heightMetricProperty, new GUIContent("Height Metric"));

            EditorGUI.EndProperty();
        }

        private static SerializedProperty FindRequiredProperty(SerializedProperty property, string propertyName)
        {
            var childProperty = property.FindPropertyRelative(propertyName);
            if (childProperty == null)
                throw new InvalidOperationException(
                    $"UI_SkinRectTransformBindingDrawer expected serialized property '{propertyName}'.");

            return childProperty;
        }
    }
}
