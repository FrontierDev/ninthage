using UnityEngine;
using UnityEditor;
using Game.Shared.Data;

namespace Game.Editor.Windows
{
    /// <summary>
    /// Editor window for viewing and managing race definitions.
    /// </summary>
    public class RaceEditorWindow : DataDefinitionEditorWindow<RaceDefinition, RaceDefinitionLibrary>
    {
        private int selectedTab;
        private readonly string[] tabNames = { "General", "Base Stats" };

        private SerializedObject cachedSerializedObject;
        private RaceDefinition cachedDefinition;

        // Pending explorer window opens deferred to next frame
        private System.Action pendingExplorerOpen;

        protected override string DefinitionTypeName => "Race";
        protected override string AssetDirectoryPath => "Assets/Data/RaceDefinitions";
        protected override string AssetPrefix => "Race";

        [MenuItem("Data/Race Definitions")]
        public static void OpenWindow()
        {
            var window = GetWindow<RaceEditorWindow>("Race Definitions");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        protected override RaceDefinitionLibrary GetLibraryInstance()
        {
            return RaceDefinitionLibrary.Instance;
        }

        protected override void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();

            if (Selected != null)
            {
                var serializedDef = GetOrCreateSerializedObject(Selected);

                selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
                EditorGUILayout.Space();

                rightScrollPosition = EditorGUILayout.BeginScrollView(rightScrollPosition);

                switch (selectedTab)
                {
                    case 0: DrawGeneralTab(serializedDef); break;
                    case 1: DrawBaseStatsTab(serializedDef); break;
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
                EditorGUILayout.LabelField("Select a race to view details", EditorStyles.centeredGreyMiniLabel);
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
            EditorGUILayout.LabelField("Race ID:", GUILayout.Width(120));
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

            EditorGUILayout.PropertyField(serializedObject.FindProperty("isPlayable"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultPVPFaction"));

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

        private SerializedObject GetOrCreateSerializedObject(RaceDefinition definition)
        {
            if (cachedDefinition != definition || cachedSerializedObject == null)
            {
                cachedDefinition = definition;
                cachedSerializedObject = new SerializedObject(definition);
            }
            return cachedSerializedObject;
        }

        // --- Inline array helpers ---



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

        protected override bool MatchesSearchFilter(RaceDefinition definition, string lowerFilter)
        {
            return (!string.IsNullOrEmpty(definition.Description) && definition.Description.ToLower().Contains(lowerFilter)) ||
                   (definition.Icon != null && definition.Icon.name.ToLower().Contains(lowerFilter));
        }
    }
}
