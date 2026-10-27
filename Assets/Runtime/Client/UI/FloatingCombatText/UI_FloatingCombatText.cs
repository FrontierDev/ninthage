using System;
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
        internal Action<UI_FloatingCombatText> ReleaseRequested;
        internal Action<UI_FloatingCombatText> DestroyedUnexpectedly;

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
        private bool _isActive;

        internal void Activate(
            string content,
            Color color,
            Vector3 direction,
            float sizeMultiplier = 1f,
            float durationMultiplier = 1f)
        {
            if (_isActive)
                throw new InvalidOperationException("Cannot activate an already active floating combat text.");
            if (_elapsedTime != 0f
                || _floatDirection != Vector3.zero
                || _sizeMultiplier != 1f
                || _durationMultiplier != 1f)
            {
                throw new InvalidOperationException("Floating combat text was not reset before activation.");
            }

            _text.text = content;
            _text.color = color;
            _floatDirection = direction.normalized;
            _elapsedTime = 0f;
            _sizeMultiplier = sizeMultiplier;
            _durationMultiplier = durationMultiplier;
            _isActive = true;
        }

        internal void Cancel()
        {
            RequestRelease();
        }

        internal void ResetForPool()
        {
            if (_isActive)
                throw new InvalidOperationException("Cannot reset active floating combat text for the pool.");

            ReleaseRequested = null;
            DestroyedUnexpectedly = null;
            _text.text = string.Empty;
            _text.color = Color.white;
            _floatDirection = Vector3.zero;
            _elapsedTime = 0f;
            _sizeMultiplier = 1f;
            _durationMultiplier = 1f;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            if (!_isActive)
                return;

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
                RequestRelease();
        }

        private void RequestRelease()
        {
            if (!_isActive)
                return;

            _isActive = false;
            var callback = ReleaseRequested;
            ReleaseRequested = null;
            callback?.Invoke(this);
        }

        private void OnDestroy()
        {
            if (_isActive)
            {
                _isActive = false;
                ReleaseRequested = null;
                var callback = DestroyedUnexpectedly;
                DestroyedUnexpectedly = null;
                callback?.Invoke(this);
                return;
            }

            ReleaseRequested = null;
            DestroyedUnexpectedly = null;
        }
    }
}
