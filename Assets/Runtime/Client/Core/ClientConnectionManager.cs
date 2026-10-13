using Authentication = Game.Shared.Networking.LoginAuthenticator;
using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Benchmark;
using Game.Shared;
using LoadingScreen = Game.Client.UI.LoadingScreenController;
using MainMenu = Game.Client.UI.UI_MainMenuWindow;
using PurrNet;
using System;
using System.Collections;
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
        private static bool _benchmarkConnection;
        /// <summary>
        /// Gets the <c>CurrentConnectionState</c> value.
        /// </summary>
        public static ConnectionState CurrentConnectionState => _connectionState;

        #region Lifecycle
        private void Awake()
        {
            _instance = this;
            ClientInitializeManager();
        }

        private void ClientInitializeManager()
        {
            var networkManager = GetComponent<NetworkManager>();
            networkManager.onClientConnectionState += OnClientConnectionStateChanged;
            _initialized = true;
        }
        #endregion

        private void OnClientConnectionStateChanged(PurrNet.Transports.ConnectionState state)
        {
            switch (state)
            {
                case PurrNet.Transports.ConnectionState.Connected:
                    _connectionState = ConnectionState.Connected;
                    Debug.Log(" ✓ Connected to the server.");
                    this.gameObject.AddComponent<ClientAccountManager>();
                    break;

                case PurrNet.Transports.ConnectionState.Disconnected:
                    var wasConnecting = _connectionState == ConnectionState.Connecting;
                    _connectionState = ConnectionState.Disconnected;

                    if (wasConnecting) Debug.Warning("Connection failed (authentication rejected or server unreachable).");
                    else Debug.Warning("Disconnected from server.");

                    if (_benchmarkConnection)
                    {
                        Debug.Error("Benchmark client disconnected before completing its authenticated startup.");
                        Application.Quit();
                        return;
                    }

                    // Dismiss the loading screen and return to the main menu.
                    // Destroy the client account manager to clear any character data, etc. that may have been loaded.
                    LoadingScreen.Instance.Finish();
                    MainMenu.Instance.Show();
                    if (TryGetComponent<ClientAccountManager>(out var accountManager))
                    {
                        Destroy(accountManager);
                    }

                    // Unload scenes.
                    Debug.Warning($"Scene unloading has not been implemented yet.");
                    break;
            }
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
            StartCoroutine(ConnectionProcess(usernameInput, passwordInput));
        }

        /// <summary>
        /// Starts the benchmark-only automated client path. Authentication, character selection and world entry
        /// still use the normal public runtime APIs and server validation.
        /// </summary>
        public void ConnectForBenchmark(BenchmarkLaunchOptions options)
        {
            if (options == null || options.Role != BenchmarkRole.Client)
                throw new ArgumentException("A validated benchmark client configuration is required.", nameof(options));

            if (_connectionState != ConnectionState.Disconnected)
                throw new InvalidOperationException("Benchmark client connection was requested more than once.");

            _benchmarkConnection = true;
            _connectionState = ConnectionState.Connecting;
            StartCoroutine(BenchmarkConnectionProcess(options));
        }

        private IEnumerator BenchmarkConnectionProcess(BenchmarkLaunchOptions options)
        {
            var networkManager = GetComponent<NetworkManager>();
            var transport = networkManager.GetComponentInChildren<PurrNet.Transports.UDPTransport>();
            if (transport == null)
            {
                Debug.Error("Benchmark client could not find UDPTransport.");
                Application.Quit();
                yield break;
            }

            Authentication.SetCredentials(options.ClientUsername, options.ClientPassword);
            transport.address = GameConfigurationManager.Config.ServerAddress;
            transport.serverPort = GameConfigurationManager.Config.ServerPort;
            networkManager.StartClient();

            var connectionDeadline = Time.realtimeSinceStartup + 60f;
            while (_connectionState == ConnectionState.Connecting && Time.realtimeSinceStartup < connectionDeadline)
                yield return null;

            if (_connectionState != ConnectionState.Connected)
            {
                Debug.Error("Benchmark client did not establish an authenticated connection within 60 seconds.");
                Application.Quit();
                yield break;
            }

            var characterDeadline = Time.realtimeSinceStartup + 60f;
            while (!ClientAccountManager.HasReceivedCharacters() && Time.realtimeSinceStartup < characterDeadline)
                yield return null;

            if (!ClientAccountManager.HasReceivedCharacters() ||
                !ClientAccountManager.TryGetCharacterList(out var characterList) ||
                characterList == null || characterList.Count == 0)
            {
                Debug.Error("Benchmark client received no character to select; character creation is not bypassed by benchmark mode.");
                Application.Quit();
                yield break;
            }

            Game.Shared.Networking.AccountEndpoint.Server_RequestEnterWorld(characterList[0].GUID);
            Debug.Log($"Benchmark client requested authenticated world entry for character '{characterList[0].GUID}'.");
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

                yield return new WaitUntil(() => _connectionState != ConnectionState.Connected);
                Debug.Log(" ✓ Step 3: Connection process completed. Moving to character selection or creation.");

                yield return new WaitUntil(() => ClientAccountManager.HasReceivedCharacters());

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
