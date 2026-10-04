using UnityEngine;
using UnityEditor;
using Game.Shared.Data;

namespace Game.Editor.Windows
{
    public class DamageSchoolEditorWindow : DataDefinitionEditorWindow<DamageSchoolDefinition, DamageSchoolDefinitionLibrary>
    {
        private int selectedTab;
        private readonly string[] tabNames = { "General", "Mitigation" };

        private SerializedObject cachedSerializedObject;
        private DamageSchoolDefinition cachedDefinition;

        protected override string DefinitionTypeName => "Damage School";
        protected override string AssetDirectoryPath => "Assets/Data/DamageSchoolDefinitions";
        protected override string AssetPrefix => "DamageSchool";

        [MenuItem("Data/Damage School Definitions")]
        public static void OpenWindow()
        {
            var window = GetWindow<DamageSchoolEditorWindow>("Damage School Definitions");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        protected override DamageSchoolDefinitionLibrary GetLibraryInstance()
        {
            return DamageSchoolDefinitionLibrary.Instance;
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
                    case 1: DrawMitigationTab(serializedDef); break;
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
                EditorGUILayout.LabelField("Select a damage school to view details", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGeneralTab(SerializedObject serializedObject)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("School ID:", GUILayout.Width(120));
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
            EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"), GUIContent.none);
            EditorGUILayout.EndHorizontal();

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

        private void DrawMitigationTab(SerializedObject serializedObject)
        {
            EditorGUILayout.LabelField("Mitigation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mitigationRatingStat"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ratingPerPercent"));

            EditorGUILayout.Space();

            if (GUILayout.Button("Preview Scaling"))
            {
                float rpp = serializedObject.FindProperty("ratingPerPercent").floatValue;
                RatingScalingPreviewWindow.Show(rpp);
            }
        }

        private SerializedObject GetOrCreateSerializedObject(DamageSchoolDefinition definition)
        {
            if (cachedDefinition != definition || cachedSerializedObject == null)
            {
                cachedDefinition = definition;
                cachedSerializedObject = new SerializedObject(definition);
            }
            return cachedSerializedObject;
        }

        protected override void DrawDefinitionFields(SerializedObject serializedObject) { }
    }
}
