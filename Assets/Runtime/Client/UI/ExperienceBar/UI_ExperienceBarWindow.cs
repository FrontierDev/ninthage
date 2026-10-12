namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_ExperienceBarWindow</c> implementation.
    /// </summary>
    public sealed class UI_ExperienceBarWindow : UI_Window
    {
        private static UI_ExperienceBarWindow _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_ExperienceBarWindow Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;
            Game.Shared.Networking.AccountEndpoint.onEnteredWorld += OnEnteredWorld;
        }

        private void OnEnteredWorld()
        {
            ShowImmediate();
        }
    }
}
