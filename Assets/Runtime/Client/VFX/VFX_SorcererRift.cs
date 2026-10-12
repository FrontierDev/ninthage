using Game.Shared;
using Game.Shared.Runtime.VFX;

namespace Game.Client.VFX
{
    using Game.Client.UI;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Serialization;

    /// <summary>
    /// Drives the <c>_OpenAmount</c> shader property on the rift's MeshRenderer.
    /// Instantiates the material on Awake (so the shared asset is never modified)
    /// and sets properties via <see cref="UIShaderProperties"/>.
    ///
    /// On enable the rift grows from its vertical centre outward over
    /// <see cref="openDuration"/>. After <see cref="lifetime"/> seconds
    /// (if >= 0) it closes quickly over <see cref="closeDuration"/> then
    /// destroys the GameObject. Call <see cref="Close"/> at any time to
    /// trigger the close animation immediately.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VFX_SorcererRift : MonoBehaviour, IVFXFadeOut, IVFXFadeIn, IVFXTrigger
    {
        [Header("Animation")]
        [Tooltip("Seconds to grow from centre to fully open.")]
        [FormerlySerializedAs("openDuration")]
        [SerializeField, Min(0.01f)] private float _openDuration = 1.2f;

        [Tooltip("Seconds to close when the rift expires or Close() is called.")]
        [FormerlySerializedAs("closeDuration")]
        [SerializeField, Min(0.01f)] private float _closeDuration = 0.35f;

        [Header("Lifetime")]
        [Tooltip("Seconds the rift stays open before auto-closing. Set to -1 to disable auto-close.")]
        [FormerlySerializedAs("lifetime")]
        [SerializeField] private float _lifetime = -1f;

        [Header("Distortion")]
        [Tooltip("Material using NinthAge/VFX/VFXQuadRipple shader.")]
        [FormerlySerializedAs("distortionMaterial")]
        [SerializeField] private Material _distortionMaterial;
        [Tooltip("Size of the gravitational-lensing quad (metres).")]
        [FormerlySerializedAs("distortionSize")]
        [SerializeField, Min(0.1f)] private float _distortionSize = 2.0f;
        [Tooltip("Vertical offset from the rift's pivot to the quad centre (metres). Set to half the rift's visual height.")]
        [FormerlySerializedAs("distortionYOffset")]
        [SerializeField] private float _distortionYOffset = 1.0f;

        [Header("Pulse Wave")]
        [Tooltip("Material using NinthAge/VFX/AoEPulseShader shader.")]
        [FormerlySerializedAs("pulseWaveMaterial")]
        [SerializeField] private Material _pulseWaveMaterial;
        [Tooltip("How far the ring travels from the rift base (metres).")]
        [FormerlySerializedAs("pulseMaxRadius")]
        [SerializeField, Min(0.1f)] private float _pulseMaxRadius = 8f;
        [Tooltip("Height of the 3D shock ring wall (metres).")]
        [FormerlySerializedAs("pulseWaveHeight")]
        [SerializeField, Min(0.05f)] private float _pulseWaveHeight = 1.5f;
        [Tooltip("Brightness of the ground trail disc left behind the wave (0=off, 1=full).")]
        [FormerlySerializedAs("pulseTrailStrength")]
        [SerializeField, Range(0f, 1f)] private float _pulseTrailStrength = 0.35f;
        [Tooltip("Seconds for the pulse wave to travel from centre to max radius.")]
        [FormerlySerializedAs("pulseDuration")]
        [SerializeField, Min(0.1f)] private float _pulseDuration = 0.8f;

        // -- Private state ---------------------------------------------------------

        private const string OPEN_AMOUNT_PROPERTY = "_OpenAmount";

        private static readonly int SHADER_PROGRESS = Shader.PropertyToID("_Progress");
        private static readonly int SHADER_TRAIL_STRENGTH = Shader.PropertyToID("_TrailStrength");

        private MeshRenderer _meshRenderer;
        private Coroutine _activeCoroutine;
        private Coroutine _triggerCoroutine;
        private bool _closing;
        private float _currentOpenAmount;
        private Material _distortionMat;
        private Transform _distortionTransform;

        // -- Unity lifecycle -------------------------------------------------------

        private void Awake()
        {
            _meshRenderer = GetComponentInChildren<MeshRenderer>(true);
            if (_meshRenderer == null)
            {
                Debug.LogError($"[RiftLifetime] No MeshRenderer found in hierarchy of '{name}'.", this);
                return;
            }

            // Create a per-instance material so the shared asset is never modified,
            // matching the same pattern used in UI_ProgressBar.SetMaterial().
            if (_meshRenderer.sharedMaterial != null)
                _meshRenderer.material = new Material(_meshRenderer.sharedMaterial);

            // Spawn the gravitational-lensing quad that persists while the rift is open.
            // It is placed slightly behind the rift (negative local Z) so the rift
            // renders on top and is not itself distorted.
            if (_distortionMaterial != null)
            {
                var distortionGO = new GameObject("RiftDistortion");
                distortionGO.transform.SetParent(transform, worldPositionStays: false);
                distortionGO.transform.localScale = Vector3.one * _distortionSize;
                distortionGO.transform.localPosition = new Vector3(0f, _distortionYOffset, 0.01f);

                var dmf = distortionGO.AddComponent<MeshFilter>();
                dmf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

                _distortionMat = new Material(_distortionMaterial);
                var dmr = distortionGO.AddComponent<MeshRenderer>();
                dmr.material = _distortionMat;
                _distortionTransform = distortionGO.transform;
            }

            SetOpenAmount(0f);
        }

        private void OnEnable()
        {
            _closing = false;
            _activeCoroutine = StartCoroutine(OpenThenWait());
        }

        private void OnDisable()
        {
            if (_activeCoroutine != null)
                StopCoroutine(_activeCoroutine);
            if (_triggerCoroutine != null)
                StopCoroutine(_triggerCoroutine);
        }

        private void LateUpdate()
        {
            if (_distortionTransform == null) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            // Rotate quad to face the camera (billboard in world space).
            _distortionTransform.rotation = Quaternion.LookRotation(
                _distortionTransform.position - cam.transform.position);
        }

        private void OnDestroy()
        {
            // Clean up the instanced materials we created.
            if (_meshRenderer != null && _meshRenderer.material != null)
                Destroy(_meshRenderer.material);
            if (_distortionMat != null)
                Destroy(_distortionMat);
        }

        // -- Public API ------------------------------------------------------------

        /// <summary>
        /// Triggers the close animation immediately. The GameObject will be
        /// destroyed once the animation completes. Safe to call multiple times.
        /// </summary>
        public void Close()
        {
            if (_closing) return;
            if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
            _activeCoroutine = StartCoroutine(CloseAndDestroy());
        }

        /// <summary>
        /// Restarts the open animation from the current state. Cancels any
        /// in-progress close animation.
        /// </summary>
        public void Open()
        {
            _closing = false;
            if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
            _activeCoroutine = StartCoroutine(OpenThenWait());
        }

        /// <summary>
        /// Emits a one-shot pulse-wave ring that travels outward along the ground
        /// from the base of the rift. Safe to call while the rift is open or opening.
        /// </summary>
        public void Trigger()
        {
            if (_pulseWaveMaterial == null) return;
            if (_triggerCoroutine != null) StopCoroutine(_triggerCoroutine);
            _triggerCoroutine = StartCoroutine(GroundPulseWave());
        }

        // -- Coroutines ------------------------------------------------------------

        private IEnumerator OpenThenWait()
        {
            float startValue = _currentOpenAmount;
            yield return Animate(startValue, 1f, _openDuration * (1f - startValue), EaseOut);

            if (_lifetime >= 0f)
            {
                yield return new WaitForSeconds(_lifetime);
                Close();
            }
        }

        private IEnumerator CloseAndDestroy()
        {
            _closing = true;

            // Animate from wherever we currently are so Close() mid-open doesn't jump.
            float startValue = _currentOpenAmount;
            yield return Animate(startValue, 0f, _closeDuration * startValue, EaseOut);

            Destroy(gameObject);
        }

        private IEnumerator Animate(float from, float to, float duration,
                                    System.Func<float, float> easing)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetOpenAmount(Mathf.Lerp(from, to, easing(t)));
                yield return null;
            }
            SetOpenAmount(to);
        }

        private IEnumerator GroundPulseWave()
        {
            // Cylinder mesh: side face = 3D shock ring, cap face = trail disc.
            // Unity cylinder: radius = 0.5 OS, height = 1.0 OS.
            // Scale XZ by currentRadius*2 to expand ring, Y by pulseRingWidth for ring height.
            var go = new GameObject("AoEPulseWave");
            go.transform.position = transform.position;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");

            var mr = go.AddComponent<MeshRenderer>();
            var mat = new Material(_pulseWaveMaterial);
            mat.SetFloat(SHADER_TRAIL_STRENGTH, _pulseTrailStrength);
            mr.material = mat;

            float elapsed = 0f;
            while (elapsed < _pulseDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / _pulseDuration);
                float currentRadius = progress * _pulseMaxRadius;
                // XZ scale: cylinder radius 0.5 OS → world radius = scale * 0.5
                // Y scale: pulseWaveHeight controls the 3D ring wall height.
                // Offset up by half world height so bottom cap sits at ground level.
                go.transform.localScale = new Vector3(currentRadius * 2f, _pulseWaveHeight, currentRadius * 2f);
                go.transform.position = transform.position + Vector3.up * (_pulseWaveHeight * 0.5f + 0.01f); // minimal z-fight lift
                mat.SetFloat(SHADER_PROGRESS, progress);
                yield return null;
            }

            Destroy(mat);
            Destroy(go);
            _triggerCoroutine = null;
        }

        private void SetOpenAmount(float value)
        {
            _currentOpenAmount = value;
            UIShaderProperties.SetFloat(_meshRenderer, OPEN_AMOUNT_PROPERTY, value);
            // Distortion is fully active when the rift is open (_Progress 1) and
            // invisible when closed (_Progress 0).
            _distortionMat?.SetFloat(SHADER_PROGRESS, value);
        }

        // -- Easing functions ------------------------------------------------------

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
        private static float EaseIn(float t) => t * t;

        /// <summary>
        /// Executes the client-side <c>VFXFadeOut</c> operation.
        /// </summary>
        public void VFXFadeOut()
        {
            // NYI
        }

        /// <summary>
        /// Executes the client-side <c>VFXFadeIn</c> operation.
        /// </summary>
        public void VFXFadeIn()
        {
            // NYI
        }
    }
}
