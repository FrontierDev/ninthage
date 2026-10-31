using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Networking.Master;
using Game.Server.Persistence;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using Game.Shared.Runtime.Spellcasting;
using PurrNet;
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>Maintains server-authoritative actor auras and aura effects.</summary>
    public sealed class ServerAuraManager : MonoBehaviour
    {
        private static ServerAuraManager _instance;
        /// <summary>Gets the active server aura manager.</summary>
        public static ServerAuraManager Instance => _instance;
        /// <summary>Gets whether the aura manager is initialized.</summary>
        public static bool IsInitialized => _instance != null;

        // Instance ID 0 is valid; negative means a deferred apply has no refresh target.
        private const int NO_EXPECTED_AURA_INSTANCE_ID = -1;
        private EntityManager _entityManager;

        private readonly Dictionary<int, AuraContext> _activeAuras = new();
        private readonly ServerSimulationMutationQueue<AuraMutation> _pendingMutations = new();
        private readonly HashSet<int> _pendingRemovalInstanceIDs = new();
        private readonly List<AuraMutation> _commitSnapshot = new();
        private int _nextInstanceID;

        private enum AuraMutationKind : byte
        {
            Apply,
            Dispel,
            RemoveAll,
        }

        private readonly struct AuraMutation
        {
            internal AuraMutation(
                AuraMutationKind kind,
                AuraDefinition definition,
                Actor caster,
                Actor target,
                Guid targetID,
                int instanceID,
                int stacks)
            {
                Kind = kind;
                Definition = definition;
                Caster = caster;
                Target = target;
                TargetID = targetID;
                InstanceID = instanceID;
                Stacks = stacks;
            }

            internal AuraMutationKind Kind { get; }
            internal AuraDefinition Definition { get; }
            internal Actor Caster { get; }
            internal Actor Target { get; }
            internal Guid TargetID { get; }
            internal int InstanceID { get; }
            internal int Stacks { get; }
        }

        private void Awake()
        {
            if (_instance != null)
                throw new InvalidOperationException("ServerAuraManager is already initialized.");

            var world = ServerECSManager.DefaultWorld;
            _entityManager = world.EntityManager;

            ActorEndpoint.onServerApplyAura += OnApplyAuraRequest;
            ActorEndpoint.onServerAuraTick += OnAuraTick;
            ActorEndpoint.onServerAuraStackExpired += OnAuraStackExpired;
            ActorEndpoint.onServerAuraExpired += OnAuraExpired;
            ActorEndpoint.onServerAuraDispelled += OnAuraDispelled;
            ActorEndpoint.onServerActorDespawned += OnActorDespawned;
            ActorEndpoint.onServerActorObserverAdded += OnActorObserverAdded;

            _instance = this;
        }

        private void OnDestroy()
        {
            if (!ReferenceEquals(_instance, this))
                return;

            ActorEndpoint.onServerApplyAura -= OnApplyAuraRequest;
            ActorEndpoint.onServerAuraTick -= OnAuraTick;
            ActorEndpoint.onServerAuraStackExpired -= OnAuraStackExpired;
            ActorEndpoint.onServerAuraExpired -= OnAuraExpired;
            ActorEndpoint.onServerAuraDispelled -= OnAuraDispelled;
            ActorEndpoint.onServerActorDespawned -= OnActorDespawned;
            ActorEndpoint.onServerActorObserverAdded -= OnActorObserverAdded;

            _activeAuras.Clear();
            _pendingMutations.Clear();
            _pendingRemovalInstanceIDs.Clear();
            _commitSnapshot.Clear();
            _instance = null;
        }

        private void OnActorDespawned(Actor actor)
        {
            if (actor == null)
                return;

            var cancelledInstanceIDs = new List<int>();
            foreach (var pair in _activeAuras)
            {
                AuraContext context = pair.Value;
                if (context.TargetID == actor.ID)
                {
                    cancelledInstanceIDs.Add(pair.Key);
                }
            }

            foreach (int instanceID in cancelledInstanceIDs)
            {
                if (!_activeAuras.Remove(instanceID, out var context))
                    continue;

                _pendingRemovalInstanceIDs.Remove(instanceID);
                context.Definition?.AuraBehaviour?.OnRemoved(context.Caster, context.Target ?? actor);
            }

            if (TryFindActorEntity(actor.ID, out Entity targetEntity))
            {
                _entityManager.GetBuffer<Systems.AuraElement>(targetEntity).Clear();
                _entityManager.SetComponentEnabled<Systems.AuraElement>(
                    targetEntity,
                    false);
            }
        }

        private void OnActorObserverAdded(Actor actor, PlayerID playerID)
        {
            PublishAuraBootstrap(actor, playerID);
        }

        /// <summary>Publishes current aura instances to a player's owner connection.</summary>
        internal void PublishAuraBootstrap(Actor actor, PlayerID playerID)
        {
            if (actor == null || !TryFindActorEntity(actor.ID, out Entity entity))
                return;

            if (!_entityManager.IsComponentEnabled<Systems.AuraElement>(entity))
                return;

            DynamicBuffer<Systems.AuraElement> buffer = _entityManager.GetBuffer<Systems.AuraElement>(entity);
            if (buffer.Length == 0)
                return;

            long currentTickID = ServerSimulationClock.CurrentTickID;
            float tickDuration = GameConfigurationManager.Config.ServerSimulationTickDuration;

            for (int i = 0; i < buffer.Length; i++)
            {
                Systems.AuraElement entry = buffer[i];
                if (_pendingRemovalInstanceIDs.Contains(entry.InstanceID))
                    continue;

                if (!_activeAuras.TryGetValue(entry.InstanceID, out AuraContext context)
                    || context == null
                    || context.TargetID != actor.ID
                    || context.Definition == null
                    || context.Definition.DefinitionID != entry.AuraDefinitionID.ToString())
                {
                    throw new InvalidOperationException(
                        $"Aura bootstrap state mismatch for actor {actor.ID}, instance {entry.InstanceID}.");
                }

                long remainingTicks = entry.ExpiryTickID - currentTickID;
                if (remainingTicks <= 0)
                    continue;

                float remainingDuration = remainingTicks * tickDuration;
                actor.PublishAuraBootstrapToObserver(
                    playerID,
                    context.Definition.DefinitionID,
                    entry.InstanceID,
                    remainingDuration,
                    context.Stacks);
            }
        }

        internal void PreparePendingMutations()
        {
            _pendingMutations.TakeSnapshot(_commitSnapshot);
        }

        internal void CommitPendingMutations()
        {
            try
            {
                foreach (AuraMutation mutation in _commitSnapshot)
                {
                    switch (mutation.Kind)
                    {
                        case AuraMutationKind.Apply:
                            if (!TryGetCurrentActor(mutation.TargetID, mutation.Target, out var target)
                                || mutation.Caster == null
                                || !TryGetCurrentActor(mutation.Caster.ID, mutation.Caster, out _))
                            {
                                Debug.Warning($"Discarding aura application for retired target {mutation.TargetID}.");
                                continue;
                            }

                            if (mutation.InstanceID != NO_EXPECTED_AURA_INSTANCE_ID
                                && (!_activeAuras.TryGetValue(mutation.InstanceID, out var activeAura)
                                    || activeAura == null
                                    || activeAura.TargetID != mutation.TargetID
                                    || activeAura.CasterID != mutation.Caster.ID
                                    || activeAura.Definition == null
                                    || activeAura.Definition.DefinitionID != mutation.Definition.DefinitionID))
                            {
                                Debug.Warning($"Discarding deferred aura refresh for retired instance {mutation.InstanceID}.");
                                continue;
                            }

                            if (!ApplyAuraNow(mutation.Definition, mutation.Caster, target, mutation.Stacks, mutation.InstanceID)
                                && mutation.InstanceID != NO_EXPECTED_AURA_INSTANCE_ID)
                            {
                                Debug.Warning($"Discarding deferred aura refresh for instance {mutation.InstanceID} because its ECS entry is no longer active.");
                            }
                            break;
                        case AuraMutationKind.Dispel:
                            _pendingRemovalInstanceIDs.Remove(mutation.InstanceID);
                            if (TryGetCurrentActor(mutation.TargetID, mutation.Target, out target))
                                DispelAuraNow(target, mutation.InstanceID);
                            break;
                        case AuraMutationKind.RemoveAll:
                            RemoveAllAurasNow(mutation.TargetID);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(mutation.Kind), mutation.Kind, "Unknown aura mutation kind.");
                    }
                }
            }
            finally
            {
                _commitSnapshot.Clear();
            }
        }

        /// <summary>Captures the active persistent aura state for one actor.</summary>
        /// <param name="actorID">The actor whose authoritative aura state is captured.</param>
        /// <param name="capturedAtUtc">The single World Server UTC observation for the capture.</param>
        /// <param name="auras">Receives a complete, non-null persistent aura snapshot on success.</param>
        /// <param name="failureReason">Receives the reason the snapshot could not be captured.</param>
        /// <returns><see langword="true"/> when a complete persistent aura snapshot was captured.</returns>
        internal bool TryCapturePersistentAuras(
            Guid actorID,
            DateTimeOffset capturedAtUtc,
            out WorldPersistenceCheckpoint.PersistentAura[] auras,
            out string failureReason)
        {
            auras = null;
            failureReason = null;

            if (!ServerECSManager.IsOnServerMainThread)
                return FailPersistentAuraCapture(
                    "persistent aura capture must run on the Unity server main thread",
                    out failureReason);
            if (ServerECSManager.IsMutationBoundaryActive)
                return FailPersistentAuraCapture(
                    "persistent aura capture cannot run during an active ECS mutation boundary",
                    out failureReason);
            if (actorID == Guid.Empty)
                return FailPersistentAuraCapture(
                    "the persistent aura capture actor ID is empty",
                    out failureReason);
            if (capturedAtUtc.Offset != TimeSpan.Zero)
                return FailPersistentAuraCapture(
                    "the persistent aura capture timestamp is not canonical UTC",
                    out failureReason);

            ServerActorManager actorManager = ServerActorManager.Instance;
            if (actorManager == null)
                return FailPersistentAuraCapture(
                    "the server actor manager is unavailable for persistent aura capture",
                    out failureReason);
            if (!actorManager.TryGetActorEntity(actorID, out Entity actorEntity))
                return FailPersistentAuraCapture(
                    $"the actor {actorID} has no authoritative ECS entity for persistent aura capture",
                    out failureReason);

            if (ServerECSManager.DefaultWorld == null || !ServerECSManager.DefaultWorld.IsCreated)
                return FailPersistentAuraCapture(
                    "the server ECS world is unavailable for persistent aura capture",
                    out failureReason);

            EntityManager entityManager = ServerECSManager.DefaultWorld.EntityManager;
            if (!entityManager.Exists(actorEntity))
                return FailPersistentAuraCapture(
                    "the actor ECS entity is no longer valid for persistent aura capture",
                    out failureReason);

            DynamicBuffer<Systems.AuraElement> auraBuffer;
            try
            {
                if (!entityManager.IsComponentEnabled<Systems.AuraElement>(actorEntity))
                {
                    auras = Array.Empty<WorldPersistenceCheckpoint.PersistentAura>();
                    return true;
                }

                auraBuffer = entityManager.GetBuffer<Systems.AuraElement>(actorEntity, true);
            }
            catch (InvalidOperationException exception)
            {
                return FailPersistentAuraCapture(
                    $"the actor aura buffer could not be read: {exception.Message}",
                    out failureReason);
            }

            AuraDefinitionLibrary definitionLibrary = AuraDefinitionLibrary.Instance;
            if (definitionLibrary == null)
                return FailPersistentAuraCapture(
                    "the aura definition library is unavailable for persistent aura capture",
                    out failureReason);

            GameConfiguration configuration = GameConfigurationManager.Config;
            if (configuration == null)
                return FailPersistentAuraCapture(
                    "the game configuration is unavailable for persistent aura capture",
                    out failureReason);

            float fixedTickDuration = configuration.ServerSimulationTickDuration;
            if (float.IsNaN(fixedTickDuration)
                || float.IsInfinity(fixedTickDuration)
                || fixedTickDuration <= 0f)
            {
                return FailPersistentAuraCapture(
                    "the server simulation tick duration is invalid for persistent aura capture",
                    out failureReason);
            }

            long currentTickID = ServerSimulationClock.CurrentTickID;
            List<WorldPersistenceCheckpoint.PersistentAura> capturedAuras = new();
            HashSet<Guid> persistentAuraIDs = new();

            for (int index = 0; index < auraBuffer.Length; index++)
            {
                Systems.AuraElement entry = auraBuffer[index];
                if (entry.ExpiryTickID <= currentTickID)
                    continue;

                string definitionID = entry.AuraDefinitionID.ToString();
                if (string.IsNullOrWhiteSpace(definitionID))
                    return FailPersistentAuraCapture(
                        "an active aura has a blank definition ID",
                        out failureReason);

                AuraDefinition definition = definitionLibrary.GetDefinition(definitionID);
                if (definition == null)
                    return FailPersistentAuraCapture(
                        $"active aura '{definitionID}' has no authored definition",
                        out failureReason);
                if (!Enum.IsDefined(typeof(AuraPersistencePolicy), definition.PersistencePolicy))
                    return FailPersistentAuraCapture(
                        $"aura '{definitionID}' has an unrecognized persistence policy",
                        out failureReason);
                if (definition.PersistencePolicy != AuraPersistencePolicy.PersistAcrossActorLifetime)
                    continue;

                if (!_activeAuras.TryGetValue(entry.InstanceID, out AuraContext context)
                    || context == null
                    || context.TargetID != actorID
                    || context.Definition == null
                    || !StringComparer.Ordinal.Equals(context.Definition.DefinitionID, definitionID))
                {
                    return FailPersistentAuraCapture(
                        $"persistent aura '{definitionID}' instance {entry.InstanceID} has no matching active context",
                        out failureReason);
                }
                if (context.PersistentAuraID == Guid.Empty)
                    return FailPersistentAuraCapture(
                        $"persistent aura '{definitionID}' has no stable persistence ID",
                        out failureReason);
                if (!persistentAuraIDs.Add(context.PersistentAuraID))
                    return FailPersistentAuraCapture(
                        $"persistent aura '{definitionID}' has a duplicate persistence ID",
                        out failureReason);
                if (definition.StackBehavior != AuraStackBehavior.RefreshDuration)
                {
                    return FailPersistentAuraCapture(
                        $"persistent aura '{definitionID}' uses a non-persistable stacking form",
                        out failureReason);
                }
                if (definition.MaxStacks <= 0
                    || context.Stacks < 1
                    || context.Stacks > definition.MaxStacks
                    || context.StackExpiryTickIDs != null)
                {
                    return FailPersistentAuraCapture(
                        $"persistent aura '{definitionID}' has an invalid stack state",
                        out failureReason);
                }
                if (context.CasterID == Guid.Empty)
                    return FailPersistentAuraCapture(
                        $"persistent aura '{definitionID}' has no usable source actor identity",
                        out failureReason);
                if (definition.Components == null)
                    return FailPersistentAuraCapture(
                        $"persistent aura '{definitionID}' has no valid component list",
                        out failureReason);

                foreach (AuraComponent component in definition.Components)
                {
                    if (component == null || component.EffectDefinition == null)
                        return FailPersistentAuraCapture(
                            $"persistent aura '{definitionID}' has an invalid component",
                            out failureReason);
                    if (component.CastPhase == AuraPhase.OnTick)
                    {
                        return FailPersistentAuraCapture(
                            $"persistent aura '{definitionID}' has unsupported offline OnTick effects",
                            out failureReason);
                    }
                }

                long remainingTicks;
                try
                {
                    remainingTicks = checked(entry.ExpiryTickID - currentTickID);
                }
                catch (OverflowException exception)
                {
                    return FailPersistentAuraCapture(
                        $"persistent aura '{definitionID}' has an invalid expiry tick range: {exception.Message}",
                        out failureReason);
                }

                double remainingSeconds = remainingTicks * (double)fixedTickDuration;
                if (double.IsNaN(remainingSeconds) || double.IsInfinity(remainingSeconds) || remainingSeconds <= 0d)
                    return FailPersistentAuraCapture(
                        $"persistent aura '{definitionID}' has a non-finite expiry duration",
                        out failureReason);

                DateTimeOffset expiresAtUtc;
                try
                {
                    expiresAtUtc = capturedAtUtc.AddSeconds(remainingSeconds);
                }
                catch (ArgumentOutOfRangeException exception)
                {
                    return FailPersistentAuraCapture(
                        $"persistent aura '{definitionID}' produces an out-of-range expiry: {exception.Message}",
                        out failureReason);
                }

                DateTimeOffset? nextTickAtUtc = null;
                float? tickIntervalSeconds = null;
                if (entry.TickIntervalTicks < 0)
                    return FailPersistentAuraCapture(
                        $"persistent aura '{definitionID}' has a negative tick interval",
                        out failureReason);
                if (entry.TickIntervalTicks > 0)
                {
                    if (entry.NextTickID <= currentTickID)
                    {
                        return FailPersistentAuraCapture(
                            $"persistent aura '{definitionID}' has a next tick that is not in the future",
                            out failureReason);
                    }

                    long nextTickRemainingTicks;
                    try
                    {
                        nextTickRemainingTicks = checked(entry.NextTickID - currentTickID);
                    }
                    catch (OverflowException exception)
                    {
                        return FailPersistentAuraCapture(
                            $"persistent aura '{definitionID}' has an invalid next tick range: {exception.Message}",
                            out failureReason);
                    }

                    double nextTickSeconds = nextTickRemainingTicks * (double)fixedTickDuration;
                    double intervalSeconds = entry.TickIntervalTicks * (double)fixedTickDuration;
                    if (double.IsNaN(nextTickSeconds)
                        || double.IsInfinity(nextTickSeconds)
                        || nextTickSeconds <= 0d
                        || double.IsNaN(intervalSeconds)
                        || double.IsInfinity(intervalSeconds)
                        || intervalSeconds <= 0d
                        || intervalSeconds > float.MaxValue)
                    {
                        return FailPersistentAuraCapture(
                            $"persistent aura '{definitionID}' has an invalid tick timing state",
                            out failureReason);
                    }

                    try
                    {
                        nextTickAtUtc = capturedAtUtc.AddSeconds(nextTickSeconds);
                    }
                    catch (ArgumentOutOfRangeException exception)
                    {
                        return FailPersistentAuraCapture(
                            $"persistent aura '{definitionID}' produces an out-of-range next tick: {exception.Message}",
                            out failureReason);
                    }

                    float intervalSecondsFloat = (float)intervalSeconds;
                    if (float.IsNaN(intervalSecondsFloat)
                        || float.IsInfinity(intervalSecondsFloat)
                        || intervalSecondsFloat <= 0f)
                    {
                        return FailPersistentAuraCapture(
                            $"persistent aura '{definitionID}' has an unrepresentable tick interval",
                            out failureReason);
                    }

                    tickIntervalSeconds = intervalSecondsFloat;
                }

                capturedAuras.Add(
                    new WorldPersistenceCheckpoint.PersistentAura(
                        context.PersistentAuraID,
                        definitionID,
                        context.CasterID,
                        context.Stacks,
                        expiresAtUtc,
                        nextTickAtUtc,
                        tickIntervalSeconds));
            }

            capturedAuras.Sort(
                (first, second) => first.PersistentAuraID.CompareTo(second.PersistentAuraID));
            auras = capturedAuras.ToArray();
            return true;
        }

        private static bool FailPersistentAuraCapture(string reason, out string failureReason)
        {
            failureReason = reason;
            return false;
        }

        /// <summary>Restores validated persistent auras into one live actor.</summary>
        /// <param name="target">The registered actor receiving the restored auras.</param>
        /// <param name="savedAuras">The persisted aura rows, or <see langword="null"/> when none exist.</param>
        /// <param name="observedAtUtc">The single World Server UTC observation for restoration.</param>
        /// <param name="failureReason">Receives the reason restoration was rejected.</param>
        /// <returns><see langword="true"/> when restoration succeeds.</returns>
        internal bool TryRestorePersistentAuras(
            Actor target,
            IReadOnlyList<MasterPersistenceProtocol.CharacterPersistentAuraState> savedAuras,
            DateTimeOffset observedAtUtc,
            out string failureReason)
        {
            failureReason = null;

            if (target == null)
                return FailPersistentAuraRestore("the persistent aura restore target is absent", out failureReason);
            if (target.ID == Guid.Empty)
                return FailPersistentAuraRestore("the persistent aura restore target ID is empty", out failureReason);
            if (!ServerECSManager.IsOnServerMainThread)
            {
                return FailPersistentAuraRestore(
                    "persistent aura restore must run on the Unity server main thread",
                    out failureReason);
            }
            if (ServerECSManager.IsMutationBoundaryActive)
            {
                return FailPersistentAuraRestore(
                    "persistent aura restore cannot run during an active ECS mutation boundary",
                    out failureReason);
            }
            if (observedAtUtc.Offset != TimeSpan.Zero)
            {
                return FailPersistentAuraRestore(
                    "persistent aura restore requires a canonical UTC observation timestamp",
                    out failureReason);
            }

            ServerActorManager actorManager = ServerActorManager.Instance;
            if (actorManager == null)
                return FailPersistentAuraRestore("the server actor manager is unavailable for persistent aura restore", out failureReason);
            if (!actorManager.TryGetActorEntity(target.ID, out Entity targetEntity))
            {
                return FailPersistentAuraRestore(
                    $"actor {target.ID} has no authoritative ECS entity for persistent aura restore",
                    out failureReason);
            }
            if (ServerECSManager.DefaultWorld == null || !ServerECSManager.DefaultWorld.IsCreated)
            {
                return FailPersistentAuraRestore(
                    "the server ECS world is unavailable for persistent aura restore",
                    out failureReason);
            }

            EntityManager entityManager = ServerECSManager.DefaultWorld.EntityManager;
            DynamicBuffer<Systems.AuraElement> auraBuffer;
            bool auraBufferWasEnabled;
            try
            {
                if (!entityManager.Exists(targetEntity))
                {
                    return FailPersistentAuraRestore(
                        "the target ECS entity is no longer valid for persistent aura restore",
                        out failureReason);
                }

                auraBuffer = entityManager.GetBuffer<Systems.AuraElement>(targetEntity);
                auraBufferWasEnabled = entityManager.IsComponentEnabled<Systems.AuraElement>(targetEntity);
            }
            catch (InvalidOperationException exception)
            {
                return FailPersistentAuraRestore(
                    $"the target aura buffer could not be read: {exception.Message}",
                    out failureReason);
            }

            if (savedAuras == null)
            {
                return FailPersistentAuraRestore(
                    "accepted character persistence state has no persistent auras collection",
                    out failureReason);
            }
            if (savedAuras.Count == 0)
                return true;

            AuraDefinitionLibrary definitionLibrary = AuraDefinitionLibrary.Instance;
            if (definitionLibrary == null)
            {
                return FailPersistentAuraRestore(
                    "the aura definition library is unavailable for persistent aura restore",
                    out failureReason);
            }

            GameConfiguration configuration = GameConfigurationManager.Config;
            if (configuration == null)
            {
                return FailPersistentAuraRestore(
                    "the game configuration is unavailable for persistent aura restore",
                    out failureReason);
            }

            float fixedTickDuration = configuration.ServerSimulationTickDuration;
            if (float.IsNaN(fixedTickDuration)
                || float.IsInfinity(fixedTickDuration)
                || fixedTickDuration <= 0f)
            {
                return FailPersistentAuraRestore(
                    "the server simulation tick duration is invalid for persistent aura restore",
                    out failureReason);
            }

            long currentTickID = ServerSimulationClock.CurrentTickID;
            long firstProgressTickID;
            try
            {
                firstProgressTickID = checked(currentTickID + 1L);
            }
            catch (OverflowException exception)
            {
                return FailPersistentAuraRestore(
                    $"the server simulation tick cannot advance for persistent aura restore: {exception.Message}",
                    out failureReason);
            }

            List<(
                AuraDefinition Definition,
                Actor Caster,
                Guid PersistentAuraID,
                int Stacks,
                float Duration,
                float TickInterval,
                long ExpiryTickID,
                long NextTickID,
                long TickIntervalTicks)> stagedAuras = new();
            HashSet<Guid> savedPersistentAuraIDs = new();

            for (int index = 0; index < savedAuras.Count; index++)
            {
                MasterPersistenceProtocol.CharacterPersistentAuraState savedAura = savedAuras[index];
                if (savedAura == null)
                {
                    return FailPersistentAuraRestore(
                        "persisted character aura state contains a null row",
                        out failureReason);
                }
                if (savedAura.PersistentAuraID == Guid.Empty
                    || !savedPersistentAuraIDs.Add(savedAura.PersistentAuraID))
                {
                    return FailPersistentAuraRestore(
                        "persisted character aura state contains an empty or duplicate persistence ID",
                        out failureReason);
                }
                if (string.IsNullOrWhiteSpace(savedAura.AuraDefinitionID))
                {
                    return FailPersistentAuraRestore(
                        "persisted character aura state contains a blank definition ID",
                        out failureReason);
                }
                if (!savedAura.SourceActorID.HasValue || savedAura.SourceActorID.Value == Guid.Empty)
                {
                    return FailPersistentAuraRestore(
                        $"persistent aura '{savedAura.AuraDefinitionID}' has no usable source actor ID",
                        out failureReason);
                }
                if (!savedAura.ExpiresAtUtc.HasValue
                    || savedAura.ExpiresAtUtc.Value == default
                    || savedAura.ExpiresAtUtc.Value.Offset != TimeSpan.Zero)
                {
                    return FailPersistentAuraRestore(
                        $"persistent aura '{savedAura.AuraDefinitionID}' has an invalid UTC expiry",
                        out failureReason);
                }

                bool hasNextTick = savedAura.NextTickAtUtc.HasValue;
                bool hasTickInterval = savedAura.TickIntervalSeconds.HasValue;
                if (hasNextTick != hasTickInterval)
                {
                    return FailPersistentAuraRestore(
                        $"persistent aura '{savedAura.AuraDefinitionID}' has an incomplete tick timing pair",
                        out failureReason);
                }
                if (hasNextTick
                    && (savedAura.NextTickAtUtc.Value == default
                        || savedAura.NextTickAtUtc.Value.Offset != TimeSpan.Zero
                        || float.IsNaN(savedAura.TickIntervalSeconds.Value)
                        || float.IsInfinity(savedAura.TickIntervalSeconds.Value)
                        || savedAura.TickIntervalSeconds.Value <= 0f))
                {
                    return FailPersistentAuraRestore(
                        $"persistent aura '{savedAura.AuraDefinitionID}' has invalid tick timing",
                        out failureReason);
                }

                AuraDefinition definition = definitionLibrary.GetDefinition(savedAura.AuraDefinitionID);
                if (definition == null)
                {
                    return FailPersistentAuraRestore(
                        $"persistent aura '{savedAura.AuraDefinitionID}' does not resolve to an authored definition",
                        out failureReason);
                }
                if (!Enum.IsDefined(typeof(AuraPersistencePolicy), definition.PersistencePolicy)
                    || definition.PersistencePolicy != AuraPersistencePolicy.PersistAcrossActorLifetime)
                {
                    return FailPersistentAuraRestore(
                        $"persistent aura '{savedAura.AuraDefinitionID}' is not enabled for actor-lifetime persistence",
                        out failureReason);
                }
                if (definition.StackBehavior != AuraStackBehavior.RefreshDuration)
                {
                    return FailPersistentAuraRestore(
                        $"persistent aura '{savedAura.AuraDefinitionID}' uses a non-persistable stacking form",
                        out failureReason);
                }
                if (definition.MaxStacks <= 0
                    || savedAura.Stacks < 1
                    || savedAura.Stacks > definition.MaxStacks)
                {
                    return FailPersistentAuraRestore(
                        $"persistent aura '{savedAura.AuraDefinitionID}' has an invalid stack count",
                        out failureReason);
                }
                if (definition.Components == null)
                {
                    return FailPersistentAuraRestore(
                        $"persistent aura '{savedAura.AuraDefinitionID}' has no valid component list",
                        out failureReason);
                }
                foreach (AuraComponent component in definition.Components)
                {
                    if (component == null || component.EffectDefinition == null)
                    {
                        return FailPersistentAuraRestore(
                            $"persistent aura '{savedAura.AuraDefinitionID}' has an invalid component",
                            out failureReason);
                    }
                    if (component.CastPhase == AuraPhase.OnTick)
                    {
                        return FailPersistentAuraRestore(
                            $"persistent aura '{savedAura.AuraDefinitionID}' has unsupported offline OnTick effects",
                            out failureReason);
                    }
                }

                if (savedAura.ExpiresAtUtc.Value <= observedAtUtc)
                    continue;

                Actor caster;
                Guid sourceActorID = savedAura.SourceActorID.Value;
                if (sourceActorID == target.ID)
                    caster = target;
                else if (!actorManager.TryGetActor(sourceActorID, out caster) || caster == null)
                {
                    return FailPersistentAuraRestore(
                        $"persistent aura '{savedAura.AuraDefinitionID}' source actor {sourceActorID} is not live",
                        out failureReason);
                }

                double remainingSeconds = (savedAura.ExpiresAtUtc.Value - observedAtUtc).TotalSeconds;
                if (double.IsNaN(remainingSeconds)
                    || double.IsInfinity(remainingSeconds)
                    || remainingSeconds <= 0d
                    || remainingSeconds > float.MaxValue)
                {
                    return FailPersistentAuraRestore(
                        $"persistent aura '{savedAura.AuraDefinitionID}' has an invalid remaining duration",
                        out failureReason);
                }

                float duration = (float)remainingSeconds;
                if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
                {
                    return FailPersistentAuraRestore(
                        $"persistent aura '{savedAura.AuraDefinitionID}' has an unrepresentable remaining duration",
                        out failureReason);
                }

                long durationTicks;
                try
                {
                    durationTicks = ServerSimulationClock.GetNonShorteningTickCount(
                        duration,
                        fixedTickDuration);
                }
                catch (OverflowException exception)
                {
                    return FailPersistentAuraRestore(
                        $"persistent aura '{savedAura.AuraDefinitionID}' has an unrepresentable duration: {exception.Message}",
                        out failureReason);
                }

                long expiryTickID;
                try
                {
                    expiryTickID = checked(firstProgressTickID + durationTicks - 1L);
                }
                catch (OverflowException exception)
                {
                    return FailPersistentAuraRestore(
                        $"persistent aura '{savedAura.AuraDefinitionID}' has an overflowing expiry tick: {exception.Message}",
                        out failureReason);
                }

                long nextTickID = 0L;
                long tickIntervalTicks = 0L;
                float tickInterval = 0f;
                if (hasNextTick)
                {
                    double savedIntervalSeconds = savedAura.TickIntervalSeconds.Value;
                    if (double.IsNaN(savedIntervalSeconds)
                        || double.IsInfinity(savedIntervalSeconds)
                        || savedIntervalSeconds <= 0d
                        || savedIntervalSeconds > TimeSpan.MaxValue.TotalSeconds)
                    {
                        return FailPersistentAuraRestore(
                            $"persistent aura '{savedAura.AuraDefinitionID}' has an invalid tick interval",
                            out failureReason);
                    }

                    long savedIntervalTicks;
                    try
                    {
                        savedIntervalTicks = checked(
                            (long)Math.Ceiling(savedIntervalSeconds * TimeSpan.TicksPerSecond));
                    }
                    catch (OverflowException exception)
                    {
                        return FailPersistentAuraRestore(
                            $"persistent aura '{savedAura.AuraDefinitionID}' has an unrepresentable tick interval: {exception.Message}",
                            out failureReason);
                    }
                    if (savedIntervalTicks <= 0)
                    {
                        return FailPersistentAuraRestore(
                            $"persistent aura '{savedAura.AuraDefinitionID}' has an unrepresentable tick interval",
                            out failureReason);
                    }

                    DateTimeOffset nextTickAtUtc = savedAura.NextTickAtUtc.Value;
                    if (nextTickAtUtc <= observedAtUtc)
                    {
                        long elapsedTicks;
                        try
                        {
                            elapsedTicks = checked(observedAtUtc.Ticks - nextTickAtUtc.Ticks);
                        }
                        catch (OverflowException exception)
                        {
                            return FailPersistentAuraRestore(
                                $"persistent aura '{savedAura.AuraDefinitionID}' has an overflowing elapsed tick interval: {exception.Message}",
                                out failureReason);
                        }

                        long intervalsToAdvance;
                        try
                        {
                            intervalsToAdvance = checked(elapsedTicks / savedIntervalTicks + 1L);
                        }
                        catch (OverflowException exception)
                        {
                            return FailPersistentAuraRestore(
                                $"persistent aura '{savedAura.AuraDefinitionID}' has an overflowing tick advancement: {exception.Message}",
                                out failureReason);
                        }

                        long advanceTicks;
                        try
                        {
                            advanceTicks = checked(savedIntervalTicks * intervalsToAdvance);
                            nextTickAtUtc = nextTickAtUtc.AddTicks(advanceTicks);
                        }
                        catch (OverflowException exception)
                        {
                            return FailPersistentAuraRestore(
                                $"persistent aura '{savedAura.AuraDefinitionID}' has an overflowing next tick: {exception.Message}",
                                out failureReason);
                        }
                        catch (ArgumentOutOfRangeException exception)
                        {
                            return FailPersistentAuraRestore(
                                $"persistent aura '{savedAura.AuraDefinitionID}' has an out-of-range next tick: {exception.Message}",
                                out failureReason);
                        }
                    }

                    double nextTickSeconds = (nextTickAtUtc - observedAtUtc).TotalSeconds;
                    if (double.IsNaN(nextTickSeconds)
                        || double.IsInfinity(nextTickSeconds)
                        || nextTickSeconds <= 0d
                        || nextTickSeconds > float.MaxValue)
                    {
                        return FailPersistentAuraRestore(
                            $"persistent aura '{savedAura.AuraDefinitionID}' has an elapsed or invalid next tick",
                            out failureReason);
                    }

                    float nextTickDuration = (float)nextTickSeconds;
                    if (float.IsNaN(nextTickDuration)
                        || float.IsInfinity(nextTickDuration)
                        || nextTickDuration <= 0f)
                    {
                        return FailPersistentAuraRestore(
                            $"persistent aura '{savedAura.AuraDefinitionID}' has an unrepresentable next tick",
                            out failureReason);
                    }

                    try
                    {
                        long nextTickTicks = ServerSimulationClock.GetNonShorteningTickCount(
                            nextTickDuration,
                            fixedTickDuration);
                        tickIntervalTicks = ServerSimulationClock.GetNonShorteningTickCount(
                            (float)savedIntervalSeconds,
                            fixedTickDuration);
                        nextTickID = checked(firstProgressTickID + nextTickTicks - 1L);
                    }
                    catch (OverflowException exception)
                    {
                        return FailPersistentAuraRestore(
                            $"persistent aura '{savedAura.AuraDefinitionID}' has overflowing tick timing: {exception.Message}",
                            out failureReason);
                    }

                    tickInterval = (float)savedIntervalSeconds;
                }

                stagedAuras.Add((
                    definition,
                    caster,
                    savedAura.PersistentAuraID,
                    savedAura.Stacks,
                    duration,
                    tickInterval,
                    expiryTickID,
                    nextTickID,
                    tickIntervalTicks));
            }

            foreach (AuraContext existingContext in _activeAuras.Values)
            {
                if (existingContext != null
                    && existingContext.TargetID == target.ID
                    && savedPersistentAuraIDs.Contains(existingContext.PersistentAuraID))
                {
                    return FailPersistentAuraRestore(
                        $"persistent aura restore would duplicate persistence ID for actor {target.ID}",
                        out failureReason);
                }
            }

            if (stagedAuras.Count == 0)
                return true;

            List<int> stagedInstanceIDs = new();
            HashSet<int> stagedInstanceIDSet = new();
            List<(int InstanceID, AuraContext Context)> runtimeAuras = new();
            List<(int InstanceID, AuraContext Context)> appliedBehaviours = new();
            try
            {
                foreach (var stagedAura in stagedAuras)
                {
                    int instanceID = _nextInstanceID++;
                    AuraContext context = new(
                        stagedAura.Definition,
                        stagedAura.Caster,
                        target,
                        stagedAura.PersistentAuraID)
                    {
                        Duration = stagedAura.Duration,
                        TickInterval = stagedAura.TickInterval,
                        Stacks = stagedAura.Stacks
                    };
                    Systems.AuraElement element = new()
                    {
                        InstanceID = instanceID,
                        AuraDefinitionID = stagedAura.Definition.DefinitionID,
                        ExpiryTickID = stagedAura.ExpiryTickID,
                        NextTickID = stagedAura.NextTickID,
                        TickIntervalTicks = stagedAura.TickIntervalTicks,
                        NextStackExpiryTickID = 0
                    };

                    _activeAuras.Add(instanceID, context);
                    stagedInstanceIDs.Add(instanceID);
                    stagedInstanceIDSet.Add(instanceID);
                    auraBuffer.Add(element);
                    runtimeAuras.Add((instanceID, context));
                }

                entityManager.SetComponentEnabled<Systems.AuraElement>(targetEntity, true);

                foreach (var runtimeAura in runtimeAuras)
                {
                    AuraBehaviour behaviour = runtimeAura.Context.Definition.AuraBehaviour;
                    if (behaviour == null)
                        continue;

                    appliedBehaviours.Add(runtimeAura);
                    behaviour.OnApplied(runtimeAura.Context.Caster, target, runtimeAura.Context.Stacks);
                }

                return true;
            }
            catch (Exception exception)
            {
                List<Exception> rollbackFailures = new();
                for (int index = appliedBehaviours.Count - 1; index >= 0; index--)
                {
                    try
                    {
                        var appliedAura = appliedBehaviours[index];
                        appliedAura.Context.Definition.AuraBehaviour.OnRemoved(
                            appliedAura.Context.Caster,
                            target);
                    }
                    catch (Exception rollbackException)
                    {
                        rollbackFailures.Add(rollbackException);
                    }
                }

                for (int index = 0; index < stagedInstanceIDs.Count; index++)
                    _activeAuras.Remove(stagedInstanceIDs[index]);

                try
                {
                    for (int index = auraBuffer.Length - 1; index >= 0; index--)
                    {
                        if (stagedInstanceIDSet.Contains(auraBuffer[index].InstanceID))
                            auraBuffer.RemoveAt(index);
                    }
                }
                catch (Exception rollbackException)
                {
                    rollbackFailures.Add(rollbackException);
                }

                try
                {
                    entityManager.SetComponentEnabled<Systems.AuraElement>(
                        targetEntity,
                        auraBufferWasEnabled);
                }
                catch (Exception rollbackException)
                {
                    rollbackFailures.Add(rollbackException);
                }

                failureReason = $"persistent aura restore failed: {exception.Message}";
                if (rollbackFailures.Count > 0)
                    failureReason += $" Rollback also failed for {rollbackFailures.Count} operation(s).";
                return false;
            }
        }

        private static bool FailPersistentAuraRestore(string reason, out string failureReason)
        {
            failureReason = reason;
            return false;
        }

        private void OnApplyAuraRequest(AuraDefinition definition, Actor caster, Actor target, int stacks)
        {
            if (definition == null || caster == null || target == null)
                return;

            Debug.Log($"Received request to apply aura '{definition.DisplayName}' from {caster.Name} to {target.Name}");
            ApplyAura(definition, caster, target, stacks);
        }

        private static bool TryValidateAuraApplication(
            AuraDefinition definition,
            int stacks,
            out string failureReason)
        {
            failureReason = null;
            if (definition == null)
            {
                failureReason = "The aura definition is required.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(definition.DefinitionID))
            {
                failureReason = "The aura definition ID is required.";
                return false;
            }
            if (!Enum.IsDefined(typeof(AuraStackBehavior), definition.StackBehavior))
            {
                failureReason = $"Aura '{definition.DefinitionID}' has an unsupported stacking mode.";
                return false;
            }
            if (!Enum.IsDefined(typeof(AuraPersistencePolicy), definition.PersistencePolicy))
            {
                failureReason = $"Aura '{definition.DefinitionID}' has an unsupported persistence policy.";
                return false;
            }
            if (definition.MaxStacks <= 0)
            {
                failureReason = $"Aura '{definition.DefinitionID}' must have a positive maximum stack count.";
                return false;
            }
            if (stacks <= 0)
            {
                failureReason = $"Aura '{definition.DefinitionID}' requires a positive requested stack count.";
                return false;
            }
            if (float.IsNaN(definition.BaseDuration)
                || float.IsInfinity(definition.BaseDuration)
                || definition.BaseDuration <= 0f)
            {
                failureReason = $"Aura '{definition.DefinitionID}' requires a finite positive duration.";
                return false;
            }
            if (float.IsNaN(definition.BaseTickInterval)
                || float.IsInfinity(definition.BaseTickInterval)
                || definition.BaseTickInterval < 0f)
            {
                failureReason = $"Aura '{definition.DefinitionID}' requires a finite non-negative tick interval.";
                return false;
            }
            if (definition.StackBehavior == AuraStackBehavior.IndependentDuration
                && definition.PersistencePolicy == AuraPersistencePolicy.PersistAcrossActorLifetime)
            {
                failureReason =
                    $"Aura '{definition.DefinitionID}' cannot persist independent stack deadlines across actor lifetime.";
                return false;
            }
            if (definition.Components == null)
            {
                failureReason = $"Aura '{definition.DefinitionID}' has no component list.";
                return false;
            }
            foreach (AuraComponent component in definition.Components)
            {
                if (component == null || component.EffectDefinition == null)
                {
                    failureReason = $"Aura '{definition.DefinitionID}' has an invalid component.";
                    return false;
                }
            }

            return true;
        }

        /// <summary>Applies an aura definition to a target actor.</summary>
        /// <param name="definition">The aura definition to apply.</param>
        /// <param name="caster">The actor applying the aura.</param>
        /// <param name="target">The actor receiving the aura.</param>
        /// <param name="stacks">The number of stacks to apply.</param>
        /// <returns><see langword="true"/> when the aura was applied.</returns>
        public bool ApplyAura(AuraDefinition definition, Actor caster, Actor target, int stacks = 1)
        {
            if (definition == null || caster == null || target == null)
                return false;
            if (!TryValidateAuraApplication(definition, stacks, out string failureReason))
            {
                Debug.Warning(failureReason);
                return false;
            }

            if (ServerECSManager.IsMutationBoundaryActive)
            {
                if (!TryGetCurrentActor(target.ID, target, out _))
                    return false;

                int refreshedInstanceID = TryFindActiveAuraInstance(
                        target.ID,
                        definition.DefinitionID,
                        caster.ID,
                        out int activeInstanceID)
                    ? activeInstanceID
                    : NO_EXPECTED_AURA_INSTANCE_ID;
                _pendingMutations.Enqueue(
                    new AuraMutation(
                        AuraMutationKind.Apply,
                        definition,
                        caster,
                        target,
                        target.ID,
                        refreshedInstanceID,
                        stacks));
                return true;
            }

            if (!TryGetCurrentActor(target.ID, target, out _))
                return false;

            return ApplyAuraNow(definition, caster, target, stacks);
        }

        private bool ApplyAuraNow(
            AuraDefinition definition,
            Actor caster,
            Actor target,
            int stacks,
            int expectedInstanceID = NO_EXPECTED_AURA_INSTANCE_ID)
        {
            if (!TryFindActorEntity(target.ID, out Entity entity))
            {
                Debug.Warning($"Could not find ECS entity for actor {target.Name} (ID: {target.ID})");
                return false;
            }

            long firstProgressTickID = GetFirstAuraProgressTickID();
            if (!TryResolveStacking(
                    definition,
                    caster,
                    target,
                    entity,
                    stacks,
                    firstProgressTickID,
                    expectedInstanceID,
                    out int instanceID,
                    out bool createdInstance))
            {
                return false;
            }

            _entityManager.SetComponentEnabled<Systems.AuraElement>(
                entity,
                true);

            var ctx = _activeAuras[instanceID];
            if (!TryGetAuraElement(
                    target.ID,
                    definition.DefinitionID,
                    instanceID,
                    out _,
                    out _,
                    out Systems.AuraElement appliedElement))
            {
                throw new InvalidOperationException(
                    $"Aura instance {instanceID} was created without an ECS element.");
            }

            // Execute OnApply phase components.
            ctx.Phase = AuraPhase.OnApply;
            foreach (var auraComponent in definition.Components)
            {
                if (auraComponent.CastPhase == AuraPhase.OnApply)
                    ExecuteAuraComponent(ctx, auraComponent);
            }

            ActorEndpoint.onServerAuraApplied?.Invoke(target.ID, definition.DefinitionID, instanceID);
            if (createdInstance)
                definition.AuraBehaviour?.OnApplied(caster, target, ctx.Stacks);

            Debug.Log($"Aura '{definition.DisplayName}' applied to {target.Name} by {caster.Name} (instance {instanceID})");

            NetworkingPerformanceMetrics.RecordCombatCriticalObserverPublication(target.GetComponent<NetworkIdentity>());
            target.Observers_ApplyAura(
                definition.DefinitionID,
                instanceID,
                GetRemainingAuraDuration(appliedElement.ExpiryTickID),
                ctx.Stacks);

            return true;
        }

        private bool TryResolveStacking(
            AuraDefinition definition,
            Actor caster,
            Actor target,
            Entity entity,
            int stacksToApply,
            long firstProgressTickID,
            int expectedInstanceID,
            out int instanceID,
            out bool createdInstance)
        {
            var buffer = _entityManager.GetBuffer<Systems.AuraElement>(entity);
            instanceID = -1;
            createdInstance = false;

            switch (definition.StackBehavior)
            {
                case AuraStackBehavior.RefreshDuration:
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            var entry = buffer[i];
                            if (entry.AuraDefinitionID.ToString() != definition.DefinitionID)
                                continue;
                            if (expectedInstanceID != NO_EXPECTED_AURA_INSTANCE_ID
                                && entry.InstanceID != expectedInstanceID)
                            {
                                continue;
                            }
                            if (!_activeAuras.TryGetValue(entry.InstanceID, out var existing)
                                || existing == null
                                || existing.CasterID != caster.ID
                                || existing.TargetID != target.ID)
                            {
                                continue;
                            }

                            InitializeAuraTiming(
                                definition,
                                firstProgressTickID,
                                definition.BaseDuration,
                                out entry.ExpiryTickID,
                                out entry.NextTickID,
                                out entry.TickIntervalTicks);
                            entry.NextStackExpiryTickID = 0;
                            buffer[i] = entry;
                            existing.Duration = definition.BaseDuration;
                            existing.Stacks += Mathf.Min(
                                stacksToApply,
                                definition.MaxStacks - existing.Stacks);
                            instanceID = entry.InstanceID;
                            return true;
                        }

                        if (expectedInstanceID != NO_EXPECTED_AURA_INSTANCE_ID)
                            return false;

                        instanceID = CreateAuraInstance(
                            definition,
                            caster,
                            target,
                            buffer,
                            stacksToApply,
                            firstProgressTickID,
                            independentDuration: false);
                        createdInstance = true;
                        return true;
                    }

                case AuraStackBehavior.IndependentDuration:
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            var entry = buffer[i];
                            if (entry.AuraDefinitionID.ToString() != definition.DefinitionID)
                                continue;
                            if (expectedInstanceID != NO_EXPECTED_AURA_INSTANCE_ID
                                && entry.InstanceID != expectedInstanceID)
                            {
                                continue;
                            }
                            if (!_activeAuras.TryGetValue(entry.InstanceID, out var existing)
                                || existing == null
                                || existing.CasterID != caster.ID
                                || existing.TargetID != target.ID)
                                continue;

                            if (existing.StackExpiryTickIDs == null)
                            {
                                throw new InvalidOperationException(
                                    $"Independent aura '{definition.DefinitionID}' has no stack expiry state.");
                            }

                            int freeStackCount = definition.MaxStacks - existing.StackExpiryTickIDs.Count;
                            int stacksToAdd = Mathf.Min(stacksToApply, Mathf.Max(freeStackCount, 0));
                            if (stacksToAdd > 0)
                            {
                                long stackExpiryTickID = checked(
                                    firstProgressTickID
                                    + GetNonShorteningDurationTicks(
                                        definition.BaseDuration,
                                        GetConfiguredSimulationTickDuration())
                                    - 1L);
                                for (int stackIndex = 0; stackIndex < stacksToAdd; stackIndex++)
                                    existing.StackExpiryTickIDs.Add(stackExpiryTickID);
                            }

                            UpdateIndependentAuraState(existing, ref entry);
                            buffer[i] = entry;
                            instanceID = entry.InstanceID;
                            return true;
                        }

                        if (expectedInstanceID != NO_EXPECTED_AURA_INSTANCE_ID)
                            return false;

                        instanceID = CreateAuraInstance(
                            definition,
                            caster,
                            target,
                            buffer,
                            stacksToApply,
                            firstProgressTickID,
                            independentDuration: true);
                        createdInstance = true;
                        return true;
                    }

                default:
                    return false;
            }
        }

        private int CreateAuraInstance(
            AuraDefinition definition,
            Actor caster,
            Actor target,
            DynamicBuffer<Systems.AuraElement> buffer,
            int initialStacks,
            long firstProgressTickID,
            bool independentDuration)
        {
            InitializeAuraTiming(
                definition,
                firstProgressTickID,
                definition.BaseDuration,
                out long expiryTickID,
                out long nextTickID,
                out long tickIntervalTicks);
            int instanceID = _nextInstanceID++;

            Guid persistentAuraID = definition.PersistencePolicy == AuraPersistencePolicy.PersistAcrossActorLifetime
                ? Guid.NewGuid()
                : Guid.Empty;
            var ctx = new AuraContext(definition, caster, target, persistentAuraID)
            {
                Stacks = Mathf.Min(initialStacks, definition.MaxStacks)
            };

            Systems.AuraElement element = new()
            {
                InstanceID = instanceID,
                AuraDefinitionID = definition.DefinitionID,
                ExpiryTickID = expiryTickID,
                NextTickID = nextTickID,
                TickIntervalTicks = tickIntervalTicks,
                NextStackExpiryTickID = 0
            };
            if (independentDuration)
            {
                ctx.StackExpiryTickIDs = new List<long>(ctx.Stacks);
                for (int stackIndex = 0; stackIndex < ctx.Stacks; stackIndex++)
                    ctx.StackExpiryTickIDs.Add(expiryTickID);
                UpdateIndependentAuraState(ctx, ref element);
            }

            _activeAuras[instanceID] = ctx;
            buffer.Add(element);
            return instanceID;
        }

        private static void UpdateIndependentAuraState(
            AuraContext context,
            ref Systems.AuraElement element)
        {
            if (context.StackExpiryTickIDs == null || context.StackExpiryTickIDs.Count == 0)
                throw new InvalidOperationException("Independent aura stack expiry state is empty.");

            long earliestExpiryTickID = long.MaxValue;
            long latestExpiryTickID = long.MinValue;
            for (int index = 0; index < context.StackExpiryTickIDs.Count; index++)
            {
                long expiryTickID = context.StackExpiryTickIDs[index];
                earliestExpiryTickID = Math.Min(earliestExpiryTickID, expiryTickID);
                latestExpiryTickID = Math.Max(latestExpiryTickID, expiryTickID);
            }

            context.Stacks = context.StackExpiryTickIDs.Count;
            element.ExpiryTickID = latestExpiryTickID;
            element.NextStackExpiryTickID = earliestExpiryTickID < latestExpiryTickID
                ? earliestExpiryTickID
                : 0L;
        }

        private bool TryGetAuraElement(
            Guid targetActorID,
            string auraDefID,
            int instanceID,
            out Entity entity,
            out int elementIndex,
            out Systems.AuraElement element)
        {
            elementIndex = -1;
            element = default;
            if (!TryFindActorEntity(targetActorID, out entity)
                || !_entityManager.Exists(entity))
            {
                entity = Entity.Null;
                return false;
            }

            DynamicBuffer<Systems.AuraElement> buffer = _entityManager.GetBuffer<Systems.AuraElement>(entity);
            for (int index = 0; index < buffer.Length; index++)
            {
                Systems.AuraElement candidate = buffer[index];
                if (candidate.InstanceID == instanceID
                    && candidate.AuraDefinitionID.ToString() == auraDefID)
                {
                    elementIndex = index;
                    element = candidate;
                    return true;
                }
            }

            return false;
        }

        private static float GetRemainingAuraDuration(long expiryTickID)
        {
            long currentTickID = ServerECSManager.CurrentSimulationTickID;
            if (currentTickID <= 0)
                currentTickID = ServerSimulationClock.CurrentTickID;

            long remainingTicks = expiryTickID - currentTickID;
            if (remainingTicks <= 0)
                return 0f;
            return (float)(remainingTicks * (double)GetConfiguredSimulationTickDuration());
        }

        private void OnAuraTick(Guid targetActorID, string auraDefID, int instanceID)
        {
            if (_pendingRemovalInstanceIDs.Contains(instanceID)
                || !_activeAuras.TryGetValue(instanceID, out var ctx)
                || ctx.TargetID != targetActorID
                || ctx.Definition == null
                || ctx.Definition.DefinitionID != auraDefID)
                return;

            if (!TryGetCurrentActor(targetActorID, null, out var target))
                return;

            ctx.Phase = AuraPhase.OnTick;
            foreach (var component in ctx.Definition.Components)
            {
                if (component.CastPhase == AuraPhase.OnTick)
                    ExecuteAuraComponent(ctx, component);
            }

            NetworkingPerformanceMetrics.RecordCombatCriticalObserverPublication(target.GetComponent<NetworkIdentity>());
            if (TryGetAuraElement(
                    targetActorID,
                    auraDefID,
                    instanceID,
                    out _,
                    out _,
                    out Systems.AuraElement element))
            {
                target.Observers_TickAura(
                    instanceID,
                    ctx.Stacks,
                    GetRemainingAuraDuration(element.ExpiryTickID));
            }
        }

        private void OnAuraStackExpired(Guid targetActorID, string auraDefID, int instanceID)
        {
            if (_pendingRemovalInstanceIDs.Contains(instanceID)
                || !_activeAuras.TryGetValue(instanceID, out var ctx)
                || ctx.TargetID != targetActorID
                || ctx.Definition == null
                || ctx.Definition.DefinitionID != auraDefID
                || ctx.Definition.StackBehavior != AuraStackBehavior.IndependentDuration
                || ctx.StackExpiryTickIDs == null)
            {
                return;
            }

            if (!TryGetAuraElement(
                    targetActorID,
                    auraDefID,
                    instanceID,
                    out Entity entity,
                    out int elementIndex,
                    out Systems.AuraElement element))
            {
                return;
            }

            long currentTickID = ServerECSManager.CurrentSimulationTickID;
            for (int index = ctx.StackExpiryTickIDs.Count - 1; index >= 0; index--)
            {
                if (ctx.StackExpiryTickIDs[index] <= currentTickID)
                    ctx.StackExpiryTickIDs.RemoveAt(index);
            }
            if (ctx.StackExpiryTickIDs.Count == 0)
                return;

            UpdateIndependentAuraState(ctx, ref element);
            DynamicBuffer<Systems.AuraElement> buffer = _entityManager.GetBuffer<Systems.AuraElement>(entity);
            buffer[elementIndex] = element;

            if (!TryGetCurrentActor(targetActorID, null, out var target))
                return;

            NetworkingPerformanceMetrics.RecordCombatCriticalObserverPublication(target.GetComponent<NetworkIdentity>());
            target.Observers_ApplyAura(
                auraDefID,
                instanceID,
                GetRemainingAuraDuration(element.ExpiryTickID),
                ctx.Stacks);
        }

        private void OnAuraExpired(Guid targetActorID, string auraDefID, int instanceID)
        {
            if (_pendingRemovalInstanceIDs.Contains(instanceID)
                || !_activeAuras.TryGetValue(instanceID, out var ctx)
                || ctx.TargetID != targetActorID
                || ctx.Definition == null
                || ctx.Definition.DefinitionID != auraDefID
                || !_activeAuras.Remove(instanceID))
                return;

            _pendingRemovalInstanceIDs.Remove(instanceID);

            if (!TryGetCurrentActor(targetActorID, null, out var target))
            {
                ctx.Definition.AuraBehaviour?.OnRemoved(ctx.Caster, ctx.Target);
                return;
            }

            ctx.Phase = AuraPhase.OnExpire;
            foreach (var component in ctx.Definition.Components)
            {
                if (component.CastPhase == AuraPhase.OnExpire)
                    ExecuteAuraComponent(ctx, component);
            }

            ctx.Definition.AuraBehaviour?.OnRemoved(ctx.Caster, target);

            NetworkingPerformanceMetrics.RecordCombatCriticalObserverPublication(target.GetComponent<NetworkIdentity>());
            target.Observers_ExpireAura(instanceID);
            Debug.Log($"Aura '{ctx.Definition.DisplayName}' expired on {target.Name} (instance {instanceID})");
        }

        private void OnAuraDispelled(Guid targetActorID, string auraDefID, int instanceID)
        {
            if (!_activeAuras.TryGetValue(instanceID, out var ctx)
                || ctx.TargetID != targetActorID
                || ctx.Definition == null
                || ctx.Definition.DefinitionID != auraDefID
                || !_activeAuras.Remove(instanceID))
                return;

            _pendingRemovalInstanceIDs.Remove(instanceID);

            if (!TryGetCurrentActor(targetActorID, null, out var target))
            {
                ctx.Definition.AuraBehaviour?.OnRemoved(ctx.Caster, ctx.Target);
                return;
            }

            ctx.Phase = AuraPhase.OnDispel;
            foreach (var component in ctx.Definition.Components)
            {
                if (component.CastPhase == AuraPhase.OnDispel)
                    ExecuteAuraComponent(ctx, component);
            }

            ctx.Definition.AuraBehaviour?.OnRemoved(ctx.Caster, target);

            NetworkingPerformanceMetrics.RecordCombatCriticalObserverPublication(target.GetComponent<NetworkIdentity>());
            target.Observers_DispelAura(instanceID);
            Debug.Log($"Aura '{ctx.Definition.DisplayName}' dispelled from {target.Name} (instance {instanceID})");
        }

        private void ExecuteAuraComponent(AuraContext ctx, AuraComponent auraComponent)
        {
            if (auraComponent.TargetDefinition == null)
            {
                auraComponent.EffectDefinition.Execute(ctx, ctx.Target);
                return;
            }

            foreach (Actor target in auraComponent.TargetDefinition.Evaluate(ctx))
                auraComponent.EffectDefinition.Execute(ctx, target);
        }

        /// <summary>Dispels one aura instance from a target actor.</summary>
        /// <param name="target">The actor whose aura is dispelled.</param>
        /// <param name="instanceID">The aura instance identifier.</param>
        /// <returns><see langword="true"/> when the aura was dispelled.</returns>
        public bool DispelAura(Actor target, int instanceID)
        {
            if (target == null
                || !_activeAuras.TryGetValue(instanceID, out var context)
                || context.TargetID != target.ID
                || _pendingRemovalInstanceIDs.Contains(instanceID))
            {
                return false;
            }

            if (ServerECSManager.IsMutationBoundaryActive)
            {
                if (!TryGetCurrentActor(target.ID, target, out _))
                    return false;

                _pendingRemovalInstanceIDs.Add(instanceID);
                try
                {
                    _pendingMutations.Enqueue(
                        new AuraMutation(
                            AuraMutationKind.Dispel,
                            null,
                            null,
                            target,
                            target.ID,
                            instanceID,
                            0));
                }
                catch
                {
                    _pendingRemovalInstanceIDs.Remove(instanceID);
                    throw;
                }
                return true;
            }

            return DispelAuraNow(target, instanceID);
        }

        private bool DispelAuraNow(Actor target, int instanceID)
        {
            if (!_activeAuras.TryGetValue(instanceID, out var context)
                || context.TargetID != target.ID
                || context.Definition == null)
            {
                return false;
            }

            string auraDefID = context.Definition.DefinitionID;
            if (TryFindActorEntity(target.ID, out Entity entity))
            {
                var buffer = _entityManager.GetBuffer<Systems.AuraElement>(entity);
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i].InstanceID != instanceID) continue;

                    buffer.RemoveAt(i);
                    break;
                }

                if (buffer.Length == 0)
                {
                    _entityManager.SetComponentEnabled<Systems.AuraElement>(
                        entity,
                        false);
                }
            }

            ActorEndpoint.onServerAuraDispelled?.Invoke(target.ID, auraDefID, instanceID);
            return true;
        }

        /// <summary>Removes all active auras from an actor.</summary>
        /// <param name="actorID">The actor identifier.</param>
        public void RemoveAllAuras(Guid actorID)
        {
            if (ServerECSManager.IsMutationBoundaryActive)
            {
                if (!TryGetCurrentActor(actorID, null, out _))
                    return;

                foreach (var pair in _activeAuras)
                {
                    if (pair.Value.TargetID == actorID)
                        _pendingRemovalInstanceIDs.Add(pair.Key);
                }

                try
                {
                    _pendingMutations.Enqueue(
                        new AuraMutation(
                            AuraMutationKind.RemoveAll,
                            null,
                            null,
                            null,
                            actorID,
                            0,
                            0));
                }
                catch
                {
                    foreach (var pair in _activeAuras)
                    {
                        if (pair.Value.TargetID == actorID)
                            _pendingRemovalInstanceIDs.Remove(pair.Key);
                    }
                    throw;
                }
                return;
            }

            RemoveAllAurasNow(actorID);
        }

        private void RemoveAllAurasNow(Guid actorID)
        {
            bool hasCurrentActor = TryGetCurrentActor(actorID, null, out _);

            var contexts = new List<AuraContext>();
            var instanceIDs = new List<int>();
            foreach (var pair in _activeAuras)
            {
                if (pair.Value.TargetID != actorID)
                    continue;

                instanceIDs.Add(pair.Key);
                contexts.Add(pair.Value);
            }

            foreach (int instanceID in instanceIDs)
            {
                _activeAuras.Remove(instanceID);
                _pendingRemovalInstanceIDs.Remove(instanceID);
            }

            if (TryFindActorEntity(actorID, out Entity entity))
            {
                var buffer = _entityManager.GetBuffer<Systems.AuraElement>(entity);
                buffer.Clear();
                _entityManager.SetComponentEnabled<Systems.AuraElement>(
                    entity,
                    false);
            }

            foreach (AuraContext context in contexts)
            {
                if (hasCurrentActor)
                {
                    context.Phase = AuraPhase.OnExpire;
                    foreach (var auraComponent in context.Definition.Components)
                    {
                        if (auraComponent.CastPhase == AuraPhase.OnExpire)
                            ExecuteAuraComponent(context, auraComponent);
                    }
                }

                context.Definition.AuraBehaviour?.OnRemoved(context.Caster, context.Target);
            }
        }

        private static long GetFirstAuraProgressTickID()
        {
            long currentSimulationTickID = ServerECSManager.CurrentSimulationTickID;
            if (currentSimulationTickID > 0)
                return currentSimulationTickID;

            return checked(ServerSimulationClock.CurrentTickID + 1L);
        }

        private static void InitializeAuraTiming(
            AuraDefinition definition,
            long firstProgressTickID,
            float duration,
            out long expiryTickID,
            out long nextTickID,
            out long tickIntervalTicks)
        {
            if (firstProgressTickID <= 0)
                throw new InvalidOperationException("Aura timing requires a positive first progress tick.");

            float fixedDeltaTime = GetConfiguredSimulationTickDuration();
            long durationTicks = GetNonShorteningDurationTicks(duration, fixedDeltaTime);
            tickIntervalTicks = GetNonShorteningIntervalTicks(definition.BaseTickInterval, fixedDeltaTime);
            expiryTickID = checked(firstProgressTickID + durationTicks - 1L);
            nextTickID = tickIntervalTicks == 0
                ? 0
                : checked(firstProgressTickID + tickIntervalTicks - 1L);
        }

        private static float GetConfiguredSimulationTickDuration()
            => GameConfigurationManager.Config.ServerSimulationTickDuration;

        private static long GetNonShorteningDurationTicks(float duration, float fixedDeltaTime)
        {
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration <= 0f)
                throw new InvalidOperationException("An active aura requires a finite positive duration.");

            return GetCeilingTickCount(duration, fixedDeltaTime);
        }

        private static long GetNonShorteningIntervalTicks(float interval, float fixedDeltaTime)
        {
            if (float.IsNaN(interval) || float.IsInfinity(interval) || interval < 0f)
                throw new InvalidOperationException("An aura tick interval requires a finite non-negative duration.");
            if (interval == 0f)
                return 0;

            return GetCeilingTickCount(interval, fixedDeltaTime);
        }

        private static long GetCeilingTickCount(float seconds, float fixedDeltaTime)
        {
            return ServerSimulationClock.GetNonShorteningTickCount(seconds, fixedDeltaTime);
        }

        private bool TryFindActiveAuraInstance(
            Guid targetID,
            string auraDefinitionID,
            Guid casterID,
            out int instanceID)
        {
            foreach (var pair in _activeAuras)
            {
                AuraContext context = pair.Value;
                if (context != null
                    && context.TargetID == targetID
                    && context.Definition != null
                    && context.Definition.DefinitionID == auraDefinitionID
                    && context.CasterID == casterID)
                {
                    instanceID = pair.Key;
                    return true;
                }
            }

            instanceID = 0;
            return false;
        }

        private bool TryGetCurrentActor(Guid actorID, Actor expectedActor, out Actor currentActor)
        {
            if (!ServerActorManager.Instance.TryGetActor(actorID, out currentActor)
                || currentActor == null
                || (expectedActor != null && currentActor != expectedActor))
            {
                currentActor = null;
                return false;
            }

            return true;
        }

        private static bool TryFindActorEntity(Guid actorID, out Entity actorEntity)
        {
            return ServerActorManager.Instance.TryGetActorEntity(actorID, out actorEntity);
        }

        /// <summary>Determines whether an actor has an aura definition active.</summary>
        /// <param name="actorID">The actor identifier.</param>
        /// <param name="auraDefinitionID">The aura definition identifier.</param>
        /// <returns><see langword="true"/> when the aura is active.</returns>
        public bool HasAura(Guid actorID, string auraDefinitionID)
        {
            if (!TryFindActorEntity(actorID, out Entity entity))
                return false;

            var buffer = _entityManager.GetBuffer<Systems.AuraElement>(entity);
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].AuraDefinitionID.ToString() == auraDefinitionID)
                    return true;
            }

            return false;
        }

        /// <summary>Gets the active stack count for an actor's aura.</summary>
        /// <param name="actorID">The actor identifier.</param>
        /// <param name="auraDefinitionID">The aura definition identifier.</param>
        /// <returns>The active stack count, or zero when the aura is absent.</returns>
        public int GetAuraStackCount(Guid actorID, string auraDefinitionID)
        {
            if (!TryFindActorEntity(actorID, out Entity entity))
                return 0;

            var buffer = _entityManager.GetBuffer<Systems.AuraElement>(entity);
            int count = 0;
            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].AuraDefinitionID.ToString() == auraDefinitionID)
                    count++;
            }

            return count;
        }
    }
}
