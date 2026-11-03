using Authentication = Game.Shared.Networking.LoginAuthenticator;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Performance;
using Newtonsoft.Json;
using System.Text;
using LoadingScreen = Game.Client.UI.LoadingScreenController;
using MainMenu = Game.Client.UI.UI_MainMenuWindow;
using WorldSelection = Game.Client.UI.WorldSelection.UI_WorldSelectionPanel;
using PurrNet;
using PurrNet.Transports;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;

namespace Game.Client
{
    /// <summary>
    /// Defines the client-side <c>ConnectionState</c> options.
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected
    }

    /// <summary>
    /// Provides the client-side <c>ClientConnectionManager</c> implementation.
    /// </summary>
    public class ClientConnectionManager : MonoBehaviour
    {
        private const float CONNECTION_ESTABLISHED_PROGRESS = 0.50f;
        private const float CHARACTER_LIST_READY_PROGRESS = 0.75f;
        private const float UNITY_SCENE_LOAD_READY_PROGRESS = 0.90f;
        private const int MASTER_REQUEST_TIMEOUT_SECONDS = 10;
        private const string CHARACTER_SELECTION_BACKGROUND_SCENE_PREFIX = "CharacterSelectionBackground_";
        private const string CHARACTER_SELECTION_BACKGROUND_SCENE_PATH =
            "Assets/Runtime/Client/Scenes/CharacterSelection/";

        private static ClientConnectionManager _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static ClientConnectionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new();
                }
                return _instance;
            }
        }
        private static bool _initialized = false;
        /// <summary>
        /// Gets the <c>Initialized</c> value.
        /// </summary>
        public static bool Initialized => _initialized;

        private static ConnectionState _connectionState = ConnectionState.Disconnected;
        /// <summary>
        /// Gets the <c>CurrentConnectionState</c> value.
        /// </summary>
        public static ConnectionState CurrentConnectionState => _connectionState;

        private NetworkManager _networkManager;
        private ITransport _transport;
        private bool _authenticatedForCurrentConnection;
        private bool _inWorldForCurrentConnection;
        private bool _logoutInProgress;
        private Coroutine _masterAuthenticationCoroutine;
        private Coroutine _homeWorldUpdateCoroutine;
        private Coroutine _worldEntryCoroutine;
        private string _selectionSessionToken;
        private Guid? _selectedWorldID;
        private readonly HashSet<string> _pendingConnectionUISceneUnloads = new();
        private string _desiredCharacterSelectionBackgroundScene;
        private string _loadedCharacterSelectionBackgroundScene;
        private Coroutine _characterSelectionBackgroundTransitionCoroutine;

        /// <summary>Gets the transient World selected during the pre-World flow.</summary>
        internal Guid? SelectedWorldID => _selectedWorldID;

        #region Lifecycle
        private void Awake()
        {
            if (_instance != null && !ReferenceEquals(_instance, this))
            {
                Debug.Error("ClientConnectionManager is already initialized.");
                Destroy(this);
                return;
            }

            _instance = this;
            ClientInitializeManager();
        }

        private void ClientInitializeManager()
        {
            _networkManager = GetComponent<NetworkManager>();
            _connectionState = _networkManager.clientState == PurrNet.Transports.ConnectionState.Connected
                ? ConnectionState.Connected
                : ConnectionState.Disconnected;
            _authenticatedForCurrentConnection = false;
            _inWorldForCurrentConnection = false;
            _networkManager.onClientConnectionState += OnClientConnectionStateChanged;
            AccountEndpoint.onCharacterListReceived += OnCharacterListReceived;
            AccountEndpoint.onEnteringWorld += OnEnteringWorld;
            _transport = _networkManager.rawTransport;
            if (_transport != null)
            {
                _transport.onDataReceived += OnTransportDataReceived;
                _transport.onDataSent += OnTransportDataSent;
            }
            SceneManager.sceneLoaded += OnConnectionUISceneLoaded;
            _initialized = true;
        }

        private void OnDestroy()
        {
            if (!ReferenceEquals(_instance, this))
                return;

            SceneManager.sceneLoaded -= OnConnectionUISceneLoaded;

            if (_networkManager != null)
                _networkManager.onClientConnectionState -= OnClientConnectionStateChanged;

            AccountEndpoint.onCharacterListReceived -= OnCharacterListReceived;
            AccountEndpoint.onEnteringWorld -= OnEnteringWorld;

            if (_transport != null)
            {
                _transport.onDataReceived -= OnTransportDataReceived;
                _transport.onDataSent -= OnTransportDataSent;
                _transport = null;
            }

            _instance = null;
            _connectionState = ConnectionState.Disconnected;
            _authenticatedForCurrentConnection = false;
            _inWorldForCurrentConnection = false;
            ClearPreWorldSelectionState();
            _initialized = false;
        }

        private void OnTransportDataReceived(Connection connection, ByteData data, bool asServer)
        {
            if (!asServer && PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(NetworkingPerformanceMetrics.CLIENT_TRANSPORT_PAYLOAD_BYTES_RECEIVED, data.length);
        }

        private void OnTransportDataSent(Connection connection, ByteData data, bool asServer)
        {
            if (!asServer && PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(NetworkingPerformanceMetrics.CLIENT_TRANSPORT_PAYLOAD_BYTES_SENT, data.length);
        }
        #endregion

        private void OnClientConnectionStateChanged(PurrNet.Transports.ConnectionState state)
        {
            switch (state)
            {
                case PurrNet.Transports.ConnectionState.Connected:
                    if (_connectionState == ConnectionState.Connected)
                        break;

                    _connectionState = ConnectionState.Connected;
                    _authenticatedForCurrentConnection = false;
                    _inWorldForCurrentConnection = false;
                    if (PerformanceTelemetry.IsEnabled)
                    {
                        PerformanceTelemetry.TryRecord(NetworkingPerformanceMetrics.CLIENT_CONNECTED_COUNT, 1);
                        RecordClientLiveConnectionCount();
                    }
                    Debug.Log(" ✓ Connected to the server.");
                    if (!TryGetComponent<ClientAccountManager>(out _))
                        this.gameObject.AddComponent<ClientAccountManager>();
                    break;

                case PurrNet.Transports.ConnectionState.Disconnected:
                    _logoutInProgress = false;
                    var wasConnecting = _connectionState == ConnectionState.Connecting;
                    var wasActive = _connectionState != ConnectionState.Disconnected;
                    _connectionState = ConnectionState.Disconnected;
                    _authenticatedForCurrentConnection = false;
                    _inWorldForCurrentConnection = false;
                    ClearCharacterSelectionBackground();
                    if (PerformanceTelemetry.IsEnabled)
                    {
                        if (wasActive)
                            PerformanceTelemetry.TryRecord(NetworkingPerformanceMetrics.CLIENT_DISCONNECTED_COUNT, 1);
                        RecordClientLiveConnectionCount();
                    }

                    if (wasConnecting) Debug.Warning("Connection failed (authentication rejected or server unreachable).");
                    else Debug.Warning("Disconnected from server.");

                    if (ClientWorldManager.Initialized)
                        ClientWorldManager.Instance.EndWorldPresentation();

                    // Dismiss the loading screen and return to the main menu.
                    // Destroy the client account manager to clear any character data, etc. that may have been loaded.
                    LoadingScreen.Instance.Finish();
                    ClearPreWorldSelectionState();
                    MainMenu.Instance.Show();
                    if (TryGetComponent<ClientAccountManager>(out var accountManager))
                    {
                        Destroy(accountManager);
                    }
                    CleanupConnectionUIScenes();
                    break;
            }
        }

        private void OnConnectionUISceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!IsConnectionUISceneName(scene.name))
                return;

            if (CurrentConnectionState != ConnectionState.Disconnected)
                return;

            RequestConnectionUISceneUnload(scene.name);
        }

        private void CleanupConnectionUIScenes()
        {
            RequestConnectionUISceneUnload("CharacterSelection");
            RequestConnectionUISceneUnload("CharacterCreation");
            RequestConnectionUISceneUnload("CharacterSelectionBackground_arhin");
            RequestConnectionUISceneUnload("CharacterSelectionBackground_eldani");
        }

        internal void SelectCharacterSelectionBackground(string raceID)
        {
            string sceneName = null;
            if (!string.IsNullOrEmpty(raceID) &&
                !TryGetCharacterSelectionBackgroundSceneName(raceID, out sceneName))
            {
                _desiredCharacterSelectionBackgroundScene = null;
                EnsureCharacterSelectionBackgroundTransition();
                return;
            }

            _desiredCharacterSelectionBackgroundScene = sceneName;
            EnsureCharacterSelectionBackgroundTransition();
        }

        internal void ClearCharacterSelectionBackground()
        {
            _desiredCharacterSelectionBackgroundScene = null;
            EnsureCharacterSelectionBackgroundTransition();
        }

        private void EnsureCharacterSelectionBackgroundTransition()
        {
            if (_characterSelectionBackgroundTransitionCoroutine == null)
            {
                _characterSelectionBackgroundTransitionCoroutine = StartCoroutine(
                    SettleCharacterSelectionBackground());
            }
        }

        private IEnumerator SettleCharacterSelectionBackground()
        {
            while (true)
            {
                if (CurrentConnectionState == ConnectionState.Disconnected)
                    _desiredCharacterSelectionBackgroundScene = null;

                string desiredScene = _desiredCharacterSelectionBackgroundScene;
                string loadedScene = _loadedCharacterSelectionBackgroundScene;
                if (!string.IsNullOrEmpty(loadedScene) &&
                    !string.Equals(loadedScene, desiredScene, StringComparison.Ordinal))
                {
                    AsyncOperation unloadOperation = RequestConnectionUISceneUnload(loadedScene);
                    if (unloadOperation != null)
                        yield return unloadOperation;
                    else
                    {
                        while (_pendingConnectionUISceneUnloads.Contains(loadedScene))
                            yield return null;
                    }

                    if (string.Equals(
                            _loadedCharacterSelectionBackgroundScene,
                            loadedScene,
                            StringComparison.Ordinal))
                    {
                        _loadedCharacterSelectionBackgroundScene = null;
                    }

                    continue;
                }

                if (string.IsNullOrEmpty(desiredScene))
                    break;

                if (!string.Equals(
                        _loadedCharacterSelectionBackgroundScene,
                        desiredScene,
                        StringComparison.Ordinal))
                {
                    Scene existingScene = SceneManager.GetSceneByName(desiredScene);
                    if (existingScene.IsValid() && existingScene.isLoaded)
                    {
                        _loadedCharacterSelectionBackgroundScene = desiredScene;
                        continue;
                    }

                    AsyncOperation loadOperation;
                    try
                    {
                        loadOperation = SceneManager.LoadSceneAsync(desiredScene, LoadSceneMode.Additive);
                    }
                    catch (Exception exception)
                    {
                        Debug.Error(
                            $"Failed to load character-selection background '{desiredScene}': "
                            + $"{exception.GetType().Name}: {exception.Message}");
                        if (string.Equals(
                                _desiredCharacterSelectionBackgroundScene,
                                desiredScene,
                                StringComparison.Ordinal))
                        {
                            _desiredCharacterSelectionBackgroundScene = null;
                        }

                        continue;
                    }

                    if (loadOperation == null)
                    {
                        Debug.Error(
                            $"Failed to start character-selection background scene load for '{desiredScene}'.");
                        if (string.Equals(
                                _desiredCharacterSelectionBackgroundScene,
                                desiredScene,
                                StringComparison.Ordinal))
                        {
                            _desiredCharacterSelectionBackgroundScene = null;
                        }

                        continue;
                    }

                    _loadedCharacterSelectionBackgroundScene = desiredScene;
                    yield return loadOperation;

                    bool isStillDesired =
                        CurrentConnectionState != ConnectionState.Disconnected
                        && string.Equals(
                            _desiredCharacterSelectionBackgroundScene,
                            desiredScene,
                            StringComparison.Ordinal);
                    Scene loadedResult = SceneManager.GetSceneByName(desiredScene);
                    if (!isStillDesired)
                    {
                        if (loadedResult.IsValid() && loadedResult.isLoaded)
                        {
                            AsyncOperation unloadOperation = RequestConnectionUISceneUnload(desiredScene);
                            if (unloadOperation != null)
                                yield return unloadOperation;
                            else
                            {
                                while (_pendingConnectionUISceneUnloads.Contains(desiredScene))
                                    yield return null;
                            }
                        }

                        if (string.Equals(
                                _loadedCharacterSelectionBackgroundScene,
                                desiredScene,
                                StringComparison.Ordinal))
                        {
                            _loadedCharacterSelectionBackgroundScene = null;
                        }

                        continue;
                    }

                    if (!loadedResult.IsValid() || !loadedResult.isLoaded)
                    {
                        Debug.Error(
                            $"Character-selection background scene '{desiredScene}' did not finish loading.");
                        _loadedCharacterSelectionBackgroundScene = null;
                        _desiredCharacterSelectionBackgroundScene = null;
                    }

                    continue;
                }

                if (_pendingConnectionUISceneUnloads.Contains(desiredScene))
                {
                    while (_pendingConnectionUISceneUnloads.Contains(desiredScene))
                        yield return null;

                    if (string.Equals(
                            _loadedCharacterSelectionBackgroundScene,
                            desiredScene,
                            StringComparison.Ordinal))
                    {
                        _loadedCharacterSelectionBackgroundScene = null;
                    }

                    continue;
                }

                Scene currentScene = SceneManager.GetSceneByName(desiredScene);
                if (!currentScene.IsValid() || !currentScene.isLoaded)
                {
                    Debug.Error(
                        $"Character-selection background scene '{desiredScene}' is no longer loaded.");
                    _loadedCharacterSelectionBackgroundScene = null;
                    _desiredCharacterSelectionBackgroundScene = null;
                    continue;
                }

                break;
            }

            _characterSelectionBackgroundTransitionCoroutine = null;
        }

        private static bool TryGetCharacterSelectionBackgroundSceneName(
            string raceID,
            out string sceneName)
        {
            sceneName = null;

            RaceDefinition raceDefinition;
            try
            {
                raceDefinition = RaceDefinitionLibrary.Instance.GetDefinition(raceID);
            }
            catch (Exception exception)
            {
                Debug.Error(
                    $"Cannot resolve character-selection background for race '{raceID}': "
                    + $"{exception.GetType().Name}: {exception.Message}");
                return false;
            }

            if (raceDefinition == null || !raceDefinition.IsPlayable)
            {
                Debug.Error($"No playable race definition exists for character-selection race '{raceID}'.");
                return false;
            }

            sceneName = raceID switch
            {
                "arhin" => $"{CHARACTER_SELECTION_BACKGROUND_SCENE_PREFIX}arhin",
                "eldani" => $"{CHARACTER_SELECTION_BACKGROUND_SCENE_PREFIX}eldani",
                _ => null
            };
            if (sceneName == null)
            {
                Debug.Error(
                    $"No character-selection background scene is authorized for playable race '{raceID}'.");
                return false;
            }

            string scenePath = $"{CHARACTER_SELECTION_BACKGROUND_SCENE_PATH}{sceneName}.unity";
            if (SceneUtility.GetBuildIndexByScenePath(scenePath) < 0)
            {
                Debug.Error(
                    $"Character-selection background scene '{sceneName}' is missing or disabled in Build Settings.");
                sceneName = null;
                return false;
            }

            return true;
        }

        private static bool IsConnectionUISceneName(string sceneName)
        {
            return sceneName == "CharacterSelection"
                || sceneName == "CharacterCreation"
                || sceneName == "CharacterSelectionBackground_arhin"
                || sceneName == "CharacterSelectionBackground_eldani";
        }

        private AsyncOperation RequestConnectionUISceneUnload(string sceneName)
        {
            if (!IsConnectionUISceneName(sceneName))
                throw new ArgumentException($"Unsupported connection UI scene: {sceneName}", nameof(sceneName));

            if (_pendingConnectionUISceneUnloads.Contains(sceneName))
                return null;

            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            _pendingConnectionUISceneUnloads.Add(sceneName);
            AsyncOperation unloadOperation;
            try
            {
                unloadOperation = SceneManager.UnloadSceneAsync(scene);
            }
            catch (Exception exception)
            {
                _pendingConnectionUISceneUnloads.Remove(sceneName);
                Debug.Error(
                    $"Failed to unload connection UI scene '{sceneName}': "
                    + $"{exception.GetType().Name}: {exception.Message}");
                return null;
            }

            if (unloadOperation == null)
            {
                _pendingConnectionUISceneUnloads.Remove(sceneName);
                Debug.Error($"Failed to request unload for connection UI scene '{sceneName}'.");
                return null;
            }

            unloadOperation.completed += _ => _pendingConnectionUISceneUnloads.Remove(sceneName);
            return unloadOperation;
        }

        /// <summary>Requests the local client disconnect from pre-World Character Selection.</summary>
        public void LogoutToMainMenu()
        {
            if (_connectionState != ConnectionState.Connected || _inWorldForCurrentConnection)
            {
                Debug.Warning("Cannot logout to Main Menu while the client is not in pre-World Character Selection.");
                return;
            }

            if (_logoutInProgress)
            {
                Debug.Warning("Logout to Main Menu is already in progress.");
                return;
            }

            _logoutInProgress = true;
            _networkManager.StopClient();
        }

        /// <summary>
        /// Executes the client-side <c>ConnectToServer</c> operation.
        /// </summary>
        public void ConnectToServer(string usernameInput = "Player", string passwordInput = "password")
        {
            if (_connectionState != ConnectionState.Disconnected)
            {
                Debug.Warning(" ✗ Already connecting/connected to server. Aborting.");
                return;
            }

            _connectionState = ConnectionState.Connecting;
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(NetworkingPerformanceMetrics.CLIENT_CONNECTION_ATTEMPT_COUNT, 1);
            StartCoroutine(ConnectionProcess(usernameInput, passwordInput));
        }

        /// <summary>Authenticates against the Master before requesting the World catalogue.</summary>
        public void AuthenticateWithMaster(string username, string password)
        {
            if (_masterAuthenticationCoroutine != null)
            {
                Debug.Warning("Master authentication is already in progress. Aborting duplicate request.");
                return;
            }

            ClearSelectionTokenAndWorld();
            _masterAuthenticationCoroutine = StartCoroutine(MasterAuthenticationProcess(username, password));
        }

        /// <summary>Requests an expected-revision-controlled Home World change from the Master.</summary>
        internal void RequestHomeWorldChange(Guid worldID, long expectedAccountRevision)
        {
            if (_homeWorldUpdateCoroutine != null)
            {
                Debug.Warning("A Home World update is already in progress. Aborting duplicate request.");
                return;
            }

            if (worldID == Guid.Empty || expectedAccountRevision < 1)
            {
                ShowWorldSelectionStatus("Home World update could not be requested.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_selectionSessionToken))
            {
                ShowWorldSelectionStatus("Selection session expired.");
                return;
            }

            _homeWorldUpdateCoroutine = StartCoroutine(
                HomeWorldUpdateProcess(worldID, expectedAccountRevision));
        }

        /// <summary>Selects a World locally for the later allocation handoff.</summary>
        internal void SelectWorld(Guid worldID)
        {
            if (worldID == Guid.Empty)
            {
                Debug.Warning("Cannot select an empty World ID.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_selectionSessionToken))
            {
                Debug.Warning("Cannot select a World without an active selection session.");
                return;
            }

            _selectedWorldID = worldID;
        }

        /// <summary>Requests Master approval to enter the selected World.</summary>
        internal void RequestWorldEntry(Guid worldID)
        {
            if (_worldEntryCoroutine != null)
            {
                Debug.Warning("A World-entry request is already in progress. Aborting duplicate request.");
                return;
            }

            if (worldID == Guid.Empty || !_selectedWorldID.HasValue || _selectedWorldID.Value != worldID)
            {
                Debug.Warning("Cannot request World entry without the selected World ID.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_selectionSessionToken))
            {
                Debug.Warning("Cannot request World entry without an active selection session.");
                return;
            }

            if (_connectionState != ConnectionState.Disconnected ||
                _networkManager == null ||
                _networkManager.clientState != PurrNet.Transports.ConnectionState.Disconnected)
            {
                Debug.Warning("Cannot request World entry while the client is connecting or connected.");
                return;
            }

            _worldEntryCoroutine = StartCoroutine(WorldEntryProcess(worldID));
        }

        /// <summary>Clears the transient World selection without ending the selection session.</summary>
        internal void ClearSelectedWorld()
        {
            _selectedWorldID = null;
        }

        /// <summary>Clears all transient pre-World selection state and active Master requests.</summary>
        internal void ClearPreWorldSelectionState()
        {
            if (_masterAuthenticationCoroutine != null)
            {
                StopCoroutine(_masterAuthenticationCoroutine);
                _masterAuthenticationCoroutine = null;
            }

            if (_homeWorldUpdateCoroutine != null)
            {
                StopCoroutine(_homeWorldUpdateCoroutine);
                _homeWorldUpdateCoroutine = null;
            }

            if (_worldEntryCoroutine != null)
            {
                StopCoroutine(_worldEntryCoroutine);
                _worldEntryCoroutine = null;
            }

            ClearSelectionTokenAndWorld();
        }

        private IEnumerator MasterAuthenticationProcess(string username, string password)
        {
            if (!GameContentBootstrap.IsInitialized)
            {
                FailMasterAuthentication("Master unavailable.");
                yield break;
            }

            if (!ClientMasterEndpointUtility.TryCreateEndpoint(
                    "client/v1/authenticate",
                    out Uri endpoint,
                    out _))
            {
                FailMasterAuthentication("Master unavailable.");
                yield break;
            }

            ClientMasterSelectionProtocol.ClientAuthenticationRequest payload = new()
            {
                LegacyUsername = username,
                Password = password
            };

            ClientMasterSelectionProtocol.ClientAuthenticationResponse response;
            if (!TrySerialize(payload, out string authenticationJson))
            {
                FailMasterAuthentication("Malformed or unavailable service.");
                yield break;
            }

            using (UnityWebRequest request = CreateJsonPostRequest(
                       endpoint,
                       authenticationJson))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.DataProcessingError)
                {
                    FailMasterAuthentication("Master unavailable.");
                    yield break;
                }

                if (!TryDeserialize(request.downloadHandler.text, out response))
                {
                    FailMasterAuthentication("Malformed or unavailable service.");
                    yield break;
                }

                if (response.Result == ClientMasterSelectionProtocol.ClientAuthenticationResult.Accepted &&
                    request.responseCode != 200)
                {
                    FailMasterAuthentication("Malformed or unavailable service.");
                    yield break;
                }
            }

            if (response.Result != ClientMasterSelectionProtocol.ClientAuthenticationResult.Accepted)
            {
                FailMasterAuthentication(GetAuthenticationFailureMessage(response.Result));
                yield break;
            }

            if (string.IsNullOrWhiteSpace(response.SelectionSessionToken) ||
                !response.ExpiresAtUtc.HasValue ||
                response.ExpiresAtUtc.Value <= DateTimeOffset.UtcNow)
            {
                FailMasterAuthentication("Malformed or unavailable service.");
                yield break;
            }

            _selectionSessionToken = response.SelectionSessionToken;
            yield return RequestWorldCatalogue();

            if (_masterAuthenticationCoroutine != null)
                _masterAuthenticationCoroutine = null;
        }

        private IEnumerator WorldEntryProcess(Guid worldID)
        {
            if (!ClientMasterEndpointUtility.TryCreateEndpoint(
                    "client/v1/worlds/enter",
                    out Uri endpoint,
                    out _))
            {
                FailWorldEntry("Master unavailable.");
                yield break;
            }

            ClientMasterSelectionProtocol.WorldEntryRequest payload = new()
            {
                WorldID = worldID
            };

            if (!TrySerialize(payload, out string worldEntryJson))
            {
                FailWorldEntry("Malformed or unavailable service.");
                yield break;
            }

            ClientMasterSelectionProtocol.WorldEntryResponse response;
            using (UnityWebRequest request = CreateJsonPostRequest(endpoint, worldEntryJson))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_selectionSessionToken}");
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.DataProcessingError)
                {
                    FailWorldEntry("Master unavailable.");
                    yield break;
                }

                if (!TryDeserialize(request.downloadHandler.text, out response))
                {
                    FailWorldEntry("Malformed or unavailable service.");
                    yield break;
                }

                if (response.Result == ClientMasterSelectionProtocol.WorldEntryResult.Accepted)
                {
                    if (request.responseCode != 200 || !TryValidateWorldEntryResponse(response, worldID))
                    {
                        FailWorldEntry("Malformed or unavailable service.");
                        yield break;
                    }
                }
            }

            if (response.Result != ClientMasterSelectionProtocol.WorldEntryResult.Accepted)
            {
                FailWorldEntry(
                    GetWorldEntryFailureMessage(response.Result),
                    response.Result == ClientMasterSelectionProtocol.WorldEntryResult.Unauthorized ||
                    response.Result == ClientMasterSelectionProtocol.WorldEntryResult.SessionExpired ||
                    response.Result == ClientMasterSelectionProtocol.WorldEntryResult.SessionAlreadyActive);
                yield break;
            }

            try
            {
                Authentication.SetWorldEntryHandoff(response.HandoffToken);
            }
            catch (Exception exception)
            {
                Debug.Error(
                    $"World-entry authentication preparation failed: {exception.GetType().Name}: {exception.Message}");
                FailWorldEntry("Malformed or unavailable service.");
                yield break;
            }

            string approvedHost = response.ClientHost;
            ushort approvedPort = (ushort)response.ClientPort.Value;
            response.HandoffToken = null;
            _selectionSessionToken = null;
            _worldEntryCoroutine = null;
            _connectionState = ConnectionState.Connecting;
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(NetworkingPerformanceMetrics.CLIENT_CONNECTION_ATTEMPT_COUNT, 1);

            yield return PreparedConnectionProcess(approvedHost, approvedPort);
        }

        private IEnumerator RequestWorldCatalogue()
        {
            if (!ClientMasterEndpointUtility.TryCreateEndpoint(
                    "client/v1/worlds",
                    out Uri endpoint,
                    out _))
            {
                FailMasterAuthentication("Master unavailable.");
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequest.Get(endpoint.AbsoluteUri))
            {
                request.timeout = MASTER_REQUEST_TIMEOUT_SECONDS;
                request.SetRequestHeader("Authorization", $"Bearer {_selectionSessionToken}");
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.DataProcessingError)
                {
                    FailMasterAuthentication("Master unavailable.");
                    yield break;
                }

                if (!TryDeserialize(
                        request.downloadHandler.text,
                        out ClientMasterSelectionProtocol.WorldSelectionCatalogueResponse response))
                {
                    FailMasterAuthentication("Malformed or unavailable service.");
                    yield break;
                }

                if (request.responseCode != 200 ||
                    response.Result != ClientMasterSelectionProtocol.WorldSelectionResult.Accepted ||
                    !TryValidateCatalogue(response))
                {
                    FailMasterAuthentication(GetCatalogueFailureMessage(response.Result));
                    yield break;
                }

                MainMenu.Instance?.ShowWorldSelection(response);
            }
        }

        private IEnumerator HomeWorldUpdateProcess(Guid worldID, long expectedAccountRevision)
        {
            if (!ClientMasterEndpointUtility.TryCreateEndpoint(
                    "client/v1/worlds/home",
                    out Uri endpoint,
                    out _))
            {
                FailHomeWorldUpdate("Master unavailable.");
                yield break;
            }

            ClientMasterSelectionProtocol.HomeWorldUpdateRequest payload = new()
            {
                WorldID = worldID,
                ExpectedAccountRevision = expectedAccountRevision
            };

            if (!TrySerialize(payload, out string homeWorldJson))
            {
                FailHomeWorldUpdate("Malformed or unavailable service.");
                yield break;
            }

            using (UnityWebRequest request = CreateJsonPostRequest(
                       endpoint,
                       homeWorldJson))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_selectionSessionToken}");
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.DataProcessingError)
                {
                    FailHomeWorldUpdate("Master unavailable.");
                    yield break;
                }

                if (!TryDeserialize(
                        request.downloadHandler.text,
                        out ClientMasterSelectionProtocol.HomeWorldUpdateResponse response))
                {
                    FailHomeWorldUpdate("Malformed or unavailable service.");
                    yield break;
                }

                if (response.Result == ClientMasterSelectionProtocol.WorldSelectionResult.Unauthorized ||
                    response.Result == ClientMasterSelectionProtocol.WorldSelectionResult.SessionExpired)
                {
                    FailHomeWorldUpdate("Selection session expired. Please log in again.", true);
                    yield break;
                }

                if (request.responseCode != 200 ||
                    response.Result != ClientMasterSelectionProtocol.WorldSelectionResult.Accepted ||
                    !response.HomeWorldID.HasValue ||
                    response.HomeWorldID.Value == Guid.Empty ||
                    !response.AccountRevision.HasValue ||
                    response.AccountRevision.Value < 1)
                {
                    FailHomeWorldUpdate(GetHomeWorldFailureMessage(response.Result));
                    yield break;
                }

                WorldSelection panel = MainMenu.Instance?.GetWorldSelectionPanel();
                if (panel == null)
                {
                    FailHomeWorldUpdate("World selection is unavailable.");
                    yield break;
                }

                panel.ApplyHomeWorldUpdate(
                    response.HomeWorldID.Value,
                    response.AccountRevision.Value);
            }

            _homeWorldUpdateCoroutine = null;
        }

        private void FailMasterAuthentication(string message)
        {
            ClearSelectionTokenAndWorld();
            _masterAuthenticationCoroutine = null;
            MainMenu.Instance?.ShowLoginFailure(message);
        }

        private void FailHomeWorldUpdate(string message, bool returnToLogin = false)
        {
            _homeWorldUpdateCoroutine = null;
            if (returnToLogin)
            {
                ClearSelectionTokenAndWorld();
                MainMenu.Instance?.ShowLoginFailure(message);
                return;
            }

            ShowWorldSelectionStatus(message);
        }

        private void FailWorldEntry(string message, bool returnToLogin = false)
        {
            _worldEntryCoroutine = null;
            if (returnToLogin)
            {
                ClearPreWorldSelectionState();
                MainMenu.Instance?.ShowLoginFailure(message);
                return;
            }

            MainMenu.Instance?.GetWorldSelectionPanel()?.SetWorldEntryFailure(message);
        }

        private void ShowWorldSelectionStatus(string message)
        {
            MainMenu.Instance?.GetWorldSelectionPanel()?.ShowStatus(message);
        }

        private static UnityWebRequest CreateJsonPostRequest(Uri endpoint, string json)
        {
            UnityWebRequest request = new(endpoint.AbsoluteUri, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = MASTER_REQUEST_TIMEOUT_SECONDS
            };
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
        }

        private static bool TryDeserialize<T>(string json, out T response) where T : class
        {
            response = null;
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                response = JsonConvert.DeserializeObject<T>(json);
                return response != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool TrySerialize<T>(T payload, out string json)
        {
            json = null;
            try
            {
                json = JsonConvert.SerializeObject(payload);
                return !string.IsNullOrEmpty(json);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool TryValidateCatalogue(
            ClientMasterSelectionProtocol.WorldSelectionCatalogueResponse response)
        {
            if (!response.HomeWorldID.HasValue ||
                response.HomeWorldID.Value == Guid.Empty ||
                !response.AccountRevision.HasValue ||
                response.AccountRevision.Value < 1 ||
                response.Worlds == null)
            {
                return false;
            }

            HashSet<Guid> worldIDs = new();
            foreach (ClientMasterSelectionProtocol.WorldSelectionWorld world in response.Worlds)
            {
                if (world == null || world.WorldID == Guid.Empty || !worldIDs.Add(world.WorldID))
                    return false;
            }

            return true;
        }

        private static bool TryValidateWorldEntryResponse(
            ClientMasterSelectionProtocol.WorldEntryResponse response,
            Guid requestedWorldID)
        {
            return response.WorldID.HasValue &&
                response.WorldID.Value == requestedWorldID &&
                !string.IsNullOrWhiteSpace(response.ClientHost) &&
                response.ClientPort.HasValue &&
                response.ClientPort.Value >= 1 &&
                response.ClientPort.Value <= ushort.MaxValue &&
                IsLowercaseHex(response.HandoffToken, 64) &&
                response.ExpiresAtUtc.HasValue &&
                response.ExpiresAtUtc.Value > DateTimeOffset.UtcNow;
        }

        private static bool IsLowercaseHex(string value, int expectedLength)
        {
            if (value == null || value.Length != expectedLength)
                return false;

            foreach (char character in value)
            {
                if (!(character >= '0' && character <= '9') &&
                    !(character >= 'a' && character <= 'f'))
                {
                    return false;
                }
            }

            return true;
        }

        private static string GetAuthenticationFailureMessage(
            ClientMasterSelectionProtocol.ClientAuthenticationResult result)
        {
            return result switch
            {
                ClientMasterSelectionProtocol.ClientAuthenticationResult.InvalidCredentials => "Invalid credentials.",
                ClientMasterSelectionProtocol.ClientAuthenticationResult.AuthenticationThrottled =>
                    "Too many attempts. Please try again later.",
                ClientMasterSelectionProtocol.ClientAuthenticationResult.SessionAlreadyActive =>
                    "Account already active.",
                _ => "Malformed or unavailable service."
            };
        }

        private static string GetCatalogueFailureMessage(
            ClientMasterSelectionProtocol.WorldSelectionResult result)
        {
            return result switch
            {
                ClientMasterSelectionProtocol.WorldSelectionResult.Unauthorized =>
                    "Selection session expired. Please log in again.",
                ClientMasterSelectionProtocol.WorldSelectionResult.SessionExpired =>
                    "Selection session expired. Please log in again.",
                ClientMasterSelectionProtocol.WorldSelectionResult.PersistenceUnavailable => "Master unavailable.",
                _ => "Malformed or unavailable service."
            };
        }

        private static string GetHomeWorldFailureMessage(
            ClientMasterSelectionProtocol.WorldSelectionResult result)
        {
            return result switch
            {
                ClientMasterSelectionProtocol.WorldSelectionResult.RevisionConflict =>
                    "Home World changed elsewhere. Refresh and try again.",
                ClientMasterSelectionProtocol.WorldSelectionResult.PersistenceUnavailable => "Master unavailable.",
                _ => "Home World could not be changed."
            };
        }

        private static string GetWorldEntryFailureMessage(
            ClientMasterSelectionProtocol.WorldEntryResult result)
        {
            return result switch
            {
                ClientMasterSelectionProtocol.WorldEntryResult.Unauthorized =>
                    "Selection session expired. Please log in again.",
                ClientMasterSelectionProtocol.WorldEntryResult.SessionExpired =>
                    "Selection session expired. Please log in again.",
                ClientMasterSelectionProtocol.WorldEntryResult.WorldUnavailable =>
                    "World is currently unavailable.",
                ClientMasterSelectionProtocol.WorldEntryResult.WorldAccessDenied =>
                    "You cannot access this World.",
                ClientMasterSelectionProtocol.WorldEntryResult.SessionAlreadyActive => "Account already active.",
                ClientMasterSelectionProtocol.WorldEntryResult.PersistenceUnavailable => "Master unavailable.",
                _ => "Unable to enter the selected World."
            };
        }

        private void ClearSelectionTokenAndWorld()
        {
            _selectionSessionToken = null;
            _selectedWorldID = null;
        }

        private void OnCharacterListReceived(System.Collections.Generic.List<Game.Shared.Persistence.CharacterData> characterList)
        {
            if (_authenticatedForCurrentConnection)
                return;

            _authenticatedForCurrentConnection = true;
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(NetworkingPerformanceMetrics.CLIENT_AUTHENTICATED_COUNT, 1);
        }

        private void OnEnteringWorld(Game.Shared.Persistence.CharacterData characterData, string sceneName)
        {
            if (_inWorldForCurrentConnection)
                return;

            _inWorldForCurrentConnection = true;
            if (PerformanceTelemetry.IsEnabled)
                PerformanceTelemetry.TryRecord(NetworkingPerformanceMetrics.CLIENT_IN_WORLD_COUNT, 1);
        }

        private void RecordClientLiveConnectionCount()
        {
            if (!PerformanceTelemetry.IsEnabled || _networkManager == null)
                return;

            PerformanceTelemetry.TryRecord(
                NetworkingPerformanceMetrics.CLIENT_LIVE_CONNECTION_COUNT,
                _networkManager.clientState == PurrNet.Transports.ConnectionState.Connected ? 1 : 0);
        }

        private IEnumerator ConnectionProcess(string username, string password)
        {
            Debug.Log("Connection process started.");
            try
            {
                Authentication.SetCredentials(username, password);
            }
            catch (Exception exception)
            {
                Debug.Error(
                    $"Client authentication preparation failed: {exception.GetType().Name}: {exception.Message}");
                _connectionState = ConnectionState.Disconnected;
                LoadingScreen.Instance.Finish();
                MainMenu.Instance.Show();
                yield break;
            }

            yield return PreparedConnectionProcess(
                GameConfigurationManager.Config.ServerAddress,
                GameConfigurationManager.Config.ServerPort);
        }

        private IEnumerator PreparedConnectionProcess(string serverAddress, ushort serverPort)
        {
            NetworkManager networkManager = _networkManager ?? GetComponent<NetworkManager>();
            PurrNet.Transports.UDPTransport transport = networkManager == null
                ? null
                : networkManager.GetComponentInChildren<PurrNet.Transports.UDPTransport>();

            // Step 1: Hide the main menu and show the loading screen.
            Debug.Log(" - Step 1: Hiding main menu, showing loading screen...");
            MainMenu.Instance.Hide();
            LoadingScreen.Instance.Show();
            Debug.Log(" ✓ Step 1: Done.");

            if (networkManager == null)
            {
                Debug.Error("Client connection cannot start without a NetworkManager.");
                _connectionState = ConnectionState.Disconnected;
                LoadingScreen.Instance.Finish();
                MainMenu.Instance.Show();
                yield break;
            }

            if (transport == null)
            {
                Debug.Error("Client connection cannot start without a UDPTransport.");
                _connectionState = ConnectionState.Disconnected;
                LoadingScreen.Instance.Finish();
                MainMenu.Instance.Show();
                yield break;
            }

            // Step 2: Connect to the server.
            Debug.Log(" - Step 2: Setting up connection...");
            if (networkManager.clientState == PurrNet.Transports.ConnectionState.Disconnected)
            {
                // Set the address and server port of the transport.
                transport.address = serverAddress;
                transport.serverPort = serverPort;

                // Start the client.
                Debug.Log(" ✓ Step 2: Attempting to connect to server...");
                networkManager.StartClient();

                yield return new WaitUntil(
                    () => _connectionState == ConnectionState.Connected
                        || _connectionState == ConnectionState.Disconnected);
                if (_connectionState != ConnectionState.Connected)
                    yield break;
                LoadingScreen.Instance.SetProgress(CONNECTION_ESTABLISHED_PROGRESS);
                Debug.Log(" ✓ Step 3: Connection process completed. Moving to character selection or creation.");

                yield return new WaitUntil(
                    () => ClientAccountManager.HasReceivedCharacters()
                        || _connectionState == ConnectionState.Disconnected);
                if (_connectionState != ConnectionState.Connected)
                    yield break;
                LoadingScreen.Instance.SetProgress(CHARACTER_LIST_READY_PROGRESS);

                string connectionUISceneName;
                bool hasCharacters =
                    ClientAccountManager.TryGetCharacterList(out var characterList)
                    && characterList.Count > 0;
                if (hasCharacters)
                {
                    Debug.Log($" ✓ Received character list with {characterList.Count} characters. Showing character selection.");
                    connectionUISceneName = "CharacterSelection";
                }
                else
                {
                    Debug.Log(" ✓ Received empty character list. Showing character creation.");
                    connectionUISceneName = "CharacterCreation";
                }

                AsyncOperation sceneLoadOperation =
                    SceneManager.LoadSceneAsync(connectionUISceneName, LoadSceneMode.Additive);
                if (sceneLoadOperation == null)
                {
                    Debug.Error($"Failed to start the connection UI scene load for '{connectionUISceneName}'.");
                    _connectionState = ConnectionState.Disconnected;
                    LoadingScreen.Instance.Finish();
                    MainMenu.Instance.Show();
                    yield break;
                }

                while (!sceneLoadOperation.isDone)
                {
                    if (_connectionState != ConnectionState.Connected)
                        yield break;

                    float normalizedProgress = Mathf.Clamp01(
                        sceneLoadOperation.progress / UNITY_SCENE_LOAD_READY_PROGRESS);
                    float loadingProgress = Mathf.Lerp(
                        CHARACTER_LIST_READY_PROGRESS,
                        1f,
                        normalizedProgress);
                    LoadingScreen.Instance.SetProgress(loadingProgress);
                    yield return null;
                }

                if (_connectionState != ConnectionState.Connected)
                    yield break;

                if (hasCharacters)
                    Game.Client.UI.UI_CharacterSelectWindow.Instance.Show();
                else
                    Game.Client.UI.UI_CharacterCreationWindow.Instance.Show();

                LoadingScreen.Instance.Finish();
            }
            else
            {
                Debug.Error(" ✗ Already connected to server. Aborting.");
                yield break;
            }

            yield return null;
        }
    }
}
