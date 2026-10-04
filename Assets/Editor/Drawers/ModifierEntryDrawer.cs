using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Game.Shared.Data;

namespace Game.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(ModifierEntry))]
    public class ModifierEntryDrawer : PropertyDrawer
    {
        private static Type[] cachedModifierTypes;
        private static string[] cachedModifierNames;

        private const float SectionBottomPadding = 4f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return height;

            height += EditorGUIUtility.standardVerticalSpacing;

            var modProp = property.FindPropertyRelative("Modifier");

            // type popup
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (modProp != null && modProp.managedReferenceValue != null)
                height += GetChildrenHeight(modProp);

            height += SectionBottomPadding;
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var modProp = property.FindPropertyRelative("Modifier");

            string typeName = modProp != null && modProp.managedReferenceValue != null
                ? ObjectNames.NicifyVariableName(modProp.managedReferenceValue.GetType().Name)
                : "(None)";

            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, typeName, true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            float y = foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing;

            EnsureCachedModifierTypes();

            var popupRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            int currentIndex = -1;
            if (modProp != null && modProp.managedReferenceValue != null)
                currentIndex = Array.IndexOf(cachedModifierTypes, modProp.managedReferenceValue.GetType());

            var options = new string[cachedModifierNames.Length + 1];
            options[0] = "(None)";
            Array.Copy(cachedModifierNames, 0, options, 1, cachedModifierNames.Length);

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(popupRect, "Type", currentIndex + 1, options);
            if (EditorGUI.EndChangeCheck())
            {
                if (newIndex == 0)
                    modProp.managedReferenceValue = null;
                else
                    modProp.managedReferenceValue = Activator.CreateInstance(cachedModifierTypes[newIndex - 1]);
            }

            y = popupRect.yMax + EditorGUIUtility.standardVerticalSpacing;

            if (modProp != null && modProp.managedReferenceValue != null)
            {
                var iterator = modProp.Copy();
                var endProp = modProp.GetEndProperty();
                bool enterChildren = true;

                while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProp))
                {
                    enterChildren = false;
                    float h = EditorGUI.GetPropertyHeight(iterator, true);
                    var fieldRect = new Rect(position.x, y, position.width, h);
                    EditorGUI.PropertyField(fieldRect, iterator, true);
                    y += h + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            EditorGUI.EndProperty();
        }

        private void EnsureCachedModifierTypes()
        {
            if (cachedModifierTypes != null) return;
            cachedModifierTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => !t.IsAbstract && typeof(Game.Shared.ISpellModifier).IsAssignableFrom(t))
                .OrderBy(t => t.Name)
                .ToArray();
            cachedModifierNames = cachedModifierTypes.Select(t => ObjectNames.NicifyVariableName(t.Name)).ToArray();
        }

        private float GetChildrenHeight(SerializedProperty prop)
        {
            float height = 0f;
            var iterator = prop.Copy();
            var endProp = prop.GetEndProperty();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProp))
            {
                enterChildren = false;
                height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }
    }
}
