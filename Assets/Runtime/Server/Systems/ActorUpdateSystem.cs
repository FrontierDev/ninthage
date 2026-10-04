using Unity.Entities;
using UnityEngine;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Server.ECS
{
    [DisableAutoCreation]
    public partial struct ActorUpdateSystem : ISystem
    {
        private EntityQuery motionQuery;

        public void OnCreate(ref SystemState state)
        {
            Debug.Log(" - Created ActorUpdateSystem.");

            // Create query for all entities with ActorComponent and MotionStateComponent
            motionQuery = state.GetEntityQuery(
                new EntityQueryDesc
                {
                    All = new[] {
                        ComponentType.ReadWrite<ActorComponent>(),
                        ComponentType.ReadWrite<MotionStateComponent>()
                    }
                });
        }

        public void OnUpdate(ref SystemState state)
        {
            var gravity = Game.Shared.GameConfigurationManager.Config.Gravity;
            var maxFallSpeed = Game.Shared.GameConfigurationManager.Config.MaxFallSpeed;
            var groundDetectionRadius = Game.Shared.GameConfigurationManager.Config.GroundDetectionRadius;
            var groundLayerName = Game.Shared.GameConfigurationManager.Config.GroundDetectionMaskName;
            var groundLayerMask = LayerMask.GetMask(groundLayerName);
            var deltaTime = UnityEngine.Time.deltaTime;

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

        public void OnDestroy(ref SystemState state)
        {
            Debug.Log(" - Destroyed ActorUpdateSystem.");
        }
    }
}