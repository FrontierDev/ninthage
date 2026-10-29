using Game.Shared.Data;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using System;
using UnityEngine;

namespace Game.Server.Persistence
{
    /// <summary>Resolves a playable race's authored starting location in the current public world.</summary>
    internal static class CharacterStartingLocationResolver
    {
        internal static bool TryResolve(
            RaceDefinition race,
            ActorSpatialContext publicContext,
            out WorldPosition position,
            out Quaternion facing,
            out string failureReason)
        {
            position = default;
            facing = default;
            failureReason = null;

            if (race == null)
                return Fail("race definition is missing", out failureReason);
            if (!race.IsPlayable)
                return Fail("race definition is not playable", out failureReason);
            if (string.IsNullOrWhiteSpace(race.StartingLocationID))
                return Fail("race definition has no StartingLocationID", out failureReason);
            if (!publicContext.IsValid || publicContext.InstanceID != Guid.Empty)
                return Fail("the current public world context is invalid", out failureReason);

            WorldLocationDefinitionLibrary locationLibrary = WorldLocationDefinitionLibrary.Instance;
            if (locationLibrary == null || !locationLibrary.ValidateLibrary())
                return Fail("world-location definition library is unavailable or invalid", out failureReason);

            WorldLocationDefinition startingLocation = null;
            foreach (WorldLocationDefinition definition in locationLibrary.GetAllDefinitions())
            {
                if (definition != null
                    && StringComparer.Ordinal.Equals(definition.DefinitionID, race.StartingLocationID))
                {
                    startingLocation = definition;
                    break;
                }
            }

            if (!CharacterWorldEntryLocationResolver.TryResolveAuthoredLocation(
                startingLocation,
                WorldLocationPurpose.Starting,
                publicContext,
                out CharacterWorldEntryLocation entryLocation,
                out failureReason))
            {
                return false;
            }

            position = entryLocation.Position;
            facing = entryLocation.Facing;
            return true;
        }

        private static bool Fail(string reason, out string failureReason)
        {
            failureReason = reason;
            return false;
        }
    }
}
