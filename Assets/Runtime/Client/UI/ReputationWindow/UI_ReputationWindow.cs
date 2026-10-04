namespace Game.Client.UI
{
    public sealed class UI_ReputationWindow : UI_Window
    {
        private static UI_ReputationWindow _instance;
        public static UI_ReputationWindow Instance => _instance;

        private void Awake()
        {
            _instance = this;
        }
    }
}