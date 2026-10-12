using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_DragGhost</c> implementation.
    /// </summary>
    public sealed class UI_DragGhost : MonoBehaviour
    {
        private static UI_DragGhost _instance;

        public static UI_DragGhost Instance
        {
            get
            {
                if (_instance == null)
                    CreateInstance();
                return _instance;
            }
        }

        private Image _iconImage;
        private RectTransform _rectTransform;
        private Canvas _rootCanvas;

        private static void CreateInstance()
        {
            var canvasObj = new GameObject("DragGhostCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            canvasObj.AddComponent<CanvasScaler>();
            DontDestroyOnLoad(canvasObj);

            var ghostObj = new GameObject("DragGhost");
            ghostObj.transform.SetParent(canvasObj.transform, false);

            var rt = ghostObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(64f, 64f);

            var image = ghostObj.AddComponent<Image>();
            image.raycastTarget = false;

            var ghost = ghostObj.AddComponent<UI_DragGhost>();
            ghost._iconImage = image;
            ghost._rectTransform = rt;
            ghost._rootCanvas = canvas;
            ghost.gameObject.SetActive(false);

            _instance = ghost;
        }

        /// <summary>
        /// Executes the client-side <c>Show</c> operation.
        /// </summary>
        public void Show(Sprite icon)
        {
            _iconImage.sprite = icon;
            _iconImage.color = new Color(1f, 1f, 1f, 0.8f);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Executes the client-side <c>UpdatePosition</c> operation.
        /// </summary>
        public void UpdatePosition(Vector2 screenPosition)
        {
            _rectTransform.position = screenPosition;
        }

        /// <summary>
        /// Executes the client-side <c>Hide</c> operation.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            _iconImage.sprite = null;
        }
    }
}
