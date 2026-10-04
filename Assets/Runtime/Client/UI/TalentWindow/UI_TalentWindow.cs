namespace Game.Client.UI
{
    public sealed class UI_TalentWindow : UI_Window
    {
        private static UI_TalentWindow _instance;
        public static UI_TalentWindow Instance => _instance;
        private static bool _initialized;
        public static bool IsInitialized => _initialized;

        private void Awake()
        {
            _instance = this;
            _initialized = true;
        }
    }
}