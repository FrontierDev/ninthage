using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Game.Shared.Data;

namespace Game.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(ConditionDefinition), useForChildren: true)]
    public class ConditionDefinitionDrawer : PropertyDrawer
    {
        private static Type[] cachedConditionTypes;
        private static string[] cachedConditionNames;

        static ConditionDefinitionDrawer()
        {
            CacheConditionSubclasses();
        }

        private static void CacheConditionSubclasses()
        {
            var list = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract && typeof(ConditionDefinition).IsAssignableFrom(t))
                .OrderBy(t => t.Name)
                .ToArray();

            cachedConditionTypes = list;
            cachedConditionNames = list.Select(t => ObjectNames.NicifyVariableName(t.Name)).ToArray();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (property.managedReferenceValue == null)
                return height;

            // Add height for child properties
            var iterator = property.Copy();
            var endProp = property.GetEndProperty();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProp))
            {
                enterChildren = false;
                height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var dropdownRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            // Determine current index
            int currentIndex = -1;
            if (property.managedReferenceValue != null)
            {
                var currentType = property.managedReferenceValue.GetType();
                currentIndex = Array.IndexOf(cachedConditionTypes, currentType);
            }

            // Build popup options with "(None)" at index 0
            var options = new string[cachedConditionNames.Length + 1];
            options[0] = "(None)";
            Array.Copy(cachedConditionNames, 0, options, 1, cachedConditionNames.Length);

            int selectedIndex = currentIndex + 1; // shift for (None)

            EditorGUI.BeginChangeCheck();
            int newSelectedIndex = EditorGUI.Popup(dropdownRect, label.text, selectedIndex, options);
            if (EditorGUI.EndChangeCheck())
            {
                if (newSelectedIndex == 0)
                {
                    property.managedReferenceValue = null;
                }
                else
                {
                    var selectedType = cachedConditionTypes[newSelectedIndex - 1];
                    if (property.managedReferenceValue == null || property.managedReferenceValue.GetType() != selectedType)
                    {
                        property.managedReferenceValue = Activator.CreateInstance(selectedType);
                    }
                }
            }

            float y = dropdownRect.yMax + EditorGUIUtility.standardVerticalSpacing;

            // Draw child properties if a condition type is selected
            if (property.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;

                var iterator = property.Copy();
                var endProp = property.GetEndProperty();
                bool enterChildren = true;

                while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProp))
                {
                    enterChildren = false;
                    float h = EditorGUI.GetPropertyHeight(iterator, true);
                    var fieldRect = new Rect(position.x, y, position.width, h);
                    EditorGUI.PropertyField(fieldRect, iterator, true);
                    y += h + EditorGUIUtility.standardVerticalSpacing;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }
}
