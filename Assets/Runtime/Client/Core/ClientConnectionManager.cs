using Authentication = Game.Shared.Networking.LoginAuthenticator;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Performance;
using LoadingScreen = Game.Client.UI.LoadingScreenController;
using MainMenu = Game.Client.UI.UI_MainMenuWindow;
using PurrNet;
using PurrNet.Transports;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        private readonly HashSet<string> _pendingConnectionUISceneUnloads = new();

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
                    var wasConnecting = _connectionState == ConnectionState.Connecting;
                    var wasActive = _connectionState != ConnectionState.Disconnected;
                    _connectionState = ConnectionState.Disconnected;
                    _authenticatedForCurrentConnection = false;
                    _inWorldForCurrentConnection = false;
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
            if (scene.name != "CharacterSelection" && scene.name != "CharacterCreation")
                return;

            if (CurrentConnectionState != ConnectionState.Disconnected)
                return;

            RequestConnectionUISceneUnload(scene.name);
        }

        private void CleanupConnectionUIScenes()
        {
            RequestConnectionUISceneUnload("CharacterSelection");
            RequestConnectionUISceneUnload("CharacterCreation");
        }

        private void RequestConnectionUISceneUnload(string sceneName)
        {
            if (sceneName != "CharacterSelection" && sceneName != "CharacterCreation")
                throw new ArgumentException($"Unsupported connection UI scene: {sceneName}", nameof(sceneName));

            if (_pendingConnectionUISceneUnloads.Contains(sceneName))
                return;

            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            _pendingConnectionUISceneUnloads.Add(sceneName);
            var unloadOperation = SceneManager.UnloadSceneAsync(scene);
            if (unloadOperation == null)
            {
                _pendingConnectionUISceneUnloads.Remove(sceneName);
                Debug.Error($"Failed to request unload for connection UI scene '{sceneName}'.");
                return;
            }

            unloadOperation.completed += _ => _pendingConnectionUISceneUnloads.Remove(sceneName);
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
            var networkManager = GetComponent<NetworkManager>();
            var transport = networkManager.GetComponentInChildren<PurrNet.Transports.UDPTransport>();

            // Step 1: Hide the main menu and show the loading screen.
            Debug.Log(" - Step 1: Hiding main menu, showing loading screen...");
            MainMenu.Instance.Hide();
            LoadingScreen.Instance.Show(10);
            Debug.Log(" ✓ Step 1: Done.");

            // Step 2: Connect to the server.
            Debug.Log(" - Step 2: Setting up connection...");
            if (networkManager.clientState == PurrNet.Transports.ConnectionState.Disconnected)
            {
                // Set the credentials of the login authentication payload.
                Authentication.SetCredentials(username, password);

                // Set the address and server port of the transport.
                transport.address = GameConfigurationManager.Config.ServerAddress;
                transport.serverPort = GameConfigurationManager.Config.ServerPort;

                // Start the client.
                Debug.Log(" ✓ Step 2: Attempting to connect to server...");
                LoadingScreen.Instance.Tick();
                networkManager.StartClient();

                yield return new WaitUntil(
                    () => _connectionState == ConnectionState.Connected
                        || _connectionState == ConnectionState.Disconnected);
                if (_connectionState != ConnectionState.Connected)
                    yield break;
                Debug.Log(" ✓ Step 3: Connection process completed. Moving to character selection or creation.");

                yield return new WaitUntil(
                    () => ClientAccountManager.HasReceivedCharacters()
                        || _connectionState == ConnectionState.Disconnected);
                if (_connectionState != ConnectionState.Connected)
                    yield break;

                if (ClientAccountManager.TryGetCharacterList(out var characterList) && characterList.Count > 0)
                {
                    Debug.Log($" ✓ Received character list with {characterList.Count} characters. Showing character selection.");
                    SceneManager.LoadSceneAsync("CharacterSelection", LoadSceneMode.Additive);
                    Game.Client.UI.UI_CharacterSelectWindow.Instance.Show();
                }
                else
                {
                    Debug.Log(" ✓ Received empty character list. Showing character creation.");
                    SceneManager.LoadSceneAsync("CharacterCreation", LoadSceneMode.Additive);
                    Game.Client.UI.UI_CharacterCreationWindow.Instance.Show();
                }

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
