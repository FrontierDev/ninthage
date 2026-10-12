using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_FloatingCombatText</c> implementation.
    /// </summary>
    public sealed class UI_FloatingCombatText : MonoBehaviour
    {
        [FormerlySerializedAs("text")]
        [SerializeField] private TextMeshProUGUI _text;
        [FormerlySerializedAs("floatSpeed")]
        [SerializeField] private float _floatSpeed = 0.5f;
        [FormerlySerializedAs("fadeDuration")]
        [SerializeField] private float _fadeDuration = 1f;
        [FormerlySerializedAs("screenSize")]
        [SerializeField] private float _screenSize = 0.04f;

        private Vector3 _floatDirection;
        private float _elapsedTime;
        private float _sizeMultiplier = 1f;
        private float _durationMultiplier = 1f;

        /// <summary>
        /// Executes the client-side <c>Initialize</c> operation.
        /// </summary>
        public void Initialize(string content, Color color, Vector3 direction, float sizeMultiplier = 1f, float durationMultiplier = 1f)
        {
            _text.text = content;
            _text.color = color;
            _floatDirection = direction.normalized;
            _elapsedTime = 0f;
            this._sizeMultiplier = sizeMultiplier;
            this._durationMultiplier = durationMultiplier;
        }

        private void Update()
        {
            // Move the text upwards
            transform.position += _floatDirection * _floatSpeed * Time.deltaTime;

            // Face camera and scale to constant screen size
            Camera cam = Camera.main;
            if (cam != null)
            {
                transform.rotation = cam.transform.rotation;

                float dist = Vector3.Distance(cam.transform.position, transform.position);
                float frustumHeight = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                float scale = frustumHeight * _screenSize * _sizeMultiplier;
                transform.localScale = Vector3.one * scale;
            }

            // Fade out over time
            _elapsedTime += Time.deltaTime;
            float totalDuration = _fadeDuration * _durationMultiplier;
            float alpha = Mathf.Lerp(1f, 0f, _elapsedTime / totalDuration);
            var c = _text.color;
            c.a = alpha;
            _text.color = c;

            if (_elapsedTime >= totalDuration)
                Destroy(gameObject);
        }
    }
}
