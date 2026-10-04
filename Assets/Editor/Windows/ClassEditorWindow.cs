using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Game.Shared.Data;
using Game.Editor.Windows;

namespace Game.Editor.Windows
{
    /// <summary>
    /// Editor window for viewing and managing class definitions.
    /// </summary>
    public class ClassEditorWindow : DataDefinitionEditorWindow<ClassDefinition, ClassDefinitionLibrary>
    {
        private int selectedTab;
        private readonly string[] tabNames = { "General", "Base Stats", "Growth", "Spells", "Focus Stats", "Weapon Types", "Specialisations" };

        private SerializedObject cachedSerializedObject;
        private ClassDefinition cachedDefinition;

        // Pending explorer window opens deferred to next frame
        private System.Action pendingExplorerOpen;

        protected override string DefinitionTypeName => "Class";
        protected override string AssetDirectoryPath => "Assets/Data/ClassDefinitions";
        protected override string AssetPrefix => "Class";

        [MenuItem("Data/Class Definitions")]
        public static void OpenWindow()
        {
            var window = GetWindow<ClassEditorWindow>("Class Definitions");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        protected override ClassDefinitionLibrary GetLibraryInstance()
        {
            return ClassDefinitionLibrary.Instance;
        }

        protected override void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();

            if (Selected != null)
            {
                var serializedDef = GetOrCreateSerializedObject(Selected);

                selectedTab = Mathf.Clamp(selectedTab, 0, tabNames.Length - 1);
                selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
                EditorGUILayout.Space();

                rightScrollPosition = EditorGUILayout.BeginScrollView(rightScrollPosition);

                switch (selectedTab)
                {
                    case 0: DrawGeneralTab(serializedDef); break;
                    case 1: DrawBaseStatsTab(serializedDef); break;
                    case 2: DrawGrowthTab(serializedDef); break;
                    case 3: DrawSpellsTab(serializedDef); break;
                    case 4: DrawFocusStatsTab(serializedDef); break;
                    case 5: DrawWeaponTypesTab(serializedDef); break;
                    case 6: DrawSpecialisationsTab(serializedDef); break;
                }

                if (serializedDef.hasModifiedProperties)
                {
                    serializedDef.ApplyModifiedProperties();
                    EditorUtility.SetDirty(Selected);
                }

                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Select a class to view details", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();

            // Execute any pending explorer window opens after layout is complete
            if (pendingExplorerOpen != null)
            {
                var action = pendingExplorerOpen;
                pendingExplorerOpen = null;
                action.Invoke();
            }
        }

        private void DrawGeneralTab(SerializedObject serializedObject)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Class ID:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("definitionId"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Display Name:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Icon:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"), GUIContent.none, GUILayout.Height(64));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Description:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("description"), GUIContent.none, GUILayout.Height(80));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Asset Information", EditorStyles.boldLabel);
            string assetPath = AssetDatabase.GetAssetPath(Selected);
            if (!string.IsNullOrEmpty(assetPath))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Path:", GUILayout.Width(50));
                EditorGUILayout.SelectableLabel(assetPath, EditorStyles.miniLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeObject = Selected;
                    EditorGUIUtility.PingObject(Selected);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawBaseStatsTab(SerializedObject serializedObject)
        {
            var arrayProp = serializedObject.FindProperty("baseStats");
            DrawStatArrayHeader("Base Stats", arrayProp, () => OpenStatExplorer(arrayProp));

            if (arrayProp.arraySize == 0)
            {
                DrawInlineArrayEmpty();
                return;
            }

            EditorGUI.indentLevel++;
            var bgRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(bgRect, new Color(0f, 0f, 0f, 0.08f));

            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                var element = arrayProp.GetArrayElementAtIndex(i);
                var defProp = element.FindPropertyRelative("definition");
                var valueProp = element.FindPropertyRelative("baseValue");
                var currentDef = defProp.objectReferenceValue as ActorStatDefinition;

                EditorGUILayout.BeginHorizontal();

                string displayText = currentDef != null ? currentDef.DefinitionId : "None";
                EditorGUILayout.LabelField(displayText, EditorStyles.textField, GUILayout.ExpandWidth(true));
                EditorGUILayout.PropertyField(valueProp, GUIContent.none, GUILayout.Width(80));

                bool shouldDelete = DrawInlineRemoveButton();

                EditorGUILayout.EndHorizontal();

                if (shouldDelete)
                {
                    arrayProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                if (i < arrayProp.arraySize - 1)
                    DrawInlineSeparator();
            }

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        private void OpenStatExplorer(SerializedProperty arrayProp)
        {
            var library = ActorStatDefinitionLibrary.Instance;
            if (library == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not load ActorStatDefinitionLibrary", "OK");
                return;
            }

            DefinitionExplorerWindow.Open(library, (selected) =>
            {
                // Check if already in list
                for (int i = 0; i < arrayProp.arraySize; i++)
                {
                    var elem = arrayProp.GetArrayElementAtIndex(i);
                    var defProp = elem.FindPropertyRelative("definition");
                    if (defProp.objectReferenceValue == selected)
                    {
                        EditorUtility.DisplayDialog("Already Added", $"Stat '{selected.DefinitionId}' is already in this list.", "OK");
                        return;
                    }
                }

                // Add new entry
                int newIndex = arrayProp.arraySize;
                arrayProp.arraySize++;
                var newElem = arrayProp.GetArrayElementAtIndex(newIndex);
                newElem.FindPropertyRelative("definition").objectReferenceValue = selected;
                newElem.FindPropertyRelative("baseValue").floatValue = 0f;
                arrayProp.serializedObject.ApplyModifiedProperties();
            });
        }

        private void DrawGrowthTab(SerializedObject serializedObject)
        {
            var arrayProp = serializedObject.FindProperty("statGrowthPerLevel");
            DrawStatArrayHeader("Stat Growth Per Level", arrayProp, () => OpenGrowthStatExplorer(arrayProp));

            if (arrayProp.arraySize == 0)
            {
                DrawInlineArrayEmpty();
                return;
            }

            EditorGUI.indentLevel++;
            var bgRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(bgRect, new Color(0f, 0f, 0f, 0.08f));

            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                var element = arrayProp.GetArrayElementAtIndex(i);
                var statProp = element.FindPropertyRelative("stat");
                var growthProp = element.FindPropertyRelative("flatPerLevel");
                var currentStat = statProp.objectReferenceValue as ActorStatDefinition;

                EditorGUILayout.BeginHorizontal();

                string displayText = currentStat != null ? currentStat.DefinitionId : "None";
                EditorGUILayout.LabelField(displayText, EditorStyles.textField, GUILayout.ExpandWidth(true));
                EditorGUILayout.PropertyField(growthProp, GUIContent.none, GUILayout.Width(80));

                bool shouldDelete = DrawInlineRemoveButton();

                EditorGUILayout.EndHorizontal();

                if (shouldDelete)
                {
                    arrayProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                if (i < arrayProp.arraySize - 1)
                    DrawInlineSeparator();
            }

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        private void OpenGrowthStatExplorer(SerializedProperty arrayProp)
        {
            var library = ActorStatDefinitionLibrary.Instance;
            if (library == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not load ActorStatDefinitionLibrary", "OK");
                return;
            }

            DefinitionExplorerWindow.Open(library, (selected) =>
            {
                // Check if already in list
                for (int i = 0; i < arrayProp.arraySize; i++)
                {
                    var elem = arrayProp.GetArrayElementAtIndex(i);
                    var statProp = elem.FindPropertyRelative("stat");
                    if (statProp.objectReferenceValue == selected)
                    {
                        EditorUtility.DisplayDialog("Already Added", $"Stat '{selected.DefinitionId}' is already in this list.", "OK");
                        return;
                    }
                }

                // Add new entry
                int newIndex = arrayProp.arraySize;
                arrayProp.arraySize++;
                var newElem = arrayProp.GetArrayElementAtIndex(newIndex);
                newElem.FindPropertyRelative("stat").objectReferenceValue = selected;
                newElem.FindPropertyRelative("flatPerLevel").floatValue = 0f;
                arrayProp.serializedObject.ApplyModifiedProperties();
            });
        }

        private void DrawSpellsTab(SerializedObject serializedObject)
        {
            var arrayProp = serializedObject.FindProperty("classSpellList");
            DrawInlineArrayHeader("Class Spells", arrayProp);

            if (arrayProp.arraySize == 0)
            {
                DrawInlineArrayEmpty();
                return;
            }

            var specialisationsProp = serializedObject.FindProperty("specialisations");

            EditorGUI.indentLevel++;
            var bgRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(bgRect, new Color(0f, 0f, 0f, 0.08f));

            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                var element = arrayProp.GetArrayElementAtIndex(i);
                var spellProp = element.FindPropertyRelative("spell");
                var levelProp = element.FindPropertyRelative("levelRequirement");

                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(spellProp, GUIContent.none);
                EditorGUILayout.PropertyField(levelProp, GUIContent.none, GUILayout.Width(60));

                // Build specialisation options (None + each spec name)
                int specCount = specialisationsProp.arraySize;
                string[] options = new string[specCount + 1];
                options[0] = "None";
                for (int s = 0; s < specCount; s++)
                {
                    var specProp = specialisationsProp.GetArrayElementAtIndex(s);
                    var nameProp = specProp.FindPropertyRelative("name");
                    options[s + 1] = string.IsNullOrEmpty(nameProp.stringValue) ? $"Specialisation {s + 1}" : nameProp.stringValue;
                }

                // Determine current selection for this spell
                UnityEngine.Object spellObj = spellProp.objectReferenceValue;
                int currentSelection = 0; // 0 == None
                if (spellObj != null)
                {
                    for (int s = 0; s < specCount; s++)
                    {
                        var specSpells = specialisationsProp.GetArrayElementAtIndex(s).FindPropertyRelative("spells");
                        for (int sp = 0; sp < specSpells.arraySize; sp++)
                        {
                            if (specSpells.GetArrayElementAtIndex(sp).objectReferenceValue == spellObj)
                            {
                                currentSelection = s + 1;
                                break;
                            }
                        }
                        if (currentSelection != 0) break;
                    }
                }

                int newSelection = EditorGUILayout.Popup(currentSelection, options, GUILayout.Width(160));
                if (newSelection != currentSelection)
                {
                    // Remove this spell from all specialisations
                    for (int s = 0; s < specCount; s++)
                    {
                        var specSpells = specialisationsProp.GetArrayElementAtIndex(s).FindPropertyRelative("spells");
                        for (int sp = specSpells.arraySize - 1; sp >= 0; sp--)
                        {
                            var spElem = specSpells.GetArrayElementAtIndex(sp);
                            if (spElem.objectReferenceValue == spellObj)
                            {
                                specSpells.DeleteArrayElementAtIndex(sp);
                                if (sp < specSpells.arraySize && specSpells.GetArrayElementAtIndex(sp).objectReferenceValue == null)
                                    specSpells.DeleteArrayElementAtIndex(sp);
                            }
                        }
                    }

                    // Add to newly selected specialisation (if not None)
                    if (newSelection > 0 && spellObj != null)
                    {
                        var targetSpells = specialisationsProp.GetArrayElementAtIndex(newSelection - 1).FindPropertyRelative("spells");
                        int insertIndex = targetSpells.arraySize;
                        targetSpells.arraySize++;
                        targetSpells.GetArrayElementAtIndex(insertIndex).objectReferenceValue = spellObj;
                    }
                }

                bool shouldDeleteSpell = DrawInlineRemoveButton();

                EditorGUILayout.EndHorizontal();

                if (shouldDeleteSpell)
                {
                    arrayProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                if (i < arrayProp.arraySize - 1)
                    DrawInlineSeparator();
            }

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        private void DrawFocusStatsTab(SerializedObject serializedObject)
        {
            DrawStatsListWithExplorer("Focus Stats", serializedObject.FindProperty("focusStats"));
            EditorGUILayout.Space(10);
            DrawStatsListWithExplorer("Relevant Stats", serializedObject.FindProperty("relevantStats"));
        }

        private void DrawStatsListWithExplorer(string label, SerializedProperty arrayProp)
        {
            DrawStatArrayHeader(label, arrayProp, () => OpenStatsListExplorer(arrayProp));

            if (arrayProp.arraySize == 0)
            {
                DrawInlineArrayEmpty();
                return;
            }

            EditorGUI.indentLevel++;
            var bgRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(bgRect, new Color(0f, 0f, 0f, 0.08f));

            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                var element = arrayProp.GetArrayElementAtIndex(i);
                var stat = element.objectReferenceValue as ActorStatDefinition;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(stat != null ? stat.DefinitionId : "None", EditorStyles.textField, GUILayout.ExpandWidth(true));

                bool shouldDelete = DrawInlineRemoveButton();

                EditorGUILayout.EndHorizontal();

                if (shouldDelete)
                {
                    arrayProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                if (i < arrayProp.arraySize - 1)
                    DrawInlineSeparator();
            }

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        private void OpenStatsListExplorer(SerializedProperty arrayProp)
        {
            var library = ActorStatDefinitionLibrary.Instance;
            if (library == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not load ActorStatDefinitionLibrary", "OK");
                return;
            }

            DefinitionExplorerWindow.Open(library, (selected) =>
            {
                // Check if already in list
                for (int i = 0; i < arrayProp.arraySize; i++)
                {
                    if (arrayProp.GetArrayElementAtIndex(i).objectReferenceValue == selected)
                    {
                        EditorUtility.DisplayDialog("Already Added", $"Stat '{selected.DefinitionId}' is already in this list.", "OK");
                        return;
                    }
                }

                // Add new entry
                int newIndex = arrayProp.arraySize;
                arrayProp.arraySize++;
                arrayProp.GetArrayElementAtIndex(newIndex).objectReferenceValue = selected;
                arrayProp.serializedObject.ApplyModifiedProperties();
            });
        }

        private void DrawWeaponTypesTab(SerializedObject serializedObject)
        {
            DrawInlineList("Weapon Types", serializedObject.FindProperty("weaponTypes"));
        }

        private void DrawSpecialisationsTab(SerializedObject serializedObject)
        {
            var arrayProp = serializedObject.FindProperty("specialisations");
            DrawInlineArrayHeader("Specialisations", arrayProp);

            if (arrayProp.arraySize == 0)
            {
                DrawInlineArrayEmpty();
                return;
            }

            EditorGUI.indentLevel++;
            var bgRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(bgRect, new Color(0f, 0f, 0f, 0.08f));

            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                var element = arrayProp.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(element.FindPropertyRelative("name"), new GUIContent("Name"));

                bool shouldDelete = DrawInlineRemoveButton();

                EditorGUILayout.EndHorizontal();

                if (shouldDelete)
                {
                    arrayProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.PropertyField(element.FindPropertyRelative("description"), new GUIContent("Description"), GUILayout.Height(40));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("icon"), new GUIContent("Icon"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("talents"), new GUIContent("Talents"), true);

                if (i < arrayProp.arraySize - 1)
                    DrawInlineSeparator();
            }

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        private SerializedObject GetOrCreateSerializedObject(ClassDefinition definition)
        {
            if (cachedDefinition != definition || cachedSerializedObject == null)
            {
                cachedDefinition = definition;
                cachedSerializedObject = new SerializedObject(definition);
            }
            return cachedSerializedObject;
        }

        // --- Inline array helpers ---

        private void DrawInlineList(string label, SerializedProperty arrayProp)
        {
            DrawInlineArrayHeader(label, arrayProp);

            if (arrayProp.arraySize == 0)
            {
                DrawInlineArrayEmpty();
                return;
            }

            EditorGUI.indentLevel++;
            var bgRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(bgRect, new Color(0f, 0f, 0f, 0.08f));

            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                var element = arrayProp.GetArrayElementAtIndex(i);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(element, GUIContent.none, GUILayout.ExpandWidth(true));

                bool shouldDelete = DrawInlineRemoveButton();

                EditorGUILayout.EndHorizontal();

                if (shouldDelete)
                {
                    arrayProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                if (i < arrayProp.arraySize - 1)
                    DrawInlineSeparator();
            }

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        private void DrawInlineArrayHeader(string label, SerializedProperty arrayProp)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{label} ({arrayProp.arraySize})", EditorStyles.boldLabel);
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Plus"), GUIStyle.none, GUILayout.Width(18), GUILayout.Height(18)))
            {
                arrayProp.arraySize++;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatArrayHeader(string label, SerializedProperty arrayProp, System.Action onAddClicked = null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{label} ({arrayProp.arraySize})", EditorStyles.boldLabel);

            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Plus"), GUIStyle.none, GUILayout.Width(18), GUILayout.Height(18)))
            {
                pendingExplorerOpen = onAddClicked;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawInlineArrayEmpty()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Empty", EditorStyles.centeredGreyMiniLabel);
            EditorGUI.indentLevel--;
        }

        private bool DrawInlineRemoveButton()
        {
            return GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Minus"), GUIStyle.none, GUILayout.Width(18), GUILayout.Height(18));
        }

        private void DrawInlineSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }

        protected override void DrawDefinitionFields(SerializedObject serializedObject) { }

        protected override bool MatchesSearchFilter(ClassDefinition definition, string lowerFilter)
        {
            return (!string.IsNullOrEmpty(definition.Description) && definition.Description.ToLower().Contains(lowerFilter)) ||
                   (definition.Icon != null && definition.Icon.name.ToLower().Contains(lowerFilter));
        }
    }
}
