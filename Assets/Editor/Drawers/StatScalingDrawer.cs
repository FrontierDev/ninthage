using UnityEngine;
using UnityEditor;
using Game.Shared.Data;

namespace Game.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(StatScaling))]
    public class StatScalingDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float spacing = 10f;
            float coeffWidth = 60f;
            float statWidth = position.width - coeffWidth - spacing;

            var statRect = new Rect(position.x, position.y, statWidth, EditorGUIUtility.singleLineHeight);
            var coeffRect = new Rect(position.x + statWidth + spacing, position.y, coeffWidth, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(statRect, property.FindPropertyRelative("Stat"), GUIContent.none);
            EditorGUI.PropertyField(coeffRect, property.FindPropertyRelative("Coefficient"), GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}
