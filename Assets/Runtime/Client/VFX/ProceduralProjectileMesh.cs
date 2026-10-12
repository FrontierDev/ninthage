namespace Game.Client.VFX
{
#if UNITY_EDITOR
    using UnityEditor;
#endif
    using UnityEngine;
    using UnityEngine.Serialization;

    /// <summary>
    /// Editor-time mesh generator for spell projectile shapes.
    /// Generates geometry and bakes to mesh assets that can be saved to prefabs.
    /// Unlike ProceduralRiftMesh, this does NOT generate meshes at runtime.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class ProceduralProjectileMesh : MonoBehaviour
    {
        /// <summary>
        /// Defines the client-side <c>ProjectileShape</c> options.
        /// </summary>
        public enum ProjectileShape
        {
            Sphere,
            Capsule,
            ElongatedCapsule,
            TaperedSphere,
        }

        [Header("Shape")]
        [FormerlySerializedAs("shapeType")]
        [SerializeField] private ProjectileShape _shapeType = ProjectileShape.Sphere;
        [FormerlySerializedAs("scale")]
        [SerializeField, Min(0.01f)] private float _scale = 1.0f;

        [Header("Quality")]
        [FormerlySerializedAs("segments")]
        [SerializeField, Min(8)] private int _segments = 24;  // horizontal divisions
        [FormerlySerializedAs("rings")]
        [SerializeField, Min(6)] private int _rings = 12;      // vertical divisions

        [Header("Capsule Only")]
        [FormerlySerializedAs("capsuleLength")]
        [SerializeField, Min(0.0f)] private float _capsuleLength = 1.5f;  // shaft length for capsule

        [Header("Tapered Sphere Only")]
        [FormerlySerializedAs("taperStrength")]
        [SerializeField, Range(0.0f, 1.0f)] private float _taperStrength = 0.5f;  // 0 = sphere, 1 = full taper

        private Mesh _cachedMesh;

        /// <summary>
        /// Gets the <c>GeneratedMesh</c> value.
        /// </summary>
        public Mesh GeneratedMesh => _cachedMesh;

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && gameObject.activeInHierarchy)
            {
                RegenerateMesh();
                AutoBakeMeshToAsset();
            }
#endif
        }

        private void RegenerateMesh()
        {
            if (_cachedMesh == null)
            {
                _cachedMesh = new() { name = $"Projectile_{_shapeType}" };
                GetComponent<MeshFilter>().sharedMesh = _cachedMesh;
            }

            _cachedMesh.Clear();

            switch (_shapeType)
            {
                case ProjectileShape.Sphere:
                    GenerateSphere(_cachedMesh);
                    break;
                case ProjectileShape.Capsule:
                    GenerateCapsule(_cachedMesh);
                    break;
                case ProjectileShape.ElongatedCapsule:
                    GenerateElongatedCapsule(_cachedMesh);
                    break;
                case ProjectileShape.TaperedSphere:
                    GenerateTaperedSphere(_cachedMesh);
                    break;
            }

            _cachedMesh.RecalculateNormals();
            _cachedMesh.RecalculateBounds();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(_cachedMesh);
#endif
        }

        private void GenerateSphere(Mesh mesh)
        {
            // UV sphere: rings (vertical), segments (horizontal)
            int vertexCount = (_rings + 1) * (_segments + 1);
            var vertices = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];

            int vertexIndex = 0;
            for (int ring = 0; ring <= _rings; ring++)
            {
                float v = ring / (float)_rings;  // 0 to 1 from bottom to top
                float phi = Mathf.PI * v;      // 0 to π

                for (int seg = 0; seg <= _segments; seg++)
                {
                    float u = seg / (float)_segments;  // 0 to 1
                    float theta = 2f * Mathf.PI * u;  // 0 to 2π

                    float sinPhi = Mathf.Sin(phi);
                    float x = Mathf.Cos(theta) * sinPhi;
                    float y = Mathf.Cos(phi);
                    float z = Mathf.Sin(theta) * sinPhi;

                    vertices[vertexIndex] = new Vector3(x, y, z) * _scale;
                    uvs[vertexIndex] = new Vector2(u, v);
                    vertexIndex++;
                }
            }

            // Triangles
            int triangleCount = _rings * _segments * 6;
            var triangles = new int[triangleCount];
            int triIndex = 0;

            for (int ring = 0; ring < _rings; ring++)
            {
                for (int seg = 0; seg < _segments; seg++)
                {
                    int a = ring * (_segments + 1) + seg;
                    int b = a + 1;
                    int c = a + (_segments + 1);
                    int d = c + 1;

                    // CCW winding for outward-facing normals
                    triangles[triIndex++] = a;
                    triangles[triIndex++] = b;
                    triangles[triIndex++] = c;

                    triangles[triIndex++] = b;
                    triangles[triIndex++] = d;
                    triangles[triIndex++] = c;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
        }

        private void GenerateCapsule(Mesh mesh)
        {
            // Capsule = hemisphere bottom + cylinder + hemisphere top
            float hemisphereDiameter = _scale;
            float cylinderLength = _capsuleLength * _scale;
            float halfCylinderLength = cylinderLength * 0.5f;

            // Build: bottom hemisphere, then cylinder, then top hemisphere
            int hemVertCount = (_rings / 2 + 1) * (_segments + 1);  // half rings for hemisphere
            int cylVertCount = 2 * (_segments + 1);                // two rings for cylinder
            int totalVerts = hemVertCount * 2 + cylVertCount;

            var vertices = new Vector3[totalVerts];
            var uvs = new Vector2[totalVerts];
            int vertIndex = 0;

            // Bottom hemisphere (y from -hemisphereDiameter/2 to 0)
            int hemRings = _rings / 2;
            for (int ring = 0; ring <= hemRings; ring++)
            {
                float v = ring / (float)hemRings;
                float phi = Mathf.PI * 0.5f * v;  // 0 to π/2

                for (int seg = 0; seg <= _segments; seg++)
                {
                    float u = seg / (float)_segments;
                    float theta = 2f * Mathf.PI * u;

                    float sinPhi = Mathf.Sin(phi);
                    float x = Mathf.Cos(theta) * sinPhi * hemisphereDiameter * 0.5f;
                    float y = -Mathf.Cos(phi) * hemisphereDiameter * 0.5f;
                    float z = Mathf.Sin(theta) * sinPhi * hemisphereDiameter * 0.5f;

                    vertices[vertIndex] = new Vector3(x, y, z);
                    uvs[vertIndex] = new Vector2(u, v * 0.5f);
                    vertIndex++;
                }
            }

            // Cylinder (two rings at y = ±halfCylinderLength)
            for (int cy = 0; cy < 2; cy++)
            {
                float y = (cy == 0) ? -halfCylinderLength : halfCylinderLength;

                for (int seg = 0; seg <= _segments; seg++)
                {
                    float u = seg / (float)_segments;
                    float theta = 2f * Mathf.PI * u;

                    float x = Mathf.Cos(theta) * hemisphereDiameter * 0.5f;
                    float z = Mathf.Sin(theta) * hemisphereDiameter * 0.5f;

                    vertices[vertIndex] = new Vector3(x, y, z);
                    uvs[vertIndex] = new Vector2(u, 0.5f + cy * 0.25f);
                    vertIndex++;
                }
            }

            // Top hemisphere (y from 0 to hemisphereDiameter/2)
            for (int ring = 0; ring <= hemRings; ring++)
            {
                float v = ring / (float)hemRings;
                float phi = Mathf.PI * 0.5f * v;

                for (int seg = 0; seg <= _segments; seg++)
                {
                    float u = seg / (float)_segments;
                    float theta = 2f * Mathf.PI * u;

                    float sinPhi = Mathf.Sin(phi);
                    float x = Mathf.Cos(theta) * sinPhi * hemisphereDiameter * 0.5f;
                    float y = Mathf.Cos(phi) * hemisphereDiameter * 0.5f + halfCylinderLength;
                    float z = Mathf.Sin(theta) * sinPhi * hemisphereDiameter * 0.5f;

                    vertices[vertIndex] = new Vector3(x, y, z);
                    uvs[vertIndex] = new Vector2(u, 0.5f + v * 0.5f);
                    vertIndex++;
                }
            }

            // Connect triangles across all three sections
            var triangles = new int[_rings * _segments * 6 + hemRings * _segments * 12];
            int triIndex = 0;

            // Bottom hemisphere triangles
            int botHemStart = 0;
            for (int ring = 0; ring < hemRings; ring++)
            {
                for (int seg = 0; seg < _segments; seg++)
                {
                    int a = botHemStart + ring * (_segments + 1) + seg;
                    int b = a + 1;
                    int c = a + (_segments + 1);
                    int d = c + 1;

                    triangles[triIndex++] = a;
                    triangles[triIndex++] = b;
                    triangles[triIndex++] = c;
                    triangles[triIndex++] = b;
                    triangles[triIndex++] = d;
                    triangles[triIndex++] = c;
                }
            }

            // Cylinder triangles
            int cylStart = hemVertCount;
            for (int seg = 0; seg < _segments; seg++)
            {
                int a = cylStart + seg;
                int b = a + 1;
                int c = cylStart + (_segments + 1) + seg;
                int d = c + 1;

                triangles[triIndex++] = a;
                triangles[triIndex++] = b;
                triangles[triIndex++] = c;
                triangles[triIndex++] = b;
                triangles[triIndex++] = d;
                triangles[triIndex++] = c;
            }

            // Top hemisphere triangles
            int topHemStart = hemVertCount + cylVertCount;
            for (int ring = 0; ring < hemRings; ring++)
            {
                for (int seg = 0; seg < _segments; seg++)
                {
                    int a = topHemStart + ring * (_segments + 1) + seg;
                    int b = a + 1;
                    int c = a + (_segments + 1);
                    int d = c + 1;

                    triangles[triIndex++] = a;
                    triangles[triIndex++] = b;
                    triangles[triIndex++] = c;
                    triangles[triIndex++] = b;
                    triangles[triIndex++] = d;
                    triangles[triIndex++] = c;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
        }

        private void GenerateElongatedCapsule(Mesh mesh)
        {
            // Capsule stretched longer (useful for arrows, bolts)
            float hemisphereDiameter = _scale * 0.6f;
            float cylinderLength = _capsuleLength * _scale * 2.5f;
            float halfCylinderLength = cylinderLength * 0.5f;

            // Simplified: just generate as regular capsule with different proportions
            GenerateCapsuleGeometry(mesh, hemisphereDiameter, cylinderLength);
        }

        private void GenerateTaperedSphere(Mesh mesh)
        {
            // Sphere that tapers toward one end (like a flame or tear drop)
            int vertexCount = (_rings + 1) * (_segments + 1);
            var vertices = new Vector3[vertexCount];
            var uvs = new Vector2[vertexCount];

            int vertexIndex = 0;
            for (int ring = 0; ring <= _rings; ring++)
            {
                float v = ring / (float)_rings;
                float phi = Mathf.PI * v;

                // Apply taper: top (v=1) gets scaled down by taperStrength
                float taperFactor = Mathf.Lerp(1.0f, 1.0f - _taperStrength, v);

                for (int seg = 0; seg <= _segments; seg++)
                {
                    float u = seg / (float)_segments;
                    float theta = 2f * Mathf.PI * u;

                    float sinPhi = Mathf.Sin(phi);
                    float x = Mathf.Cos(theta) * sinPhi * taperFactor;
                    float y = Mathf.Cos(phi);
                    float z = Mathf.Sin(theta) * sinPhi * taperFactor;

                    vertices[vertexIndex] = new Vector3(x, y, z) * _scale;
                    uvs[vertexIndex] = new Vector2(u, v);
                    vertexIndex++;
                }
            }

            var triangles = new int[_rings * _segments * 6];
            int triIndex = 0;

            for (int ring = 0; ring < _rings; ring++)
            {
                for (int seg = 0; seg < _segments; seg++)
                {
                    int a = ring * (_segments + 1) + seg;
                    int b = a + 1;
                    int c = a + (_segments + 1);
                    int d = c + 1;

                    // CCW winding for outward-facing normals
                    triangles[triIndex++] = a;
                    triangles[triIndex++] = b;
                    triangles[triIndex++] = c;

                    triangles[triIndex++] = b;
                    triangles[triIndex++] = d;
                    triangles[triIndex++] = c;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
        }

        private void GenerateCapsuleGeometry(Mesh mesh, float hemisphereDiameter, float cylinderLength)
        {
            // Helper for both capsule variants
            float halfCylinderLength = cylinderLength * 0.5f;
            int hemRings = Mathf.Max(_rings / 2, 2);
            int hemVertCount = (hemRings + 1) * (_segments + 1);
            int cylVertCount = 2 * (_segments + 1);
            int totalVerts = hemVertCount * 2 + cylVertCount;

            var vertices = new Vector3[totalVerts];
            var uvs = new Vector2[totalVerts];
            int vertIndex = 0;

            // Bottom hemisphere
            for (int ring = 0; ring <= hemRings; ring++)
            {
                float v = ring / (float)hemRings;
                float phi = Mathf.PI * 0.5f * v;

                for (int seg = 0; seg <= _segments; seg++)
                {
                    float u = seg / (float)_segments;
                    float theta = 2f * Mathf.PI * u;

                    float sinPhi = Mathf.Sin(phi);
                    float x = Mathf.Cos(theta) * sinPhi * hemisphereDiameter * 0.5f;
                    float y = -Mathf.Cos(phi) * hemisphereDiameter * 0.5f;
                    float z = Mathf.Sin(theta) * sinPhi * hemisphereDiameter * 0.5f;

                    vertices[vertIndex] = new Vector3(x, y, z);
                    uvs[vertIndex] = new Vector2(u, v * 0.25f);
                    vertIndex++;
                }
            }

            // Cylinder
            for (int cy = 0; cy < 2; cy++)
            {
                float y = (cy == 0) ? -halfCylinderLength : halfCylinderLength;
                for (int seg = 0; seg <= _segments; seg++)
                {
                    float u = seg / (float)_segments;
                    float theta = 2f * Mathf.PI * u;

                    float x = Mathf.Cos(theta) * hemisphereDiameter * 0.5f;
                    float z = Mathf.Sin(theta) * hemisphereDiameter * 0.5f;

                    vertices[vertIndex] = new Vector3(x, y, z);
                    uvs[vertIndex] = new Vector2(u, 0.25f + cy * 0.5f);
                    vertIndex++;
                }
            }

            // Top hemisphere
            for (int ring = 0; ring <= hemRings; ring++)
            {
                float v = ring / (float)hemRings;
                float phi = Mathf.PI * 0.5f * v;

                for (int seg = 0; seg <= _segments; seg++)
                {
                    float u = seg / (float)_segments;
                    float theta = 2f * Mathf.PI * u;

                    float sinPhi = Mathf.Sin(phi);
                    float x = Mathf.Cos(theta) * sinPhi * hemisphereDiameter * 0.5f;
                    float y = Mathf.Cos(phi) * hemisphereDiameter * 0.5f + halfCylinderLength;
                    float z = Mathf.Sin(theta) * sinPhi * hemisphereDiameter * 0.5f;

                    vertices[vertIndex] = new Vector3(x, y, z);
                    uvs[vertIndex] = new Vector2(u, 0.75f + v * 0.25f);
                    vertIndex++;
                }
            }

            var triangles = new int[hemRings * _segments * 12 + _segments * 6];
            int triIndex = 0;

            int botHemStart = 0;
            for (int ring = 0; ring < hemRings; ring++)
            {
                for (int seg = 0; seg < _segments; seg++)
                {
                    int a = botHemStart + ring * (_segments + 1) + seg;
                    int b = a + 1;
                    int c = a + (_segments + 1);
                    int d = c + 1;

                    triangles[triIndex++] = a; triangles[triIndex++] = b; triangles[triIndex++] = c;
                    triangles[triIndex++] = b; triangles[triIndex++] = d; triangles[triIndex++] = c;
                }
            }

            int cylStart = hemVertCount;
            for (int seg = 0; seg < _segments; seg++)
            {
                int a = cylStart + seg;
                int b = a + 1;
                int c = cylStart + (_segments + 1) + seg;
                int d = c + 1;

                triangles[triIndex++] = a; triangles[triIndex++] = b; triangles[triIndex++] = c;
                triangles[triIndex++] = b; triangles[triIndex++] = d; triangles[triIndex++] = c;
            }

            int topHemStart = hemVertCount + cylVertCount;
            for (int ring = 0; ring < hemRings; ring++)
            {
                for (int seg = 0; seg < _segments; seg++)
                {
                    int a = topHemStart + ring * (_segments + 1) + seg;
                    int b = a + 1;
                    int c = a + (_segments + 1);
                    int d = c + 1;

                    triangles[triIndex++] = a; triangles[triIndex++] = b; triangles[triIndex++] = c;
                    triangles[triIndex++] = b; triangles[triIndex++] = d; triangles[triIndex++] = c;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
        }

        private void AutoBakeMeshToAsset()
        {
#if UNITY_EDITOR
            if (_cachedMesh == null) return;

            // Create unique filename based on shape and parameters
            string meshName = $"Projectile_{_shapeType}_{_scale:F2}seg{_segments}ring{_rings}";
            string meshPath = $"Assets/Resources/Meshes/Projectiles/{meshName}.mesh";

            // Ensure directory exists
            string directory = System.IO.Path.GetDirectoryName(meshPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            // Save as unique asset
            string uniquePath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(meshPath);

            // Check if asset already exists at this path
            var existingAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(uniquePath);
            if (existingAsset != null && existingAsset == _cachedMesh)
            {
                // Mesh already saved, just mark dirty
                UnityEditor.EditorUtility.SetDirty(_cachedMesh);
                UnityEditor.AssetDatabase.SaveAssets();
            }
            else
            {
                // New mesh, create asset
                UnityEditor.AssetDatabase.CreateAsset(_cachedMesh, uniquePath);
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
                Debug.Log($"✓ Projectile mesh baked to: {uniquePath}");
            }
#endif
        }

#if UNITY_EDITOR
        [ContextMenu("Bake Mesh to Asset (Manual)")]
        private void BakeMeshToAsset()
        {
            AutoBakeMeshToAsset();
            if (_cachedMesh != null)
            {
                UnityEditor.EditorGUIUtility.PingObject(_cachedMesh);
            }
        }
#endif
    }
}
