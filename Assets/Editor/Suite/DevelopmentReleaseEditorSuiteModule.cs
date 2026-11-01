using UnityEditor;
using UnityEngine;

namespace Game.Editor.Suite
{
    /// <summary>
    /// Provides navigation to the existing Development Release editor window.
    /// </summary>
    internal sealed class DevelopmentReleaseEditorSuiteModule : IEditorSuiteModule
    {
        /// <inheritdoc />
        public string Label => "Development Release";

        /// <inheritdoc />
        public void Draw()
        {
            EditorGUILayout.LabelField(Label, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Current Player Version", PlayerSettings.bundleVersion);

            if (GUILayout.Button("Open Development Release")
                && !EditorApplication.ExecuteMenuItem("Window/NinthAge/Development Release"))
            {
                Debug.LogError(
                    "Could not open the registered menu item 'Window/NinthAge/Development Release'.");
            }

            EditorGUILayout.HelpBox(
                "Version increments and embedding the release workflow in the Suite are separate future work.",
                MessageType.Info);
        }
    }
}
