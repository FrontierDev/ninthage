using UnityEngine;

namespace Game.Shared.Runtime.World
{
    /// <summary>Defines a finite transfer plane using the local XY bounds of its object's mesh.</summary>
    [RequireComponent(typeof(MeshFilter))]
    public sealed class TransferCollider : MonoBehaviour
    {
        /// <summary>Identifies the server-lifetime private instance associated with this entrance.</summary>
        [SerializeField] private string _instanceKey;

        private MeshFilter _meshFilter;

        /// <summary>Gets the stable authored instance key.</summary>
        public string InstanceKey => _instanceKey;
        /// <summary>Gets the mesh's finite local XY dimensions.</summary>
        public Vector2 Size
        {
            get
            {
                Bounds bounds = GetMeshBounds();
                return new Vector2(bounds.size.x, bounds.size.y);
            }
        }

        /// <summary>Identifies the direction in which an accepted movement segment crossed this plane.</summary>
        public enum CrossingDirection
        {
            IntoInstance,
            IntoPublic
        }

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
        }

        /// <summary>Determines whether a physical segment crosses this plane within its rectangular extent.</summary>
        public bool TryGetCrossing(
            Vector3 previousPhysicalPosition,
            Vector3 currentPhysicalPosition,
            out CrossingDirection direction,
            out float crossingFraction)
        {
            direction = default;
            crossingFraction = 0f;

            Vector3 previous = transform.InverseTransformPoint(previousPhysicalPosition);
            Vector3 current = transform.InverseTransformPoint(currentPhysicalPosition);
            Bounds bounds = GetMeshBounds();
            previous.z -= bounds.center.z;
            current.z -= bounds.center.z;

            if (previous.z <= 0f && current.z > 0f)
            {
                direction = CrossingDirection.IntoInstance;
            }
            else if (previous.z >= 0f && current.z < 0f)
            {
                direction = CrossingDirection.IntoPublic;
            }
            else
            {
                return false;
            }

            crossingFraction = previous.z / (previous.z - current.z);
            if (crossingFraction < 0f || crossingFraction > 1f)
                return false;

            Vector3 intersection = Vector3.Lerp(previous, current, crossingFraction);
            return intersection.x >= bounds.min.x
                && intersection.x <= bounds.max.x
                && intersection.y >= bounds.min.y
                && intersection.y <= bounds.max.y;
        }

        private void OnDrawGizmosSelected()
        {
            Bounds bounds = GetMeshBounds();
            Vector3 bottomLeft = transform.TransformPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.center.z));
            Vector3 bottomRight = transform.TransformPoint(new Vector3(bounds.max.x, bounds.min.y, bounds.center.z));
            Vector3 topRight = transform.TransformPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.center.z));
            Vector3 topLeft = transform.TransformPoint(new Vector3(bounds.min.x, bounds.max.y, bounds.center.z));
            Vector3 center = transform.TransformPoint(bounds.center);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
            Gizmos.DrawLine(center, center + transform.TransformVector(Vector3.forward * 0.5f));
        }

        private Bounds GetMeshBounds()
        {
            if (_meshFilter == null && !TryGetComponent(out _meshFilter))
                return default;

            return _meshFilter.sharedMesh == null ? default : _meshFilter.sharedMesh.bounds;
        }
    }
}
