using Debug = Game.Shared.Utility.FormattedDebug;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>Loads and exposes the authored world-location definitions.</summary>
    [CreateAssetMenu(
        fileName = "WorldLocationDefinitionLibrary",
        menuName = "NinthAge/Definition Libraries/World Location")]
    public sealed class WorldLocationDefinitionLibrary : DataDefinitionLibrary<WorldLocationDefinition>
    {
        private static WorldLocationDefinitionLibrary _instance;

        /// <summary>Gets the world-location library loaded from Resources.</summary>
        public static WorldLocationDefinitionLibrary Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<WorldLocationDefinitionLibrary>(
                        "WorldLocationDefinitionLibrary");
                    if (_instance == null)
                        Debug.Error("Could not load WorldLocationDefinitionLibrary from Resources");
                }

                return _instance;
            }
        }

        /// <summary>Resets the singleton instance for controlled reloads.</summary>
        public static void ResetInstance()
        {
            _instance = null;
        }
    }
}
