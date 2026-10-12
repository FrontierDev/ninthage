using Classes = Game.Shared.Data.ClassDefinitionLibrary;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Data;
using System;
using UnityEngine.SceneManagement;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterCreationWindow</c> implementation.
    /// </summary>
    public sealed class UI_CharacterCreationWindow : UI_Window
    {
        private static UI_CharacterCreationWindow _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_CharacterCreationWindow Instance => _instance;
        private static bool _initialized = false;
        /// <summary>
        /// Gets the <c>Initialized</c> value.
        /// </summary>
        public static bool Initialized => _initialized;

        // Actions
        /// <summary>
        /// Stores the <c>onClassSelected</c> value.
        /// </summary>
        public Action<ClassDefinition> onClassSelected;
        /// <summary>
        /// Stores the <c>onRaceSelected</c> value.
        /// </summary>
        public Action<RaceDefinition> onRaceSelected;

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

        /// <inheritdoc/>
        public override void Show()
        {
            base.Show();
            ShowPanel("RaceSelection");
        }
    }
}
