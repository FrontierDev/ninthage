using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
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

        private Image iconImage;
        private RectTransform rectTransform;
        private Canvas rootCanvas;

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
            ghost.iconImage = image;
            ghost.rectTransform = rt;
            ghost.rootCanvas = canvas;
            ghost.gameObject.SetActive(false);

            _instance = ghost;
        }

        public void Show(Sprite icon)
        {
            iconImage.sprite = icon;
            iconImage.color = new Color(1f, 1f, 1f, 0.8f);
            gameObject.SetActive(true);
        }

        public void UpdatePosition(Vector2 screenPosition)
        {
            rectTransform.position = screenPosition;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            iconImage.sprite = null;
        }
    }
}
