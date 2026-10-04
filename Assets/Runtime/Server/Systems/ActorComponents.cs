using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using PurrNet;
using PurrNet.Packing;
using System;

namespace Game.Server.ECS
{
    public struct ActorComponent : IComponentData
    {
        public Guid ActorId;
        public FixedString128Bytes ActorName;
        public PlayerID Owner;
        public SceneID CurrentScene;
        public double3 WorldPositionD; // Double-precision authoritative position
    }

    public struct ResourceRegenElement : IBufferElementData
    {
        public FixedString64Bytes ResourceId;
        public float RegenAmount;
    }

    public struct MotionStateComponent : IComponentData
    {
        public float VerticalVelocity;
        public bool IsGrounded;
        public float TimeSinceLastGround;
        public bool UsesNavMeshAgent;
    }

    public struct GroundDetectionComponent : IComponentData
    {
        public float groundDetectionRadius;
        public float3 lastGroundCheck;
    }

    public struct SpellCastComponent : IComponentData, IEnableableComponent
    {
        public FixedString64Bytes SpellID;
        public float RemainingCastTime;
        public float TotalCastTime;
        public float TotalTicks;
        public float TimeSinceLastTick;
    }

    public struct CooldownElement : IBufferElementData
    {
        public FixedString64Bytes SpellID;
        public float RemainingCooldown;
    }

    public struct AuraElement : IBufferElementData
    {
        public int InstanceId;
        public FixedString64Bytes AuraDefinitionId;
        public float RemainingDuration;
        public float TickInterval;
        public float TimeSinceLastTick;
    }

    public struct TickerElement : IBufferElementData
    {
        public int TickerId;
        public float RemainingDuration; // float.MaxValue = indefinite
        public float TickInterval;
        public float TimeSinceLastTick;
    }
}