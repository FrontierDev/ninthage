using UnityEngine;

namespace Game.Client.UI
{
    public sealed class UI_CharacterWindow : UI_Window
    {
        private static UI_CharacterWindow _instance;
        public static UI_CharacterWindow Instance => _instance;
        private static bool _initialized;
        public static bool IsInitialized => _initialized;

        private void Awake()
        {
            _instance = this;
            _initialized = true;

        }
    }
}