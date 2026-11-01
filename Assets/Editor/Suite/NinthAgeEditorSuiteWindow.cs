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

        private IEditorSuiteModule[] _modules;
        private DataEditorsEditorSuiteModule _dataEditorsModule;
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
            window.minSize = new Vector2(1050f, 600f);
            window.Show();
        }

        /// <summary>
        /// Focuses the Suite and selects an embedded data editor.
        /// </summary>
        internal static void OpenDataEditor(DataEditorKind kind)
        {
            var window = GetWindow<NinthAgeEditorSuiteWindow>();
            window._selectedModuleIndex = 2;
            window._dataEditorsModule.Select(kind);
            window.Show();
            window.Repaint();
        }

        private void OnEnable()
        {
            _dataEditorsModule = new DataEditorsEditorSuiteModule(Repaint);
            _modules = new IEditorSuiteModule[]
            {
                new AssetValidationEditorSuiteModule(),
                new DevelopmentReleaseEditorSuiteModule(),
                _dataEditorsModule
            };
        }

        private void OnDisable()
        {
            _dataEditorsModule?.Reset();
            _dataEditorsModule = null;
            _modules = null;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            try
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(SIDEBAR_WIDTH));
                try
                {
                    EditorGUILayout.LabelField("Ninth Age", EditorStyles.boldLabel);
                    EditorGUILayout.Space(4f);

                    for (int moduleIndex = 0; moduleIndex < _modules.Length; moduleIndex++)
                    {
                        var module = _modules[moduleIndex];
                        GUIStyle buttonStyle = moduleIndex == _selectedModuleIndex
                            ? EditorStyles.toolbarButton
                            : EditorStyles.miniButton;
                        if (GUILayout.Button(module.Label, buttonStyle, GUILayout.ExpandWidth(true)))
                            _selectedModuleIndex = moduleIndex;
                    }
                }
                finally
                {
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                try
                {
                    if (_selectedModuleIndex == 2)
                    {
                        _modules[_selectedModuleIndex].Draw();
                    }
                    else
                    {
                        _rightPanelScrollPosition = EditorGUILayout.BeginScrollView(_rightPanelScrollPosition);
                        try
                        {
                            _modules[_selectedModuleIndex].Draw();
                        }
                        finally
                        {
                            EditorGUILayout.EndScrollView();
                        }
                    }
                }
                finally
                {
                    EditorGUILayout.EndVertical();
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
