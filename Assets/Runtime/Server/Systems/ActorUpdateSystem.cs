using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Core;
using Game.Shared.Performance;
using Game.Shared.Utility;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Server.Systems
{
    /// <summary>Updates authoritative server actor movement and gravity.</summary>
    [DisableAutoCreation]
    [UpdateAfter(typeof(WorldSpawnSystem))]
    public partial struct ActorUpdateSystem : ISystem
    {
        private static readonly PerformanceMetricDefinition ACTOR_UPDATE_METRIC = new(
            PerformanceMetricIds.SERVER_ECS_ACTOR_UPDATE_DURATION,
            PerformanceMetricUnit.Milliseconds,
            PerformanceMetricSide.Server,
            "ecs.actor_update",
            "server_update");
        private static readonly PerformanceMarker ACTOR_UPDATE_MARKER = new(ACTOR_UPDATE_METRIC, "Server.ECS.ActorUpdate");

        internal static void RegisterPerformanceMetrics()
        {
            PerformanceTelemetry.RegisterMetric(ACTOR_UPDATE_METRIC, PerformanceTelemetry.RECOMMENDED_SAMPLES_PER_METRIC);
        }

        private float _gravity;
        private int _groundLayerMask;
        private float _maxFallSpeed;

        /// <summary>Caches immutable movement settings used by the actor movement system.</summary>
        public void OnCreate(ref SystemState state)
        {
            Debug.Log(" - Created ActorUpdateSystem.");

            var configuration = Game.Shared.GameConfigurationManager.Config;
            _gravity = configuration.Gravity;
            _maxFallSpeed = configuration.MaxFallSpeed;
            _groundLayerMask = LayerMask.GetMask(configuration.GroundDetectionMaskName);
        }

        /// <summary>Applies server-side movement and gravity to eligible actors.</summary>
        public void OnUpdate(ref SystemState state)
        {
            using var markerScope = ACTOR_UPDATE_MARKER.Begin();
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (actor, motion, groundCheck, entity) in
                SystemAPI.Query<RefRW<ActorComponent>, RefRW<MotionStateComponent>, RefRO<GroundDetectionComponent>>()
                         .WithEntityAccess())
            {
                double3 previousPosition = actor.ValueRO.WorldPositionD;

                float groundDetectionRadius = groundCheck.ValueRO.groundDetectionRadius;

                var worldPosition = new WorldPosition(
                    actor.ValueRO.WorldPositionD.x,
                    actor.ValueRO.WorldPositionD.y,
                    actor.ValueRO.WorldPositionD.z);

                // Raycast downward from the actor's position to detect ground.
                // Origin is offset up slightly so actors standing on the surface still detect it.
                var rayOrigin = new WorldPosition(
                    worldPosition.x,
                    worldPosition.y + groundDetectionRadius,
                    worldPosition.z);
                var maxRayDistance = groundDetectionRadius * 2f + 10f;
                var hitGround = ServerPhysicsManager.Instance.RaycastGround(
                    rayOrigin,
                    maxRayDistance,
                    _groundLayerMask,
                    out var hitWorldPosition,
                    out var hitDistance);

                var distanceToGround = hitGround
                    ? hitDistance - groundDetectionRadius
                    : float.MaxValue;
                var isGrounded = hitGround && distanceToGround <= groundDetectionRadius;

                motion.ValueRW.IsGrounded = isGrounded;

                if (isGrounded)
                {
                    motion.ValueRW.TimeSinceLastGround = 0f;

                    if (motion.ValueRW.VerticalVelocity < 0)
                        motion.ValueRW.VerticalVelocity = 0;

                    // Snap to ground surface so NPCs don't float above it.
                    actor.ValueRW.WorldPositionD.y = hitWorldPosition.y;
                }
                else
                {
                    motion.ValueRW.TimeSinceLastGround += deltaTime;

                    motion.ValueRW.VerticalVelocity += _gravity * deltaTime;

                    if (motion.ValueRW.VerticalVelocity < -_maxFallSpeed)
                        motion.ValueRW.VerticalVelocity = -_maxFallSpeed;

                    actor.ValueRW.WorldPositionD.y += motion.ValueRW.VerticalVelocity * deltaTime;
                }

                double3 currentPosition = actor.ValueRO.WorldPositionD;
                if (previousPosition.x != currentPosition.x
                    || previousPosition.y != currentPosition.y
                    || previousPosition.z != currentPosition.z)
                {
                    state.EntityManager.SetComponentEnabled<AcceptedPositionDirtyComponent>(entity, true);
                }
            }
        }

        /// <summary>Releases system state when the actor movement system is destroyed.</summary>
        public void OnDestroy(ref SystemState state)
        {
            Debug.Log(" - Destroyed ActorUpdateSystem.");
        }
    }
}
