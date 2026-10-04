using UnityEngine;
using UnityEditor;
using Game.Shared.Data;

namespace Game.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(ClassSpell))]
    public class ClassSpellDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float spacing = 10f;
            float levelWidth = 60f;
            float spellWidth = position.width - levelWidth - spacing;

            var spellRect = new Rect(position.x, position.y, spellWidth, EditorGUIUtility.singleLineHeight);
            var levelRect = new Rect(position.x + spellWidth + spacing, position.y, levelWidth, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(spellRect, property.FindPropertyRelative("spell"), GUIContent.none);
            EditorGUI.PropertyField(levelRect, property.FindPropertyRelative("levelRequirement"), GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}
