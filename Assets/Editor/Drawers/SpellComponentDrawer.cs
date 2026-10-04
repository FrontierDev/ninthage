using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Game.Shared.Data;

namespace Game.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(SpellComponent))]
    public class SpellComponentDrawer : PropertyDrawer
    {
        private static Type[] cachedEffectTypes;
        private static string[] cachedEffectNames;

        private static Type[] cachedTargetTypes;
        private static string[] cachedTargetNames;

        private const float SectionSeparatorHeight = 6f;
        private const float SectionBottomPadding = 4f;
        private const float ArrayElementSeparatorHeight = 1f;
        private static float SectionHeaderHeight => EditorGUIUtility.singleLineHeight;

        private static readonly Color SectionSeparatorColor = new(0f, 0f, 0f, 0.15f);
        private static readonly Color SectionHeaderBgColor = new(0f, 0f, 0f, 0.06f);
        private static readonly Color ArrayElementSeparatorColor = new(0f, 0f, 0f, 0.12f);

        static SpellComponentDrawer()
        {
            CacheSubclasses<SpellEffectDefinition>(out cachedEffectTypes, out cachedEffectNames);
            CacheSubclasses<SpellTargetDefinition>(out cachedTargetTypes, out cachedTargetNames);
        }

        private static void CacheSubclasses<T>(out Type[] types, out string[] names)
        {
            var list = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => !t.IsAbstract && typeof(T).IsAssignableFrom(t))
                .OrderBy(t => t.Name)
                .ToArray();

            types = list;
            names = list.Select(t => ObjectNames.NicifyVariableName(t.Name)).ToArray();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight; // foldout row

            if (!property.isExpanded)
                return height;

            height += EditorGUIUtility.standardVerticalSpacing;

            // Cast phase
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Target section: separator + header + dropdown + fields + bottom padding
            height += SectionSeparatorHeight;
            height += SectionHeaderHeight + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var targetProp = property.FindPropertyRelative("TargetDefinition");
            if (targetProp != null && targetProp.managedReferenceValue != null)
            {
                height += GetChildrenHeight(targetProp);
            }
            height += SectionBottomPadding;

            // Effect section: separator + header + dropdown + fields + bottom padding
            height += SectionSeparatorHeight;
            height += SectionHeaderHeight + EditorGUIUtility.standardVerticalSpacing;
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            var effectProp = property.FindPropertyRelative("EffectDefinition");
            if (effectProp != null && effectProp.managedReferenceValue != null)
            {
                height += GetChildrenHeight(effectProp);
            }
            height += SectionBottomPadding;

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var effectProp = property.FindPropertyRelative("EffectDefinition");
            var targetProp = property.FindPropertyRelative("TargetDefinition");

            // Build a nicer label from the concrete types
            string effectName = effectProp?.managedReferenceValue != null
                ? ObjectNames.NicifyVariableName(effectProp.managedReferenceValue.GetType().Name)
                : "(No Effect)";
            string targetName = targetProp?.managedReferenceValue != null
                ? ObjectNames.NicifyVariableName(targetProp.managedReferenceValue.GetType().Name)
                : "(No Target)";

            var foldoutLabel = new GUIContent($"{effectName}  \u2192  {targetName}");

            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, foldoutLabel, true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            float y = foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.indentLevel++;

            // --- Cast Phase ---
            var castPhaseRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            var castPhaseProp = property.FindPropertyRelative("CastPhase");
            EditorGUI.PropertyField(castPhaseRect, castPhaseProp);
            y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // --- Target ---
            y = DrawSection(position, y, targetProp, "Target", cachedTargetTypes, cachedTargetNames);

            // --- Effect ---
            y = DrawSection(position, y, effectProp, "Effect", cachedEffectTypes, cachedEffectNames);

            EditorGUI.indentLevel--;

            EditorGUI.EndProperty();
        }

        private float DrawSection(
            Rect position, float y,
            SerializedProperty prop, string label,
            Type[] types, string[] names)
        {
            float indent = EditorGUI.indentLevel * 15f;

            // Separator line
            var separatorRect = new Rect(position.x + indent, y + SectionSeparatorHeight * 0.5f - 0.5f,
                position.width - indent, 1f);
            EditorGUI.DrawRect(separatorRect, SectionSeparatorColor);
            y += SectionSeparatorHeight;

            // Section header background + label
            var headerBgRect = new Rect(position.x + indent, y, position.width - indent, SectionHeaderHeight);
            EditorGUI.DrawRect(headerBgRect, SectionHeaderBgColor);
            var headerLabelRect = new Rect(position.x + indent + 4f, y, position.width - indent - 4f, SectionHeaderHeight);
            EditorGUI.LabelField(headerLabelRect, label, EditorStyles.boldLabel);
            y += SectionHeaderHeight + EditorGUIUtility.standardVerticalSpacing;

            y = DrawSerializeReferenceField(position, y, prop, label, types, names);

            y += SectionBottomPadding;
            return y;
        }

        private float DrawSerializeReferenceField(
            Rect position, float y,
            SerializedProperty prop, string label,
            Type[] types, string[] names)
        {
            var dropdownRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);

            // Determine current index
            int currentIndex = -1;
            if (prop.managedReferenceValue != null)
            {
                var currentType = prop.managedReferenceValue.GetType();
                currentIndex = Array.IndexOf(types, currentType);
            }

            // Build popup options with "(None)" at index 0
            var options = new string[names.Length + 1];
            options[0] = "(None)";
            Array.Copy(names, 0, options, 1, names.Length);

            int selectedIndex = currentIndex + 1; // shift for (None)

            EditorGUI.BeginChangeCheck();
            int newSelectedIndex = EditorGUI.Popup(dropdownRect, label, selectedIndex, options);
            if (EditorGUI.EndChangeCheck())
            {
                if (newSelectedIndex == 0)
                {
                    prop.managedReferenceValue = null;
                }
                else
                {
                    var selectedType = types[newSelectedIndex - 1];
                    // Only replace if type actually changed
                    if (prop.managedReferenceValue == null || prop.managedReferenceValue.GetType() != selectedType)
                    {
                        prop.managedReferenceValue = Activator.CreateInstance(selectedType);
                    }
                }
            }

            y = dropdownRect.yMax + EditorGUIUtility.standardVerticalSpacing;

            // Draw inline fields of the concrete type
            if (prop.managedReferenceValue != null)
            {
                var iterator = prop.Copy();
                var endProp = prop.GetEndProperty();
                bool enterChildren = true;
                bool drawnProjectileHeader = false;

                while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProp))
                {
                    enterChildren = false;

                    // Insert Projectile sub-header before the first projectile field
                    if (!drawnProjectileHeader && IsProjectileField(iterator.name))
                    {
                        drawnProjectileHeader = true;
                        float indent = EditorGUI.indentLevel * 15f;

                        var subSepRect = new Rect(position.x + indent, y + SectionSeparatorHeight * 0.5f - 0.5f,
                            position.width - indent, 1f);
                        EditorGUI.DrawRect(subSepRect, SectionSeparatorColor);
                        y += SectionSeparatorHeight;

                        var subHeaderBgRect = new Rect(position.x + indent, y, position.width - indent, SectionHeaderHeight);
                        EditorGUI.DrawRect(subHeaderBgRect, SectionHeaderBgColor);
                        var subHeaderRect = new Rect(position.x + indent + 4f, y, position.width - indent - 4f, SectionHeaderHeight);
                        EditorGUI.LabelField(subHeaderRect, "Projectile", EditorStyles.boldLabel);
                        y += SectionHeaderHeight + EditorGUIUtility.standardVerticalSpacing;
                    }

                    if (iterator.isArray && iterator.propertyType == SerializedPropertyType.Generic)
                    {
                        y = DrawInlineArray(position, y, iterator);
                    }
                    else
                    {
                        float h = EditorGUI.GetPropertyHeight(iterator, true);
                        var fieldRect = new Rect(position.x, y, position.width, h);
                        EditorGUI.PropertyField(fieldRect, iterator, true);
                        y += h + EditorGUIUtility.standardVerticalSpacing;
                    }
                }
            }

            return y;
        }

        private float DrawInlineArray(Rect position, float y, SerializedProperty arrayProp)
        {
            float indent = EditorGUI.indentLevel * 15f;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float buttonSize = 18f;

            // Header row: label + count + add button
            var labelRect = new Rect(position.x, y, position.width - buttonSize - 4, lineHeight);
            EditorGUI.LabelField(labelRect, $"{arrayProp.displayName} ({arrayProp.arraySize})", EditorStyles.boldLabel);

            var addRect = new Rect(position.x + position.width - buttonSize, y, buttonSize, lineHeight);
            if (GUI.Button(addRect, EditorGUIUtility.IconContent("d_Toolbar Plus"), GUIStyle.none))
            {
                arrayProp.arraySize++;
            }
            y += lineHeight + spacing;

            if (arrayProp.arraySize == 0)
            {
                EditorGUI.indentLevel++;
                var emptyRect = new Rect(position.x, y, position.width, lineHeight);
                EditorGUI.LabelField(emptyRect, "Empty", EditorStyles.centeredGreyMiniLabel);
                y += lineHeight + spacing;
                EditorGUI.indentLevel--;
                return y;
            }

            EditorGUI.indentLevel++;

            // Background box
            float elementsStartY = y;
            float elementsHeight = 0f;
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                if (i > 0)
                    elementsHeight += ArrayElementSeparatorHeight + spacing;
                elementsHeight += EditorGUI.GetPropertyHeight(arrayProp.GetArrayElementAtIndex(i), GUIContent.none, true) + spacing;
            }

            var bgRect = new Rect(position.x + indent, elementsStartY - 2, position.width - indent, elementsHeight + 4);
            EditorGUI.DrawRect(bgRect, new Color(0f, 0f, 0f, 0.08f));

            // Draw each element
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                // Separator between elements
                if (i > 0)
                {
                    var sepRect = new Rect(position.x + indent, y, position.width - indent, ArrayElementSeparatorHeight);
                    EditorGUI.DrawRect(sepRect, ArrayElementSeparatorColor);
                    y += ArrayElementSeparatorHeight + spacing;
                }

                var element = arrayProp.GetArrayElementAtIndex(i);
                float h = EditorGUI.GetPropertyHeight(element, GUIContent.none, true);

                var elementRect = new Rect(position.x, y, position.width - buttonSize - 4, h);
                EditorGUI.PropertyField(elementRect, element, GUIContent.none, true);

                // Remove button
                var removeRect = new Rect(position.x + position.width - buttonSize, y + (h - lineHeight) * 0.5f, buttonSize, lineHeight);
                if (GUI.Button(removeRect, EditorGUIUtility.IconContent("d_Toolbar Minus"), GUIStyle.none))
                {
                    arrayProp.DeleteArrayElementAtIndex(i);
                    i--;
                    EditorGUI.indentLevel--;
                    return DrawInlineArray(position, elementsStartY - lineHeight - spacing, arrayProp); // redraw after delete
                }

                y += h + spacing;
            }

            EditorGUI.indentLevel--;

            return y;
        }

        private float GetChildrenHeight(SerializedProperty prop)
        {
            float height = 0f;
            var iterator = prop.Copy();
            var endProp = prop.GetEndProperty();
            bool enterChildren = true;
            bool countedProjectileHeader = false;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProp))
            {
                enterChildren = false;

                if (!countedProjectileHeader && IsProjectileField(iterator.name))
                {
                    countedProjectileHeader = true;
                    height += SectionSeparatorHeight + SectionHeaderHeight + EditorGUIUtility.standardVerticalSpacing;
                }

                if (iterator.isArray && iterator.propertyType == SerializedPropertyType.Generic)
                {
                    height += GetInlineArrayHeight(iterator);
                }
                else
                {
                    height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            return height;
        }

        private float GetInlineArrayHeight(SerializedProperty arrayProp)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            float height = lineHeight + spacing; // header row

            if (arrayProp.arraySize == 0)
            {
                height += lineHeight + spacing; // "Empty" label
                return height;
            }

            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                if (i > 0)
                    height += ArrayElementSeparatorHeight + spacing;
                var element = arrayProp.GetArrayElementAtIndex(i);
                height += EditorGUI.GetPropertyHeight(element, GUIContent.none, true) + spacing;
            }

            return height;
        }

        private static bool IsProjectileField(string fieldName)
        {
            return fieldName.StartsWith("Projectile", StringComparison.Ordinal)
                || fieldName == "UsesProjectile";
        }
    }
}
