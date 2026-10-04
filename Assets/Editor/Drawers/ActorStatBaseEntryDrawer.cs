using UnityEngine;
using UnityEditor;
using Game.Shared;
using Game.Shared.Data;

namespace Game.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(ActorStatBaseEntry))]
    public class ActorStatBaseEntryDrawer : PropertyDrawer
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

            var defProp = property.FindPropertyRelative("definition");
            var currentDef = defProp.objectReferenceValue as ActorStatDefinition;

            // Display current selection or "None"
            string displayText = currentDef != null ? currentDef.DefinitionId : "None";
            EditorGUI.LabelField(statRect, displayText, EditorStyles.textField);

            // Value field
            EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("baseValue"), GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}
