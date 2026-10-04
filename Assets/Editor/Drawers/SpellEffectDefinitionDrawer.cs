using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Game.Shared.Data;

namespace Game.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(SpellEffectDefinition))]
    public class SpellEffectDefinitionDrawer : PropertyDrawer
    {
        private static Type[] cachedTypes;
        private static string[] cachedNames;

        static SpellEffectDefinitionDrawer()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => !t.IsAbstract && typeof(SpellEffectDefinition).IsAssignableFrom(t))
                .OrderBy(t => t.Name)
                .ToArray();

            cachedTypes = types;
            cachedNames = types.Select(t => ObjectNames.NicifyVariableName(t.Name)).ToArray();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (property.managedReferenceValue == null)
                return height;

            var iterator = property.Copy();
            var end = property.GetEndProperty();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;
                height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
            }
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var options = new string[cachedNames.Length + 1];
            options[0] = "(None)";
            Array.Copy(cachedNames, 0, options, 1, cachedNames.Length);

            int currentIndex = -1;
            if (property.managedReferenceValue != null)
                currentIndex = Array.IndexOf(cachedTypes, property.managedReferenceValue.GetType());

            var dropdownRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(dropdownRect, label.text, currentIndex + 1, options);
            if (EditorGUI.EndChangeCheck())
            {
                if (newIndex == 0)
                    property.managedReferenceValue = null;
                else
                    property.managedReferenceValue = Activator.CreateInstance(cachedTypes[newIndex - 1]);
            }

            if (property.managedReferenceValue != null)
            {
                float y = dropdownRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                var iterator = property.Copy();
                var end = property.GetEndProperty();
                bool enterChildren = true;
                EditorGUI.indentLevel++;
                while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
                {
                    enterChildren = false;
                    float h = EditorGUI.GetPropertyHeight(iterator, true);
                    EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), iterator, true);
                    y += h + EditorGUIUtility.standardVerticalSpacing;
                }
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }
}
