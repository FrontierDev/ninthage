namespace Game.Client.UI
{
    public sealed class UI_ActionBarWindow : UI_Window
    {
        private static UI_ActionBarWindow _instance;
        public static UI_ActionBarWindow Instance => _instance;
        private static bool _initialized = false;
        public static bool Initialized => _initialized;

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

        protected override void Start()
        {
            base.Start(); // populates panels[]

            int currentIndex = 0;
            foreach (var panel in panels)
            {
                if (panel is UI_ActionBarPanel abPanel)
                {
                    abPanel.Initialize(currentIndex);
                    currentIndex++;
                }
            }

            _initialized = true;
        }

        private void OnActionBarInput(int barIndex, int slotIndex)
        {
            if (barIndex >= 0 && barIndex < panels.Length && panels[barIndex] is UI_ActionBarPanel abPanel)
                abPanel.ActivateSlot(slotIndex);
        }

        private void OnEnteredWorld()
        {
            ShowImmediate();
            ClientInputController.Instance.onActionBarInput += OnActionBarInput;
        }
    }
}