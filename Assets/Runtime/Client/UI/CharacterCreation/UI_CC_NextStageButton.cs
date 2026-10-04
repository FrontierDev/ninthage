namespace Game.Client.UI
{
    public sealed class UI_CC_NextStageButton : UI_Button
    {
        public static UI_CC_NextStageButton _instance;
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

        public void SetCurrentPanel(string panelName)
        {
            currentPanel = panelName;
        }

        public void SetNextPanel(string panelName)
        {
            nextPanel = panelName;

            if (!IsVisible()) Show();
        }
    }
}