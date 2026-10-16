using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Performance;
using Unity.Entities;
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

        private EntityQuery _motionQuery;

        /// <summary>Creates the query used by the actor movement system.</summary>
        public void OnCreate(ref SystemState state)
        {
            Debug.Log(" - Created ActorUpdateSystem.");

            // Create query for all entities with ActorComponent and MotionStateComponent
            _motionQuery = state.GetEntityQuery(
                new EntityQueryDesc
                {
                    All = new[] {
                        ComponentType.ReadWrite<ActorComponent>(),
                        ComponentType.ReadWrite<MotionStateComponent>()
                    }
                });
        }

        /// <summary>Applies server-side movement and gravity to eligible actors.</summary>
        public void OnUpdate(ref SystemState state)
        {
            using var markerScope = ACTOR_UPDATE_MARKER.Begin();
            var gravity = Game.Shared.GameConfigurationManager.Config.Gravity;
            var maxFallSpeed = Game.Shared.GameConfigurationManager.Config.MaxFallSpeed;
            var groundDetectionRadius = Game.Shared.GameConfigurationManager.Config.GroundDetectionRadius;
            var groundLayerName = Game.Shared.GameConfigurationManager.Config.GroundDetectionMaskName;
            var groundLayerMask = LayerMask.GetMask(groundLayerName);
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (actor, motion, groundCheck) in
                SystemAPI.Query<RefRW<ActorComponent>, RefRW<MotionStateComponent>, RefRW<GroundDetectionComponent>>())
            {
                // Players own their vertical movement — only simulate gravity for NPC entities.
                // NPCs with NavMeshAgent handle their own gravity and movement.
                if (actor.ValueRO.Owner != default)
                    continue;
                if (motion.ValueRO.UsesNavMeshAgent)
                    continue;

                var worldPos = new UnityEngine.Vector3((float)actor.ValueRO.WorldPositionD.x, (float)actor.ValueRO.WorldPositionD.y, (float)actor.ValueRO.WorldPositionD.z);

                // Raycast downward from the actor's position to detect ground.
                // Origin is offset up slightly so actors standing on the surface still detect it.
                var rayOrigin = worldPos + new UnityEngine.Vector3(0, groundDetectionRadius, 0);
                float maxRayDist = groundDetectionRadius * 2f + 10f;
                bool hitGround = UnityEngine.Physics.Raycast(
                    rayOrigin, UnityEngine.Vector3.down, out var hit,
                    maxRayDist, groundLayerMask);

                float distToGround = hitGround ? hit.distance - groundDetectionRadius : float.MaxValue;
                bool isGrounded = hitGround && distToGround <= groundDetectionRadius;

                motion.ValueRW.IsGrounded = isGrounded;

                if (isGrounded)
                {
                    motion.ValueRW.TimeSinceLastGround = 0f;

                    if (motion.ValueRW.VerticalVelocity < 0)
                        motion.ValueRW.VerticalVelocity = 0;

                    // Snap to ground surface so NPCs don't float above it.
                    actor.ValueRW.WorldPositionD.y = hit.point.y;
                }
                else
                {
                    motion.ValueRW.TimeSinceLastGround += deltaTime;

                    motion.ValueRW.VerticalVelocity += gravity * deltaTime;

                    if (motion.ValueRW.VerticalVelocity < -maxFallSpeed)
                        motion.ValueRW.VerticalVelocity = -maxFallSpeed;

                    actor.ValueRW.WorldPositionD.y += motion.ValueRW.VerticalVelocity * deltaTime;
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
