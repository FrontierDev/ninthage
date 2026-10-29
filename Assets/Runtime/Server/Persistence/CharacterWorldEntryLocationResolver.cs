using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Server.Core;
using Game.Server.Networking.Master;
using Game.Shared.Data;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Server.Persistence
{
    /// <summary>Identifies the ordered source of a recovered world-entry location.</summary>
    internal enum LocationRecoveryTier
    {
        SavedPrivateRuntime = 1,
        SavedPublicPosition = 2,
        SafeReturnAnchor = 3,
        RegionalFallback = 4,
        RaceStartingLocation = 5
    }

    /// <summary>Resolves and validates a persisted character location for world entry.</summary>
    internal static class CharacterWorldEntryLocationResolver
    {
        /// <summary>Contains one deterministic, validated world-entry recovery candidate.</summary>
        internal readonly struct RecoveryCandidate
        {
            internal RecoveryCandidate(
                CharacterWorldEntryLocation entryLocation,
                LocationRecoveryTier tier,
                string locationDefinitionID,
                bool requiresDurableRelocation)
            {
                EntryLocation = entryLocation;
                Tier = tier;
                LocationDefinitionID = locationDefinitionID;
                RequiresDurableRelocation = requiresDurableRelocation;
            }

            internal CharacterWorldEntryLocation EntryLocation { get; }
            internal LocationRecoveryTier Tier { get; }
            internal string LocationDefinitionID { get; }
            internal bool RequiresDurableRelocation { get; }
        }

        /// <summary>Resolves a persisted location without creating or replacing a runtime instance.</summary>
        /// <param name="saved">The persisted character world state.</param>
        /// <param name="currentPublicContext">The current server public runtime context.</param>
        /// <param name="worldManager">The current server world manager.</param>
        /// <param name="resolved">The validated world-entry location.</param>
        /// <param name="failureReason">The explicit reason resolution failed.</param>
        /// <returns><see langword="true"/> when the saved location is valid and reconnectable.</returns>
        internal static bool TryResolve(
            MasterPersistenceProtocol.CharacterWorldState saved,
            ActorSpatialContext currentPublicContext,
            ServerWorldManager worldManager,
            out CharacterWorldEntryLocation resolved,
            out string failureReason)
        {
            resolved = default;
            failureReason = null;

            if (saved == null)
                return Fail("saved world state is absent", out failureReason);
            if (worldManager == null)
                return Fail("the server world manager is unavailable", out failureReason);
            if (!currentPublicContext.IsValid)
                return Fail("the current public world context is invalid", out failureReason);
            if (currentPublicContext.InstanceID != Guid.Empty)
                return Fail("the current runtime context is not public", out failureReason);
            if (saved.WorldID == Guid.Empty)
                return Fail("saved WorldID is empty", out failureReason);
            if (saved.WorldID != currentPublicContext.WorldID)
            {
                return Fail(
                    $"saved WorldID {saved.WorldID} does not match current WorldID {currentPublicContext.WorldID}",
                    out failureReason);
            }
            if (!StringComparer.Ordinal.Equals(saved.MapID, currentPublicContext.MapID))
            {
                return Fail(
                    $"saved MapID '{saved.MapID}' does not match current MapID '{currentPublicContext.MapID}'",
                    out failureReason);
            }

            WorldPosition position = new(saved.PositionX, saved.PositionY, saved.PositionZ);
            if (!position.IsFinite)
                return Fail("saved world position is not finite", out failureReason);

            Quaternion facing = new(saved.FacingX, saved.FacingY, saved.FacingZ, saved.FacingW);
            double facingLengthSquared = (double)facing.x * facing.x
                + (double)facing.y * facing.y
                + (double)facing.z * facing.z
                + (double)facing.w * facing.w;
            if (float.IsNaN(facing.x)
                || float.IsInfinity(facing.x)
                || float.IsNaN(facing.y)
                || float.IsInfinity(facing.y)
                || float.IsNaN(facing.z)
                || float.IsInfinity(facing.z)
                || float.IsNaN(facing.w)
                || float.IsInfinity(facing.w)
                || Math.Abs(facingLengthSquared - 1d) > 0.001d)
            {
                return Fail("saved facing is not a finite unit quaternion", out failureReason);
            }

            ActorSpatialContext context = currentPublicContext;
            if (saved.InstanceID.HasValue)
            {
                if (saved.InstanceID.Value == Guid.Empty)
                    return Fail("saved InstanceID is empty", out failureReason);

                try
                {
                    context = ActorSpatialContext.CreateInstanced(
                        saved.WorldID,
                        saved.MapID,
                        saved.InstanceID.Value);
                }
                catch (ArgumentException exception)
                {
                    return Fail($"saved instance context is invalid: {exception.Message}", out failureReason);
                }

                if (!worldManager.TryGetTransferRuntime(context, out ServerWorldManager.TransferRuntime runtime)
                    || runtime == null
                    || runtime.Context != context)
                {
                    return Fail(
                        $"saved instance {saved.InstanceID.Value} is not a live runtime on this world server",
                        out failureReason);
                }
            }

            try
            {
                resolved = new CharacterWorldEntryLocation(context, position, facing);
                return true;
            }
            catch (ArgumentException exception)
            {
                return Fail(exception.Message, out failureReason);
            }
        }

        /// <summary>
        /// Builds the ordered recovery candidates for an acquired character without loading scenes or changing state.
        /// </summary>
        /// <param name="state">The raw character state acquired from Master.</param>
        /// <param name="currentPublicContext">The current server public runtime context.</param>
        /// <param name="worldManager">The current server world manager.</param>
        /// <returns>Deterministically ordered candidates; invalid candidates are omitted.</returns>
        internal static IReadOnlyList<RecoveryCandidate> BuildCandidates(
            MasterPersistenceProtocol.CharacterPersistenceState state,
            ActorSpatialContext currentPublicContext,
            ServerWorldManager worldManager)
        {
            List<RecoveryCandidate> candidates = new();
            if (state == null || state.WorldState == null)
            {
                LogSkippedCandidate("saved world state is absent");
                return candidates;
            }
            if (worldManager == null)
            {
                LogSkippedCandidate("the server world manager is unavailable");
                return candidates;
            }
            if (!currentPublicContext.IsValid || currentPublicContext.InstanceID != Guid.Empty)
            {
                LogSkippedCandidate("the current runtime context is not a valid public context");
                return candidates;
            }

            MasterPersistenceProtocol.CharacterWorldState saved = state.WorldState;
            bool savedPrivate = saved.InstanceID.HasValue;

            // TransferRuntime has no character/group admission authority. Reusing an instance without that
            // decision would make an unrelated runtime appear reconnectable, so tier 1 is intentionally skipped.
            if (savedPrivate)
                LogSkippedCandidate("saved private runtime has no character-scope admission authority");

            string savedPublicFailure = null;
            CharacterWorldEntryLocation savedPublicLocation = default;
            bool savedPublicResolved = !savedPrivate
                && TryResolve(
                    saved,
                    currentPublicContext,
                    worldManager,
                    out savedPublicLocation,
                    out savedPublicFailure);
            if (savedPublicResolved)
            {
                candidates.Add(
                    new RecoveryCandidate(
                        savedPublicLocation,
                        LocationRecoveryTier.SavedPublicPosition,
                        null,
                        false));
            }
            else if (!savedPrivate)
            {
                LogSkippedCandidate($"saved public position: {savedPublicFailure}");
            }

            WorldLocationDefinition safeReturnDefinition = null;
            WorldLocationDefinitionLibrary locationLibrary = WorldLocationDefinitionLibrary.Instance;
            if (locationLibrary != null
                && locationLibrary.ValidateLibrary()
                && !string.IsNullOrWhiteSpace(saved.SafeReturnLocationID))
            {
                foreach (WorldLocationDefinition definition in locationLibrary.GetAllDefinitions())
                {
                    if (definition != null
                        && StringComparer.Ordinal.Equals(
                            definition.DefinitionID,
                            saved.SafeReturnLocationID))
                    {
                        safeReturnDefinition = definition;
                        break;
                    }
                }
            }

            if (TryResolveAuthoredLocation(
                safeReturnDefinition,
                WorldLocationPurpose.SafeReturn,
                currentPublicContext,
                out CharacterWorldEntryLocation safeReturnLocation,
                out string safeReturnFailure))
            {
                candidates.Add(
                    new RecoveryCandidate(
                        safeReturnLocation,
                        LocationRecoveryTier.SafeReturnAnchor,
                        saved.SafeReturnLocationID,
                        true));
            }
            else
            {
                LogSkippedCandidate($"safe-return anchor: {safeReturnFailure}");
            }

            AddRegionalFallbackCandidates(
                saved,
                currentPublicContext,
                candidates);
            AddRaceStartingCandidate(state, currentPublicContext, candidates);
            return candidates;
        }

        /// <summary>Resolves an authored location for one required purpose and public context.</summary>
        internal static bool TryResolveAuthoredLocation(
            WorldLocationDefinition location,
            WorldLocationPurpose requiredPurpose,
            ActorSpatialContext currentPublicContext,
            out CharacterWorldEntryLocation resolved,
            out string failureReason)
        {
            resolved = default;
            failureReason = null;

            if (location == null)
                return Fail("location definition is absent", out failureReason);
            if (!location.IsValid())
            {
                return Fail(
                    $"location '{location.DefinitionID}' is invalid",
                    out failureReason);
            }
            if (requiredPurpose != WorldLocationPurpose.Starting
                && requiredPurpose != WorldLocationPurpose.SafeReturn
                && requiredPurpose != WorldLocationPurpose.RegionalFallback)
            {
                return Fail(
                    $"location purpose '{requiredPurpose}' is not one known nonzero purpose",
                    out failureReason);
            }
            if ((location.Purposes & requiredPurpose) == 0)
            {
                return Fail(
                    $"location '{location.DefinitionID}' does not have purpose {requiredPurpose}",
                    out failureReason);
            }
            if (!currentPublicContext.IsValid || currentPublicContext.InstanceID != Guid.Empty)
                return Fail("the current context is not a valid public context", out failureReason);
            if (!StringComparer.Ordinal.Equals(location.MapID, currentPublicContext.MapID))
            {
                return Fail(
                    $"location '{location.DefinitionID}' targets MapID '{location.MapID}', "
                    + $"not current MapID '{currentPublicContext.MapID}'",
                    out failureReason);
            }

            if (!FitsWorldPositionCoordinate(location.PositionX)
                || !FitsWorldPositionCoordinate(location.PositionY)
                || !FitsWorldPositionCoordinate(location.PositionZ))
            {
                return Fail(
                    $"location '{location.DefinitionID}' has coordinates that cannot be represented safely",
                    out failureReason);
            }

            double facingX = location.FacingX;
            double facingY = location.FacingY;
            double facingZ = location.FacingZ;
            double facingW = location.FacingW;
            double facingSquaredNorm = facingX * facingX
                + facingY * facingY
                + facingZ * facingZ
                + facingW * facingW;
            if (!IsFinite(facingSquaredNorm)
                || Math.Abs(facingSquaredNorm - 1d) > 0.001d
                || facingSquaredNorm <= 0d)
            {
                return Fail(
                    $"location '{location.DefinitionID}' has an invalid facing quaternion",
                    out failureReason);
            }

            double facingNorm = Math.Sqrt(facingSquaredNorm);
            Quaternion normalizedFacing = new(
                (float)(facingX / facingNorm),
                (float)(facingY / facingNorm),
                (float)(facingZ / facingNorm),
                (float)(facingW / facingNorm));
            if (!IsFinite(normalizedFacing))
            {
                return Fail(
                    $"location '{location.DefinitionID}' has a facing quaternion that cannot be normalized safely",
                    out failureReason);
            }

            WorldPosition position = new(location.PositionX, location.PositionY, location.PositionZ);
            try
            {
                resolved = new CharacterWorldEntryLocation(
                    currentPublicContext,
                    position,
                    normalizedFacing);
                return true;
            }
            catch (ArgumentException exception)
            {
                return Fail(
                    $"location '{location.DefinitionID}' has invalid spatial data: {exception.Message}",
                    out failureReason);
            }
        }

        private static void AddRegionalFallbackCandidates(
            MasterPersistenceProtocol.CharacterWorldState saved,
            ActorSpatialContext currentPublicContext,
            List<RecoveryCandidate> candidates)
        {
            if (saved.InstanceID.HasValue
                || saved.WorldID != currentPublicContext.WorldID
                || !StringComparer.Ordinal.Equals(saved.MapID, currentPublicContext.MapID)
                || !IsFinite(saved.PositionX)
                || !IsFinite(saved.PositionZ))
            {
                LogSkippedCandidate("regional fallback requires a valid saved public position");
                return;
            }

            WorldLocationDefinitionLibrary library = WorldLocationDefinitionLibrary.Instance;
            if (library == null || !library.ValidateLibrary())
            {
                LogSkippedCandidate("regional fallback requires a valid world-location definition library");
                return;
            }

            List<RegionalCandidate> regionalCandidates = new();
            foreach (WorldLocationDefinition definition in library.GetAllDefinitions())
            {
                if (definition == null
                    || (definition.Purposes & WorldLocationPurpose.RegionalFallback) == 0
                    || !StringComparer.Ordinal.Equals(definition.MapID, currentPublicContext.MapID))
                {
                    continue;
                }

                double deltaX = definition.PositionX - saved.PositionX;
                double deltaZ = definition.PositionZ - saved.PositionZ;
                double distanceSquared = deltaX * deltaX + deltaZ * deltaZ;
                double radiusSquared = definition.RegionalFallbackRadius * definition.RegionalFallbackRadius;
                if (!IsFinite(distanceSquared)
                    || !IsFinite(radiusSquared)
                    || distanceSquared > radiusSquared)
                {
                    continue;
                }

                if (!TryResolveAuthoredLocation(
                    definition,
                    WorldLocationPurpose.RegionalFallback,
                    currentPublicContext,
                    out CharacterWorldEntryLocation entryLocation,
                    out string failureReason))
                {
                    LogSkippedCandidate($"regional fallback '{definition.DefinitionID}': {failureReason}");
                    continue;
                }

                regionalCandidates.Add(
                    new RegionalCandidate(
                        entryLocation,
                        definition.DefinitionID,
                        distanceSquared));
            }

            regionalCandidates.Sort(
                (first, second) =>
                {
                    int distanceComparison = first.DistanceSquared.CompareTo(second.DistanceSquared);
                    return distanceComparison != 0
                        ? distanceComparison
                        : StringComparer.Ordinal.Compare(first.LocationDefinitionID, second.LocationDefinitionID);
                });

            foreach (RegionalCandidate regionalCandidate in regionalCandidates)
            {
                candidates.Add(
                    new RecoveryCandidate(
                        regionalCandidate.EntryLocation,
                        LocationRecoveryTier.RegionalFallback,
                        regionalCandidate.LocationDefinitionID,
                        true));
            }
        }

        private static void AddRaceStartingCandidate(
            MasterPersistenceProtocol.CharacterPersistenceState state,
            ActorSpatialContext currentPublicContext,
            List<RecoveryCandidate> candidates)
        {
            RaceDefinitionLibrary library = RaceDefinitionLibrary.Instance;
            if (library == null || !library.ValidateLibrary())
            {
                LogSkippedCandidate("race starting location requires a valid race definition library");
                return;
            }

            RaceDefinition race = library.GetDefinition(state.RaceDefinitionID);
            if (race == null)
            {
                LogSkippedCandidate($"race definition '{state.RaceDefinitionID}' was not found");
                return;
            }
            if (!race.IsPlayable || string.IsNullOrWhiteSpace(race.StartingLocationID))
            {
                LogSkippedCandidate(
                    $"race '{race.DefinitionID}' has no valid playable starting location");
                return;
            }

            WorldLocationDefinition startingDefinition = null;
            WorldLocationDefinitionLibrary locationLibrary = WorldLocationDefinitionLibrary.Instance;
            if (locationLibrary != null && locationLibrary.ValidateLibrary())
            {
                foreach (WorldLocationDefinition definition in locationLibrary.GetAllDefinitions())
                {
                    if (definition != null
                        && StringComparer.Ordinal.Equals(
                            definition.DefinitionID,
                            race.StartingLocationID))
                    {
                        startingDefinition = definition;
                        break;
                    }
                }
            }

            if (!TryResolveAuthoredLocation(
                startingDefinition,
                WorldLocationPurpose.Starting,
                currentPublicContext,
                out CharacterWorldEntryLocation entryLocation,
                out string failureReason))
            {
                LogSkippedCandidate($"race starting location '{race.StartingLocationID}': {failureReason}");
                return;
            }

            candidates.Add(
                new RecoveryCandidate(
                    entryLocation,
                    LocationRecoveryTier.RaceStartingLocation,
                    race.StartingLocationID,
                    true));
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsFinite(Quaternion value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
                && !float.IsNaN(value.z) && !float.IsInfinity(value.z)
                && !float.IsNaN(value.w) && !float.IsInfinity(value.w);
        }

        private static bool FitsWorldPositionCoordinate(double value)
        {
            return IsFinite(value) && value >= -float.MaxValue && value <= float.MaxValue;
        }

        private static void LogSkippedCandidate(string reason)
        {
            Debug.Warning($"[WorldEntryRecovery] Skipped candidate: {reason}.");
        }

        private readonly struct RegionalCandidate
        {
            internal RegionalCandidate(
                CharacterWorldEntryLocation entryLocation,
                string locationDefinitionID,
                double distanceSquared)
            {
                EntryLocation = entryLocation;
                LocationDefinitionID = locationDefinitionID;
                DistanceSquared = distanceSquared;
            }

            internal CharacterWorldEntryLocation EntryLocation { get; }
            internal string LocationDefinitionID { get; }
            internal double DistanceSquared { get; }
        }

        private static bool Fail(string reason, out string failureReason)
        {
            failureReason = reason;
            return false;
        }
    }
}
