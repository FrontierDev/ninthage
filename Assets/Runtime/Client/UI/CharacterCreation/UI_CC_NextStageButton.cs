namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CC_NextStageButton</c> implementation.
    /// </summary>
    public sealed class UI_CC_NextStageButton : UI_Button
    {
        /// <summary>
        /// Stores the <c>_instance</c> value.
        /// </summary>
        public static UI_CC_NextStageButton _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_CC_NextStageButton Instance => _instance;

        private string currentPanel;
        private string nextPanel;

        private void Awake()
        {
            if (_instance != null) return;
            _instance = this;
            Hide();
        }

        private void OnDestroy()
        {
            _instance = null;
        }

        /// <inheritdoc/>
        public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (string.IsNullOrEmpty(nextPanel)) return;
            if (UI_CharacterCreationWindow.Instance == null) return;

            if (!string.IsNullOrEmpty(currentPanel))
                UI_CharacterCreationWindow.Instance.HidePanel(currentPanel);

            UI_CharacterCreationWindow.Instance.ShowPanel(nextPanel);
            currentPanel = nextPanel;

            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }

        /// <summary>
        /// Sets the value managed by <c>SetCurrentPanel</c>.
        /// </summary>
        public void SetCurrentPanel(string panelName)
        {
            currentPanel = panelName;
        }

        /// <summary>
        /// Sets the value managed by <c>SetNextPanel</c>.
        /// </summary>
        public void SetNextPanel(string panelName)
        {
            nextPanel = panelName;

            if (!IsVisible()) Show();
        }
    }
}