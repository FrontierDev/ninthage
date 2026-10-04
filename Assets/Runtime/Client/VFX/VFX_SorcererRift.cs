using Game.Shared;

namespace Game.Client.VFX
{
    using System.Collections;
    using UnityEngine;
    using Game.Client.UI;

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
        [SerializeField, Min(0.01f)] private float openDuration = 1.2f;

        [Tooltip("Seconds to close when the rift expires or Close() is called.")]
        [SerializeField, Min(0.01f)] private float closeDuration = 0.35f;

        [Header("Lifetime")]
        [Tooltip("Seconds the rift stays open before auto-closing. Set to -1 to disable auto-close.")]
        [SerializeField] private float lifetime = -1f;

        [Header("Distortion")]
        [Tooltip("Material using NinthAge/VFX/VFXQuadRipple shader.")]
        [SerializeField] private Material distortionMaterial;
        [Tooltip("Size of the gravitational-lensing quad (metres).")]
        [SerializeField, Min(0.1f)] private float distortionSize = 2.0f;
        [Tooltip("Vertical offset from the rift's pivot to the quad centre (metres). Set to half the rift's visual height.")]
        [SerializeField] private float distortionYOffset = 1.0f;

        [Header("Pulse Wave")]
        [Tooltip("Material using NinthAge/VFX/AoEPulseShader shader.")]
        [SerializeField] private Material pulseWaveMaterial;
        [Tooltip("How far the ring travels from the rift base (metres).")]
        [SerializeField, Min(0.1f)] private float pulseMaxRadius = 8f;
        [Tooltip("Height of the 3D shock ring wall (metres).")]
        [SerializeField, Min(0.05f)] private float pulseWaveHeight = 1.5f;
        [Tooltip("Brightness of the ground trail disc left behind the wave (0=off, 1=full).")]
        [SerializeField, Range(0f, 1f)] private float pulseTrailStrength = 0.35f;
        [Tooltip("Seconds for the pulse wave to travel from centre to max radius.")]
        [SerializeField, Min(0.1f)] private float pulseDuration = 0.8f;

        // -- Private state ---------------------------------------------------------

        private const string OpenAmountProperty = "_OpenAmount";

        private static readonly int ShaderProgress = Shader.PropertyToID("_Progress");
        private static readonly int ShaderTrailStrength = Shader.PropertyToID("_TrailStrength");

        private MeshRenderer meshRenderer;
        private Coroutine activeCoroutine;
        private Coroutine triggerCoroutine;
        private bool closing;
        private float currentOpenAmount;
        private Material distortionMat;
        private Transform distortionTransform;

        // -- Unity lifecycle -------------------------------------------------------

        private void Awake()
        {
            meshRenderer = GetComponentInChildren<MeshRenderer>(true);
            if (meshRenderer == null)
            {
                Debug.LogError($"[RiftLifetime] No MeshRenderer found in hierarchy of '{name}'.", this);
                return;
            }

            // Create a per-instance material so the shared asset is never modified,
            // matching the same pattern used in UI_ProgressBar.SetMaterial().
            if (meshRenderer.sharedMaterial != null)
                meshRenderer.material = new Material(meshRenderer.sharedMaterial);

            // Spawn the gravitational-lensing quad that persists while the rift is open.
            // It is placed slightly behind the rift (negative local Z) so the rift
            // renders on top and is not itself distorted.
            if (distortionMaterial != null)
            {
                var distortionGO = new GameObject("RiftDistortion");
                distortionGO.transform.SetParent(transform, worldPositionStays: false);
                distortionGO.transform.localScale = Vector3.one * distortionSize;
                distortionGO.transform.localPosition = new Vector3(0f, distortionYOffset, 0.01f);

                var dmf = distortionGO.AddComponent<MeshFilter>();
                dmf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

                distortionMat = new Material(distortionMaterial);
                var dmr = distortionGO.AddComponent<MeshRenderer>();
                dmr.material = distortionMat;
                distortionTransform = distortionGO.transform;
            }

            SetOpenAmount(0f);
        }

