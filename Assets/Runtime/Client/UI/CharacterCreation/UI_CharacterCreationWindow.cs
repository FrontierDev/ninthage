using Debug = Game.Shared.FormattedDebug;
using Classes = Game.Shared.Data.ClassDefinitionLibrary;
using System;
using Game.Shared.Data;
using UnityEngine.SceneManagement;

namespace Game.Client.UI
{
    public sealed class UI_CharacterCreationWindow : UI_Window
    {
        private static UI_CharacterCreationWindow _instance;
        public static UI_CharacterCreationWindow Instance => _instance;
        private static bool _initialized = false;
        public static bool Initialized => _initialized;

        // Actions
        public Action<ClassDefinition> onClassSelected;
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

        public override void Show()
        {
            base.Show();
            ShowPanel("RaceSelection");
        }
    }
}