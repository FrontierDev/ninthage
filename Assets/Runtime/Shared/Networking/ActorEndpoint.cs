using Game.Shared.Data;
using Game.Shared.Runtime.Actors;
using Game.Shared.Runtime.Spellcasting;
using PurrNet;
using Game.Shared.Performance;
using Game.Shared.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Networking
{
    /// <summary>
    /// This manages all actor-related events and RPCs, such as spawning/despawning actors, 
    /// assigning ownership, and notifying clients about changes to their player actor. It serves as a central hub for actor lifecycle events and ownership management in the networking layer.
    /// </summary>
    public static class ActorEndpoint
    {
        internal static void RecordRpcHandlerInvocation(PerformanceMetricDefinition category)
        {
            NetworkingPerformanceMetrics.RecordRpcHandlerInvocation(category);
        }

        /// <summary>Raised when an actor dies.</summary>
        public static Action<Actor> onActorDeath;

        // Client callbacks.
        /// <summary>Raised when an actor is spawned on the client.</summary>
        public static Action<Actor> onActorSpawned;
        /// <summary>Raised when an actor is despawned on the client.</summary>
        public static Action<Actor> onActorDespawned;
        /// <summary>Raised when a client receives a newer server-accepted actor position snapshot.</summary>
        public static Action<Actor, ActorPositionSnapshot> onClientAcceptedPositionChanged;
        /// <summary>Raised when the client receives an instance validation toggle result.</summary>
        public static Action<bool, string, Guid> onClientInstanceValidationToggleResult;
        /// <summary>Raised when an actor owner changes.</summary>
        public static Action<Actor> onActorOwnerChanged;
        /// <summary>Raised when the local client takes actor ownership.</summary>
        public static Action<Actor> onActorOwnershipTaken;
        /// <summary>Raised when the local player actor is assigned.</summary>
        public static Action<Actor> onPlayerActorAssigned;

        // .. targeting and interaction callbacks
        /// <summary>Raised when the hovered actor changes.</summary>
        public static Action<Actor> onClientHoveredActorChanged;
        /// <summary>Raised when the targeted actor changes.</summary>
        public static Action<Actor> onClientTargetedActorChanged;
        /// <summary>Raised when a late target change is received.</summary>
        public static Action<Actor, Actor> onClientLateTargetedActorChanged;
        /// <summary>Raised when the client enters combat.</summary>
        public static Action onClientEnteredCombat;
        /// <summary>Raised when the client exits combat.</summary>
        public static Action onClientExitedCombat;

        // ... Spell casting callbacks
        /// <summary>Raised when a spell cast starts.</summary>
        public static Action<Actor, string, float> onSpellCastStarted;
        /// <summary>Raised when a spell cast is interrupted.</summary>
        public static Action<Actor> onSpellCastInterrupted;
        /// <summary>Raised when a spell cast completes.</summary>
        public static Action<Actor> onSpellCastCompleted;
        /// <summary>Raised when a combat log entry is received.</summary>
        public static Action<CombatLogEntry> onCombatLogEntryReceived;
        /// <summary>Raised when the global cooldown starts.</summary>
        public static Action onStartGlobalCooldown;
        /// <summary>Raised when a named cooldown starts.</summary>
        public static Action<string, float> onStartCooldown;

        /// <summary>Raised when an aura is applied on the client.</summary>
        public static Action<AuraInstance, Actor, int> onClientApplyAura;
        /// <summary>Raised when an aura ticks on the client.</summary>
        public static Action<AuraInstance, Actor, int> onClientTickAura;
        /// <summary>Raised when an aura is updated on the client.</summary>
        public static Action<AuraInstance, Actor, int> onClientUpdateAura;
        /// <summary>Raised when an aura expires on the client.</summary>
        public static Action<AuraInstance, Actor, int> onClientExpireAura;
        /// <summary>Raised when an aura is dispelled on the client.</summary>
        public static Action<AuraInstance, Actor, int> onClientDispelAura;

        /// <summary>Raised to spawn a client visual effect at a position.</summary>
        public static Action<ActorVFXController, int, string, WorldPosition, float> onClientSpawnVFXAtPosition;
        /// <summary>Raised to spawn a client projectile.</summary>
        public static Action<ActorVFXController, Actor, string, float> onClientSpawnProjectile;
        /// <summary>Raised to trigger a client visual effect.</summary>
        public static Action<ActorVFXController, int> onClientTriggerEffect;

        /// <summary>Raised to play a client sound effect.</summary>
        public static Action<string> onClientPlaySFX;
        /// <summary>Raised to play a sound effect on a client audio source.</summary>
        public static Action<AudioSource, string> onClientPlaySFXOnAudioSource;

        // Server callbacks
        /// <summary>Raised when an actor is spawned on the server.</summary>
        public static Action<Actor> onServerActorSpawned;
        /// <summary>Raised when an actor is despawned on the server.</summary>
        public static Action<Actor> onServerActorDespawned;
        /// <summary>Raised when a player becomes an observer of a server actor.</summary>
        public static Action<Actor, PlayerID> onServerActorObserverAdded;
        /// <summary>Raised when a server actor owner changes.</summary>
        public static Action<Actor> onServerActorOwnerChanged;
        /// <summary>Raised when server actor interest changes.</summary>
        public static Action<Actor, SceneID> onServerActorInterestChanged;
        /// <summary>Raised after positive damage has been committed to a surviving server NPC.</summary>
        public static Action<Actor> onServerNPCDamaged;
        /// <summary>Raised when a client requests an instance validation toggle.</summary>
        public static Action<PlayerID> onServerInstanceValidationToggleRequested;
        /// <summary>
        /// Raised after a player's authoritative accepted position
        /// has been committed by the server.
        /// </summary>
        public static Action<Actor, WorldPosition>
            onServerAcceptedPlayerPositionChanged;
        /// <summary>Queries authoritative actors within a context-qualified absolute world-space range.</summary>
        public static Func<ActorSpatialContext, WorldPosition, double, List<Actor>>
            onServerQueryActorsInRange;

        // ... Spell casting callbacks
        /// <summary>Raised when a server spell cast starts.</summary>
        public static Action<Actor, SpellOverride> onServerSpellCastStarted;
        /// <summary>Raised when a server spell cast ticks.</summary>
        public static Action<System.Guid, string> onServerSpellCastTick;
        /// <summary>Raised when a server spell cast is interrupted.</summary>
        public static Action<System.Guid, string> onServerSpellCastInterrupted;
        /// <summary>Raised when a server spell cast completes.</summary>
        public static Action<System.Guid, string> onServerSpellCastCompleted;
        /// <summary>Raised to schedule a delayed server spell action.</summary>
        public static Action<float, Action> onServerSpellCastDelayedAction;
        /// <summary>Raised when a server spell cast cancellation is requested.</summary>
        public static Action<Actor> onServerRequestSpellCastCancel;

        // ... Cooldown callbacks
        /// <summary>Raised when a server cooldown starts.</summary>
        public static Action<System.Guid, string, float> onServerStartCooldown;

        // ... Aura callbacks
        /// <summary>Raised to apply an aura on the server.</summary>
        public static Action<AuraDefinition, Actor, Actor, int> onServerApplyAura;
        /// <summary>Raised when a server aura is applied.</summary>
        public static Action<System.Guid, string, int> onServerAuraApplied;
        /// <summary>Raised when a server aura ticks.</summary>
        public static Action<System.Guid, string, int> onServerAuraTick;
        /// <summary>Raised when a server aura expires.</summary>
        public static Action<System.Guid, string, int> onServerAuraExpired;
        /// <summary>Raised when a server aura is dispelled.</summary>
        public static Action<System.Guid, string, int> onServerAuraDispelled;

        // ... Ticker callbacks (ECS → manager)
        /// <summary>Raised when a server actor ticker ticks.</summary>
        public static Action<System.Guid, int> onServerActorTick;
        /// <summary>Raised when a server actor ticker expires.</summary>
        public static Action<System.Guid, int> onServerActorTickerExpired;
        // Cross-assembly bridge: Game.Shared callers fire these to register/remove tickers.
        // Signature: (actor, tickInterval, duration, tag, onTick, onExpire)
        /// <summary>Raised to register a server actor ticker.</summary>
        public static Action<Actor, float, float, string, Action<Actor>, Action<Actor>> onServerAddTicker;
        /// <summary>Raised to remove a server actor ticker by tag.</summary>
        public static Action<Actor, string> onServerRemoveTickerByTag;

        // Auto-attack callbacks
        /// <summary>Raised when a server auto attack starts.</summary>
        public static Action<Actor> onServerStartAutoAttack;
        /// <summary>Raised when a server auto attack stops.</summary>
        public static Action<Actor> onServerStopAutoAttack;

        /// <summary>Requests a server-authorised validation-instance toggle for the sender's player.</summary>
        [ServerRpc]
        public static void Server_RequestInstanceValidationToggle(RPCInfo rpcInfo = default)
        {
            onServerInstanceValidationToggleRequested?.Invoke(rpcInfo.sender);
        }

        /// <summary>Reports the authoritative result of a validation-instance toggle to one client.</summary>
        [TargetRpc]
        public static void Client_InstanceValidationToggleResult(
            PlayerID target,
            bool succeeded,
            string instanceLabel,
            Guid instanceID,
            RPCInfo rpcInfo = default)
        {
            onClientInstanceValidationToggleResult?.Invoke(
                succeeded,
                instanceLabel,
                instanceID);
        }
    }
}
