using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Persistence;
using System;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterSelectWindow</c> implementation.
    /// </summary>
    public sealed class UI_CharacterSelectWindow : UI_Window
    {
        private static UI_CharacterSelectWindow _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_CharacterSelectWindow Instance => _instance;
        private static bool _initialized;
        /// <summary>
        /// Gets whether the client-side <c>IsInitialized</c> condition is true.
        /// </summary>
        public static bool IsInitialized => _initialized;

        /// <summary>
        /// Stores the <c>onCharacterSelected</c> value.
        /// </summary>
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

        /// <summary>
        /// Executes the client-side <c>PopulateCharacterList</c> operation.
        /// </summary>
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
