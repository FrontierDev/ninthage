using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>
    /// Abstract base for all data definitions.
    /// Provides common fields: unique ID and display name.
    /// </summary>
    public abstract class DataDefinition : ScriptableObject
    {
        [SerializeField]
        private string definitionId;

        [SerializeField]
        private string displayName;

        /// <summary>
        /// Unique identifier for this definition.
        /// </summary>
        public string DefinitionId => definitionId;

        /// <summary>
        /// Human-readable name displayed in UI.
        /// </summary>
        public string DisplayName => displayName;

        /// <summary>
        /// Prefix used when auto-renaming the asset file (e.g. "Class" produces "Class_warrior.asset").
        /// </summary>
        protected abstract string AssetPrefix { get; }

        /// <summary>
        /// Validates the definition configuration.
        /// </summary>
        public virtual bool IsValid()
        {
            if (string.IsNullOrEmpty(definitionId))
            {
                Debug.LogError($"[{GetType().Name}] Definition '{name}' has empty ID");
                return false;
            }

            if (string.IsNullOrEmpty(displayName))
            {
                Debug.LogError($"[{GetType().Name}] Definition '{definitionId}' has empty display name");
                return false;
            }

            return true;
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(definitionId) && !string.IsNullOrEmpty(name))
            {
                definitionId = name.ToLower().Replace(" ", "_");
            }

            if (!string.IsNullOrEmpty(definitionId) && !string.IsNullOrEmpty(AssetPrefix))
            {
                string currentPath = UnityEditor.AssetDatabase.GetAssetPath(this);
                if (!string.IsNullOrEmpty(currentPath))
                {
                    string expectedFileName = $"{AssetPrefix}_{definitionId}.asset";
                    string directory = System.IO.Path.GetDirectoryName(currentPath);
                    string expectedPath = System.IO.Path.Combine(directory, expectedFileName).Replace("\\", "/");

                    if (currentPath != expectedPath && !string.IsNullOrEmpty(directory))
                    {
                        UnityEditor.EditorApplication.delayCall += () =>
                        {
                            if (this == null) return;

                            string latestPath = UnityEditor.AssetDatabase.GetAssetPath(this);
                            if (string.IsNullOrEmpty(latestPath)) return;

                            string latestFileName = System.IO.Path.GetFileName(latestPath);
                            if (latestFileName == expectedFileName) return;

                            string renameError = UnityEditor.AssetDatabase.RenameAsset(latestPath, System.IO.Path.GetFileNameWithoutExtension(expectedFileName));
                            if (!string.IsNullOrEmpty(renameError))
                            {
                                Debug.LogError($"[{GetType().Name}] Failed to rename asset: {renameError}");
                            }
                        };
                    }
                }
            }
#endif
            OnDefinitionValidate();
        }

        /// <summary>
        /// Override to add definition-specific validation logic in OnValidate.
        /// </summary>
        protected virtual void OnDefinitionValidate() { }
    }
}
