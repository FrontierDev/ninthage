namespace Game.Client.VFX
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Serialization;

    /// <summary>
    /// Periodically fires procedural lightning bolts from random points on the
    /// rift surface to the nearest collider within <see cref="range"/>.
    ///
    /// The bolt is rendered with a LineRenderer using the <c>NinthAge/VFX/RiftBolt</c>
    /// shader (vertex-colour additive).  No separate mesh component is needed.
    /// </summary>
    [RequireComponent(typeof(ProceduralRiftMesh))]
    public sealed class RiftLightning : MonoBehaviour
    {
        [Header("Timing")]
        [Tooltip("Minimum seconds between strike attempts.")]
        [FormerlySerializedAs("intervalMin")]
        [SerializeField, Min(0.1f)] private float _intervalMin = 1.5f;

        [Tooltip("Maximum seconds between strike attempts.")]
        [FormerlySerializedAs("intervalMax")]
        [SerializeField, Min(0.1f)] private float _intervalMax = 4.0f;

        [Header("Strike")]
        [Tooltip("Sphere radius in metres used to find collider targets.")]
        [FormerlySerializedAs("range")]
        [SerializeField, Min(0.5f)] private float _range = 8f;

        [Tooltip("Minimum distance (metres) to target's nearest surface. Rejects targets that are too close — e.g. terrain right at the rift base.")]
        [FormerlySerializedAs("minTargetDistance")]
        [SerializeField, Min(0f)] private float _minTargetDistance = 1.5f;

        [Tooltip("Layers the lightning bolt will target.")]
        [FormerlySerializedAs("hitLayers")]
        [SerializeField] private LayerMask _hitLayers = -1; // -1 == Everything

        [Tooltip("Maximum simultaneous bolts in flight.")]
        [FormerlySerializedAs("maxSimultaneous")]
        [SerializeField, Range(1, 8)] private int _maxSimultaneous = 2;

        [Header("Bolt Visuals")]
        [ColorUsage(true, true)]
        [FormerlySerializedAs("boltColor")]
        [SerializeField] private Color _boltColor = new(2f, 1.2f, 4f, 1f);

        [FormerlySerializedAs("boltWidth")]
        [SerializeField, Min(0.001f)] private float _boltWidth = 0.04f;

        [Tooltip("Number of line segments.  More = more jagged detail.")]
        [FormerlySerializedAs("boltSegments")]
        [SerializeField, Range(4, 48)] private int _boltSegments = 14;

        [Tooltip("Maximum perpendicular displacement (metres) per segment at mid-bolt.")]
        [FormerlySerializedAs("jitter")]
        [SerializeField, Min(0f)] private float _jitter = 0.35f;

        [Tooltip("Seconds the bolt is visible.")]
        [FormerlySerializedAs("flashDuration")]
        [SerializeField, Min(0.02f)] private float _flashDuration = 0.18f;

        [Tooltip("Optional override material.  If unset, NinthAge/VFX/RiftBolt is used.")]
        [FormerlySerializedAs("boltMaterial")]
        [SerializeField] private Material _boltMaterial;

        // ── Runtime state ─────────────────────────────────────────────────────

        private ProceduralRiftMesh _riftMesh;
        private int _activeStrikes;
        private Material _runtimeMaterial;
        // All colliders that live on the rift's own GameObject or any of its
        // children/descendants — rebuilt whenever the rift regenerates.
        private readonly System.Collections.Generic.HashSet<Collider> _selfColliders
            = new System.Collections.Generic.HashSet<Collider>();
        // Tracks every live bolt GO so we can destroy them if the component is disabled mid-flash.
        private readonly System.Collections.Generic.HashSet<GameObject> _activeBoltObjects
            = new System.Collections.Generic.HashSet<GameObject>();

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake() => _riftMesh = GetComponent<ProceduralRiftMesh>();

        private void OnEnable()
        {
            RebuildSelfColliders();
            _riftMesh.onGenerated += RebuildSelfColliders;
            _activeStrikes = 0;
            StartCoroutine(StrikeLoop());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (_riftMesh != null) _riftMesh.onGenerated -= RebuildSelfColliders;
            DestroyAllBolts();
        }

        private void OnDestroy()
        {
            DestroyAllBolts();
            if (_runtimeMaterial == null) return;
            if (Application.isPlaying) Destroy(_runtimeMaterial);
            else DestroyImmediate(_runtimeMaterial);
        }

        private void DestroyAllBolts()
        {
            foreach (GameObject go in _activeBoltObjects)
            {
                if (go == null) continue;
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
            _activeBoltObjects.Clear();
        }

        // ── Self-collider cache ───────────────────────────────────────────────

        private void RebuildSelfColliders()
        {
            _selfColliders.Clear();
            // Include inactive objects — child VFX components might be toggled off.
            foreach (Collider c in GetComponentsInChildren<Collider>(true))
                _selfColliders.Add(c);
        }

        // ── Strike loop ───────────────────────────────────────────────────────

        private IEnumerator StrikeLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(_intervalMin, _intervalMax));

                if (_activeStrikes < _maxSimultaneous)
                    StartCoroutine(FireStrike());
            }
        }

        private IEnumerator FireStrike()
        {
            Mesh mesh = _riftMesh != null ? _riftMesh.SharedMesh : null;
            if (mesh == null) yield break;

            Vector3[] verts = mesh.vertices;
            // Vertex layout: (segments+1)*4 body verts followed by 2 tip verts.
            // Exclude the tips so bolts never originate at ground level or apex.
            int bodyVertCount = Mathf.Clamp((_riftMesh.Segments + 1) * 4, 1, verts.Length);

            // Target search is centred on the rift's world position — NOT on a random
            // vertex — so proximity is measured relative to the whole rift, not just
            // whichever vertex happened to be picked as the visual bolt origin.
            Vector3 riftCentre = transform.position;

            // Find the closest valid collider within range.
            Collider[] candidates = Physics.OverlapSphere(riftCentre, _range, _hitLayers, QueryTriggerInteraction.Ignore);

            // Build a list of valid targets with their nearest surface points.
            // Use a temporary list to avoid allocating a generic List type repeatedly.
            var validTargets = new System.Collections.Generic.List<(Collider col, Vector3 pt, float dist)>();
            float minSq = _minTargetDistance * _minTargetDistance;

            foreach (Collider c in candidates)
            {
                if (_selfColliders.Contains(c)) continue;

                Vector3 pt = c.bounds.ClosestPoint(riftCentre);
                float sq = (pt - riftCentre).sqrMagnitude;
                if (sq < minSq) continue;

                validTargets.Add((c, pt, Mathf.Sqrt(sq)));
            }

            if (validTargets.Count == 0) yield break;

            // Weighted random selection: closer targets are more likely but not guaranteed.
            // Weight = 1/distance, so nearer colliders have higher probability.
            float totalWeight = 0f;
            foreach (var t in validTargets) totalWeight += 1f / t.dist;

            float roll = Random.value * totalWeight;
            float accumulated = 0f;
            var chosen = validTargets[validTargets.Count - 1]; // fallback
            foreach (var t in validTargets)
            {
                accumulated += 1f / t.dist;
                if (roll <= accumulated) { chosen = t; break; }
            }

            // Pick a random body vertex as the VISUAL bolt origin — this is just
            // the line start point, not the search anchor.
            Vector3 worldOrigin = transform.TransformPoint(verts[Random.Range(0, bodyVertCount)]);

            _activeStrikes++;
            yield return BoltFlash(worldOrigin, chosen.pt);
            _activeStrikes--;
        }

        // ── Bolt rendering ────────────────────────────────────────────────────

        private IEnumerator BoltFlash(Vector3 from, Vector3 to)
        {
            // Build a temporary LineRenderer for this bolt's lifetime.
            var go = new GameObject("__LightningBolt") { hideFlags = HideFlags.HideAndDontSave };
            _activeBoltObjects.Add(go);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.sharedMaterial = GetBoltMaterial();
            lr.widthMultiplier = _boltWidth;
            lr.positionCount = _boltSegments + 1;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            // Compute two axes perpendicular to the bolt direction for random jitter.
            Vector3 dir = to - from;
            Vector3 up = Mathf.Abs(Vector3.Dot(dir.normalized, Vector3.up)) > 0.9f
                            ? Vector3.right : Vector3.up;
            Vector3 perp1 = Vector3.Cross(dir.normalized, up).normalized;
            Vector3 perp2 = Vector3.Cross(dir.normalized, perp1);

            RefreshBoltPositions(lr, from, to, perp1, perp2);

            // Animate: fade out over flashDuration with one mid-flash re-jitter.
            float elapsed = 0f;
            bool flickered = false;

            while (elapsed < _flashDuration)
            {
                elapsed += Time.deltaTime;

                float fade = 1f - elapsed / _flashDuration;

                // HDR colour fades out; endpoint is dimmer than origin.
                lr.startColor = _boltColor * fade;
                lr.endColor = _boltColor * (fade * 0.4f);

                // One re-jitter at 25 % through flash gives a convincing flicker.
                if (!flickered && elapsed > _flashDuration * 0.25f)
                {
                    RefreshBoltPositions(lr, from, to, perp1, perp2);
                    flickered = true;
                }

                yield return null;
            }

            _activeBoltObjects.Remove(go);
            Destroy(go);
        }

        private void RefreshBoltPositions(LineRenderer lr, Vector3 from, Vector3 to,
                                          Vector3 perp1, Vector3 perp2)
        {
            int n = lr.positionCount;
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / (n - 1);
                float envelope = Mathf.Sin(s * Mathf.PI); // zero at endpoints

                Vector3 p = Vector3.Lerp(from, to, s)
                          + perp1 * Random.Range(-_jitter, _jitter) * envelope
                          + perp2 * Random.Range(-_jitter, _jitter) * envelope;

                lr.SetPosition(i, p);
            }
        }

        private Material GetBoltMaterial()
        {
            if (_boltMaterial != null) return _boltMaterial;

            if (_runtimeMaterial != null) return _runtimeMaterial;

            var shader = Shader.Find("NinthAge/VFX/RiftBolt")
                      ?? Shader.Find("Universal Render Pipeline/Unlit");

            _runtimeMaterial = new(shader) { hideFlags = HideFlags.HideAndDontSave };
            return _runtimeMaterial;
        }
    }
}
