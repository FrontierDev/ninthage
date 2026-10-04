using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Game.Shared.Data;

namespace Game.Editor.Windows
{
    /// <summary>
    /// Editor window for viewing and managing actor stat definitions.
    /// </summary>
    public class ActorStatEditorWindow : DataDefinitionEditorWindow<ActorStatDefinition, ActorStatDefinitionLibrary>
    {
        private int selectedTab;
        private readonly string[] tabNames = { "General", "Base Value", "Regeneration" };

        private SerializedObject cachedSerializedObject;
        private ActorStatDefinition cachedDefinition;

        protected override string DefinitionTypeName => "Actor Stat";
        protected override string AssetDirectoryPath => "Assets/Data/ActorStatDefinitions";
        protected override string AssetPrefix => "ActorStat";

        [MenuItem("Data/Actor Stat Definitions")]
        public static void OpenWindow()
        {
            var window = GetWindow<ActorStatEditorWindow>("Actor Stat Definitions");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        protected override ActorStatDefinitionLibrary GetLibraryInstance()
        {
            return ActorStatDefinitionLibrary.Instance;
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
                    case 1: DrawBaseValueTab(serializedDef); break;
                    case 2: DrawRegenerationTab(serializedDef); break;
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
                EditorGUILayout.LabelField("Select an actor stat to view details", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGeneralTab(SerializedObject serializedObject)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Stat ID:", GUILayout.Width(120));
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

            EditorGUILayout.PropertyField(serializedObject.FindProperty("category"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("replicationMode"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("tags"), true);

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

        private void DrawBaseValueTab(SerializedObject serializedObject)
        {
            EditorGUILayout.LabelField("Base Value", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseValueMode"));

            var baseValueMode = (ActorStatBaseValueMode)serializedObject.FindProperty("baseValueMode").enumValueIndex;
            if (baseValueMode == ActorStatBaseValueMode.Fixed)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("baseValue"));
                EditorGUI.indentLevel--;
            }
            else if (baseValueMode == ActorStatBaseValueMode.Derived)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceStat"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("multiplier"));
                EditorGUI.indentLevel--;
            }
            else if (baseValueMode == ActorStatBaseValueMode.Rating)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sourceStat"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("multiplier"), new GUIContent("Rating Per Percent"));

                if (GUILayout.Button("Preview Scaling"))
                {
                    float rpp = serializedObject.FindProperty("multiplier").floatValue;
                    RatingScalingPreviewWindow.Show(rpp);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("startsAtZero"));
        }

        private void DrawRegenerationTab(SerializedObject serializedObject)
        {
            EditorGUILayout.LabelField("Regeneration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("regenMode"));

            var regenMode = (ActorStatBaseValueMode)serializedObject.FindProperty("regenMode").enumValueIndex;
            if (regenMode == ActorStatBaseValueMode.Fixed)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("regenPerSecond"));
                EditorGUI.indentLevel--;
            }
            else if (regenMode == ActorStatBaseValueMode.Derived)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("regenSourceStat"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("regenMultiplier"));
                EditorGUI.indentLevel--;
            }
        }

        private SerializedObject GetOrCreateSerializedObject(ActorStatDefinition definition)
        {
            if (cachedDefinition != definition || cachedSerializedObject == null)
            {
                cachedDefinition = definition;
                cachedSerializedObject = new SerializedObject(definition);
            }
            return cachedSerializedObject;
        }

        protected override void DrawDefinitionFields(SerializedObject serializedObject) { }

        protected override bool SupportsTags => true;

        protected override IReadOnlyList<string> GetTags(ActorStatDefinition definition)
        {
            return definition.Tags;
        }

        protected override bool MatchesSearchFilter(ActorStatDefinition definition, string lowerFilter)
        {
            if ((!string.IsNullOrEmpty(definition.Description) && definition.Description.ToLower().Contains(lowerFilter)) ||
                   definition.Category.ToString().ToLower().Contains(lowerFilter))
                return true;

            if (definition.Tags != null)
            {
                foreach (var tag in definition.Tags)
                {
                    if (!string.IsNullOrEmpty(tag) && tag.ToLower().Contains(lowerFilter))
                        return true;
                }
            }

            return false;
        }
    }
}
