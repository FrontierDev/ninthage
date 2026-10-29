using Game.Server.Networking.Master;
using Game.Shared.Data;
using Game.Shared.Persistence;
using System;

namespace Game.Server.Persistence
{
    /// <summary>Validates persisted character definitions before world entry.</summary>
    internal static class CharacterPersistenceDefinitionValidator
    {
        /// <summary>Validates the authored definitions referenced by an accepted character state.</summary>
        internal static void ValidateForWorldEntry(
            CharacterData data,
            MasterPersistenceProtocol.CharacterPersistenceState state)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (!StringComparer.Ordinal.Equals(data.PVPFactionID, state.FactionDefinitionID))
                throw Invalid(state, "the projected PvP faction does not match the persisted faction");

            RaceDefinitionLibrary raceLibrary = RaceDefinitionLibrary.Instance;
            ClassDefinitionLibrary classLibrary = ClassDefinitionLibrary.Instance;
            FactionDefinitionLibrary factionLibrary = FactionDefinitionLibrary.Instance;
            SpellDefinitionLibrary spellLibrary = SpellDefinitionLibrary.Instance;
            ItemDefinitionLibrary itemLibrary = ItemDefinitionLibrary.Instance;
            TalentDefinitionLibrary talentLibrary = TalentDefinitionLibrary.Instance;
            QuestDefinitionLibrary questLibrary = QuestDefinitionLibrary.Instance;

            RaceDefinition raceDefinition = raceLibrary?.GetDefinition(state.RaceDefinitionID);
            if (raceDefinition == null)
                throw Invalid(state, $"unknown race definition '{state.RaceDefinitionID}'");

            ClassDefinition classDefinition = classLibrary?.GetDefinition(state.ClassDefinitionID);
            if (classDefinition == null)
                throw Invalid(state, $"unknown class definition '{state.ClassDefinitionID}'");
            if (classDefinition.WeaponTypes == null)
                throw Invalid(state, $"class definition '{state.ClassDefinitionID}' has no weapon type list");

            FactionDefinition factionDefinition = factionLibrary?.GetDefinition(state.FactionDefinitionID);
            if (factionDefinition == null)
                throw Invalid(state, $"unknown faction definition '{state.FactionDefinitionID}'");

            if (state.Abilities == null)
                throw Invalid(state, "no abilities array");
            foreach (MasterPersistenceProtocol.CharacterAbilityState ability in state.Abilities)
            {
                if (ability == null || string.IsNullOrWhiteSpace(ability.AbilityDefinitionID))
                    throw Invalid(state, "an ability has no definition ID");
                if (spellLibrary?.GetDefinition(ability.AbilityDefinitionID) == null)
                    throw Invalid(state, $"unknown spell definition '{ability.AbilityDefinitionID}'");
            }

            if (state.Talents == null)
                throw Invalid(state, "no talents array");
            foreach (MasterPersistenceProtocol.CharacterTalentState talent in state.Talents)
            {
                if (talent == null || string.IsNullOrWhiteSpace(talent.TalentDefinitionID))
                    throw Invalid(state, "a talent has no definition ID");
                if (talent.Rank < 1)
                    throw Invalid(state, $"talent '{talent.TalentDefinitionID}' has an invalid rank");
                if (talentLibrary?.GetDefinition(talent.TalentDefinitionID) == null)
                    throw Invalid(state, $"unknown talent definition '{talent.TalentDefinitionID}'");
            }

            if (state.WeaponSkills == null)
                throw Invalid(state, "no weapon skills array");
            foreach (MasterPersistenceProtocol.CharacterWeaponSkillState weaponSkill in state.WeaponSkills)
            {
                if (weaponSkill == null
                    || !Enum.IsDefined(typeof(ItemWeaponType), weaponSkill.WeaponType)
                    || weaponSkill.Level < 1
                    || weaponSkill.Experience < 0)
                {
                    throw Invalid(state, $"weapon skill '{weaponSkill?.WeaponType}' is invalid");
                }

                if (!SupportsWeaponType(classDefinition, (ItemWeaponType)weaponSkill.WeaponType))
                {
                    throw Invalid(
                        state,
                        $"class definition '{state.ClassDefinitionID}' does not support weapon type '{weaponSkill.WeaponType}'");
                }
            }

