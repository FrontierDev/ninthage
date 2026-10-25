using PurrNet;
using PurrNet.Packing;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;

namespace Game.Server.Systems
{
    /// <summary>Stores authoritative actor state in the server ECS world.</summary>
    public struct ActorComponent : IComponentData
    {
        /// <summary>The actor's unique identifier.</summary>
        public Guid ActorID;
        /// <summary>The actor's display name.</summary>
        public FixedString128Bytes ActorName;
        /// <summary>The owning player, when the actor is player-controlled.</summary>
        public PlayerID Owner;
        /// <summary>The scene currently containing the actor.</summary>
        public SceneID CurrentScene;
        /// <summary>The authoritative actor position using double precision.</summary>
        public double3 WorldPositionD; // Double-precision authoritative position
    }

    /// <summary>Marks an actor whose canonical accepted position requires post-ECS publication.</summary>
    public struct AcceptedPositionDirtyComponent : IComponentData, IEnableableComponent
    {
    }

    /// <summary>Stores a resource regeneration entry for an actor.</summary>
    public struct ResourceRegenElement : IBufferElementData
    {
        /// <summary>The resource identifier.</summary>
        public FixedString64Bytes ResourceID;
        /// <summary>The amount regenerated per update.</summary>
        public float RegenAmount;
    }

    /// <summary>Stores movement and gravity state for an actor.</summary>
    public struct MotionStateComponent : IComponentData
    {
        /// <summary>The actor's current vertical velocity.</summary>
        public float VerticalVelocity;
        /// <summary>Whether the actor is currently grounded.</summary>
        public bool IsGrounded;
        /// <summary>The time since the actor was last grounded.</summary>
        public float TimeSinceLastGround;
        /// <summary>Whether a NavMeshAgent owns actor movement.</summary>
        public bool UsesNavMeshAgent;
    }

    /// <summary>Stores the server's ground-detection settings and last result.</summary>
    public struct GroundDetectionComponent : IComponentData
    {
        /// <summary>The radius used for ground detection.</summary>
        public float groundDetectionRadius;
        /// <summary>The last ground-check direction or offset.</summary>
        public float3 lastGroundCheck;
    }

    /// <summary>Stores the active spell-cast state for an actor.</summary>
    public struct SpellCastComponent : IComponentData, IEnableableComponent
    {
        /// <summary>The spell identifier being cast.</summary>
        public FixedString64Bytes SpellID;
        /// <summary>The unique cast generation associated with this active cast.</summary>
        public long CastID;
        /// <summary>The authoritative tick on which this cast completes.</summary>
        public long CompletionTickID;
        /// <summary>The remaining cast time.</summary>
        public float RemainingCastTime;
        /// <summary>The total cast duration.</summary>
        public float TotalCastTime;
        /// <summary>The number of channel ticks.</summary>
        public float TotalTicks;
        /// <summary>The time since the last channel tick.</summary>
        public float TimeSinceLastTick;
    }

    /// <summary>Stores a cooldown entry for an actor.</summary>
    public struct CooldownElement : IBufferElementData
    {
        /// <summary>The spell or cooldown identifier.</summary>
        public FixedString64Bytes SpellID;
        /// <summary>The remaining cooldown duration.</summary>
        public float RemainingCooldown;
    }

    /// <summary>Stores an active aura entry for an actor.</summary>
    public struct AuraElement : IBufferElementData, IEnableableComponent
    {
        /// <summary>The aura instance identifier.</summary>
        public int InstanceID;
        /// <summary>The aura definition identifier.</summary>
        public FixedString64Bytes AuraDefinitionID;
        /// <summary>The authoritative tick on which the aura expires.</summary>
        public long ExpiryTickID;
        /// <summary>The next authoritative tick on which the aura ticks.</summary>
        public long NextTickID;
        /// <summary>The fixed-tick interval between aura ticks.</summary>
        public long TickIntervalTicks;
    }

    /// <summary>Stores a generic time-based callback entry for an actor.</summary>
    public struct TickerElement : IBufferElementData, IEnableableComponent
    {
        /// <summary>The ticker identifier.</summary>
        public int TickerID;
        /// <summary>The authoritative tick on which the ticker expires, or zero for indefinite tickers.</summary>
        public long ExpiryTickID;
        /// <summary>The next authoritative tick on which the ticker ticks, or zero when disabled.</summary>
        public long NextTickID;
        /// <summary>The fixed-tick interval between ticker callbacks.</summary>
        public long TickIntervalTicks;
    }
}
