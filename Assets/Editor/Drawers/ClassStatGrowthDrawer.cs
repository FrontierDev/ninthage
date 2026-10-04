using UnityEngine;
using UnityEditor;
using Game.Shared.Data;

namespace Game.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(ClassStatGrowth))]
    public class ClassStatGrowthDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float spacing = 10f;
            float valueWidth = 60f;
            float statWidth = position.width - valueWidth - spacing;

            var statRect = new Rect(position.x, position.y, statWidth, EditorGUIUtility.singleLineHeight);
            var valueRect = new Rect(position.x + statWidth + spacing, position.y, valueWidth, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(statRect, property.FindPropertyRelative("stat"), GUIContent.none);
            EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("flatPerLevel"), GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}
