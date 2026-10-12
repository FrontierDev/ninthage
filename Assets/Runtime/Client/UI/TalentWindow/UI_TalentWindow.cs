namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_TalentWindow</c> implementation.
    /// </summary>
    public sealed class UI_TalentWindow : UI_Window
    {
        private static UI_TalentWindow _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_TalentWindow Instance => _instance;
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