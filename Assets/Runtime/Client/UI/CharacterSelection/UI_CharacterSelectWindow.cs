using System;
using Game.Shared.Persistence;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Client.UI
{
    public sealed class UI_CharacterSelectWindow : UI_Window
    {
        private static UI_CharacterSelectWindow _instance;
        public static UI_CharacterSelectWindow Instance => _instance;
        private static bool _initialized;
        public static bool IsInitialized => _initialized;

        public Action<CharacterData> onCharacterSelected;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;
            _initialized = true;
        }

        public void PopulateCharacterList(System.Collections.Generic.List<CharacterData> characters)
        {
            if (TryGetPanel("CharacterListPanel", out UI_Panel panel))
                if (panel is UI_CharacterListPanel characterListPanel)
                    characterListPanel.Refresh();
                else
                    Debug.Error("Panel 'CharacterListPanel' not found in UI_CharacterSelectWindow.");
            else
                Debug.Error("Panel 'CharacterListPanel' not found in UI_CharacterSelectWindow.");
        }
    }
}