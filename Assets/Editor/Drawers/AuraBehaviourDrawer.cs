using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Game.Shared.Data;

namespace Game.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(AuraBehaviour))]
    public class AuraBehaviourDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded || property.objectReferenceValue == null)
                return height;

            var behaviour = property.objectReferenceValue as AuraBehaviour;
            if (behaviour != null && !string.IsNullOrEmpty(behaviour.Description))
            {
                height += EditorGUIUtility.standardVerticalSpacing;
                height += EditorStyles.helpBox.CalcHeight(new GUIContent(behaviour.Description), 300f);
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.ObjectField(headerRect, property, typeof(AuraBehaviour), label);

            if (property.isExpanded && property.objectReferenceValue != null)
            {
                var behaviour = property.objectReferenceValue as AuraBehaviour;
                if (behaviour != null && !string.IsNullOrEmpty(behaviour.Description))
                {
                    float y = headerRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                    float helpHeight = EditorStyles.helpBox.CalcHeight(new GUIContent(behaviour.Description), position.width);
                    var helpRect = new Rect(position.x, y, position.width, helpHeight);
                    EditorGUI.HelpBox(helpRect, behaviour.Description, MessageType.Info);
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
