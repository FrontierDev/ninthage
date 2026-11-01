using UnityEditor;
using UnityEngine;

namespace Game.Editor.Suite
{
    /// <summary>
    /// Provides the dockable navigation shell for project-owned editor tools.
    /// </summary>
    internal sealed class NinthAgeEditorSuiteWindow : EditorWindow
    {
        private const float SIDEBAR_WIDTH = 210f;
        private static readonly IEditorSuiteModule[] MODULES =
        {
            new AssetValidationEditorSuiteModule(),
            new DevelopmentReleaseEditorSuiteModule(),
            new DataEditorsEditorSuiteModule()
        };

        private int _selectedModuleIndex = 2;
        private Vector2 _rightPanelScrollPosition;

        /// <summary>
        /// Opens or focuses the dockable Editor Suite window.
        /// </summary>
        [MenuItem("Window/NinthAge/Editor Suite")]
        private static void Open()
        {
            var window = GetWindow<NinthAgeEditorSuiteWindow>();
            window.titleContent = new GUIContent("Ninth Age Editor Suite");
            window.minSize = new Vector2(760f, 440f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(SIDEBAR_WIDTH));
            EditorGUILayout.LabelField("Ninth Age", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            for (int moduleIndex = 0; moduleIndex < MODULES.Length; moduleIndex++)
            {
                var module = MODULES[moduleIndex];
                GUIStyle buttonStyle = moduleIndex == _selectedModuleIndex
                    ? EditorStyles.toolbarButton
                    : EditorStyles.miniButton;
                if (GUILayout.Button(module.Label, buttonStyle, GUILayout.ExpandWidth(true)))
                    _selectedModuleIndex = moduleIndex;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            _rightPanelScrollPosition = EditorGUILayout.BeginScrollView(_rightPanelScrollPosition);
            MODULES[_selectedModuleIndex].Draw();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }
    }
}
