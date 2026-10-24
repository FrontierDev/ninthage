using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Runtime.World
{
    /// <summary>Defines a finite transfer surface using the planar mesh on this object.</summary>
    [RequireComponent(typeof(MeshFilter))]
    public sealed class TransferCollider : MonoBehaviour
    {
        private const float PLANAR_TOLERANCE = 1e-4f;
        private const float DEGENERATE_TRIANGLE_SQUARED = 1e-12f;

        [SerializeField] private List<string> _instanceSceneAddresses = new();

        private MeshFilter _meshFilter;
        private Mesh _cachedMesh;
        private Vector3[] _vertices;
        private int[] _triangles;
        private Vector3 _planePoint;
        private Vector3 _planeNormal;
        private bool _geometryCached;

        /// <summary>Gets the Addressable scenes to load for a private transfer instance.</summary>
        public IReadOnlyList<string> InstanceSceneAddresses => _instanceSceneAddresses;

        /// <summary>Identifies the direction in which an accepted movement segment crossed this plane.</summary>
        public enum CrossingDirection
        {
            IntoInstance,
            IntoPublic
        }

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            CacheGeometry();
        }

        /// <summary>Validates the authored surface and its instance scene address list.</summary>
        public void ValidateConfiguration()
        {
            CacheGeometry();

            if (_instanceSceneAddresses == null || _instanceSceneAddresses.Count == 0)
                throw new InvalidOperationException("A TransferCollider requires at least one instance scene address.");

            var uniqueAddresses = new HashSet<string>(StringComparer.Ordinal);
            foreach (string address in _instanceSceneAddresses)
            {
                if (string.IsNullOrWhiteSpace(address))
                    throw new InvalidOperationException("A TransferCollider contains an empty instance scene address.");
                if (!uniqueAddresses.Add(address))
                    throw new InvalidOperationException(
                        $"A TransferCollider contains duplicate instance scene address '{address}'.");
            }
        }

        /// <summary>Determines whether a physical segment crosses the mesh plane inside its triangles.</summary>
        public bool TryGetCrossing(
            Vector3 previousPhysicalPosition,
            Vector3 currentPhysicalPosition,
            out CrossingDirection direction,
            out float crossingFraction)
        {
            direction = default;
            crossingFraction = 0f;
            CacheGeometry();

            Vector3 previous = transform.InverseTransformPoint(previousPhysicalPosition);
            Vector3 current = transform.InverseTransformPoint(currentPhysicalPosition);
            float previousDistance = Vector3.Dot(previous - _planePoint, _planeNormal);
            float currentDistance = Vector3.Dot(current - _planePoint, _planeNormal);

            if (previousDistance <= 0f && currentDistance > 0f)
                direction = CrossingDirection.IntoInstance;
            else if (previousDistance >= 0f && currentDistance < 0f)
                direction = CrossingDirection.IntoPublic;
            else
                return false;

            crossingFraction = previousDistance / (previousDistance - currentDistance);
            if (crossingFraction < 0f || crossingFraction > 1f)
                return false;

            Vector3 intersection = Vector3.Lerp(previous, current, crossingFraction);
            for (int index = 0; index < _triangles.Length; index += 3)
            {
                Vector3 a = _vertices[_triangles[index]];
                Vector3 b = _vertices[_triangles[index + 1]];
                Vector3 c = _vertices[_triangles[index + 2]];
                if (IsPointInTriangle(intersection, a, b, c))
                    return true;
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            try
            {
                CacheGeometry();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireMesh(_cachedMesh);
            Vector3 center = _cachedMesh.bounds.center;
            Gizmos.DrawLine(center, center + _planeNormal * 0.5f);
            Gizmos.matrix = previousMatrix;
        }

        private void CacheGeometry()
        {
            if (_geometryCached && _meshFilter != null && _meshFilter.sharedMesh == _cachedMesh)
                return;

            if (_meshFilter == null && !TryGetComponent(out _meshFilter))
                throw new InvalidOperationException("A TransferCollider requires a MeshFilter.");

            _cachedMesh = _meshFilter.sharedMesh;
            if (_cachedMesh == null)
                throw new InvalidOperationException("A TransferCollider requires a MeshFilter with a shared mesh.");

            var vertices = new List<Vector3>();
            _cachedMesh.GetVertices(vertices);
            _vertices = vertices.ToArray();
            _triangles = _cachedMesh.triangles;
            if (_triangles.Length < 3)
                throw new InvalidOperationException("A TransferCollider mesh must contain at least one triangle.");

            bool foundPlane = false;
            for (int index = 0; index < _triangles.Length; index += 3)
            {
                Vector3 a = _vertices[_triangles[index]];
                Vector3 b = _vertices[_triangles[index + 1]];
                Vector3 c = _vertices[_triangles[index + 2]];
                Vector3 cross = Vector3.Cross(b - a, c - a);
                if (cross.sqrMagnitude <= DEGENERATE_TRIANGLE_SQUARED)
                    continue;

                _planePoint = a;
                _planeNormal = cross.normalized;
                foundPlane = true;
                break;
            }

            if (!foundPlane)
                throw new InvalidOperationException("A TransferCollider mesh must contain a non-degenerate triangle.");

            for (int index = 0; index < _vertices.Length; index++)
            {
                float distance = Mathf.Abs(Vector3.Dot(_vertices[index] - _planePoint, _planeNormal));
                if (distance > PLANAR_TOLERANCE)
                    throw new InvalidOperationException(
                        $"A TransferCollider mesh must be planar within {PLANAR_TOLERANCE} local units.");
            }

            _geometryCached = true;
        }

        private static bool IsPointInTriangle(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 edge0 = b - a;
            Vector3 edge1 = c - a;
            Vector3 offset = point - a;
            float d00 = Vector3.Dot(edge0, edge0);
            float d01 = Vector3.Dot(edge0, edge1);
            float d11 = Vector3.Dot(edge1, edge1);
            float d20 = Vector3.Dot(offset, edge0);
            float d21 = Vector3.Dot(offset, edge1);
            float denominator = d00 * d11 - d01 * d01;
            if (Mathf.Abs(denominator) <= DEGENERATE_TRIANGLE_SQUARED)
                return false;

            float v = (d11 * d20 - d01 * d21) / denominator;
            float w = (d00 * d21 - d01 * d20) / denominator;
            float u = 1f - v - w;
            const float EDGE_TOLERANCE = 1e-6f;
            return u >= -EDGE_TOLERANCE
                && v >= -EDGE_TOLERANCE
                && w >= -EDGE_TOLERANCE;
        }
    }
}
