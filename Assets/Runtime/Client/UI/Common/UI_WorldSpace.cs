using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_WorldSpace</c> implementation.
    /// </summary>
    public class UI_WorldSpace : MonoBehaviour
    {
        private static UI_WorldSpace instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_WorldSpace Instance => instance;

        [Header("UI References")]
        [SerializeField] public Canvas root;
        [SerializeField] protected CanvasGroup rootCanvasGroup;
        [SerializeField] public Transform nameplateContainer;
        [SerializeField] public Transform fctContainer;

        private void Awake()
        {
            if (instance != null) return;

            instance = this;
            root = GetComponent<Canvas>();
            rootCanvasGroup = GetComponent<CanvasGroup>();

            DontDestroyOnLoad(this.gameObject);
        }
    }
}