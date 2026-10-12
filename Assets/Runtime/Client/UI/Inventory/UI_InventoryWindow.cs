namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_InventoryWindow</c> implementation.
    /// </summary>
    public sealed class UI_InventoryWindow : UI_Window
    {
        private static UI_InventoryWindow _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_InventoryWindow Instance => _instance;
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