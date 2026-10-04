namespace Game.Client.VFX
{
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Rendering;

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
        [SerializeField, Min(0.1f)] private float intervalMin = 1.5f;

        [Tooltip("Maximum seconds between strike attempts.")]
        [SerializeField, Min(0.1f)] private float intervalMax = 4.0f;

        [Header("Strike")]
        [Tooltip("Sphere radius in metres used to find collider targets.")]
        [SerializeField, Min(0.5f)] private float range = 8f;

        [Tooltip("Minimum distance (metres) to target's nearest surface. Rejects targets that are too close — e.g. terrain right at the rift base.")]
        [SerializeField, Min(0f)] private float minTargetDistance = 1.5f;

        [Tooltip("Layers the lightning bolt will target.")]
        [SerializeField] private LayerMask hitLayers = -1; // -1 == Everything

        [Tooltip("Maximum simultaneous bolts in flight.")]
        [SerializeField, Range(1, 8)] private int maxSimultaneous = 2;

        [Header("Bolt Visuals")]
        [ColorUsage(true, true)]
        [SerializeField] private Color boltColor = new Color(2f, 1.2f, 4f, 1f);

        [SerializeField, Min(0.001f)] private float boltWidth = 0.04f;

        [Tooltip("Number of line segments.  More = more jagged detail.")]
        [SerializeField, Range(4, 48)] private int boltSegments = 14;

        [Tooltip("Maximum perpendicular displacement (metres) per segment at mid-bolt.")]
        [SerializeField, Min(0f)] private float jitter = 0.35f;

        [Tooltip("Seconds the bolt is visible.")]
        [SerializeField, Min(0.02f)] private float flashDuration = 0.18f;

        [Tooltip("Optional override material.  If unset, NinthAge/VFX/RiftBolt is used.")]
        [SerializeField] private Material boltMaterial;

        // ── Runtime state ─────────────────────────────────────────────────────

        private ProceduralRiftMesh riftMesh;
        private int activeStrikes;
        private Material runtimeMaterial;
        // All colliders that live on the rift's own GameObject or any of its
        // children/descendants — rebuilt whenever the rift regenerates.
        private readonly System.Collections.Generic.HashSet<Collider> selfColliders
            = new System.Collections.Generic.HashSet<Collider>();
        // Tracks every live bolt GO so we can destroy them if the component is disabled mid-flash.
        private readonly System.Collections.Generic.HashSet<GameObject> activeBoltObjects
            = new System.Collections.Generic.HashSet<GameObject>();

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake() => riftMesh = GetComponent<ProceduralRiftMesh>();

        private void OnEnable()
        {
            RebuildSelfColliders();
            riftMesh.OnGenerated += RebuildSelfColliders;
            activeStrikes = 0;
            StartCoroutine(StrikeLoop());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (riftMesh != null) riftMesh.OnGenerated -= RebuildSelfColliders;
            DestroyAllBolts();
        }

        private void OnDestroy()
        {
            DestroyAllBolts();
            if (runtimeMaterial == null) return;
            if (Application.isPlaying) Destroy(runtimeMaterial);
            else DestroyImmediate(runtimeMaterial);
        }

        private void DestroyAllBolts()
        {
            foreach (GameObject go in activeBoltObjects)
            {
                if (go == null) continue;
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
            activeBoltObjects.Clear();
        }

        // ── Self-collider cache ───────────────────────────────────────────────

        private void RebuildSelfColliders()
        {
            selfColliders.Clear();
            // Include inactive objects — child VFX components might be toggled off.
            foreach (Collider c in GetComponentsInChildren<Collider>(true))
                selfColliders.Add(c);
        }

        // ── Strike loop ───────────────────────────────────────────────────────

        private IEnumerator StrikeLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(intervalMin, intervalMax));

                if (activeStrikes < maxSimultaneous)
                    StartCoroutine(FireStrike());
            }
        }

        private IEnumerator FireStrike()
        {
            Mesh mesh = riftMesh != null ? riftMesh.SharedMesh : null;
            if (mesh == null) yield break;

            Vector3[] verts = mesh.vertices;
            // Vertex layout: (segments+1)*4 body verts followed by 2 tip verts.
            // Exclude the tips so bolts never originate at ground level or apex.
            int bodyVertCount = Mathf.Clamp((riftMesh.Segments + 1) * 4, 1, verts.Length);

            // Target search is centred on the rift's world position — NOT on a random
            // vertex — so proximity is measured relative to the whole rift, not just
            // whichever vertex happened to be picked as the visual bolt origin.
            Vector3 riftCentre = transform.position;

            // Find the closest valid collider within range.
            Collider[] candidates = Physics.OverlapSphere(riftCentre, range, hitLayers, QueryTriggerInteraction.Ignore);

            // Build a list of valid targets with their nearest surface points.
            // Use a temporary list to avoid allocating a generic List type repeatedly.
            var validTargets = new System.Collections.Generic.List<(Collider col, Vector3 pt, float dist)>();
            float minSq = minTargetDistance * minTargetDistance;

            foreach (Collider c in candidates)
            {
                if (selfColliders.Contains(c)) continue;

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

            activeStrikes++;
            yield return BoltFlash(worldOrigin, chosen.pt);
            activeStrikes--;
        }

        // ── Bolt rendering ────────────────────────────────────────────────────

        private IEnumerator BoltFlash(Vector3 from, Vector3 to)
        {
            // Build a temporary LineRenderer for this bolt's lifetime.
            var go = new GameObject("__LightningBolt") { hideFlags = HideFlags.HideAndDontSave };
            activeBoltObjects.Add(go);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.sharedMaterial = GetBoltMaterial();
            lr.widthMultiplier = boltWidth;
            lr.positionCount = boltSegments + 1;
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

            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;

                float fade = 1f - elapsed / flashDuration;

                // HDR colour fades out; endpoint is dimmer than origin.
                lr.startColor = boltColor * fade;
                lr.endColor = boltColor * (fade * 0.4f);

                // One re-jitter at 25 % through flash gives a convincing flicker.
                if (!flickered && elapsed > flashDuration * 0.25f)
                {
                    RefreshBoltPositions(lr, from, to, perp1, perp2);
                    flickered = true;
                }

                yield return null;
            }

            activeBoltObjects.Remove(go);
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
                          + perp1 * Random.Range(-jitter, jitter) * envelope
                          + perp2 * Random.Range(-jitter, jitter) * envelope;

                lr.SetPosition(i, p);
            }
        }

        private Material GetBoltMaterial()
        {
            if (boltMaterial != null) return boltMaterial;

            if (runtimeMaterial != null) return runtimeMaterial;

            var shader = Shader.Find("NinthAge/VFX/RiftBolt")
                      ?? Shader.Find("Universal Render Pipeline/Unlit");

            runtimeMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            return runtimeMaterial;
        }
    }
}
