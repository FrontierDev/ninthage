using UnityEditor;

namespace Game.Editor.Suite
{
    /// <summary>
    /// Displays the future Asset Validation module entry point.
    /// </summary>
    internal sealed class AssetValidationEditorSuiteModule : IEditorSuiteModule
    {
        /// <inheritdoc />
        public string Label => "Asset Validation";

        /// <inheritdoc />
        public void Draw()
        {
            EditorGUILayout.LabelField(Label, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Asset validation will be implemented in a subsequent Editor Suite issue.",
                MessageType.Info);
        }
    }
}
