namespace Game.Client.VFX
{
    using System;
    using UnityEngine;
    using UnityEngine.Serialization;

    /// <summary>
    /// Provides the client-side <c>ProceduralRiftMesh</c> implementation.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class ProceduralRiftMesh : MonoBehaviour
    {
        [Header("Cylinder Bounds")]
        [FormerlySerializedAs("radius")]
        [SerializeField] private float _radius = 1.5f;
        [FormerlySerializedAs("height")]
        [SerializeField] private float _height = 3.0f;

        [Header("Shape")]
        [FormerlySerializedAs("segments")]
        [SerializeField, Min(4)] private int _segments = 18;
        [FormerlySerializedAs("minWidthFactor")]
        [SerializeField, Range(0.0f, 1.0f)] private float _minWidthFactor = 0.18f;
        [FormerlySerializedAs("maxWidthFactor")]
        [SerializeField, Range(0.0f, 1.0f)] private float _maxWidthFactor = 0.75f;
        [FormerlySerializedAs("edgeJitter")]
        [SerializeField, Min(0.0f)] private float _edgeJitter = 0.15f;
        [FormerlySerializedAs("centreWobble")]
        [SerializeField, Min(0.0f)] private float _centreWobble = 0.1f;
        [FormerlySerializedAs("thickness")]
        [SerializeField, Min(0.01f)] private float _thickness = 0.2f;

        [Header("Spiral")]
        [FormerlySerializedAs("spiralTurns")]
        [SerializeField, Range(-5.0f, 5.0f)] private float _spiralTurns = 0.75f;
        [FormerlySerializedAs("spiralJitter")]
        [SerializeField, Range(0.0f, 1.0f)] private float _spiralJitter = 0.3f;

        [Header("Random")]
        [FormerlySerializedAs("randomiseOnAwake")]
        [SerializeField] private bool _randomiseOnAwake = true;
        [FormerlySerializedAs("seed")]
        [SerializeField] private int _seed = 0;

        /// <summary>Fired at the end of every Generate() call.</summary>
        public event System.Action onGenerated;

        /// <summary>
        /// Gets the <c>SharedMesh</c> value.
        /// </summary>
        public Mesh SharedMesh => _mesh;
        /// <summary>
        /// Gets the <c>Segments</c> value.
        /// </summary>
        public int Segments => _segments;

        private Mesh _mesh;

        private void Awake()
        {
            if (_randomiseOnAwake)
                Generate(UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            else
                Generate(_seed);
        }

        /// <summary>
        /// Executes the client-side <c>Generate</c> operation.
        /// </summary>
        public void Generate(int newSeed)
        {
            _seed = newSeed;

            if (_mesh == null)
            {
                _mesh = new()
                {
                    name = "Procedural Rift Mesh"
                };

                GetComponent<MeshFilter>().sharedMesh = _mesh;
            }
            else
            {
                _mesh.Clear();
            }

            System.Random rng = new(_seed);

            int rowCount = _segments + 1;

            // Diamond cross-section per row: left (0), right (1), front peak (2), back peak (3).
            // Two tip vertices close off the top and bottom ends.
            var vertices = new Vector3[rowCount * 4 + 2];
            var uvs = new Vector2[rowCount * 4 + 2];

            // Side: segments × 4 faces × 2 tris × 3 verts = segments × 24 indices.
            // Caps: 2 ends × 4 tris × 3 verts = 24 indices.
            var triangles = new int[_segments * 24 + 24];

            float halfHeight = _height * 0.5f;
            float maxHalfWidth = _radius * _maxWidthFactor;
            float minHalfWidth = _radius * _minWidthFactor;

            // Spine Z accumulates as we go up, pulled back to 0 at both tips.
            float spineZ = 0f;
            float rollAngle = 0f;  // accumulated spiral angle in radians
            float baseAngleStep = _spiralTurns * Mathf.PI * 2f / _segments;

            for (int i = 0; i < rowCount; i++)
            {
                float t = i / (float)_segments;
                float y = Mathf.Lerp(-halfHeight, halfHeight, t);
                float profile = Mathf.Sin(t * Mathf.PI); // 0 at tips, 1 at mid

                float baseHalfWidth = Mathf.Lerp(minHalfWidth, maxHalfWidth, profile);
                float jitterStrength = _edgeJitter * profile;

                float leftJitter = RandomRange(rng, -jitterStrength, jitterStrength);
                float rightJitter = RandomRange(rng, -jitterStrength, jitterStrength);
                float centreOffset = RandomRange(rng, -_centreWobble, _centreWobble) * profile;

                // Spine wanders through Z space, tapering back to 0 at the tips.
                float spineStep = RandomRange(rng, -_centreWobble, _centreWobble) * 0.5f;
                spineZ = Mathf.Lerp(0f, spineZ + spineStep, profile);

                // Spiral: accumulate angle each step, with optional per-step jitter.
                float jitteredStep = baseAngleStep * (1f + RandomRange(rng, -_spiralJitter, _spiralJitter));
                if (i > 0) rollAngle += jitteredStep;

                float leftX = Mathf.Clamp(centreOffset - baseHalfWidth + leftJitter, -_radius, _radius);
                float rightX = Mathf.Clamp(centreOffset + baseHalfWidth + rightJitter, -_radius, _radius);
                float centreX = (leftX + rightX) * 0.5f;

                // Diamond half-depth is a fixed world-space value, not scaled by radius.
                float halfDepth = _thickness * profile;

                // Rotate the four diamond corners around the spine centre in the XZ plane.
                float cos = Mathf.Cos(rollAngle);
                float sin = Mathf.Sin(rollAngle);

                var spine = new Vector3(centreX, y, spineZ);

                Vector3 Rotate(float ox, float oz)
                {
                    return spine + new Vector3(ox * cos - oz * sin, 0f, ox * sin + oz * cos);
                }

                int b = i * 4;
                vertices[b + 0] = Rotate(leftX - centreX, 0f);        // left
                vertices[b + 1] = Rotate(rightX - centreX, 0f);        // right
                vertices[b + 2] = Rotate(0f, halfDepth);              // front peak
                vertices[b + 3] = Rotate(0f, -halfDepth);              // back peak

                uvs[b + 0] = new Vector2(0f, t);
                uvs[b + 1] = new Vector2(1f, t);
                uvs[b + 2] = new Vector2(0.5f, t);
                uvs[b + 3] = new Vector2(0.5f, t);
            }

            // Tip vertices: average of their respective end-row diamond, offset slightly.
            int bottomTip = rowCount * 4;
            int topTip = rowCount * 4 + 1;

            Vector3 botAvg = (vertices[0] + vertices[1] + vertices[2] + vertices[3]) * 0.25f;
            Vector3 topAvg = (vertices[_segments * 4] + vertices[_segments * 4 + 1] +
                              vertices[_segments * 4 + 2] + vertices[_segments * 4 + 3]) * 0.25f;

            vertices[bottomTip] = botAvg - new Vector3(0f, _height * 0.03f, 0f);
            vertices[topTip] = topAvg + new Vector3(0f, _height * 0.03f, 0f);
            uvs[bottomTip] = new Vector2(0.5f, 0f);
            uvs[topTip] = new Vector2(0.5f, 1f);

            // --- Triangles ---
            int tri = 0;

            // Side faces. Diamond CCW from +Y: L(0) → F(2) → R(1) → B(3) → L.
            // Each face pair (A→B): triangles (A0,B0,B1) and (A0,B1,A1).
            for (int i = 0; i < _segments; i++)
            {
                int b0 = i * 4, b1 = (i + 1) * 4;
                int L0 = b0, R0 = b0 + 1, F0 = b0 + 2, B0 = b0 + 3;
                int L1 = b1, R1 = b1 + 1, F1 = b1 + 2, B1 = b1 + 3;

                // Left → Front
                triangles[tri++] = L0; triangles[tri++] = F0; triangles[tri++] = F1;
                triangles[tri++] = L0; triangles[tri++] = F1; triangles[tri++] = L1;
                // Front → Right
                triangles[tri++] = F0; triangles[tri++] = R0; triangles[tri++] = R1;
                triangles[tri++] = F0; triangles[tri++] = R1; triangles[tri++] = F1;
                // Right → Back
                triangles[tri++] = R0; triangles[tri++] = B0; triangles[tri++] = B1;
                triangles[tri++] = R0; triangles[tri++] = B1; triangles[tri++] = R1;
                // Back → Left
                triangles[tri++] = B0; triangles[tri++] = L0; triangles[tri++] = L1;
                triangles[tri++] = B0; triangles[tri++] = L1; triangles[tri++] = B1;
            }

            // Bottom cap — normal faces −Y.
            {
                int L = 0, R = 1, F = 2, B = 3;
                triangles[tri++] = bottomTip; triangles[tri++] = L; triangles[tri++] = B;
                triangles[tri++] = bottomTip; triangles[tri++] = B; triangles[tri++] = R;
                triangles[tri++] = bottomTip; triangles[tri++] = R; triangles[tri++] = F;
                triangles[tri++] = bottomTip; triangles[tri++] = F; triangles[tri++] = L;
            }

            // Top cap — normal faces +Y.
            {
                int bt = _segments * 4;
                int L = bt, R = bt + 1, F = bt + 2, B = bt + 3;
                triangles[tri++] = topTip; triangles[tri++] = L; triangles[tri++] = F;
                triangles[tri++] = topTip; triangles[tri++] = F; triangles[tri++] = R;
                triangles[tri++] = topTip; triangles[tri++] = R; triangles[tri++] = B;
                triangles[tri++] = topTip; triangles[tri++] = B; triangles[tri++] = L;
            }

            _mesh.vertices = vertices;
            _mesh.uv = uvs;
            _mesh.triangles = triangles;

            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            onGenerated?.Invoke();
        }

        private static float RandomRange(System.Random rng, float min, float max)
        {
            return Mathf.Lerp(min, max, (float)rng.NextDouble());
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _segments = Mathf.Max(4, _segments);
            _radius = Mathf.Max(0.01f, _radius);
            _height = Mathf.Max(0.01f, _height);

            if (!Application.isPlaying)
                Generate(_seed);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.25f);
            Gizmos.DrawWireCube(transform.position, new Vector3(_radius * 2f, _height, _radius * 2f));
        }
#endif
    }
}
