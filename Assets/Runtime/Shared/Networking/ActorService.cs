using System;
using Game.Shared.Data;
using PurrNet;
using UnityEngine;

namespace Game.Shared.Networking
{
    /// <summary>
    /// This manages all actor-related events and RPCs, such as spawning/despawning actors, 
    /// assigning ownership, and notifying clients about changes to their player actor. It serves as a central hub for actor lifecycle events and ownership management in the networking layer.
    /// </summary>
    public static class ActorService
    {
        public static Action<Actor> onActorDeath;

        // Client callbacks.
        public static Action<Actor> onActorSpawned;
        public static Action<Actor> onActorOwnerChanged;
        public static Action<Actor> onActorOwnershipTaken;
        public static Action<Actor> onPlayerActorAssigned;

        // .. targeting and interaction callbacks
        public static Action<Actor> onClientHoveredActorChanged;
        public static Action<Actor> onClientTargetedActorChanged;
        public static Action<Actor, Actor> onClientLateTargetedActorChanged;
        public static Action onClientEnteredCombat;
        public static Action onClientExitedCombat;

        // ... Spell casting callbacks
        public static Action<Actor, string, float> onSpellCastStarted;
        public static Action<Actor> onSpellCastInterrupted;
        public static Action<Actor> onSpellCastCompleted;
        public static Action<CombatLogEntry> onCombatLogEntryReceived;
        public static Action onStartGlobalCooldown;
        public static Action<string, float> onStartCooldown;

        public static Action<AuraInstance, Actor, int> onClientApplyAura;
        public static Action<AuraInstance, Actor, int> onClientTickAura;
        public static Action<AuraInstance, Actor, int> onClientUpdateAura;
        public static Action<AuraInstance, Actor, int> onClientExpireAura;
        public static Action<AuraInstance, Actor, int> onClientDispelAura;

        public static Action<ActorVFXController, int, string, Vector3, float> onClientSpawnVFXAtPosition;
        public static Action<ActorVFXController, Actor, string, float> onClientSpawnProjectile;
        public static Action<ActorVFXController, int> onClientTriggerEffect;

        public static Action<string> onClientPlaySFX;
        public static Action<AudioSource, string> onClientPlaySFXOnAudioSource;

        // Server callbacks
        public static Action<Actor> onServerActorSpawned;
        public static Action<Actor> onServerActorDespawned;
        public static Action<Actor> onServerActorOwnerChanged;
        public static Action<Actor, SceneID> onServerActorInterestChanged;

        // ... Spell casting callbacks
        public static Action<Actor, SpellOverride> onServerSpellCastStarted;
        public static Action<System.Guid, string> onServerSpellCastTick;
        public static Action<System.Guid, string> onServerSpellCastInterrupted;
        public static Action<System.Guid, string> onServerSpellCastCompleted;
        public static Action<float, Action> onServerSpellCastDelayedAction;
        public static Action<Actor> onServerRequestSpellCastCancel;

        // ... Cooldown callbacks
        public static Action<System.Guid, string, float> onServerStartCooldown;

        // ... Aura callbacks
        public static Action<AuraDefinition, Actor, Actor, int> onServerApplyAura;
        public static Action<System.Guid, string, int> onServerAuraApplied;
        public static Action<System.Guid, string, int> onServerAuraTick;
        public static Action<System.Guid, string, int> onServerAuraExpired;
        public static Action<System.Guid, string, int> onServerAuraDispelled;

        // ... Ticker callbacks (ECS → manager)
        public static Action<System.Guid, int> onServerActorTick;
        public static Action<System.Guid, int> onServerActorTickerExpired;
        // Cross-assembly bridge: Game.Shared callers fire these to register/remove tickers.
        // Signature: (actor, tickInterval, duration, tag, onTick, onExpire)
        public static Action<Actor, float, float, string, Action<Actor>, Action<Actor>> onServerAddTicker;
        public static Action<Actor, string> onServerRemoveTickerByTag;

        // Auto-attack callbacks
        public static Action<Actor> onServerStartAutoAttack;
        public static Action<Actor> onServerStopAutoAttack;
    }
}