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

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_MainMenuWindow</c> implementation.
    /// </summary>
    public class UI_MainMenuWindow : UI_Window
    {
        private const int MOTD_TIMEOUT_SECONDS = 5;
        private const int MOTD_MAX_LENGTH = 2048;
        private const int LOGIN_STATUS_MAX_LENGTH = 256;

        private static UI_MainMenuWindow _instance;
        private Coroutine _motdCoroutine;

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
        [SerializeField] private TMP_Text _loginStatusText;
        [SerializeField] private UI_WorldSelectionPanel _worldSelectionPanel;

        [Header("UI References - MOTD")]
        [SerializeField] private TMP_Text _messageOfTheDayText;

        [Header("UI References - Build Information")]
        [SerializeField] private TMP_Text _buildInfoText;

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

            RefreshBuildInfo();
            RefreshMessageOfTheDay();
        }

        public override void Show()
        {
            _loginButton.EnableButton();
            _worldSelectionPanel?.HideImmediate();
            base.Show();
            RefreshMessageOfTheDay();
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
        /// <summary>Shows a bounded login failure and restores the ordinary login state.</summary>
        internal void ShowLoginFailure(string message)
        {
            if (_loginStatusText != null)
                _loginStatusText.text = BoundStatusMessage(message);

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
                _loginStatusText?.SetText(string.Empty);
                _worldSelectionPanel.Initialize(catalogue);
                _worldSelectionPanel.Show();
            }
            catch (InvalidOperationException)
            {
                ShowLoginFailure("World selection is unavailable.");
            }
        }

        internal UI_WorldSelectionPanel GetWorldSelectionPanel() => _worldSelectionPanel;

        private static string BoundStatusMessage(string message)
        {
            string boundedMessage = string.IsNullOrWhiteSpace(message)
                ? "Master unavailable."
                : message.Trim();

            if (boundedMessage.Length > LOGIN_STATUS_MAX_LENGTH)
                boundedMessage = boundedMessage.Substring(0, LOGIN_STATUS_MAX_LENGTH);

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
