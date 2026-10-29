using Game.Server.Networking.Master;
using Game.Shared.Persistence;
using System;
using System.Collections.Generic;

namespace Game.Server.Persistence
{
    /// <summary>Projects accepted Master character contracts into server compatibility models.</summary>
    internal static class CharacterPersistenceProjection
    {
        internal static List<CharacterData> ToActiveRoster(
            MasterPersistenceProtocol.CharacterRosterResponse response)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));
            if (response.Result != MasterPersistenceProtocol.PersistenceResult.Accepted)
                throw new ArgumentException(
                    $"Cannot project a roster with result {response.Result}.");
            if (response.ActiveCharacters == null)
                throw new InvalidOperationException("An accepted roster must contain active characters.");

            List<CharacterData> roster = new(response.ActiveCharacters.Length);
            foreach (MasterPersistenceProtocol.CharacterRosterEntry entry in response.ActiveCharacters)
            {
                if (entry == null)
                    throw new InvalidOperationException("An accepted roster contains a null character entry.");
                ValidateIdentity(entry.CharacterID, entry.FirstName, entry.LastName, entry.RaceDefinitionID,
                    entry.ClassDefinitionID, entry.FactionDefinitionID);

                roster.Add(new CharacterData(
                    entry.CharacterID.ToString("D"),
                    BuildDisplayName(entry.FirstName, entry.LastName),
                    entry.Level,
                    entry.Experience,
                    entry.Title,
                    entry.ClassDefinitionID,
                    entry.RaceDefinitionID,
                    entry.FactionDefinitionID));
            }

            return roster;
        }

        internal static CharacterData ToCharacterData(
            MasterPersistenceProtocol.CharacterPersistenceState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            ValidateIdentity(state.CharacterID, state.FirstName, state.LastName, state.RaceDefinitionID,
                state.ClassDefinitionID, state.FactionDefinitionID);
            if (state.Experience < 0)
                throw new InvalidOperationException("Character persistence state contains negative experience.");

            HashSet<string> abilityIDs = new(StringComparer.Ordinal);
            HashSet<Guid> itemInstanceIDs = new();
            HashSet<int> inventorySlots = new();
            HashSet<int> equipmentSlots = new();
            HashSet<string> talentIDs = new(StringComparer.Ordinal);
            HashSet<string> reputationIDs = new(StringComparer.Ordinal);
            HashSet<string> questIDs = new(StringComparer.Ordinal);
            HashSet<string> resourceIDs = new(StringComparer.Ordinal);
            HashSet<int> weaponTypes = new();

            CharacterData characterData = new(
                state.CharacterID.ToString("D"),
                BuildDisplayName(state.FirstName, state.LastName),
                state.Level,
                state.Experience,
                state.Title,
                state.ClassDefinitionID,
                state.RaceDefinitionID,
                state.FactionDefinitionID);

            if (state.Abilities == null)
                throw new InvalidOperationException("Character persistence state has no abilities array.");
            foreach (MasterPersistenceProtocol.CharacterAbilityState ability in state.Abilities)
            {
                if (ability == null
                    || string.IsNullOrWhiteSpace(ability.AbilityDefinitionID)
                    || !abilityIDs.Add(ability.AbilityDefinitionID))
                    throw new InvalidOperationException("Character persistence state contains an invalid ability.");
                characterData.KnownSpellIDs.Add(ability.AbilityDefinitionID);
            }

            if (state.Items == null)
                throw new InvalidOperationException("Character persistence state has no items array.");
            foreach (MasterPersistenceProtocol.CharacterItemState item in state.Items)
            {
                if (item == null
                    || item.ItemInstanceID == Guid.Empty
                    || !itemInstanceIDs.Add(item.ItemInstanceID)
                    || string.IsNullOrWhiteSpace(item.ItemDefinitionID)
                    || item.SlotIndex < 0
                    || item.StackSize <= 0)
                    throw new InvalidOperationException("Character persistence state contains an invalid item.");
                if (item.ModificationIDs == null)
                    throw new InvalidOperationException("Character persistence state contains an item without modifications.");

                List<string> modifications = new(item.ModificationIDs.Length);
                foreach (string modificationID in item.ModificationIDs)
                {
                    if (modificationID == null)
                        throw new InvalidOperationException("Character persistence state contains a null modification.");
                    modifications.Add(modificationID);
                }

                switch (item.LocationKind)
                {
                    case MasterPersistenceProtocol.CharacterItemLocationKind.Inventory:
                        if (!inventorySlots.Add(item.SlotIndex))
                            throw new InvalidOperationException("Character persistence state contains duplicate inventory slots.");
                        characterData.SavedInventory.Add(new SavedInventoryEntry
                        {
                            SlotIndex = item.SlotIndex,
                            ItemGUID = item.ItemInstanceID.ToString("D"),
                            ItemID = item.ItemDefinitionID,
                            StackSize = item.StackSize,
                            Durability = item.Durability ?? 0,
                            Modifications = modifications
                        });
                        break;

                    case MasterPersistenceProtocol.CharacterItemLocationKind.Equipment:
                        if (!equipmentSlots.Add(item.SlotIndex))
                            throw new InvalidOperationException("Character persistence state contains duplicate equipment slots.");
                        characterData.SavedEquipment.Add(new SavedEquipmentEntry
                        {
                            SlotIndex = item.SlotIndex,
                            ItemGUID = item.ItemInstanceID.ToString("D"),
                            ItemID = item.ItemDefinitionID,
                            StackSize = item.StackSize,
                            Durability = item.Durability ?? 0,
                            Modifications = modifications
                        });
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Character persistence state contains unsupported item location {item.LocationKind}.");
                }
            }

            if (state.WeaponSkills == null)
                throw new InvalidOperationException("Character persistence state has no weapon skills array.");
            foreach (MasterPersistenceProtocol.CharacterWeaponSkillState weaponSkill in state.WeaponSkills)
            {
                if (weaponSkill == null)
                    throw new InvalidOperationException("Character persistence state contains a null weapon skill.");
                if (!weaponTypes.Add(weaponSkill.WeaponType))
                {
                    throw new InvalidOperationException(
                        $"Character persistence state contains duplicate weapon type {weaponSkill.WeaponType}.");
                }
                characterData.SavedWeaponSkills.Add(new SavedWeaponSkill
                {
                    WeaponType = weaponSkill.WeaponType,
                    Level = weaponSkill.Level,
                    Experience = weaponSkill.Experience
                });
            }

            if (state.Talents == null)
                throw new InvalidOperationException("Character persistence state has no talents array.");
            foreach (MasterPersistenceProtocol.CharacterTalentState talent in state.Talents)
            {
                if (talent == null
                    || string.IsNullOrWhiteSpace(talent.TalentDefinitionID)
                    || !talentIDs.Add(talent.TalentDefinitionID)
                    || talent.Rank <= 0)
                    throw new InvalidOperationException("Character persistence state contains an invalid talent.");
                characterData.SavedTalents.Add(new SavedTalentEntry
                {
                    TalentID = talent.TalentDefinitionID,
                    Rank = talent.Rank
                });
            }

            if (state.Reputations == null)
                throw new InvalidOperationException("Character persistence state has no reputations array.");
            foreach (MasterPersistenceProtocol.CharacterReputationState reputation in state.Reputations)
            {
                if (reputation == null
                    || string.IsNullOrWhiteSpace(reputation.FactionDefinitionID)
                    || !reputationIDs.Add(reputation.FactionDefinitionID))
                    throw new InvalidOperationException("Character persistence state contains an invalid reputation.");
                characterData.SavedReputations.Add(new SavedReputationEntry
                {
                    FactionID = reputation.FactionDefinitionID,
                    Reputation = reputation.Reputation
                });
            }

            if (state.Quests == null)
                throw new InvalidOperationException("Character persistence state has no quests array.");
            foreach (MasterPersistenceProtocol.CharacterQuestState quest in state.Quests)
            {
                if (quest == null
                    || string.IsNullOrWhiteSpace(quest.QuestDefinitionID)
                    || !questIDs.Add(quest.QuestDefinitionID)
                    || quest.Progress == null)
                    throw new InvalidOperationException("Character persistence state contains an invalid quest.");
                foreach (int progress in quest.Progress)
                {
                    if (progress < 0)
                        throw new InvalidOperationException("Character persistence state contains negative quest progress.");
                }
                characterData.SavedQuests.Add(new SavedQuestEntry
                {
                    QuestID = quest.QuestDefinitionID,
                    Progress = (int[])quest.Progress.Clone(),
                    IsCompleted = quest.IsCompleted
                });
            }

            if (state.Resources == null)
                throw new InvalidOperationException("Character persistence state has no resources array.");
            foreach (MasterPersistenceProtocol.CharacterResourceState resource in state.Resources)
            {
                if (resource == null
                    || string.IsNullOrWhiteSpace(resource.ResourceDefinitionID)
                    || !resourceIDs.Add(resource.ResourceDefinitionID))
                    throw new InvalidOperationException("Character persistence state contains an invalid resource.");
                characterData.SavedResources.Add(new SavedResourceEntry
                {
                    StatID = resource.ResourceDefinitionID,
                    Current = resource.CurrentValue
                });
            }

            return characterData;
        }

        private static string BuildDisplayName(string firstName, string lastName)
        {
            return $"{firstName} {lastName}";
        }

        private static void ValidateIdentity(
            Guid characterID,
            string firstName,
            string lastName,
            string raceDefinitionID,
            string classDefinitionID,
            string factionDefinitionID)
        {
            if (characterID == Guid.Empty
                || string.IsNullOrWhiteSpace(firstName)
                || string.IsNullOrWhiteSpace(lastName)
                || string.IsNullOrWhiteSpace(raceDefinitionID)
                || string.IsNullOrWhiteSpace(classDefinitionID)
                || string.IsNullOrWhiteSpace(factionDefinitionID))
            {
                throw new InvalidOperationException("Character persistence state contains incomplete identity data.");
            }
        }
    }
}