        private void OnEnable()
        {
            closing = false;
            activeCoroutine = StartCoroutine(OpenThenWait());
        }

        private void OnDisable()
        {
            if (activeCoroutine != null)
                StopCoroutine(activeCoroutine);
            if (triggerCoroutine != null)
                StopCoroutine(triggerCoroutine);
        }

        private void LateUpdate()
        {
            if (distortionTransform == null) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            // Rotate quad to face the camera (billboard in world space).
            distortionTransform.rotation = Quaternion.LookRotation(
                distortionTransform.position - cam.transform.position);
        }

        private void OnDestroy()
        {
            // Clean up the instanced materials we created.
            if (meshRenderer != null && meshRenderer.material != null)
                Destroy(meshRenderer.material);
            if (distortionMat != null)
                Destroy(distortionMat);
        }

        // -- Public API ------------------------------------------------------------

        /// <summary>
        /// Triggers the close animation immediately. The GameObject will be
        /// destroyed once the animation completes. Safe to call multiple times.
        /// </summary>
        public void Close()
        {
            if (closing) return;
            if (activeCoroutine != null) StopCoroutine(activeCoroutine);
            activeCoroutine = StartCoroutine(CloseAndDestroy());
        }

        /// <summary>
        /// Restarts the open animation from the current state. Cancels any
        /// in-progress close animation.
        /// </summary>
        public void Open()
        {
            closing = false;
            if (activeCoroutine != null) StopCoroutine(activeCoroutine);
            activeCoroutine = StartCoroutine(OpenThenWait());
        }

        /// <summary>
        /// Emits a one-shot pulse-wave ring that travels outward along the ground
        /// from the base of the rift. Safe to call while the rift is open or opening.
        /// </summary>
        public void Trigger()
        {
            if (pulseWaveMaterial == null) return;
            if (triggerCoroutine != null) StopCoroutine(triggerCoroutine);
            triggerCoroutine = StartCoroutine(GroundPulseWave());
        }

        // -- Coroutines ------------------------------------------------------------

        private IEnumerator OpenThenWait()
        {
            float startValue = currentOpenAmount;
            yield return Animate(startValue, 1f, openDuration * (1f - startValue), EaseOut);

            if (lifetime >= 0f)
            {
                yield return new WaitForSeconds(lifetime);
                Close();
            }
        }

        private IEnumerator CloseAndDestroy()
        {
            closing = true;

            // Animate from wherever we currently are so Close() mid-open doesn't jump.
            float startValue = currentOpenAmount;
            yield return Animate(startValue, 0f, closeDuration * startValue, EaseOut);

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
            var mat = new Material(pulseWaveMaterial);
            mat.SetFloat(ShaderTrailStrength, pulseTrailStrength);
            mr.material = mat;

            float elapsed = 0f;
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / pulseDuration);
                float currentRadius = progress * pulseMaxRadius;
                // XZ scale: cylinder radius 0.5 OS → world radius = scale * 0.5
                // Y scale: pulseWaveHeight controls the 3D ring wall height.
                // Offset up by half world height so bottom cap sits at ground level.
                go.transform.localScale = new Vector3(currentRadius * 2f, pulseWaveHeight, currentRadius * 2f);
                go.transform.position = transform.position + Vector3.up * (pulseWaveHeight * 0.5f + 0.01f); // minimal z-fight lift
                mat.SetFloat(ShaderProgress, progress);
                yield return null;
            }

            Destroy(mat);
            Destroy(go);
            triggerCoroutine = null;
        }

        private void SetOpenAmount(float value)
        {
            currentOpenAmount = value;
            UIShaderProperties.SetFloat(meshRenderer, OpenAmountProperty, value);
            // Distortion is fully active when the rift is open (_Progress 1) and
            // invisible when closed (_Progress 0).
            distortionMat?.SetFloat(ShaderProgress, value);
        }

        // -- Easing functions ------------------------------------------------------

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
        private static float EaseIn(float t) => t * t;

        public void VFXFadeOut()
        {
            // NYI
        }

        public void VFXFadeIn()
        {
            // NYI
        }
    }
}
