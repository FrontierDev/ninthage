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

        private string _currentPanel;
        private string _nextPanel;

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
            if (string.IsNullOrEmpty(_nextPanel)) return;
            if (UI_CharacterCreationWindow.Instance == null) return;

            if (!string.IsNullOrEmpty(_currentPanel))
                UI_CharacterCreationWindow.Instance.HidePanel(_currentPanel);

            UI_CharacterCreationWindow.Instance.ShowPanel(_nextPanel);
            _currentPanel = _nextPanel;

            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }

        /// <summary>
        /// Sets the value managed by <c>SetCurrentPanel</c>.
        /// </summary>
        public void SetCurrentPanel(string panelName)
        {
            _currentPanel = panelName;
        }

        /// <summary>
        /// Sets the value managed by <c>SetNextPanel</c>.
        /// </summary>
        public void SetNextPanel(string panelName)
        {
            _nextPanel = panelName;

            if (!IsVisible()) Show();
        }
    }
}
