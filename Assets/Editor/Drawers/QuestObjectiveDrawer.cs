using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Game.Shared.Data;

namespace Game.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(QuestObjective), useForChildren: true)]
    public class QuestObjectiveDrawer : PropertyDrawer
    {
        private static Type[] cachedTypes;
        private static string[] cachedNames;

        static QuestObjectiveDrawer()
        {
            var list = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => !t.IsAbstract && typeof(QuestObjective).IsAssignableFrom(t))
                .OrderBy(t => t.Name)
                .ToArray();

            cachedTypes = list;
            cachedNames = list.Select(t => ObjectNames.NicifyVariableName(t.Name)).ToArray();
        }

        private static readonly Color SeparatorColor = new(0f, 0f, 0f, 0.15f);
        private static readonly Color HeaderBgColor = new(0f, 0f, 0f, 0.06f);
        private const float HeaderHeight = 18f;
        private const float Padding = 4f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Type dropdown row
            float height = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (property.managedReferenceValue == null)
                return height;

            // Section header
            height += HeaderHeight + EditorGUIUtility.standardVerticalSpacing;

            // All child fields
            var iterator = property.Copy();
            var end = property.GetEndProperty();
            bool enter = true;
            while (iterator.NextVisible(enter) && !SerializedProperty.EqualContents(iterator, end))
            {
                enter = false;
                height += EditorGUI.GetPropertyHeight(iterator, true) + EditorGUIUtility.standardVerticalSpacing;
            }

            height += Padding;
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float y = position.y;

            // --- Type dropdown ---
            int currentIndex = -1;
            if (property.managedReferenceValue != null)
            {
                var current = property.managedReferenceValue.GetType();
                currentIndex = Array.IndexOf(cachedTypes, current);
            }

            var options = new string[cachedNames.Length + 1];
            options[0] = "(None)";
            Array.Copy(cachedNames, 0, options, 1, cachedNames.Length);
            int selectedIndex = currentIndex + 1;

            var dropdownRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(dropdownRect, "Objective Type", selectedIndex, options);
            if (EditorGUI.EndChangeCheck())
            {
                property.managedReferenceValue = newIndex == 0
                    ? null
                    : Activator.CreateInstance(cachedTypes[newIndex - 1]);
            }

            y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (property.managedReferenceValue == null)
            {
                EditorGUI.EndProperty();
                return;
            }

            // --- Section header ---
            string typeName = ObjectNames.NicifyVariableName(property.managedReferenceValue.GetType().Name);
            var headerBg = new Rect(position.x, y, position.width, HeaderHeight);
            EditorGUI.DrawRect(headerBg, HeaderBgColor);
            var sepLine = new Rect(position.x, y - 1f, position.width, 1f);
            EditorGUI.DrawRect(sepLine, SeparatorColor);
            EditorGUI.LabelField(new Rect(position.x + 4f, y, position.width - 4f, HeaderHeight), typeName, EditorStyles.boldLabel);
            y += HeaderHeight + EditorGUIUtility.standardVerticalSpacing;

            // --- Child fields ---
            EditorGUI.indentLevel++;

            var iterator = property.Copy();
            var end = property.GetEndProperty();
            bool enter = true;
            while (iterator.NextVisible(enter) && !SerializedProperty.EqualContents(iterator, end))
            {
                enter = false;
                float h = EditorGUI.GetPropertyHeight(iterator, true);

                // For Quest_KillObjective.targetNpcID, show an NPC prefab picker instead of a raw string
                if (iterator.name == "targetNpcID")
                {
                    DrawNpcIDField(new Rect(position.x, y, position.width, h), iterator);
                }
                // For Quest_ItemObjective.targetItemID, show an ItemDefinition picker
                else if (iterator.name == "targetItemID")
                {
                    DrawItemIDField(new Rect(position.x, y, position.width, h), iterator);
                }
                else
                {
                    EditorGUI.PropertyField(new Rect(position.x, y, position.width, h), iterator, true);
                }

                y += h + EditorGUIUtility.standardVerticalSpacing;
            }

            EditorGUI.indentLevel--;

            EditorGUI.EndProperty();
        }

        // Draws an NPCActor prefab picker and writes its NpcID string back to the property.
        private static void DrawNpcIDField(Rect rect, SerializedProperty prop)
        {
            float labelWidth = EditorGUIUtility.labelWidth;
            var labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
            var fieldRect = new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, rect.height);

            EditorGUI.LabelField(labelRect, "Target NPC");

            // Try to resolve the currently stored ID back to a prefab for display
            string currentId = prop.stringValue;
            GameObject currentPrefab = null;
            if (!string.IsNullOrEmpty(currentId))
            {
                currentPrefab = FindNpcPrefabById(currentId);
            }

            EditorGUI.BeginChangeCheck();
            var picked = (GameObject)EditorGUI.ObjectField(fieldRect, currentPrefab, typeof(GameObject), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (picked != null && picked.TryGetComponent<Game.Shared.NPCActor>(out var npc))
                    prop.stringValue = npc.NpcID ?? string.Empty;
                else
                    prop.stringValue = string.Empty;
            }
        }

        // Draws an ItemDefinition asset picker and writes its DefinitionId back to the property.
        private static void DrawItemIDField(Rect rect, SerializedProperty prop)
        {
            float labelWidth = EditorGUIUtility.labelWidth;
            var labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
            var fieldRect = new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, rect.height);

            EditorGUI.LabelField(labelRect, "Target Item");

            string currentId = prop.stringValue;
            ItemDefinition currentItem = null;
            if (!string.IsNullOrEmpty(currentId))
            {
                currentItem = AssetDatabase.FindAssets("t:ItemDefinition")
                    .Select(g => AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                    .FirstOrDefault(i => i != null && i.DefinitionId == currentId);
            }

            EditorGUI.BeginChangeCheck();
            var picked = (ItemDefinition)EditorGUI.ObjectField(fieldRect, currentItem, typeof(ItemDefinition), false);
            if (EditorGUI.EndChangeCheck())
                prop.stringValue = picked != null ? picked.DefinitionId : string.Empty;
        }

        private static GameObject FindNpcPrefabById(string npcId)
        {
            return AssetDatabase.FindAssets("t:Prefab")
                .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(go =>
                {
                    if (go == null) return false;
                    if (!go.TryGetComponent<Game.Shared.NPCActor>(out var npc)) return false;
                    return npc.NpcID == npcId;
                });
        }
    }
}
