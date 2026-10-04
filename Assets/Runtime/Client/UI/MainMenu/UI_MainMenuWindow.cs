using UnityEngine;
using TMPro;

namespace Game.Client.UI
{
    public class UI_MainMenuWindow : UI_Window
    {
        private static UI_MainMenuWindow _instance;
        public static UI_MainMenuWindow Instance => _instance;

        [Header("UI References - Login")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private UI_Button loginButton;

        #region Lifecycle
        protected override void Start()
        {
            base.Start();

            if (_instance != null)
            {
                Debug.LogWarning($"An instance of UI_MainMenuWindow already exists. {this.gameObject.name} will not be set as an instance.");
                return;
            }
            else
            {
                _instance = this;
            }
        }
        #endregion

        #region Getters
        public string GetUsername() => usernameInput.text;
        public string GetPassword() => passwordInput.text;
        #endregion
    }
}