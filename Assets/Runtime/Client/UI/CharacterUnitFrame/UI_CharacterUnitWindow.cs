namespace Game.Client.UI
{
    public sealed class UI_CharacterUnitWindow : UI_Window
    {
        private static UI_CharacterUnitWindow _instance;
        public static UI_CharacterUnitWindow Instance => _instance;

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