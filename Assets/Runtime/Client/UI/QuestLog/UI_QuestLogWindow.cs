using UnityEngine;

namespace Game.Client.UI
{
    public sealed class UI_QuestLogWindow : UI_Window
    {
        private static UI_QuestLogWindow _instance;
        public static UI_QuestLogWindow Instance => _instance;
        private static bool _initialized;
        public static bool IsInitialized => _initialized;

        private void Awake()
        {
            _instance = this;
            _initialized = true;
        }
    }
}