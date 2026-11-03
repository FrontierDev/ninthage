using System;
using System.Collections;
using Game.Client;
using Game.Client.UI.WorldSelection;
using Game.Shared;
using Game.Shared.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_MainMenuWindow</c> implementation.
    /// </summary>
    public class UI_MainMenuWindow : UI_Window
    {
        private const int MOTD_TIMEOUT_SECONDS = 5;
        private const int MOTD_MAX_LENGTH = 2048;
        private const int LOGIN_TOAST_MAX_LENGTH = 256;
        private const float LOGIN_TOAST_VISIBLE_SECONDS = 4f;
        private const float LOGIN_TOAST_FADE_SECONDS = 0.15f;
        private const string REMEMBER_ACCOUNT_NAME_SETTING =
            "login_remember_account_name";
        private const string REMEMBERED_ACCOUNT_NAME_SETTING =
            "login_remembered_account_name";

        private static UI_MainMenuWindow _instance;
        private Coroutine _motdCoroutine;
        private Coroutine _loginToastCoroutine;

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
        [SerializeField] private Toggle _rememberAccountNameToggle;
        [SerializeField] private CanvasGroup _loginToastCanvasGroup;
        [SerializeField] private TMP_Text _loginToastText;
        [SerializeField] private UI_WorldSelectionPanel _worldSelectionPanel;

        [Header("UI References - Main Menu Chrome")]
        [SerializeField] private GameObject _logoRoot;
        [SerializeField] private GameObject _messageOfTheDayRoot;

        [Header("UI References - MOTD")]
        [SerializeField] private TMP_Text _messageOfTheDayText;

        [Header("UI References - Build Information")]
        [SerializeField] private TMP_Text _buildInfoText;

        #region Lifecycle
        protected override void Start()
        {
            base.Start();
            HideLoginToastImmediate();

            if (_instance != null)
            {
                Debug.LogWarning($"An instance of UI_MainMenuWindow already exists. {this.gameObject.name} will not be set as an instance.");
                return;
            }
            else
            {
                _instance = this;
            }

            bool rememberAccountName =
                ClientSettingsManager.GetSetting(REMEMBER_ACCOUNT_NAME_SETTING, false);
            _rememberAccountNameToggle?.SetIsOnWithoutNotify(rememberAccountName);
            if (rememberAccountName)
            {
                string rememberedAccountName = ClientSettingsManager.GetSetting(
                    REMEMBERED_ACCOUNT_NAME_SETTING,
                    string.Empty);
                if (!string.IsNullOrWhiteSpace(rememberedAccountName))
                    _usernameInput.text = rememberedAccountName;
            }

            _rememberAccountNameToggle?.onValueChanged.AddListener(OnRememberAccountNameChanged);

            RefreshBuildInfo();
            RefreshMessageOfTheDay();
        }

        private void OnDestroy()
        {
            _rememberAccountNameToggle?.onValueChanged.RemoveListener(OnRememberAccountNameChanged);
        }

        public override void Show()
        {
            HideLoginToastImmediate();
            SetPrimaryMenuChromeVisible(true);
            _loginButton.EnableButton();
            _worldSelectionPanel?.HideImmediate();
            base.Show();
            RefreshMessageOfTheDay();
        }

        private void SetPrimaryMenuChromeVisible(bool visible)
        {
            if (_logoRoot != null)
                _logoRoot.SetActive(visible);
            if (_messageOfTheDayRoot != null)
                _messageOfTheDayRoot.SetActive(visible);
        }
        #endregion

        #region Build Information
        private void RefreshBuildInfo()
        {
            if (_buildInfoText == null)
                throw new InvalidOperationException("UI_MainMenuWindow requires a serialized BuildInfoText reference.");

            string version = Application.version;
            try
            {
                string branch = ClientSourceBranch.LoadCurrent();
                _buildInfoText.text = $"v{version} · {branch}";
            }
            catch (InvalidOperationException)
            {
                Debug.LogWarning("Client source branch metadata could not be loaded; displaying branch unavailable.", this);
                _buildInfoText.text = $"v{version} · branch unavailable";
            }
        }
        #endregion

        #region Message Of The Day
        private void RefreshMessageOfTheDay()
        {
            if (_motdCoroutine != null)
                StopCoroutine(_motdCoroutine);

            _messageOfTheDayText.text = "Loading message of the day...";
            _motdCoroutine = StartCoroutine(FetchMessageOfTheDay());
        }

        private IEnumerator FetchMessageOfTheDay()
        {
            yield return new WaitUntil(() => GameContentBootstrap.IsInitialized);

            if (!TryCreateMessageOfTheDayEndpoint(out Uri endpoint, out string failure))
            {
                SetMessageOfTheDayUnavailable(failure);
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequest.Get(endpoint.AbsoluteUri))
            {
                request.timeout = MOTD_TIMEOUT_SECONDS;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    SetMessageOfTheDayUnavailable($"transport failure ({request.result})");
                    yield break;
                }

                if (request.responseCode != 200)
                {
                    SetMessageOfTheDayUnavailable($"unexpected HTTP status ({request.responseCode})");
                    yield break;
                }

                if (!TryParseMessageOfTheDay(request.downloadHandler.text, out string message, out failure))
                {
                    SetMessageOfTheDayUnavailable(failure);
                    yield break;
                }

                _messageOfTheDayText.text = string.IsNullOrEmpty(message)
                    ? "No announcements at this time."
                    : message;
            }

            _motdCoroutine = null;
        }

        private static bool TryCreateMessageOfTheDayEndpoint(out Uri endpoint, out string failure)
        {
            return ClientMasterEndpointUtility.TryCreateEndpoint("client/v1/motd", out endpoint, out failure);
        }

        private static bool TryParseMessageOfTheDay(string response, out string message, out string failure)
        {
            message = null;

            if (string.IsNullOrEmpty(response))
            {
                failure = "response is empty";
                return false;
            }

            try
            {
                JObject payload = JObject.Parse(response);
                JToken messageToken = payload["message"];

                if (messageToken == null || messageToken.Type != JTokenType.String)
                {
                    failure = "response is missing a string message property";
                    return false;
                }

                message = messageToken.Value<string>();
                if (message.Length > MOTD_MAX_LENGTH)
                {
                    failure = $"message exceeds the {MOTD_MAX_LENGTH}-character limit";
                    return false;
                }

                failure = null;
                return true;
            }
            catch (JsonException)
            {
                failure = "response contains malformed JSON";
                return false;
            }
        }

        private void SetMessageOfTheDayUnavailable(string failure)
        {
            _messageOfTheDayText.text = "Message unavailable.";
            _motdCoroutine = null;
            Debug.LogError($"Unable to retrieve the message of the day: {failure}.");
        }
        #endregion

        #region Login and World Selection State
        private void OnRememberAccountNameChanged(bool remember)
        {
            ClientSettingsManager.SetSetting(REMEMBER_ACCOUNT_NAME_SETTING, remember);
            if (!remember)
                ClientSettingsManager.DeleteSetting(REMEMBERED_ACCOUNT_NAME_SETTING);
        }

        internal void PersistSubmittedAccountName(string accountName)
        {
            if (_rememberAccountNameToggle == null || !_rememberAccountNameToggle.isOn)
            {
                ClientSettingsManager.DeleteSetting(REMEMBERED_ACCOUNT_NAME_SETTING);
                return;
            }

            string trimmedAccountName = accountName?.Trim();
            if (string.IsNullOrWhiteSpace(trimmedAccountName))
                ClientSettingsManager.DeleteSetting(REMEMBERED_ACCOUNT_NAME_SETTING);
            else
                ClientSettingsManager.SetSetting(
                    REMEMBERED_ACCOUNT_NAME_SETTING,
                    trimmedAccountName);
        }

        /// <summary>Shows a bounded login failure and restores the ordinary login state.</summary>
        internal void ShowLoginFailure(string message)
        {
            SetPrimaryMenuChromeVisible(true);
            ShowLoginToast(message);

            _loginButton.EnableButton();
            _worldSelectionPanel?.HideImmediate();
        }

        /// <summary>Initializes and shows the in-place World Selection panel.</summary>
        internal void ShowWorldSelection(
            ClientMasterSelectionProtocol.WorldSelectionCatalogueResponse catalogue)
        {
            if (_worldSelectionPanel == null)
            {
                ShowLoginFailure("World selection is unavailable.");
                return;
            }

            try
            {
                _worldSelectionPanel.Initialize(catalogue);
                HideLoginToastImmediate();
                SetPrimaryMenuChromeVisible(false);
                _worldSelectionPanel.Show();
            }
            catch (InvalidOperationException)
            {
                ShowLoginFailure("World selection is unavailable.");
            }
        }

        private void ShowLoginToast(string message)
        {
            if (_loginToastCanvasGroup == null || _loginToastText == null)
                throw new InvalidOperationException(
                    "UI_MainMenuWindow requires serialized login toast CanvasGroup and TMP_Text references.");

            if (_loginToastCoroutine != null)
            {
                StopCoroutine(_loginToastCoroutine);
                _loginToastCoroutine = null;
            }

            _loginToastText.text = BoundStatusMessage(message);
            _loginToastCanvasGroup.alpha = 1f;
            _loginToastCanvasGroup.interactable = false;
            _loginToastCanvasGroup.blocksRaycasts = false;
            _loginToastCoroutine = StartCoroutine(LoginToastLifetime());
        }

        private IEnumerator LoginToastLifetime()
        {
            yield return new WaitForSecondsRealtime(LOGIN_TOAST_VISIBLE_SECONDS);

            float elapsed = 0f;
            while (elapsed < LOGIN_TOAST_FADE_SECONDS)
            {
                elapsed += Time.unscaledDeltaTime;
                _loginToastCanvasGroup.alpha = Mathf.Lerp(
                    1f,
                    0f,
                    Mathf.Clamp01(elapsed / LOGIN_TOAST_FADE_SECONDS));
                yield return null;
            }

            _loginToastCanvasGroup.alpha = 0f;
            _loginToastText.text = string.Empty;
            _loginToastCoroutine = null;
        }

        private void HideLoginToastImmediate()
        {
            if (_loginToastCoroutine != null)
            {
                StopCoroutine(_loginToastCoroutine);
                _loginToastCoroutine = null;
            }

            if (_loginToastCanvasGroup != null)
            {
                _loginToastCanvasGroup.alpha = 0f;
                _loginToastCanvasGroup.interactable = false;
                _loginToastCanvasGroup.blocksRaycasts = false;
            }

            if (_loginToastText != null)
                _loginToastText.text = string.Empty;
        }

        internal UI_WorldSelectionPanel GetWorldSelectionPanel() => _worldSelectionPanel;

        private static string BoundStatusMessage(string message)
        {
            string boundedMessage = string.IsNullOrWhiteSpace(message)
                ? "Master unavailable."
                : message.Trim();

            if (boundedMessage.Length > LOGIN_TOAST_MAX_LENGTH)
                boundedMessage = boundedMessage.Substring(0, LOGIN_TOAST_MAX_LENGTH);

            return boundedMessage;
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
