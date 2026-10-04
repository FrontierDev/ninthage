using UnityEngine;

namespace Game.Client.UI
{
    public class UI_WorldSpace : MonoBehaviour
    {
        private static UI_WorldSpace instance;
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