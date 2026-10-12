using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_MainMenuWindow</c> implementation.
    /// </summary>
    public class UI_MainMenuWindow : UI_Window
    {
        private static UI_MainMenuWindow _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_MainMenuWindow Instance => _instance;

        [Header("UI References - Login")]
        [FormerlySerializedAs("usernameInput")]
        [SerializeField] private TMP_InputField _usernameInput;
        [FormerlySerializedAs("passwordInput")]
        [SerializeField] private TMP_InputField _passwordInput;
        [FormerlySerializedAs("loginButton")]
        [SerializeField] private UI_Button _loginButton;

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
        /// <summary>
        /// Gets the value provided by <c>GetUsername</c>.
        /// </summary>
        public string GetUsername() => _usernameInput.text;
        /// <summary>
        /// Gets the value provided by <c>GetPassword</c>.
        /// </summary>
        public string GetPassword() => _passwordInput.text;
        #endregion
    }
}
