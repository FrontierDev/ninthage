using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Client.Collections;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
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
        [FormerlySerializedAs("canvasGroup")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [FormerlySerializedAs("splashImage")]
        [SerializeField] private Image _splashImage;
        [FormerlySerializedAs("loadingIcon")]
        [SerializeField] private Image _loadingIcon;

        private int _stage = 0;
        private int _maxStages = 1;
        private float _targetFillAmount = 0f;
        private const float FILL_SPEED = 5.0f;
        private bool _isShowing = false;
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
            this._maxStages = stages;
            this._stage = 0;
            this._targetFillAmount = 0f;

            // Reset the loading bar/loading icon.
            _loadingIcon.fillAmount = 0f;

            // Show the loading screen and refresh the splash image.
            ShowLoadingScreen();
            RefreshSplashImage();
            _isShowing = true;
        }

        /// <summary>
        /// Executes the client-side <c>Tick</c> operation.
        /// </summary>
        public void Tick()
        {
            if (_stage >= _maxStages) return;
            _stage++;
            _targetFillAmount = (float)_stage / _maxStages;
        }

        private void Update()
        {
            if (!_isShowing) return;

            if (_loadingIcon.fillAmount >= 0.99f) Finish();
            else _loadingIcon.fillAmount = Mathf.Lerp(_loadingIcon.fillAmount, _targetFillAmount, Time.deltaTime * FILL_SPEED);
        }

        /// <summary>
        /// Executes the client-side <c>Finish</c> operation.
        /// </summary>
        public void Finish()
        {
            if (!_isShowing) return;
            _isShowing = false;
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

            _isShowing = false;
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
        #endregion

        #region Helper Methods
        private void ShowLoadingScreen()
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
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
                _canvasGroup.alpha = alpha;
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _fadeCoroutine = null;
        }

        private void RefreshSplashImage()
        {
            if (Collection != null && Collection.Count > 0)
            {
                _splashImage.sprite = Collection.GetRandom();
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
