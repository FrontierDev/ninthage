using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Game.Shared.Data;

namespace Game.Editor.Windows
{
    /// <summary>
    /// Editor window for viewing and managing faction definitions.
    /// </summary>
    public class FactionEditorWindow : DataDefinitionEditorWindow<FactionDefinition, FactionDefinitionLibrary>
    {
        private int selectedTab;
        private readonly string[] tabNames = { "General", "Appearance", "Relations", "Rewards" };

        private ReorderableList alliedList;
        private ReorderableList enemyList;

        private SerializedObject cachedSerializedObject;
        private FactionDefinition cachedDefinition;

        protected override string DefinitionTypeName => "Faction";
        protected override string AssetDirectoryPath => "Assets/Data/FactionDefinitions";
        protected override string AssetPrefix => "Faction";

        [MenuItem("Data/Faction Definitions")]
        public static void OpenWindow()
        {
            var window = GetWindow<FactionEditorWindow>("Faction Definitions");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        protected override FactionDefinitionLibrary GetLibraryInstance()
        {
            return FactionDefinitionLibrary.Instance;
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
                    case 1: DrawAppearanceTab(serializedDef); break;
                    case 2: DrawRelationsTab(serializedDef); break;
                    case 3: DrawRewardsTab(serializedDef); break;
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
                EditorGUILayout.LabelField("Select a faction to view details", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGeneralTab(SerializedObject serializedObject)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Faction ID:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("definitionId"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Display Name:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Description:", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("description"), GUIContent.none, GUILayout.Height(80));

            EditorGUILayout.Space();

            var isPlayableProp = serializedObject.FindProperty("isPlayable");
            if (isPlayableProp != null)
                EditorGUILayout.PropertyField(isPlayableProp);

            var isHiddenProp = serializedObject.FindProperty("isHidden");
            if (isHiddenProp != null)
                EditorGUILayout.PropertyField(isHiddenProp);

            EditorGUILayout.Space();

            var canWarProp = serializedObject.FindProperty("canWar");
            if (canWarProp != null)
                EditorGUILayout.PropertyField(canWarProp, new GUIContent("Can War"));

            var maxPointsProp = serializedObject.FindProperty("maxPoints");
            if (maxPointsProp != null)
                EditorGUILayout.PropertyField(maxPointsProp, new GUIContent("Max Points"));

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

        private void DrawRelationsTab(SerializedObject serializedObject)
        {
            EditorGUILayout.LabelField("Relations", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            GetAlliedList(serializedObject).DoLayoutList();
            EditorGUILayout.Space();
            GetEnemyList(serializedObject).DoLayoutList();
        }

        private void DrawRewardsTab(SerializedObject serializedObject)
        {
            EditorGUILayout.LabelField("Reward Tiers", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            var rewardsProp = serializedObject.FindProperty("rewardDescriptions");
            if (rewardsProp != null && rewardsProp.arraySize > 0)
            {
                DrawThresholdVisualization(rewardsProp);
                EditorGUILayout.Space();
            }

            DrawRewardDescriptionsList(rewardsProp);
        }
        private void DrawAppearanceTab(SerializedObject serializedObject)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Icon:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"), GUIContent.none, GUILayout.Height(64));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Color:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("color"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Category:", GUILayout.Width(120));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("category"), GUIContent.none);
            EditorGUILayout.EndHorizontal();
        }

        private SerializedObject GetOrCreateSerializedObject(FactionDefinition definition)
        {
            if (cachedDefinition != definition || cachedSerializedObject == null)
            {
                cachedDefinition = definition;
                cachedSerializedObject = new SerializedObject(definition);
                alliedList = null;
                enemyList = null;
            }
            return cachedSerializedObject;
        }

        private ReorderableList GetAlliedList(SerializedObject serializedObject)
        {
            if (alliedList == null)
            {
                var prop = serializedObject.FindProperty("alliedFactionIDs");
                alliedList = new ReorderableList(serializedObject, prop, true, true, true, true)
                {
                    drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, "Allied Factions"),
                    drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        var element = prop.GetArrayElementAtIndex(index);
                        rect.y += 1;

                        string currentId = element.stringValue;
                        FactionDefinition currentDef = null;
                        if (!string.IsNullOrEmpty(currentId) && FactionDefinitionLibrary.Instance != null)
                            currentDef = FactionDefinitionLibrary.Instance.GetDefinition(currentId);

                        EditorGUI.BeginChangeCheck();
                        var newDef = (FactionDefinition)EditorGUI.ObjectField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), currentDef, typeof(FactionDefinition), false);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (newDef != null)
                                element.stringValue = newDef.DefinitionId;
                            else
                                element.stringValue = string.Empty;
                        }
                    },
                    elementHeightCallback = (index) =>
                    {
                        return EditorGUIUtility.singleLineHeight + 4;
                    }
                };
            }
            return alliedList;
        }

        private ReorderableList GetEnemyList(SerializedObject serializedObject)
        {
            if (enemyList == null)
            {
                var prop = serializedObject.FindProperty("enemyFactionIDs");
                enemyList = new ReorderableList(serializedObject, prop, true, true, true, true)
                {
                    drawHeaderCallback = (rect) => EditorGUI.LabelField(rect, "Enemy Factions"),
                    drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        var element = prop.GetArrayElementAtIndex(index);
                        rect.y += 1;

                        string currentId = element.stringValue;
                        FactionDefinition currentDef = null;
                        if (!string.IsNullOrEmpty(currentId) && FactionDefinitionLibrary.Instance != null)
                            currentDef = FactionDefinitionLibrary.Instance.GetDefinition(currentId);

                        EditorGUI.BeginChangeCheck();
                        var newDef = (FactionDefinition)EditorGUI.ObjectField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), currentDef, typeof(FactionDefinition), false);
                        if (EditorGUI.EndChangeCheck())
                        {
                            if (newDef != null)
                                element.stringValue = newDef.DefinitionId;
                            else
                                element.stringValue = string.Empty;
                        }
                    },
                    elementHeightCallback = (index) =>
                    {
                        return EditorGUIUtility.singleLineHeight + 4;
                    }
                };
            }
            return enemyList;
        }

        // --- Inline array drawing helpers (matching SpellEditorWindow style) ---

        private void DrawThresholdVisualization(SerializedProperty arrayProp)
        {
            if (arrayProp.arraySize == 0)
                return;

            // Sort entries by threshold for visualization
            var entries = new System.Collections.Generic.List<(float threshold, string title, int index)>();
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                var elem = arrayProp.GetArrayElementAtIndex(i);
                var threshProp = elem.FindPropertyRelative("threshold");
                var titleProp = elem.FindPropertyRelative("title");
                float threshold = threshProp != null ? Mathf.Clamp01(threshProp.floatValue) : 0f;
                string title = titleProp != null ? titleProp.stringValue : $"Tier {i}";
                entries.Add((threshold, title, i));
            }
            entries.Sort((a, b) => a.threshold.CompareTo(b.threshold));

            EditorGUILayout.LabelField("Reputation Thresholds", EditorStyles.boldLabel);

            // Draw segmented bar
            var barRect = EditorGUILayout.GetControlRect(false, 40);
            EditorGUI.DrawRect(barRect, new Color(0.15f, 0.15f, 0.15f, 1f));

            float barStart = barRect.x + 4;
            float barWidth = barRect.width - 8;

            // Draw tier segments (each tier spans from previous threshold to current threshold)
            for (int i = 0; i < entries.Count; i++)
            {
                var (threshold, title, _) = entries[i];

                // Start position: 0 for first tier, previous threshold for others
                float rangeStart = (i == 0) ? 0f : entries[i - 1].threshold;
                float rangeEnd = threshold;

                float segmentStart = barStart + (rangeStart * barWidth);
                float segmentWidth = (rangeEnd - rangeStart) * barWidth;

                // Draw segment color based on position
                Color segmentColor = new Color(0.2f + threshold * 0.6f, 0.4f, 0.8f - threshold * 0.3f, 0.7f);
                EditorGUI.DrawRect(new Rect(segmentStart, barRect.y + 4, Mathf.Max(segmentWidth, 1f), barRect.height - 8), segmentColor);

                // Draw threshold label at segment start
                var labelRect = new Rect(segmentStart + 2, barRect.y + 6, 120, 16);
                GUI.Label(labelRect, $"{title} ({rangeStart:P0}–{rangeEnd:P0})", EditorStyles.miniLabel);
            }

            // Draw overflow segment if last threshold < 1.0
            if (entries.Count > 0 && entries[entries.Count - 1].threshold < 1f)
            {
                float lastThreshold = entries[entries.Count - 1].threshold;
                float segmentStart = barStart + (lastThreshold * barWidth);
                float segmentWidth = (1f - lastThreshold) * barWidth;

                Color overflowColor = new Color(0.7f, 0.2f, 0.2f, 0.5f);
                EditorGUI.DrawRect(new Rect(segmentStart, barRect.y + 4, segmentWidth, barRect.height - 8), overflowColor);
            }
        }

        private void DrawRewardDescriptionsList(SerializedProperty arrayProp)
        {
            if (arrayProp == null)
                return;

            DrawInlineArrayHeader("Reward Descriptions", arrayProp);

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

                EditorGUILayout.BeginVertical();

                // Summary header row
                var titleProp = element.FindPropertyRelative("title");
                var iconProp = element.FindPropertyRelative("icon");
                var isPositiveProp = element.FindPropertyRelative("isPositive");
                var thresholdProp = element.FindPropertyRelative("threshold");
                var benefitsProp = element.FindPropertyRelative("benefits");
                var penaltiesProp = element.FindPropertyRelative("penalties");

                EditorGUILayout.BeginHorizontal();

                // Title field (flexible width)
                EditorGUILayout.LabelField("Title:", GUILayout.Width(40));
                if (titleProp != null)
                    EditorGUILayout.PropertyField(titleProp, GUIContent.none, GUILayout.MinWidth(100));

                GUILayout.Space(10);

                // Icon field
                EditorGUILayout.LabelField("Icon:", GUILayout.Width(40));
                if (iconProp != null)
                    EditorGUILayout.PropertyField(iconProp, GUIContent.none, GUILayout.Width(60), GUILayout.Height(18));

                GUILayout.Space(10);

                // IsPositive checkbox
                if (isPositiveProp != null)
                {
                    EditorGUILayout.LabelField(new GUIContent("Positive", "Check if this is a positive reputation tier"), GUILayout.Width(60));
                    isPositiveProp.boolValue = EditorGUILayout.Toggle(isPositiveProp.boolValue, GUILayout.Width(20));
                }

                GUILayout.Space(10);

                // Threshold field (0-1 slider)
                EditorGUILayout.LabelField("Threshold:", GUILayout.Width(70));
                if (thresholdProp != null)
                {
                    float threshold = thresholdProp.floatValue;
                    threshold = EditorGUILayout.Slider(threshold, 0f, 1f, GUILayout.Width(100));
                    thresholdProp.floatValue = Mathf.Clamp01(threshold);
                }

                GUILayout.Space(10);

                // Count labels
                if (benefitsProp != null)
                    EditorGUILayout.LabelField($"Benefits: {benefitsProp.arraySize}", GUILayout.Width(90));

                GUILayout.Space(5);

                if (penaltiesProp != null)
                    EditorGUILayout.LabelField($"Penalties: {penaltiesProp.arraySize}", GUILayout.Width(90));

                GUILayout.FlexibleSpace();

                // Toggle expand button
                if (GUILayout.Button(element.isExpanded ? "Hide" : "Edit", GUILayout.Width(50)))
                {
                    element.isExpanded = !element.isExpanded;
                }

                // Remove button
                if (DrawInlineRemoveButton())
                {
                    arrayProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();

                // Expanded details
                if (element.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.Space();

                    // Benefits array
                    if (benefitsProp != null)
                    {
                        DrawInlineArrayHeader("Benefits", benefitsProp);
                        if (benefitsProp.arraySize == 0)
                        {
                            DrawInlineArrayEmpty();
                        }
                        else
                        {
                            EditorGUI.indentLevel++;
                            for (int j = 0; j < benefitsProp.arraySize; j++)
                            {
                                var bElem = benefitsProp.GetArrayElementAtIndex(j);
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(bElem, GUIContent.none);
                                if (DrawInlineRemoveButton())
                                {
                                    benefitsProp.DeleteArrayElementAtIndex(j);
                                    break;
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            EditorGUI.indentLevel--;
                        }
                    }

                    EditorGUILayout.Space();

                    // Penalties array
                    if (penaltiesProp != null)
                    {
                        DrawInlineArrayHeader("Penalties", penaltiesProp);
                        if (penaltiesProp.arraySize == 0)
                        {
                            DrawInlineArrayEmpty();
                        }
                        else
                        {
                            EditorGUI.indentLevel++;
                            for (int j = 0; j < penaltiesProp.arraySize; j++)
                            {
                                var pElem = penaltiesProp.GetArrayElementAtIndex(j);
                                EditorGUILayout.BeginHorizontal();
                                EditorGUILayout.PropertyField(pElem, GUIContent.none);
                                if (DrawInlineRemoveButton())
                                {
                                    penaltiesProp.DeleteArrayElementAtIndex(j);
                                    break;
                                }
                                EditorGUILayout.EndHorizontal();
                            }
                            EditorGUI.indentLevel--;
                        }
                    }

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();

                if (i < arrayProp.arraySize - 1)
                    DrawInlineSeparator();
            }

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }

        // --- Shared inline array helpers ---

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

        protected override bool MatchesSearchFilter(FactionDefinition definition, string lowerFilter)
        {
            if (!string.IsNullOrEmpty(definition.Description) && definition.Description.ToLower().Contains(lowerFilter))
                return true;
            if (definition.Icon != null && definition.Icon.name.ToLower().Contains(lowerFilter))
                return true;

            foreach (var id in definition.AlliedFactionIDs)
            {
                if (!string.IsNullOrEmpty(id) && id.ToLower().Contains(lowerFilter))
                    return true;
            }

            foreach (var id in definition.EnemyFactionIDs)
            {
                if (!string.IsNullOrEmpty(id) && id.ToLower().Contains(lowerFilter))
                    return true;
            }

            return false;
        }
    }
}
