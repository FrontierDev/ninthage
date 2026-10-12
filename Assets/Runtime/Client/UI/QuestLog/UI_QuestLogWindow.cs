using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_QuestLogWindow</c> implementation.
    /// </summary>
    public sealed class UI_QuestLogWindow : UI_Window
    {
        private static UI_QuestLogWindow _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_QuestLogWindow Instance => _instance;
        private static bool _initialized;
        /// <summary>
        /// Gets whether the client-side <c>IsInitialized</c> condition is true.
        /// </summary>
        public static bool IsInitialized => _initialized;

        private void Awake()
        {
            _instance = this;
            _initialized = true;
        }
    }
}