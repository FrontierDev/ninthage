using UnityEngine;

namespace Game.Shared.Runtime.World
{
    /// <summary>Defines a finite authored plane used to transfer players between public and private contexts.</summary>
    public sealed class TransferCollider : MonoBehaviour
    {
        /// <summary>Identifies the server-lifetime private instance associated with this entrance.</summary>
        [SerializeField] private string _instanceKey;
        /// <summary>Defines the plane's local XY dimensions.</summary>
        [SerializeField] private Vector2 _size = new(4f, 4f);

        /// <summary>Gets the stable authored instance key.</summary>
        public string InstanceKey => _instanceKey;
        /// <summary>Gets the finite local XY dimensions.</summary>
        public Vector2 Size => _size;

        /// <summary>Identifies the direction in which an accepted movement segment crossed this plane.</summary>
        public enum CrossingDirection
        {
            IntoInstance,
            IntoPublic
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
            return Mathf.Abs(intersection.x) <= _size.x * 0.5f
                && Mathf.Abs(intersection.y) <= _size.y * 0.5f;
        }

        private void OnDrawGizmosSelected()
        {
            float halfWidth = _size.x * 0.5f;
            float halfHeight = _size.y * 0.5f;
            Vector3 bottomLeft = transform.TransformPoint(new Vector3(-halfWidth, -halfHeight, 0f));
            Vector3 bottomRight = transform.TransformPoint(new Vector3(halfWidth, -halfHeight, 0f));
            Vector3 topRight = transform.TransformPoint(new Vector3(halfWidth, halfHeight, 0f));
            Vector3 topLeft = transform.TransformPoint(new Vector3(-halfWidth, halfHeight, 0f));

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
            Gizmos.DrawLine(transform.position, transform.TransformPoint(Vector3.forward * 0.5f));
        }
    }
}
