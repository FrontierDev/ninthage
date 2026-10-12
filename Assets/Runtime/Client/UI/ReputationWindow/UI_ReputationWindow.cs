namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_ReputationWindow</c> implementation.
    /// </summary>
    public sealed class UI_ReputationWindow : UI_Window
    {
        private static UI_ReputationWindow _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_ReputationWindow Instance => _instance;

        private void Awake()
        {
            _instance = this;
        }
    }
}