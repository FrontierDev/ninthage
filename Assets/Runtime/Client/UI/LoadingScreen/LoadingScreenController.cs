using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Client.Collections;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>LoadingScreenController</c> implementation.
    /// </summary>
    public class LoadingScreenController : MonoBehaviour
    {
        private static LoadingScreenController _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static LoadingScreenController Instance => _instance;

        [SerializeField]
        private LoadingScreenCollection _collection;
        /// <summary>
        /// Gets the <c>Collection</c> value.
        /// </summary>
        public LoadingScreenCollection Collection => _collection;

        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image splashImage;
        [SerializeField] private Image loadingIcon;

        private int stage = 0;
        private int maxStages = 1;
        private float targetFillAmount = 0f;
        private const float FILL_SPEED = 5.0f;
        private bool isShowing = false;
        private Coroutine _fadeCoroutine;

        #region Lifecycle
        private void Awake()
        {
            if (_instance != null)
            {
                Debug.Warning($"An instance of LoadingScreenController already exists. {this.gameObject.name} will not be set as an instance.");
                return;
            }
            else
            {
                _instance = this;
            }

            if (Collection == null)
            {
                Debug.Warning("No default LoadingScreenCollection has been assigned to LoadingScreenController.");
            }
        }

        /// <summary>
        /// Sets the loading screen splash art collection.
        /// </summary>
        /// <param name="collection">The new loading screen collection to be used.</param>
        public void SetCollection(LoadingScreenCollection collection)
        {
            if (collection == null)
            {
                Debug.Error("Attempted to set LoadingScreenController's collection to null.");
                return;
            }

            _collection = collection;
        }

        /// <summary>
        /// Executes the client-side <c>Show</c> operation.
        /// </summary>
        public void Show(int stages = 1)
        {
            // Cancel any in-progress fade-out from a previous loading screen.
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            // Set the expected number of stages.
            this.maxStages = stages;
            this.stage = 0;
            this.targetFillAmount = 0f;

            // Reset the loading bar/loading icon.
            loadingIcon.fillAmount = 0f;

            // Show the loading screen and refresh the splash image.
            ShowLoadingScreen();
            RefreshSplashImage();
            isShowing = true;
        }

        /// <summary>
        /// Executes the client-side <c>Tick</c> operation.
        /// </summary>
        public void Tick()
        {
            if (stage >= maxStages) return;
            stage++;
            targetFillAmount = (float)stage / maxStages;
        }

        private void Update()
        {
            if (!isShowing) return;

            if (loadingIcon.fillAmount >= 0.99f) Finish();
            else loadingIcon.fillAmount = Mathf.Lerp(loadingIcon.fillAmount, targetFillAmount, Time.deltaTime * FILL_SPEED);
        }

        /// <summary>
        /// Executes the client-side <c>Finish</c> operation.
        /// </summary>
        public void Finish()
        {
            if (!isShowing) return;
            isShowing = false;
            HideLoadingScreen();
        }

        /// <summary>
        /// Executes the client-side <c>Cancel</c> operation.
        /// </summary>
        public void Cancel()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            isShowing = false;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        #endregion

        #region Helper Methods
        private void ShowLoadingScreen()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private void HideLoadingScreen()
        {
            _fadeCoroutine = StartCoroutine(FadeOut());
        }

        private IEnumerator FadeOut()
        {
            float duration = 1f; // Duration of the fade-out effect in seconds.
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
                canvasGroup.alpha = alpha;
                yield return null;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            _fadeCoroutine = null;
        }

        private void RefreshSplashImage()
        {
            if (Collection != null && Collection.Count > 0)
            {
                splashImage.sprite = Collection.GetRandom();
            }
            else
            {
                Debug.Error("Assigned art collection is null or empty. No splash image will be displayed.");
                return;
            }
        }
        #endregion
    }
}
