using Game.Shared.Networking;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared
{
    public sealed class SpellProjectile : MonoBehaviour
    {
        private Transform _target;
        private float _speed;
        private Vector3 _spawnPosition;
        private Vector3 _initialDirection;
        private float _elapsedTime;
        private float _totalDistance;

        [SerializeField] private GameObject impactEffectPrefab;

        [SerializeField] public string onCreateSFX;
        [SerializeField] public string onImpactSFX;
        [SerializeField] public string onLoopSFX;

        [Header("Trajectory")]
        [SerializeField]
        [Tooltip("Height of parabolic arc trajectory (0 = straight line)")]
        private float arcHeight = 0f;

        [SerializeField]
        [Tooltip("Amount of horizontal deviation perpendicular to travel direction")]
        private float horizontalDeviationAmount = 0f;

        /// <summary>
        /// Initialize the projectile with target transform and speed.
        /// Trajectory properties (arc height, deviation) are configured on the prefab.
        /// </summary>
        public void Initialize(Transform target, float speed)
        {
            _target = target;
            _speed = speed;
            _spawnPosition = transform.position;

            if (_target != null)
            {
                _initialDirection = (_target.position - _spawnPosition).normalized;
                _totalDistance = Vector3.Distance(_spawnPosition, _target.position);
            }

            _elapsedTime = 0f;
        }

        private void Update()
        {
            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            _elapsedTime += Time.deltaTime;

            // Calculate progress along the path (0 to 1)
            float travelDistance = _speed * _elapsedTime;
            float progress = _totalDistance > 0f ? Mathf.Clamp01(travelDistance / _totalDistance) : 0f;

            // Calculate the position on the path at current progress
            Vector3 straightLinePoint = _spawnPosition + _initialDirection * (_totalDistance * progress);

            // Parabolic arc: peaks at 0.5 progress
            float arcOffset = 0f;
            if (arcHeight != 0f)
            {
                arcOffset = arcHeight * 4f * progress * (1f - progress);
            }

            // Calculate horizontal deviation: smooth parabolic curve to the right
            float deviationOffset = 0f;
            if (horizontalDeviationAmount > 0f)
            {
                deviationOffset = horizontalDeviationAmount * 4f * progress * (1f - progress);
            }

            // Build orthonormal basis for arc and deviation directions (fixed to initial direction)
            Vector3 upDirection = Vector3.up;
            Vector3 rightDirection = Vector3.Cross(upDirection, _initialDirection).normalized;

            // Handle case where direction is nearly vertical
            if (rightDirection.sqrMagnitude < 0.01f)
            {
                rightDirection = Vector3.Cross(Vector3.forward, _initialDirection).normalized;
            }

            Vector3 perpendicularUp = Vector3.Cross(rightDirection, _initialDirection).normalized;

            // Calculate the exact position on the curved path
            Vector3 pathPosition = straightLinePoint;
            pathPosition += perpendicularUp * arcOffset;
            pathPosition += rightDirection * deviationOffset;

            // Move towards the adjusted position
            Vector3 direction = (pathPosition - transform.position).normalized;
            transform.position += direction * _speed * Time.deltaTime;

            // Rotate so that the up axis (local Y) points in the direction of travel
            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            }

            // Check for collision with the target
            if (progress >= 1.0f || Vector3.Distance(transform.position, _target.position) < 0.5f)
            {
                if (impactEffectPrefab != null)
                {
                    Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
                }

                // Play impact sound on the target actor
                if (!string.IsNullOrEmpty(onImpactSFX) && _target != null)
                {
                    var sfxController = _target.GetComponentInParent<ActorSFXController>();
                    if (sfxController != null)
                    {
                        sfxController.PlaySFX(onImpactSFX);
                    }
                    else
                    {
                        Debug.Warning($"No ActorSFXController found on {_target.name}");
                    }
                }
                else
                {
                    Debug.Warning($"No Actor component found on target {_target.name}");
                }

                Destroy(gameObject);
            }
        }
    }
}