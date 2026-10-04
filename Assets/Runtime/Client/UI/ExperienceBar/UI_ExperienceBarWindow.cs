namespace Game.Client.UI
{
    public sealed class UI_ExperienceBarWindow : UI_Window
    {
        private static UI_ExperienceBarWindow _instance;
        public static UI_ExperienceBarWindow Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;
            Game.Shared.Networking.AccountService.onEnteredWorld += OnEnteredWorld;
        }

        private void OnEnteredWorld()
        {
            ShowImmediate();
        }
    }
}