            if (state.Reputations == null)
                throw Invalid(state, "no reputations array");
            foreach (MasterPersistenceProtocol.CharacterReputationState reputation in state.Reputations)
            {
                if (reputation == null || string.IsNullOrWhiteSpace(reputation.FactionDefinitionID))
                    throw Invalid(state, "a reputation has no faction definition ID");
                if (factionLibrary?.GetDefinition(reputation.FactionDefinitionID) == null)
                    throw Invalid(state, $"unknown reputation faction definition '{reputation.FactionDefinitionID}'");
            }

            if (state.Quests == null)
                throw Invalid(state, "no quests array");
            foreach (MasterPersistenceProtocol.CharacterQuestState quest in state.Quests)
            {
                if (quest == null || string.IsNullOrWhiteSpace(quest.QuestDefinitionID))
                    throw Invalid(state, "a quest has no definition ID");

                QuestDefinition questDefinition = questLibrary?.GetDefinition(quest.QuestDefinitionID);
                if (questDefinition == null)
                    throw Invalid(state, $"unknown quest definition '{quest.QuestDefinitionID}'");
                if (quest.Progress == null
                    || questDefinition.Objectives == null
                    || quest.Progress.Length != questDefinition.Objectives.Count)
                {
                    throw Invalid(
                        state,
                        $"quest '{quest.QuestDefinitionID}' has an invalid progress array");
                }
            }

            if (state.Items == null)
                throw Invalid(state, "no items array");
            foreach (MasterPersistenceProtocol.CharacterItemState item in state.Items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ItemDefinitionID))
                    throw Invalid(state, "an item has no definition ID");

                ItemDefinition itemDefinition = itemLibrary?.GetDefinition(item.ItemDefinitionID);
                if (itemDefinition == null)
                    throw Invalid(state, $"unknown item definition '{item.ItemDefinitionID}'");
                if (item.LocationKind == MasterPersistenceProtocol.CharacterItemLocationKind.Inventory)
                {
                    if (item.SlotIndex < 0)
                        throw Invalid(state, $"inventory item '{item.ItemDefinitionID}' has an invalid slot");
                }
                else if (item.LocationKind == MasterPersistenceProtocol.CharacterItemLocationKind.Equipment)
                {
                    ValidateEquipmentItem(state, item, itemDefinition);
                }
                else
                {
                    throw Invalid(state, $"item '{item.ItemDefinitionID}' has an invalid location");
                }
            }
        }

        private static bool SupportsWeaponType(ClassDefinition classDefinition, ItemWeaponType weaponType)
        {
            foreach (ItemWeaponType supportedWeaponType in classDefinition.WeaponTypes)
            {
                if (supportedWeaponType == weaponType)
                    return true;
            }

            return false;
        }

        private static void ValidateEquipmentItem(
            MasterPersistenceProtocol.CharacterPersistenceState state,
            MasterPersistenceProtocol.CharacterItemState item,
            ItemDefinition itemDefinition)
        {
            if (itemDefinition.ItemType != ItemType.Weapon && itemDefinition.ItemType != ItemType.Armor)
                throw Invalid(state, $"equipment item '{item.ItemDefinitionID}' is not a weapon or armor");
            if (!Enum.IsDefined(typeof(ItemSlot), item.SlotIndex)
                || item.SlotIndex == (int)ItemSlot.None)
            {
                throw Invalid(state, $"equipment item '{item.ItemDefinitionID}' has an invalid slot");
            }
            if (itemDefinition.ValidSlots == null)
                throw Invalid(state, $"equipment item '{item.ItemDefinitionID}' has no valid slots");

            ItemSlot slot = (ItemSlot)item.SlotIndex;
            foreach (ItemSlot validSlot in itemDefinition.ValidSlots)
            {
                if (validSlot == slot)
                    return;
            }

            throw Invalid(
                state,
                $"equipment item '{item.ItemDefinitionID}' cannot be placed in slot '{item.SlotIndex}'");
        }

        private static InvalidOperationException Invalid(
            MasterPersistenceProtocol.CharacterPersistenceState state,
            string reason)
        {
            return new InvalidOperationException($"Character '{state.CharacterID}' {reason}.");
        }
    }
}
