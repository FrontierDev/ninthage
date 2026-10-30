using Game.Server.Core;
using Game.Server.Networking;
using Game.Server.Networking.Master;
using Game.Shared.Data;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Server.Persistence
{
    /// <summary>Captures one validated checkpoint from a live authoritative player actor.</summary>
    internal static class CharacterLiveCheckpointCapture
    {
        private const string STAT_HEALTH = "health";

        /// <summary>
        /// Captures the current authoritative actor state for persistence.
        /// The caller must invoke this synchronously on the Unity server main thread.
        /// </summary>
        /// <param name="session">The online character session to capture.</param>
        /// <param name="capturedAtUtc">The single canonical UTC observation for this capture.</param>
        /// <param name="checkpoint">Receives the immutable checkpoint when capture succeeds.</param>
        /// <param name="failureReason">Receives the reason capture failed.</param>
        /// <returns><see langword="true"/> when a complete checkpoint was captured.</returns>
        internal static bool TryCapture(
            PlayerSessionData session,
            DateTimeOffset capturedAtUtc,
            out WorldPersistenceCheckpoint checkpoint,
            out string failureReason)
        {
            checkpoint = null;
            failureReason = null;

            if (session == null)
                return Fail("the player session is absent", out failureReason);
            if (!(session.PlayerActor is PlayerActor actor))
                return Fail("the session has no live PlayerActor", out failureReason);
            if (session.AccountID == Guid.Empty)
                return Fail("the session AccountID is empty", out failureReason);
            if (session.CharacterID == Guid.Empty)
                return Fail("the session CharacterID is empty", out failureReason);
            if (capturedAtUtc.Offset != TimeSpan.Zero)
                return Fail("the capture timestamp is not canonical UTC", out failureReason);

            ServerTimeAuthority timeAuthority = ServerInitialization.TimeAuthority;
            if (timeAuthority == null)
                return Fail("the server time authority is unavailable", out failureReason);

            WorldPersistenceCoordinator coordinator;
            try
            {
                coordinator = ServerInitialization.PersistenceCoordinator;
            }
            catch (InvalidOperationException exception)
            {
                return Fail($"the persistence coordinator is unavailable: {exception.Message}", out failureReason);
            }

            if (coordinator.WorldID == Guid.Empty)
                return Fail("the persistence coordinator WorldID is empty", out failureReason);

            ActorSpatialContext context = actor.SpatialContext;
            if (!context.IsValid)
                return Fail("the player actor spatial context is invalid", out failureReason);
            if (context.WorldID != coordinator.WorldID)
            {
                return Fail(
                    $"the player actor WorldID {context.WorldID} does not match persistence WorldID {coordinator.WorldID}",
                    out failureReason);
            }
            if (string.IsNullOrWhiteSpace(context.MapID))
                return Fail("the player actor MapID is invalid", out failureReason);

            ServerActorManager actorManager = ServerActorManager.Instance;
            if (actorManager == null)
                return Fail("the server actor manager is unavailable", out failureReason);
            if (!actorManager.TryGetAcceptedWorldPosition(actor.ID, out WorldPosition position))
                return Fail("the player actor has no live accepted ECS world position", out failureReason);
            if (!position.IsFinite)
                return Fail("the accepted ECS world position is not finite", out failureReason);

            Quaternion rawFacing = actor.transform.rotation;
            if (!TryNormalizeFacing(rawFacing, out Quaternion facing))
                return Fail("the player actor facing is not a finite quaternion with positive norm", out failureReason);

            ActorStatContainer statContainer = actor.GetComponent<ActorStatContainer>();
            if (statContainer == null)
                return Fail("the player actor has no stat container", out failureReason);

            if (!TryCaptureResources(
                statContainer,
                capturedAtUtc,
                out WorldPersistenceCheckpoint.Resource[] resources,
                out bool hasHealth,
                out failureReason))
            {
                return false;
            }

            if (!hasHealth)
                return Fail("the player actor has no health resource", out failureReason);

            Guid? instanceID = context.InstanceID == Guid.Empty
                ? null
                : context.InstanceID;
            WorldPersistenceCheckpoint.WorldStateSnapshot worldState =
                new(
                    context.MapID,
                    instanceID,
                    position.x,
                    position.y,
                    position.z,
                    facing.x,
                    facing.y,
                    facing.z,
                    facing.w,
                    session.CurrentSafeReturnLocationID,
                    capturedAtUtc);

            checkpoint = new WorldPersistenceCheckpoint(worldState, resources, actor.isDead);
            return true;
        }

        private static bool TryCaptureResources(
            ActorStatContainer statContainer,
            DateTimeOffset capturedAtUtc,
            out WorldPersistenceCheckpoint.Resource[] resources,
            out bool hasHealth,
            out string failureReason)
        {
            resources = null;
            hasHealth = false;
            failureReason = null;

            ActorStatDefinitionLibrary library = ActorStatDefinitionLibrary.Instance;
            if (library == null)
                return Fail("the actor stat definition library is unavailable", out failureReason);

            List<WorldPersistenceCheckpoint.Resource> capturedResources = new();
            HashSet<string> resourceIDs = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, StatInstance> entry in statContainer.All)
            {
                if (string.IsNullOrWhiteSpace(entry.Key))
                    return Fail("the player actor contains a stat with an invalid ID", out failureReason);

                ActorStatDefinition definition = library.GetDefinition(entry.Key);
                if (definition == null)
                {
                    return Fail(
                        $"the player actor references unknown stat definition '{entry.Key}'",
                        out failureReason);
                }
                if (definition.Category != ActorStatCategory.Resource)
                    continue;

                if (!resourceIDs.Add(entry.Key))
                    return Fail(
                        $"the player actor contains duplicate resource '{entry.Key}'",
                        out failureReason);
                if (entry.Value == null)
                    return Fail(
                        $"the player actor resource '{entry.Key}' has no stat instance",
                        out failureReason);

                float effectiveMaximum = entry.Value.EffectiveMaximum;
                float currentValue = entry.Value.CurrentValue;
                if (float.IsNaN(effectiveMaximum)
                    || float.IsInfinity(effectiveMaximum)
                    || effectiveMaximum < 0f)
                {
                    return Fail(
                        $"the player actor resource '{entry.Key}' has an invalid effective maximum",
                        out failureReason);
                }
                if (float.IsNaN(currentValue)
                    || float.IsInfinity(currentValue)
                    || currentValue < 0f
                    || currentValue > effectiveMaximum)
                {
                    return Fail(
                        $"the player actor resource '{entry.Key}' has an invalid current value",
                        out failureReason);
                }

                capturedResources.Add(
                    new WorldPersistenceCheckpoint.Resource(
                        entry.Key,
                        currentValue,
                        capturedAtUtc));
                if (StringComparer.Ordinal.Equals(entry.Key, STAT_HEALTH))
                    hasHealth = true;
            }

            capturedResources.Sort(
                (first, second) => StringComparer.Ordinal.Compare(
                    first.ResourceDefinitionID,
                    second.ResourceDefinitionID));
            resources = capturedResources.ToArray();
            return true;
        }

        private static bool TryNormalizeFacing(
            Quaternion rawFacing,
            out Quaternion normalizedFacing)
        {
            normalizedFacing = default;
            if (float.IsNaN(rawFacing.x)
                || float.IsInfinity(rawFacing.x)
                || float.IsNaN(rawFacing.y)
                || float.IsInfinity(rawFacing.y)
                || float.IsNaN(rawFacing.z)
                || float.IsInfinity(rawFacing.z)
                || float.IsNaN(rawFacing.w)
                || float.IsInfinity(rawFacing.w))
            {
                return false;
            }

            double x = rawFacing.x;
            double y = rawFacing.y;
            double z = rawFacing.z;
            double w = rawFacing.w;
            double squaredNorm = x * x + y * y + z * z + w * w;
            if (double.IsNaN(squaredNorm)
                || double.IsInfinity(squaredNorm)
                || squaredNorm <= 1e-8d)
            {
                return false;
            }

            double norm = Math.Sqrt(squaredNorm);
            normalizedFacing = new Quaternion(
                (float)(x / norm),
                (float)(y / norm),
                (float)(z / norm),
                (float)(w / norm));
            return !(float.IsNaN(normalizedFacing.x)
                || float.IsInfinity(normalizedFacing.x)
                || float.IsNaN(normalizedFacing.y)
                || float.IsInfinity(normalizedFacing.y)
                || float.IsNaN(normalizedFacing.z)
                || float.IsInfinity(normalizedFacing.z)
                || float.IsNaN(normalizedFacing.w)
                || float.IsInfinity(normalizedFacing.w));
        }

        private static bool Fail(string reason, out string failureReason)
        {
            failureReason = reason;
            return false;
        }
    }
}
