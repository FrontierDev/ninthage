namespace Game.Client.UI
{
    public sealed class UI_InventoryWindow : UI_Window
    {
        private static UI_InventoryWindow _instance;
        public static UI_InventoryWindow Instance => _instance;
        private static bool _initialized;
        public static bool IsInitialized => _initialized;

        private void Awake()
        {
            _instance = this;
            _initialized = true;
        }
    }
}