using System;

namespace Game.Server.Networking.Master
{
    internal sealed partial class MasterPersistenceClient
    {
        internal static void ValidateCharacterMutationResponse(
            MasterPersistenceProtocol.CharacterMutationResponse response)
        {
            if (response.Result == MasterPersistenceProtocol.PersistenceResult.Accepted)
            {
                if (!response.CharacterID.HasValue
                    || response.CharacterID.Value == Guid.Empty
                    || !response.Revision.HasValue
                    || response.Revision.Value < 1)
                {
                    throw new InvalidOperationException("The accepted Master character mutation response was contradictory.");
                }

                return;
            }

            if (response.CharacterID.HasValue || response.Revision.HasValue)
                throw new InvalidOperationException("The rejected Master character mutation response contained success fields.");
        }

        internal static void ValidateExistingCharacterMutationResponse(
            MasterPersistenceProtocol.CharacterMutationResponse response,
            Guid requestedCharacterID)
        {
            ValidateCharacterMutationResponse(response);
            if (response.Result == MasterPersistenceProtocol.PersistenceResult.Accepted
                && response.CharacterID.Value != requestedCharacterID)
            {
                throw new InvalidOperationException("The Master character mutation response had a different character.");
            }
        }

        internal static void ValidateCharacterPersistenceState(
            MasterPersistenceProtocol.CharacterPersistenceState character,
            Guid characterPersistenceLeaseID,
            long revision,
            Guid expectedAccountID,
            Guid expectedCharacterID)
        {
            if (characterPersistenceLeaseID == Guid.Empty
                || revision < 1
                || character == null
                || character.AccountID != expectedAccountID
                || character.CharacterID != expectedCharacterID
                || character.Revision != revision
                || character.Resources == null
                || character.Cooldowns == null
                || character.PersistentAuras == null
                || character.Abilities == null
                || character.Talents == null
                || character.WeaponSkills == null
                || character.Reputations == null
                || character.Quests == null
                || character.Items == null
                || !HasValidWorldState(character.WorldState))
            {
                throw new InvalidOperationException("The accepted Master character persistence state was contradictory.");
            }
        }

        private static bool HasValidWorldState(MasterPersistenceProtocol.CharacterWorldState worldState)
        {
            return worldState == null
                || (worldState.WorldID != Guid.Empty
                    && (!worldState.InstanceID.HasValue || worldState.InstanceID.Value != Guid.Empty));
        }
    }
}
