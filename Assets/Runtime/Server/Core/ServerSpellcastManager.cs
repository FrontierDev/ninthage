using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Services;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Data.SpellTargetDefinitions;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using Game.Shared.Runtime.Spellcasting;
using Game.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace Game.Server.Core
{
    /// <summary>Maintains server-authoritative spell casts and effects.</summary>
    public sealed class ServerSpellCastManager : MonoBehaviour
    {
        private static ServerSpellCastManager _instance;
        /// <summary>Gets the active server spell-cast manager.</summary>
        public static ServerSpellCastManager Instance => _instance;
        /// <summary>Gets whether the spell-cast manager is initialized.</summary>
        public static bool IsInitialized => _instance != null;

        private EntityManager _entityManager;

        // Store active spell casts by actor ID for quick access during ticks and completion.
        private readonly Dictionary<Guid, SpellContext> _activeSpellCasts = new();
        private readonly Dictionary<Guid, long> _activeCastIDs = new();
        private readonly List<Guid> _spellTargetCandidateIDs = new();
        private readonly List<PendingAction> _pendingActions = new();
        private readonly ServerSimulationMutationQueue<SpellMutation> _pendingMutations = new();
        private readonly Dictionary<long, SpellMutation> _pendingStarts = new();
        private readonly Dictionary<Guid, List<long>> _pendingStartIDsByActor = new();
        private readonly Dictionary<long, SpellMutation> _pendingInterrupts = new();
        private readonly Dictionary<Guid, long> _pendingInterruptIDsByActor = new();
        private readonly List<SpellMutation> _commitSnapshot = new();
        private long _nextCastID;
        private Guid _delayedActionActorID;
        private long _delayedActionCastID;
        private bool _hasDelayedActionContext;

        private enum SpellMutationKind : byte
        {
            Start,
            Interrupt,
        }

        private readonly struct SpellMutation
        {
            internal SpellMutation(
                SpellMutationKind kind,
                Guid actorID,
                Actor caster,
                SpellOverride spell,
                SpellContext context,
                string spellID,
                long castID,
                bool hasWorldPositionTarget,
                WorldPosition worldPositionTarget)
            {
                Kind = kind;
                ActorID = actorID;
                Caster = caster;
                Spell = spell;
                Context = context;
                SpellID = spellID;
                CastID = castID;
                HasWorldPositionTarget = hasWorldPositionTarget;
                WorldPositionTarget = worldPositionTarget;
            }

            internal SpellMutationKind Kind { get; }
            internal Guid ActorID { get; }
            internal Actor Caster { get; }
            internal SpellOverride Spell { get; }
            internal SpellContext Context { get; }
            internal string SpellID { get; }
            internal long CastID { get; }
            internal bool HasWorldPositionTarget { get; }
            internal WorldPosition WorldPositionTarget { get; }
        }

        private sealed class PendingAction
        {
            internal PendingAction(Guid actorID, long castID, Action callback)
            {
                ActorID = actorID;
                CastID = castID;
                Callback = callback;
            }

            internal readonly Guid ActorID;
            internal readonly long CastID;
            internal readonly Action Callback;
            internal ServerSimulationRegistrationHandle Registration;
        }


        private void Awake()
        {
            if (_instance != null)
                throw new InvalidOperationException("ServerSpellCastManager is already initialized.");

            var world = ServerECSManager.DefaultWorld;
            _entityManager = world.EntityManager;

            ActorEndpoint.onServerSpellCastStarted += OnSpellCastStarted;
            ActorEndpoint.onServerSpellCastTick += OnSpellCastTick;
            ActorEndpoint.onServerSpellCastCompleted += OnSpellCastCompleted;
            ActorEndpoint.onServerSpellCastDelayedAction += OnSpellCastDelayedAction;
            ActorEndpoint.onServerSpellCastInterrupted += OnSpellCastInterrupted;
            ActorEndpoint.onServerRequestSpellCastCancel += OnSpellCastCancelRequested;
            ActorEndpoint.onServerActorDespawned += OnActorDespawned;

            _instance = this;
        }

        private void OnDestroy()
        {
            if (!ReferenceEquals(_instance, this))
                return;

            foreach (PendingAction pendingAction in _pendingActions)
                pendingAction.Registration.Cancel();

            _activeSpellCasts.Clear();
            _activeCastIDs.Clear();
            _spellTargetCandidateIDs.Clear();
            _pendingActions.Clear();
            _pendingMutations.Clear();
            _pendingStarts.Clear();
            _pendingStartIDsByActor.Clear();
            _pendingInterrupts.Clear();
            _pendingInterruptIDsByActor.Clear();
            _commitSnapshot.Clear();

            ActorEndpoint.onServerSpellCastStarted -= OnSpellCastStarted;
            ActorEndpoint.onServerSpellCastTick -= OnSpellCastTick;
            ActorEndpoint.onServerSpellCastCompleted -= OnSpellCastCompleted;
            ActorEndpoint.onServerSpellCastInterrupted -= OnSpellCastInterrupted;
            ActorEndpoint.onServerSpellCastDelayedAction -= OnSpellCastDelayedAction;
            ActorEndpoint.onServerRequestSpellCastCancel -= OnSpellCastCancelRequested;
            ActorEndpoint.onServerActorDespawned -= OnActorDespawned;

            _instance = null;
        }

        private void OnSpellCastCancelRequested(Actor actor)
        {
            TryInterruptCast(actor.ID);
        }

        private void OnActorDespawned(Actor actor)
        {
            if (actor == null)
                return;

            CancelPendingActions(actor.ID);
            _activeSpellCasts.Remove(actor.ID);
            _activeCastIDs.Remove(actor.ID);
            CancelPendingStarts(actor.ID);
            RemovePendingInterrupt(actor.ID);
        }

        internal void PreparePendingMutations()
        {
            _pendingMutations.TakeSnapshot(_commitSnapshot);
        }

        internal void CommitPendingMutations()
        {
            try
            {
                foreach (SpellMutation mutation in _commitSnapshot)
                {
                    switch (mutation.Kind)
                    {
                        case SpellMutationKind.Start:
                            if (!TryTakePendingStart(mutation, out SpellMutation pendingStart))
                                continue;

                            if (pendingStart.Caster == null
                                || ServerActorManager.Instance == null
                                || !ServerActorManager.Instance.TryGetActor(pendingStart.ActorID, out var currentCaster)
                                || currentCaster != pendingStart.Caster)
                            {
                                Debug.Warning($"Discarding spell start for retired actor {pendingStart.ActorID}.");
                                continue;
                            }

                            OnSpellCastStartedNow(
                                currentCaster,
                                pendingStart.Spell,
                                pendingStart.CastID,
                                pendingStart.HasWorldPositionTarget,
                                pendingStart.WorldPositionTarget);
                            break;
                        case SpellMutationKind.Interrupt:
                            if (TryTakePendingInterrupt(mutation, out SpellMutation pendingInterrupt))
                                CompleteDeferredInterrupt(pendingInterrupt);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(mutation.Kind), mutation.Kind, "Unknown spell mutation kind.");
                    }
                }
            }
            finally
            {
                _commitSnapshot.Clear();
            }
        }

        #region Spell Cast Lifecycle
        /// <summary>
        /// Internal, server-side validation for spell casts.
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="spellDef"></param>
        /// <returns></returns>
        private bool IsValidated(Actor caster, SpellOverride spell)
        {
            // Cooldown gate: block if this spell is on individual cooldown.
            if (ServerCooldownManager.Instance.IsOnCooldown(caster.ID, spell.BaseDefinition.DefinitionID))
            {
                return false;
            }

            // GCD gate: block if a GCD-triggering spell is attempted while GCD is active.
            if (spell.BaseDefinition.TriggersGCD && ServerCooldownManager.Instance.IsOnCooldown(caster.ID, ServerCooldownManager.GCD_ID))
            {
                return false;
            }

            if (spell.Components.Any(
                    component => component.TargetDefinition.RequiresTarget))
            {
                if (caster.Target == null)
                {
                    Debug.Warning(
                        $"Actor {caster.Name} attempted to cast '{spell.BaseDefinition.DefinitionID}' which requires a target, but no target was selected.");
                    return false;
                }
                if (!ServerActorManager.Instance.TryGetActor(caster.Target.ID, out Actor currentTarget)
                    || !ReferenceEquals(currentTarget, caster.Target))
                {
                    Debug.Warning(
                        $"Actor {caster.Name} attempted to cast '{spell.BaseDefinition.DefinitionID}' on a stale or despawned target.");
                    return false;
                }

                ActorSpatialContext casterContext = caster.SpatialContext;
                ActorSpatialContext targetContext = caster.Target.SpatialContext;
                if (!casterContext.IsCompatibleWith(targetContext))
                {
                    Debug.Warning(
                        $"Actor {caster.Name} attempted to cast '{spell.BaseDefinition.DefinitionID}' on a target in an incompatible spatial context.");
                    return false;
                }

                if (float.IsNaN(spell.Range)
                    || float.IsInfinity(spell.Range)
                    || spell.Range < 0f)
                {
                    Debug.Warning(
                        $"Spell '{spell.BaseDefinition.DefinitionID}' has an invalid effective range ({spell.Range}).");
                    return false;
                }

                double distance = ServerActorManager.Instance
                    .GetAcceptedSurfaceDistance(
                        caster,
                        caster.Target);
                float maxRange = spell.Range;
                if (distance > maxRange)
                {
                    Debug.Warning(
                        $"Actor {caster.Name} attempted to cast '{spell.BaseDefinition.DefinitionID}' on target {caster.Target.Name} which is out of range. Distance: {distance}, Max Range: {maxRange}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>Resolves spell recipients from current server actor state and spatial membership.</summary>
        private List<Actor> ResolveSpellTargets(
            SpellContext context,
            SpellTargetDefinition targetDefinition)
        {
            if (context == null
                || context.Caster == null
                || context.Spell == null
                || context.Spell.BaseDefinition == null
                || targetDefinition == null)
            {
                throw new InvalidOperationException(
                    "Spell target resolution requires a complete spell context and target definition.");
            }
            Actor caster = context.Caster;
            ActorSpatialContext spatialContext = caster.SpatialContext;

            WorldPosition ResolveAcceptedPosition(Guid actorID)
            {
                if (!ServerActorManager.Instance.TryGetAcceptedWorldPosition(actorID, out WorldPosition position))
                {
                    throw new InvalidOperationException(
                        $"Spatial index returned actor '{actorID}' without a live accepted world position.");
                }

                return position;
            }

            var results = new List<Actor>();
            if (targetDefinition is SpellTarget_Single)
            {
                Actor target = context.ActorTarget;
                if (target == null
                    || !ServerActorManager.Instance.TryGetActor(target.ID, out Actor currentTarget)
                    || !ReferenceEquals(currentTarget, target))
                {
                    return results;
                }

                ActorSpatialContext targetContext = target.SpatialContext;
                if (spatialContext.IsCompatibleWith(targetContext))
                    results.Add(target);

                return results;
            }

            if (targetDefinition is SpellTarget_Cone cone)
            {
                if (float.IsNaN(cone.Range)
                    || float.IsInfinity(cone.Range)
                    || cone.Range < 0f)
                {
                    throw new InvalidOperationException(
                        $"Cone target for spell '{context.Spell.BaseDefinition.DefinitionID}' has an invalid range ({cone.Range}).");
                }
                if (float.IsNaN(cone.Angle)
                    || float.IsInfinity(cone.Angle)
                    || cone.Angle < 0f
                    || cone.Angle > 360f)
                {
                    throw new InvalidOperationException(
                        $"Cone target for spell '{context.Spell.BaseDefinition.DefinitionID}' has an invalid angle ({cone.Angle}).");
                }
                if (!ServerActorManager.Instance.TryGetAcceptedWorldPosition(caster.ID, out WorldPosition casterPosition))
                {
                    throw new InvalidOperationException(
                        $"Spell caster '{caster.ID}' has no live accepted world position.");
                }

                _spellTargetCandidateIDs.Clear();
                ServerActorManager.Instance.QuerySpatialInRange(
                    spatialContext,
                    casterPosition,
                    cone.Range,
                    _spellTargetCandidateIDs,
                    ResolveAcceptedPosition,
                    out _,
                    includeVerticalDistance: true);

                Vector3 facing = caster.transform.forward;
                double facingX = facing.x;
                double facingY = facing.y;
                double facingZ = facing.z;
                double facingMagnitude = Math.Sqrt(
                    facingX * facingX + facingY * facingY + facingZ * facingZ);
                if (double.IsNaN(facingMagnitude)
                    || double.IsInfinity(facingMagnitude)
                    || facingMagnitude == 0d)
                {
                    throw new InvalidOperationException(
                        $"Spell caster '{caster.ID}' has an invalid facing direction for cone targeting.");
                }

                double minimumDot = Math.Cos(cone.Angle * 0.5d * Math.PI / 180d);
                foreach (Guid candidateID in _spellTargetCandidateIDs)
                {
                    if (candidateID == caster.ID)
                        continue;

                    if (!ServerActorManager.Instance.TryGetActor(candidateID, out Actor candidate))
                    {
                        throw new InvalidOperationException(
                            $"Spatial index returned actor '{candidateID}', but no live actor mapping exists.");
                    }
                    if (!spatialContext.IsCompatibleWith(candidate.SpatialContext))
                    {
                        throw new InvalidOperationException(
                            $"Spatial index returned actor '{candidateID}' from an incompatible context.");
                    }

                    WorldPosition candidatePosition = ResolveAcceptedPosition(candidateID);
                    double deltaX = candidatePosition.x - casterPosition.x;
                    double deltaY = candidatePosition.y - casterPosition.y;
                    double deltaZ = candidatePosition.z - casterPosition.z;
                    double displacementSquared =
                        deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ;
                    if (displacementSquared == 0d)
                    {
                        results.Add(candidate);
                        continue;
                    }

                    double displacementMagnitude = Math.Sqrt(displacementSquared);
                    double normalizedDot =
                        (facingX * deltaX + facingY * deltaY + facingZ * deltaZ)
                        / (facingMagnitude * displacementMagnitude);
                    normalizedDot = Math.Max(-1d, Math.Min(1d, normalizedDot));
                    if (normalizedDot >= minimumDot)
                        results.Add(candidate);
                }

                return results;
            }

            if (targetDefinition is SpellTarget_GroundLocation groundTarget)
            {
                if (!groundTarget.GetActorsInRadius)
                {
                    results.Add(caster);
                    return results;
                }
                if (!context.HasWorldPositionTarget || !context.WorldPositionTarget.IsFinite)
                {
                    throw new InvalidOperationException(
                        "Ground-radius target resolution requires a finite authoritative world target.");
                }
                if (float.IsNaN(groundTarget.Radius)
                    || float.IsInfinity(groundTarget.Radius)
                    || groundTarget.Radius < 0f)
                {
                    throw new InvalidOperationException(
                        $"Ground target for spell '{context.Spell.BaseDefinition.DefinitionID}' has an invalid radius ({groundTarget.Radius}).");
                }

                _spellTargetCandidateIDs.Clear();
                ServerActorManager.Instance.QuerySpatialInRange(
                    spatialContext,
                    context.WorldPositionTarget,
                    groundTarget.Radius,
                    _spellTargetCandidateIDs,
                    ResolveAcceptedPosition,
                    out _,
                    includeVerticalDistance: true);

                foreach (Guid candidateID in _spellTargetCandidateIDs)
                {
                    if (candidateID == caster.ID)
                        continue;

                    if (!ServerActorManager.Instance.TryGetActor(candidateID, out Actor candidate))
                    {
                        throw new InvalidOperationException(
                            $"Spatial index returned actor '{candidateID}', but no live actor mapping exists.");
                    }
                    if (!spatialContext.IsCompatibleWith(candidate.SpatialContext))
                    {
                        throw new InvalidOperationException(
                            $"Spatial index returned actor '{candidateID}' from an incompatible context.");
                    }

                    results.Add(candidate);
                }

                return results;
            }

            throw new NotSupportedException(
                $"Spell target definition type '{targetDefinition.GetType().FullName}' is not supported by the server target resolver.");
        }

        /// <summary>
        /// Handles the start of a spell cast. This is where we create the SpellContext, calculate the actual cast time 
        /// after modifiers, and initialize the ECS components for the spell cast.
        /// ActorSpellcaster performs request-side validation, while this manager revalidates
        /// authoritative cooldown, target and range state before setup.
        /// </summary>
        private void OnSpellCastStarted(Actor caster, SpellOverride spell)
        {
            if (caster == null || spell == null)
                return;

            bool hasWorldPositionTarget = spell.HasWorldPositionTarget;
            WorldPosition worldPositionTarget = spell.WorldPositionTarget;
            if (hasWorldPositionTarget && !worldPositionTarget.IsFinite)
            {
                Debug.Warning($"Actor {caster.Name} attempted to cast '{spell.BaseDefinition.DefinitionID}' with a non-finite ground target.");
                return;
            }

            if (ServerECSManager.IsMutationBoundaryActive
                || _pendingStartIDsByActor.ContainsKey(caster.ID)
                || _pendingInterruptIDsByActor.ContainsKey(caster.ID))
            {
                QueuePendingStart(caster, spell, hasWorldPositionTarget, worldPositionTarget);
                return;
            }

            OnSpellCastStartedNow(caster, spell, 0, hasWorldPositionTarget, worldPositionTarget);
        }

        private bool OnSpellCastStartedNow(
            Actor caster,
            SpellOverride spell,
            long requestedCastID = 0,
            bool hasWorldPositionTarget = false,
            WorldPosition worldPositionTarget = default)
        {
            bool requiresWorldPositionTarget = spell.Components.Any(
                component => component.TargetDefinition is SpellTarget_GroundLocation);
            if (requiresWorldPositionTarget != hasWorldPositionTarget)
            {
                Debug.Warning(
                    $"Actor {caster.Name} attempted to cast '{spell.BaseDefinition.DefinitionID}' with an invalid ground-target request shape.");
                return false;
            }

            if (requiresWorldPositionTarget)
            {
                if (!IsGroundTargetInRange(caster, spell, worldPositionTarget))
                    return false;
            }

            if (!IsValidated(caster, spell)) return false;

            if (!TryFindActorEntity(caster.ID, out Entity entity))
            {
                Debug.Warning($"Could not find ECS entity for actor {caster.Name} (ID: {caster.ID})");
                return false;
            }

            // Create the spell context here.
            SpellContext ctx = new(spell, caster, caster.Target);
            if (hasWorldPositionTarget)
            {
                ctx.HasWorldPositionTarget = true;
                ctx.WorldPositionTarget = worldPositionTarget;
                ctx.PositionTarget = ServerPhysicsManager.Instance.ToPhysicalPosition(worldPositionTarget);
            }
            long castID = requestedCastID > 0 ? requestedCastID : checked(++_nextCastID);
            InvalidateActiveCast(caster.ID);
            _activeSpellCasts[caster.ID] = ctx;
            _activeCastIDs[caster.ID] = castID;

            // Skip ECS and event invocation if the cast time is 0 or less
            // - just complete the cast immediately.
            if (ctx.IsInstantCast())
            {
                _entityManager.SetComponentData(entity, new Systems.SpellCastComponent
                {
                    SpellID = spell.BaseDefinition.DefinitionID,
                    CastID = castID,
                    FirstProgressTickID = 0,
                    CompletionTickID = 0,
                    TotalCastTime = 0f,
                    TotalTicks = 0f,
                    TimeSinceLastTick = 0f
                });
                _entityManager.SetComponentEnabled<Systems.SpellCastComponent>(entity, false);

                // Trigger GCD for instant casts.
                if (spell.BaseDefinition.TriggersGCD)
                    ActorEndpoint.onServerStartCooldown?.Invoke(caster.ID, ServerCooldownManager.GCD_ID, GameConfigurationManager.Config.GlobalCooldownDuration);

                // Instant cast - skip straight to completion.
                CompleteCastNow(caster.ID, spell.BaseDefinition.DefinitionID, castID, ctx);
                return true;
            }
            else
            {
                if (float.IsNaN(spell.CastTime)
                    || float.IsInfinity(spell.CastTime)
                    || spell.CastTime <= 0f)
                {
                    throw new InvalidOperationException(
                        "A non-instant spell cast requires a finite positive cast time.");
                }

                var firstProgressTickID = GetFirstCastProgressTickID();
                var durationTicks = ServerSimulationClock.GetNonShorteningTickCount(
                    spell.CastTime,
                    GameConfigurationManager.Config.ServerSimulationTickDuration);
                var completionTickID = checked(
                    firstProgressTickID + durationTicks - 1L);

                _entityManager.SetComponentData(entity, new Systems.SpellCastComponent
                {
                    SpellID = spell.BaseDefinition.DefinitionID,
                    CastID = castID,
                    FirstProgressTickID = firstProgressTickID,
                    CompletionTickID = completionTickID,
                    TotalCastTime = spell.CastTime,
                    TotalTicks = spell.TotalTicks,
                    TimeSinceLastTick = 0f
                });
                _entityManager.SetComponentEnabled<Systems.SpellCastComponent>(entity, true);

                caster.GetComponent<ActorSpellcaster>()
                      ?.Observers_StartSpellCast(spell.BaseDefinition.DefinitionID, spell.CastTime);

                Debug.Log($"Spell cast started: {caster.Name} casting '{spell.BaseDefinition.DisplayName}' ({spell.CastTime}s)");
            }

            // Trigger GCD for casted spells (at cast start).
            if (spell.BaseDefinition.TriggersGCD)
            {
                ActorEndpoint.onServerStartCooldown?.Invoke(caster.ID, ServerCooldownManager.GCD_ID, GameConfigurationManager.Config.GlobalCooldownDuration);

                if (caster.Owner != null)
                    caster.GetComponent<ActorSpellcaster>()?.Client_StartGlobalCooldown(caster.Owner.Value, GameConfigurationManager.Config.GlobalCooldownDuration);
            }

            // Channelled spells start their individual cooldown immediately.
            if (spell.BaseDefinition.IsChanneled && spell.Cooldown > 0f)
            {
                ActorEndpoint.onServerStartCooldown?.Invoke(caster.ID, spell.BaseDefinition.DefinitionID, spell.Cooldown);

                if (caster.Owner != null)
                    caster.GetComponent<ActorSpellcaster>()?.Client_StartSpellCooldown(caster.Owner.Value, spell.BaseDefinition.DefinitionID, spell.Cooldown);
            }

            // Apply resource costs here.
            var statContainer = caster.GetComponent<ActorStatContainer>();
            foreach (var cost in spell.BaseDefinition.BaseResourceCosts)
            {
                if (cost.CastPhase != SpellCastPhase.OnCastStart) continue;

                var stat = statContainer.GetStat(cost.ResourceType.DefinitionID);
                float newCurrent = Mathf.Max(0f, stat.CurrentValue - cost.Amount);
                statContainer.SetStat(cost.ResourceType.DefinitionID, new StatInstance(
                    stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                    stat.EffectiveMaximum, newCurrent));

                if (cost.ResourceType.ReplicationMode == ActorStatReplicationMode.Observers)
                    statContainer.Observers_ReceiveSnapshot(statContainer.BuildSnapshot(ActorStatReplicationMode.Observers));
                else if (cost.ResourceType.ReplicationMode == ActorStatReplicationMode.Owner)
                    statContainer.Client_ReceiveSnapshot(ctx.Caster.Owner.Value, statContainer.BuildSnapshot(ActorStatReplicationMode.Owner));
            }

            return true;
        }

        private static bool IsGroundTargetInRange(Actor caster, SpellOverride spell, WorldPosition targetPosition)
        {
            if (float.IsNaN(spell.Range)
                || float.IsInfinity(spell.Range)
                || spell.Range < 0f)
            {
                Debug.Warning($"Spell '{spell.BaseDefinition.DefinitionID}' has an invalid effective range ({spell.Range}).");
                return false;
            }

            if (!ServerActorManager.Instance.TryGetAcceptedWorldPosition(caster.ID, out WorldPosition casterPosition))
                throw new InvalidOperationException($"Ground spell caster {caster.ID} has no live accepted world position.");

            double dx = casterPosition.x - targetPosition.x;
            double dy = casterPosition.y - targetPosition.y;
            double dz = casterPosition.z - targetPosition.z;
            double distanceSquared = dx * dx + dy * dy + dz * dz;
            double rangeSquared = (double)spell.Range * spell.Range;
            if (distanceSquared > rangeSquared)
            {
                Debug.Warning(
                    $"Actor {caster.Name} attempted to cast '{spell.BaseDefinition.DefinitionID}' out of ground-target range. Distance squared: {distanceSquared}, Max range: {spell.Range}.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Handles spell cast ticks for channelled spells. 
        /// This is where we apply the tick effects and deduct tick-based resource costs.
        /// </summary>
        /// <param name="actorID"></param>
        /// <param name="spellID"></param>
        private void OnSpellCastTick(Guid actorID, string spellID)
        {
            if (!TryResolveCastEvent(actorID, spellID, out long castID)
                || !TryGetCurrentCast(actorID, spellID, castID, out var ctx))
                return;

            ctx.Phase = SpellCastPhase.OnChannelTick;

            foreach (var component in ctx.Spell.Components)
            {
                if (component.CastPhase == SpellCastPhase.OnChannelTick)
                {
                    ApplySpellEffect(ctx, component, castID);
                    // Cast may have been interrupted by target evaluation
                    if (!IsCurrentCast(actorID, spellID, castID, ctx)) return;
                }
            }

            var statContainer = ctx.Caster.GetComponent<ActorStatContainer>();
            foreach (var cost in ctx.Spell.BaseDefinition.BaseResourceCosts)
            {
                if (cost.CastPhase != SpellCastPhase.OnChannelTick) continue;

                var stat = statContainer.GetStat(cost.ResourceType.DefinitionID);
                float newCurrent = Mathf.Max(0f, stat.CurrentValue - cost.Amount);
                statContainer.SetStat(cost.ResourceType.DefinitionID, new StatInstance(
                    stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                    stat.EffectiveMaximum, newCurrent));

                if (cost.ResourceType.ReplicationMode == ActorStatReplicationMode.Observers)
                    statContainer.Observers_ReceiveSnapshot(statContainer.BuildSnapshot(ActorStatReplicationMode.Observers));
                else if (cost.ResourceType.ReplicationMode == ActorStatReplicationMode.Owner)
                    statContainer.Client_ReceiveSnapshot(ctx.Caster.Owner.Value, statContainer.BuildSnapshot(ActorStatReplicationMode.Owner));
            }

            if (IsCurrentCast(actorID, spellID, castID, ctx))
                ctx.Caster.GetComponent<ActorSpellcaster>()?.Observers_TickSpellCast(spellID);
        }

        /// <summary>
        /// This method allows scheduling of delayed actions related to a spell cast, 
        /// such as delayed damage or buffs that occur after the cast completes. Primarily, this
        /// occurs when a spell has a projectile and when we want the damage to occur when the
        /// impact occurs rather than at cast completion.
        /// </summary>
        /// <param name="delay">The delay in seconds before the action becomes due.</param>
        /// <param name="action">The action to invoke at the authoritative due tick.</param>
        private void OnSpellCastDelayedAction(float delay, Action action)
        {
            if (action == null)
                return;

            float tickDuration = GameConfigurationManager.Config.ServerSimulationTickDuration;
            if (float.IsNaN(delay) || float.IsInfinity(delay) || delay < 0f)
                throw new ArgumentOutOfRangeException(nameof(delay), "A delayed spell action requires a finite, non-negative delay.");

            long delayTicks = ServerSimulationClock.GetNonShorteningTickCount(delay, tickDuration);
            // Even zero-delay work waits for the next simulation boundary; it never runs during
            // the current dispatch.
            delayTicks = Math.Max(1L, delayTicks);
            long dueTickID = checked(ServerSimulationClock.CurrentTickID + delayTicks);

            // ApplySpellEffect or the current spell event supplies the source cast. Without either,
            // actorID remains Guid.Empty and castID remains 0, so actor/cast cancellation cannot match
            // the action; it remains unscoped and can only be canceled during teardown.
            Guid actorID = default;
            long castID = 0;
            if (_hasDelayedActionContext)
            {
                actorID = _delayedActionActorID;
                castID = _delayedActionCastID;
            }
            else if (ServerECSManager.TryGetCurrentSimulationEvent(out var simulationEvent)
                && (simulationEvent.Kind == ServerSimulationEventKind.SpellCastTick
                    || simulationEvent.Kind == ServerSimulationEventKind.SpellCastCompleted)
                && simulationEvent.CastID > 0)
            {
                actorID = simulationEvent.ActorID;
                castID = simulationEvent.CastID;
            }

            PendingAction pendingAction = new(actorID, castID, action);
            pendingAction.Registration = ServerSimulationClock.RegisterDue(
                dueTickID,
                _ => ExecutePendingAction(pendingAction));
            _pendingActions.Add(pendingAction);
        }

        private void ExecutePendingAction(PendingAction pendingAction)
        {
            if (!_pendingActions.Remove(pendingAction))
                return;

            // Remove and cancel before user/effect code to guarantee one delivery and isolate
            // callback-triggered registrations or cancellations from the current dispatch.
            pendingAction.Registration.Cancel();
            pendingAction.Callback.Invoke();
        }

        /// <summary>
        /// Handles spell cast interruption. This can be triggered by the caster (e.g. by moving or using another action that interrupts casting)
        /// or by external factors (e.g. being hit by an interrupting attack). This method is responsible for cleaning up the active spell cast context, disabling the ECS components, refunding resources if applicable, and notifying clients of the interruption.
        /// </summary>
        /// <param name="actorID"></param>
        /// <param name="spellID"></param>
        private void OnSpellCastInterrupted(Guid actorID, string spellID)
        {
            if (!TryResolveInterruptCast(actorID, spellID, out long castID))
                return;

            if (!TryGetCurrentCast(actorID, spellID, castID, out var ctx))
                return;

            CancelPendingActions(actorID, castID);
            if (ServerECSManager.IsMutationBoundaryActive)
            {
                QueuePendingInterrupt(actorID, spellID, castID, ctx);
                return;
            }

            InterruptCastNow(actorID, spellID, castID, ctx);
        }

        private void CompleteDeferredInterrupt(SpellMutation mutation)
        {
            if (!TryGetCurrentCast(mutation.ActorID, mutation.SpellID, mutation.CastID, out var context)
                || context != mutation.Context)
                return;

            InterruptCastNow(mutation.ActorID, mutation.SpellID, mutation.CastID, mutation.Context);
        }

        private void InterruptCastNow(Guid actorID, string spellID, long castID, SpellContext ctx)
        {
            if (_activeCastIDs.TryGetValue(actorID, out long activeCastID))
            {
                if (activeCastID != castID
                    || !_activeSpellCasts.TryGetValue(actorID, out var activeContext)
                    || activeContext != ctx)
                {
                    return;
                }

                CancelPendingActions(actorID, castID);
                InvalidateActiveCast(actorID);
            }

            DisableCastComponent(actorID, castID);

            if (ctx?.Caster == null
                || ServerActorManager.Instance == null
                || !ServerActorManager.Instance.TryGetActor(actorID, out var currentCaster)
                || currentCaster != ctx.Caster)
            {
                return;
            }

            // Refund resources based on RefundOnInterrupt
            // TODO: Once resource cost deduction is implemented, refund here using
            // ctx.Definition.BaseResourceCosts[i].RefundOnInterrupt

            // Notify clients
            ctx.Caster.GetComponent<ActorSpellcaster>()?.Observers_InterruptSpellCast(spellID);
            Debug.Log($"Spell cast interrupted: {ctx.Caster.Name} stopped casting '{spellID}'");
        }


        /// <summary>
        /// Handles the completion of a spell cast. This is where we apply the OnCastEnd effects, start cooldowns, and notify clients that the cast has completed so they can resolve the spell effects and targets.
        /// </summary>
        /// <param name="actorID"></param>
        /// <param name="spellID"></param>
        private void OnSpellCastCompleted(Guid actorID, string spellID)
        {
            if (!TryResolveCastEvent(actorID, spellID, out long castID)
                || !TryGetCurrentCast(actorID, spellID, castID, out var ctx))
                return;

            if (ServerECSManager.TryGetCurrentSimulationEvent(out var currentEvent)
                && (currentEvent.Kind != ServerSimulationEventKind.SpellCastCompleted
                    || currentEvent.ActorID != actorID
                    || currentEvent.CastID != castID))
            {
                QueueCompletionForNextBoundary(actorID, spellID, castID, currentEvent.SourceEntity);
                return;
            }

            if (ServerECSManager.IsMutationBoundaryActive
                && !ServerECSManager.TryGetCurrentSimulationEvent(out currentEvent))
            {
                if (TryFindActorEntity(actorID, out Entity sourceEntity))
                    QueueCompletionForNextBoundary(actorID, spellID, castID, sourceEntity);
                return;
            }

            CompleteCastNow(actorID, spellID, castID, ctx);
        }

        private void CompleteCastNow(Guid actorID, string spellID, long castID, SpellContext ctx)
        {
            if (!TryGetCurrentCast(actorID, spellID, castID, out var activeContext)
                || activeContext != ctx)
            {
                return;
            }

            InvalidateActiveCast(actorID);

            // Set the current cast phase to OnCastEnd.
            ctx.Phase = SpellCastPhase.OnCastEnd;

            // Apply OnCastEnd components using ctx.
            // TODO: Modifier components.
            foreach (var component in ctx.Spell.Components)
            {
                if (component.CastPhase == SpellCastPhase.OnCastEnd)
                    ApplySpellEffect(ctx, component, castID);
            }

            // Start individual spell cooldown if defined (channelled spells already started theirs at cast start).
            if (ctx.Spell.Cooldown > 0f && !ctx.Spell.BaseDefinition.IsChanneled)
            {
                ActorEndpoint.onServerStartCooldown?.Invoke(actorID, spellID, ctx.Spell.Cooldown);

                if (ctx.Caster.Owner != null)
                {
                    var owner = ctx.Caster.Owner.Value;
                    ctx.Caster.GetComponent<ActorSpellcaster>()?.Client_StartSpellCooldown(owner, spellID, ctx.Spell.Cooldown);
                    Debug.Log($"Started cooldown for spell '{spellID}' on actor {ctx.Caster.Name} (ID: {actorID}) with duration {ctx.Spell.Cooldown}s");
                }
            }

            // Apply resource costs.
            var statContainer = ctx.Caster.GetComponent<ActorStatContainer>();
            foreach (var cost in ctx.Spell.BaseDefinition.BaseResourceCosts)
            {
                if (cost.CastPhase != SpellCastPhase.OnCastEnd) continue;

                var stat = statContainer.GetStat(cost.ResourceType.DefinitionID);
                float newCurrent = Mathf.Max(0f, stat.CurrentValue - cost.Amount);
                statContainer.SetStat(cost.ResourceType.DefinitionID, new StatInstance(
                    stat.BaseValue, stat.FlatModifier, stat.PercentModifier,
                    stat.EffectiveMaximum, newCurrent));

                if (cost.ResourceType.ReplicationMode == ActorStatReplicationMode.Observers)
                    statContainer.Observers_ReceiveSnapshot(statContainer.BuildSnapshot(ActorStatReplicationMode.Observers));
                else if (cost.ResourceType.ReplicationMode == ActorStatReplicationMode.Owner)
                    statContainer.Client_ReceiveSnapshot(ctx.Caster.Owner.Value, statContainer.BuildSnapshot(ActorStatReplicationMode.Owner));
            }

            // If it was a weapon skill, add experience to the character's weapon skill.
            if (ctx.Spell.BaseDefinition.Tags.Contains("weapon") && ctx.Caster.TryGetComponent<PlayerEquipment>(out var equipment))
            {
                // This should be based on the player's weapon skill level.
                int experienceGained = 4;

                // We need to get the type of weapon the player has equipped.
                if (equipment.TryGetItem(ItemSlot.MainHand, out var mainHandItem))
                {
                    new CharacterWeaponSkillService((PlayerActor)ctx.Caster, mainHandItem.BaseItem.WeaponType, experienceGained);
                }
                else
                {
                    Debug.Warning($"Player {ctx.Caster.Name} cast a weapon skill without a weapon equipped.");
                }
            }

            // Notify the observers that the cast has completed, 
            // so they can resolve the spell effects and targets.
            ctx.Caster.GetComponent<ActorSpellcaster>()?.Observers_FinishSpellCast(spellID);
            ctx.Caster.GetComponent<ActorEvents>()?.onServerSpellCastComplete?.Invoke(ctx);
            Debug.Log($"Spell cast completed: {ctx.Caster.Name} finished '{spellID}'");
        }

        private bool TryResolveCastEvent(Guid actorID, string spellID, out long castID)
        {
            if (ServerECSManager.TryGetCurrentSimulationEvent(out var simulationEvent))
            {
                if ((simulationEvent.Kind != ServerSimulationEventKind.SpellCastTick
                        && simulationEvent.Kind != ServerSimulationEventKind.SpellCastCompleted)
                    || simulationEvent.ActorID != actorID
                    || simulationEvent.DefinitionID.ToString() != spellID
                    || simulationEvent.CastID <= 0)
                {
                    castID = 0;
                    return false;
                }

                castID = simulationEvent.CastID;
                return true;
            }

            return _activeCastIDs.TryGetValue(actorID, out castID) && castID > 0;
        }

        private bool TryResolveInterruptCast(Guid actorID, string spellID, out long castID)
        {
            if (ServerECSManager.TryGetCurrentSimulationEvent(out var simulationEvent)
                && (simulationEvent.Kind == ServerSimulationEventKind.SpellCastTick
                    || simulationEvent.Kind == ServerSimulationEventKind.SpellCastCompleted)
                && simulationEvent.ActorID == actorID
                && simulationEvent.DefinitionID.ToString() == spellID)
            {
                castID = simulationEvent.CastID;
                return castID > 0;
            }

            return _activeCastIDs.TryGetValue(actorID, out castID) && castID > 0;
        }

        private bool TryGetCurrentCast(Guid actorID, string spellID, long castID, out SpellContext context)
        {
            if (!_activeSpellCasts.TryGetValue(actorID, out context)
                || !_activeCastIDs.TryGetValue(actorID, out long activeCastID)
                || activeCastID != castID
                || (_pendingInterruptIDsByActor.TryGetValue(actorID, out long pendingInterruptID)
                    && pendingInterruptID == castID)
                || context?.Spell?.BaseDefinition?.DefinitionID != spellID)
            {
                context = null;
                return false;
            }

            return true;
        }

        private bool IsCurrentCast(Guid actorID, string spellID, long castID, SpellContext context)
        {
            return TryGetCurrentCast(actorID, spellID, castID, out var currentContext)
                && currentContext == context;
        }

        private void QueuePendingInterrupt(Guid actorID, string spellID, long castID, SpellContext context)
        {
            if (_pendingInterruptIDsByActor.ContainsKey(actorID))
                return;

            SpellMutation mutation = new(
                SpellMutationKind.Interrupt,
                actorID,
                null,
                null,
                context,
                spellID,
                castID,
                false,
                default);

            _pendingInterrupts.Add(castID, mutation);
            _pendingInterruptIDsByActor.Add(actorID, castID);
            try
            {
                _pendingMutations.Enqueue(mutation);
            }
            catch
            {
                RemovePendingInterrupt(actorID);
                throw;
            }
        }

        private bool TryTakePendingInterrupt(SpellMutation mutation, out SpellMutation pendingInterrupt)
        {
            if (!_pendingInterrupts.TryGetValue(mutation.CastID, out pendingInterrupt)
                || pendingInterrupt.ActorID != mutation.ActorID
                || pendingInterrupt.Context != mutation.Context
                || pendingInterrupt.SpellID != mutation.SpellID)
            {
                pendingInterrupt = default;
                return false;
            }

            RemovePendingInterrupt(mutation.ActorID);
            return true;
        }

        private void RemovePendingInterrupt(Guid actorID)
        {
            if (!_pendingInterruptIDsByActor.Remove(actorID, out long castID))
                return;

            _pendingInterrupts.Remove(castID);
        }

        private void QueuePendingStart(
            Actor caster,
            SpellOverride spell,
            bool hasWorldPositionTarget,
            WorldPosition worldPositionTarget)
        {
            long castID = checked(++_nextCastID);
            SpellMutation mutation = new(
                SpellMutationKind.Start,
                caster.ID,
                caster,
                spell,
                null,
                null,
                castID,
                hasWorldPositionTarget,
                worldPositionTarget);

            _pendingStarts.Add(castID, mutation);
            if (!_pendingStartIDsByActor.TryGetValue(caster.ID, out var pendingIDs))
            {
                pendingIDs = new List<long>();
                _pendingStartIDsByActor.Add(caster.ID, pendingIDs);
            }

            pendingIDs.Add(castID);
            try
            {
                _pendingMutations.Enqueue(mutation);
            }
            catch
            {
                RemovePendingStart(castID);
                throw;
            }
        }

        private bool TryTakePendingStart(SpellMutation mutation, out SpellMutation pendingStart)
        {
            if (!_pendingStarts.TryGetValue(mutation.CastID, out pendingStart)
                || pendingStart.ActorID != mutation.ActorID
                || pendingStart.Caster != mutation.Caster
                || pendingStart.Spell != mutation.Spell)
            {
                pendingStart = default;
                return false;
            }

            RemovePendingStart(mutation.CastID);
            return true;
        }

        private void RemovePendingStart(long castID)
        {
            if (!_pendingStarts.Remove(castID, out var pendingStart))
                return;

            if (!_pendingStartIDsByActor.TryGetValue(pendingStart.ActorID, out var pendingIDs))
                return;

            pendingIDs.Remove(castID);
            if (pendingIDs.Count == 0)
                _pendingStartIDsByActor.Remove(pendingStart.ActorID);
        }

        private bool CancelPendingStarts(Guid actorID)
        {
            if (!_pendingStartIDsByActor.TryGetValue(actorID, out var pendingIDs))
                return false;

            bool cancelled = false;
            for (int i = pendingIDs.Count - 1; i >= 0; i--)
            {
                long castID = pendingIDs[i];
                if (!_pendingStarts.Remove(castID))
                    continue;

                pendingIDs.RemoveAt(i);
                cancelled = true;
            }

            _pendingStartIDsByActor.Remove(actorID);

            return cancelled;
        }

        private void CancelPendingActions(Guid actorID, long castID = 0)
        {
            // A zero cast ID retires all delayed actions for the actor; a positive ID targets
            // only that cast generation. Successful completion intentionally does not call this.
            for (int i = _pendingActions.Count - 1; i >= 0; i--)
            {
                PendingAction pendingAction = _pendingActions[i];
                if (pendingAction.ActorID != actorID
                    || (castID > 0 && pendingAction.CastID != castID))
                {
                    continue;
                }

                pendingAction.Registration.Cancel();
                _pendingActions.RemoveAt(i);
            }
        }

        private void InvalidateActiveCast(Guid actorID)
        {
            _activeSpellCasts.Remove(actorID);
            _activeCastIDs.Remove(actorID);
        }

        private void DisableCastComponent(Guid actorID, long castID)
        {
            if (castID <= 0
                || !TryFindActorEntity(actorID, out Entity entity))
                return;

            Systems.SpellCastComponent component =
                _entityManager.GetComponentData<Systems.SpellCastComponent>(entity);
            if (component.CastID == castID)
            {
                _entityManager.SetComponentEnabled<Systems.SpellCastComponent>(entity, false);
            }
        }

        private static bool TryFindActorEntity(Guid actorID, out Entity actorEntity)
        {
            return ServerActorManager.Instance.TryGetActorEntity(actorID, out actorEntity);
        }

        private static long GetFirstCastProgressTickID()
        {
            var currentSimulationTickID = ServerECSManager.CurrentSimulationTickID;
            if (currentSimulationTickID > 0)
                return currentSimulationTickID;

            return checked(ServerSimulationClock.CurrentTickID + 1L);
        }

        private void QueueCompletionForNextBoundary(Guid actorID, string spellID, long castID, Entity sourceEntity)
        {
            ServerECSManager.QueueSimulationEvent(
                ServerSimulationEventKind.SpellCastCompleted,
                actorID,
                sourceEntity,
                spellID,
                castID: castID);
        }

        #endregion

        private bool ValidateSingleTargetContinuationRange(
            SpellContext context,
            SpellComponent component)
        {
            if (component.TargetDefinition is not SpellTarget_Single)
                return true;
            if (context.Phase != SpellCastPhase.OnChannelTick
                && context.Phase != SpellCastPhase.OnCastEnd)
            {
                return true;
            }
            if (context.Caster == null
                || context.ActorTarget == null
                || context.Spell == null
                || context.Spell.BaseDefinition == null)
            {
                throw new InvalidOperationException(
                    "Single-target continuation range validation requires a complete spell context.");
            }
            if (!ServerActorManager.Instance.TryGetActor(context.ActorTarget.ID, out Actor currentTarget)
                || !ReferenceEquals(currentTarget, context.ActorTarget)
                || !context.Caster.SpatialContext.IsCompatibleWith(context.ActorTarget.SpatialContext))
            {
                ActorEndpoint.onServerSpellCastInterrupted?.Invoke(
                    context.Caster.ID,
                    context.Spell.BaseDefinition.DefinitionID);
                return false;
            }

            double distance = ServerActorManager.Instance
                .GetAcceptedCenterDistance(
                    context.Caster,
                    context.ActorTarget);
            float maxRange =
                (context.Phase == SpellCastPhase.OnChannelTick
                    ? GameConfigurationManager.Config.SpellCastRangeTickBuffer
                    : GameConfigurationManager.Config.SpellCastRangeEndBuffer)
                + context.Spell.Range;
            if (float.IsNaN(maxRange)
                || float.IsInfinity(maxRange)
                || maxRange < 0f)
            {
                throw new InvalidOperationException(
                    $"Single-target continuation range for spell '{context.Spell.BaseDefinition.DefinitionID}' is invalid.");
            }

            if (distance <= maxRange)
                return true;

            ActorEndpoint.onServerSpellCastInterrupted
                ?.Invoke(
                    context.Caster.ID,
                    context.Spell.BaseDefinition.DefinitionID);
            return false;
        }

        private void ApplySpellEffect(SpellContext ctx, SpellComponent component, long castID)
        {
            if (!ValidateSingleTargetContinuationRange(
                    ctx,
                    component))
            {
                return;
            }

            List<Actor> targets = ResolveSpellTargets(ctx, component.TargetDefinition);
            foreach (var target in targets)
            {
                // Scope source identity across effect execution, including OnCastEnd after the active
                // cast has been invalidated; finally restores any nested scope safely.
                Guid previousActorID = _delayedActionActorID;
                long previousCastID = _delayedActionCastID;
                bool hadDelayedActionContext = _hasDelayedActionContext;
                _delayedActionActorID = ctx.Caster.ID;
                _delayedActionCastID = castID;
                _hasDelayedActionContext = true;
                try
                {
                    component.EffectDefinition.Execute(ctx, target);
                }
                finally
                {
                    _delayedActionActorID = previousActorID;
                    _delayedActionCastID = previousCastID;
                    _hasDelayedActionContext = hadDelayedActionContext;
                }
            }
        }

        /// <summary>Determines whether an actor currently has an active spell cast.</summary>
        /// <param name="actorID">The actor identifier.</param>
        /// <returns><see langword="true"/> when the actor is casting.</returns>
        public bool IsCasting(Guid actorID)
        {
            return _activeSpellCasts.ContainsKey(actorID);
        }

        /// <summary>Gets the remaining authoritative cast time for an actor.</summary>
        /// <param name="actorID">The actor to query.</param>
        /// <returns>The remaining cast time in seconds, or zero when no cast is active.</returns>
        public float GetRemainingCastTime(Guid actorID)
        {
            if (!TryFindActorEntity(actorID, out Entity entity)
                || !_entityManager.IsComponentEnabled<Systems.SpellCastComponent>(entity))
            {
                return 0f;
            }

            var component = _entityManager.GetComponentData<Systems.SpellCastComponent>(entity);
            var completedProgressTicks = Math.Max(
                0L,
                ServerSimulationClock.CurrentTickID - component.FirstProgressTickID);
            var elapsedProgressSeconds = completedProgressTicks
                * (double)GameConfigurationManager.Config.ServerSimulationTickDuration;
            return Mathf.Max(
                0f,
                component.TotalCastTime - (float)elapsedProgressSeconds);
        }

        /// <summary>Attempts to interrupt an actor's active spell cast or cancel a pending start.</summary>
        /// <param name="actorID">The actor identifier.</param>
        /// <returns><see langword="true"/> when an active cast was interrupted or a pending start was cancelled.</returns>
        public bool TryInterruptCast(Guid actorID)
        {
            // This public actor-level cancellation intentionally cancels every pending start first.
            bool cancelledPendingStart = CancelPendingStarts(actorID);
            if (!_activeSpellCasts.TryGetValue(actorID, out var ctx))
                return cancelledPendingStart;

            ActorEndpoint.onServerSpellCastInterrupted?.Invoke(actorID, ctx.Spell.BaseDefinition.DefinitionID);
            return true;
        }
    }
}
