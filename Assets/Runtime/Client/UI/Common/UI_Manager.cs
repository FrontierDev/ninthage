using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Debug = Game.Shared.FormattedDebug;
using System;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public class UI_Manager : MonoBehaviour
    {
        private static UI_Manager instance;
        public static UI_Manager Instance => instance;

        [Header("UI References")]
        [SerializeField] protected Canvas root;
        [SerializeField] protected CanvasGroup rootCanvasGroup;
        [SerializeField] protected List<UI_Window> windows;

        [Header("Runtime")]
        [SerializeField] protected Queue<UI_Window> openedWindows = new Queue<UI_Window>();

        private void Awake()
        {
            if (Game.Shared.Runtime.IsServer())
            {
                Debug.Log("Removing UI from the server.");
                Destroy(this.gameObject);
                return;
            }

            instance = this;
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

        public void Initialize()
        {
            ClientInputController.Instance.onInterfaceHotkeyInput += (hotkey) => ToggleWindow(hotkey);
        }

        #region Window Management
        public bool TryGetWindow(string windowName, out UI_Window window)
        {
            window = windows.FirstOrDefault(w => w.WindowName == windowName);
            return window != null;
        }

        public virtual void ShowWindow(string windowName)
        {
            if (TryGetWindow(windowName, out var window)) window.Show();
            else Debug.Error($"Window with name '{windowName}' not found.");
        }

        public virtual void HideWindow(string windowName)
        {
            if (TryGetWindow(windowName, out var window)) window.Hide();
            else Debug.Error($"Window with name '{windowName}' not found.");
        }

        public virtual void ToggleWindow(string windowName)
        {
            if (TryGetWindow(windowName, out var window)) window.Toggle();
            else Debug.Error($"Window with name '{windowName}' not found.");
        }

        public virtual void OnShowWindow(UI_Window window) => openedWindows.Enqueue(window);
        public virtual void OnHideWindow(UI_Window window) => openedWindows = new Queue<UI_Window>(openedWindows.Where(w => w != window));
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