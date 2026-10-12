using Debug = Game.Shared.Utility.FormattedDebug;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_Manager</c> implementation.
    /// </summary>
    public class UI_Manager : MonoBehaviour
    {
        private static UI_Manager _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_Manager Instance => _instance;

        [Header("UI References")]
        [SerializeField] protected Canvas root;
        [SerializeField] protected CanvasGroup rootCanvasGroup;
        [SerializeField] protected List<UI_Window> windows;

        [Header("Runtime")]
        [SerializeField] protected Queue<UI_Window> openedWindows = new();

        private bool _initialized;

        private void Awake()
        {
            if (Game.Shared.Runtime.Runtime.IsServer())
            {
                Debug.Log("Removing UI from the server.");
                Destroy(this.gameObject);
                return;
            }

            _instance = this;
            root = GetComponent<Canvas>();
            rootCanvasGroup = GetComponent<CanvasGroup>();

            // Enable all windows in the children of the manager.
            foreach (CanvasGroup go in GetComponentsInChildren<CanvasGroup>(true))
                if (!go.gameObject.activeInHierarchy) go.gameObject.SetActive(true);

            DontDestroyOnLoad(this.gameObject);

            // Automatically finds all windows in the children of the manager.
            UI_Window[] foundWindows = GetComponentsInChildren<UI_Window>(true);
            foreach (var window in foundWindows)
                if (!windows.Contains(window)) windows.Add(window);
        }

        /// <summary>
        /// Executes the client-side <c>Initialize</c> operation.
        /// </summary>
        public void Initialize()
        {
            if (_initialized)
                return;

            ClientInputController.Instance.onInterfaceHotkeyInput += OnInterfaceHotkeyInput;
            _initialized = true;
        }

        private void OnDestroy()
        {
            var inputController = ClientInputController.Instance;
            if (inputController != null)
                inputController.onInterfaceHotkeyInput -= OnInterfaceHotkeyInput;
        }

        private void OnInterfaceHotkeyInput(string hotkey)
        {
            ToggleWindow(hotkey);
        }

        #region Window Management
        /// <summary>
        /// Executes the client-side <c>TryGetWindow</c> operation.
        /// </summary>
        public bool TryGetWindow(string windowName, out UI_Window window)
        {
            window = windows.FirstOrDefault(w => w.WindowName == windowName);
            return window != null;
        }

        /// <summary>
        /// Executes the client-side <c>ShowWindow</c> operation.
        /// </summary>
        public virtual void ShowWindow(string windowName)
        {
            if (TryGetWindow(windowName, out var window)) window.Show();
            else Debug.Error($"Window with name '{windowName}' not found.");
        }

        /// <summary>
        /// Executes the client-side <c>HideWindow</c> operation.
        /// </summary>
        public virtual void HideWindow(string windowName)
        {
            if (TryGetWindow(windowName, out var window)) window.Hide();
            else Debug.Error($"Window with name '{windowName}' not found.");
        }

        /// <summary>
        /// Executes the client-side <c>ToggleWindow</c> operation.
        /// </summary>
        public virtual void ToggleWindow(string windowName)
        {
            if (TryGetWindow(windowName, out var window)) window.Toggle();
            else Debug.Error($"Window with name '{windowName}' not found.");
        }

        /// <summary>
        /// Handles the <c>OnShowWindow</c> callback.
        /// </summary>
        public virtual void OnShowWindow(UI_Window window) => openedWindows.Enqueue(window);
        /// <summary>
        /// Handles the <c>OnHideWindow</c> callback.
        /// </summary>
        public virtual void OnHideWindow(UI_Window window) => openedWindows = new Queue<UI_Window>(openedWindows.Where(w => w != window));
        /// <summary>
        /// Executes the client-side <c>CloseLastWindow</c> operation.
        /// </summary>
        public virtual void CloseLastWindow()
        {
            if (openedWindows.Count > 0)
            {
                var lastWindow = openedWindows.Dequeue();
                lastWindow.Hide();
            }
        }
        #endregion
    }
}
