using Game.Server.Networking.Master;
using Game.Shared.Data;
using System;

namespace Game.Server.Persistence
{
    /// <summary>Immutable server-domain input for creating one durable character.</summary>
    internal sealed class CharacterCreationPersistenceCommand
    {
        private readonly InitialResource[] _resources;
        private readonly InitialAbility[] _abilities;
        private readonly InitialWeaponSkill[] _weaponSkills;
        private readonly InitialReputation[] _reputations;
        private readonly InitialItem[] _items;

        /// <summary>Creates a character creation command from authored persistence values.</summary>
        internal CharacterCreationPersistenceCommand(
            Guid accountID,
            string firstName,
            string lastName,
            string raceDefinitionID,
            string classDefinitionID,
            string factionDefinitionID,
            InitialWorldState worldState,
            InitialResource[] resources,
            InitialAbility[] abilities,
            InitialWeaponSkill[] weaponSkills,
            InitialReputation[] reputations,
            InitialItem[] items)
        {
            if (resources == null)
                throw new ArgumentNullException(nameof(resources));
            if (abilities == null)
                throw new ArgumentNullException(nameof(abilities));
            if (weaponSkills == null)
                throw new ArgumentNullException(nameof(weaponSkills));
            if (reputations == null)
                throw new ArgumentNullException(nameof(reputations));
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            AccountID = accountID;
            FirstName = firstName;
            LastName = lastName;
            RaceDefinitionID = raceDefinitionID;
            ClassDefinitionID = classDefinitionID;
            FactionDefinitionID = factionDefinitionID;
            WorldState = worldState;
            _resources = (InitialResource[])resources.Clone();
            _abilities = (InitialAbility[])abilities.Clone();
            _weaponSkills = (InitialWeaponSkill[])weaponSkills.Clone();
            _reputations = (InitialReputation[])reputations.Clone();
            _items = (InitialItem[])items.Clone();

            foreach (InitialAbility ability in _abilities)
            {
                if (ability == null)
                    throw new ArgumentException("Initial abilities cannot contain null entries.", nameof(abilities));
            }

            foreach (InitialWeaponSkill weaponSkill in _weaponSkills)
            {
                if (weaponSkill == null)
                {
                    throw new ArgumentException(
                        "Initial weapon skills cannot contain null entries.",
                        nameof(weaponSkills));
                }
            }
        }

        internal Guid AccountID { get; }
        internal string FirstName { get; }
        internal string LastName { get; }
        internal string RaceDefinitionID { get; }
        internal string ClassDefinitionID { get; }
        internal string FactionDefinitionID { get; }
        internal InitialWorldState WorldState { get; }
        internal InitialResource[] Resources => (InitialResource[])_resources.Clone();
        internal InitialAbility[] Abilities => (InitialAbility[])_abilities.Clone();
        internal InitialWeaponSkill[] WeaponSkills => (InitialWeaponSkill[])_weaponSkills.Clone();
        internal InitialReputation[] Reputations => (InitialReputation[])_reputations.Clone();
        internal InitialItem[] Items => (InitialItem[])_items.Clone();

        /// <summary>Initial known ability granted to a new character.</summary>
        internal sealed class InitialAbility
        {
            /// <summary>Creates an initial ability grant.</summary>
            internal InitialAbility(string abilityDefinitionID, int rank)
            {
                if (string.IsNullOrWhiteSpace(abilityDefinitionID))
                    throw new ArgumentException("An ability definition ID is required.", nameof(abilityDefinitionID));
                if (rank < 1)
                    throw new ArgumentOutOfRangeException(nameof(rank), "Ability rank must be at least 1.");

                AbilityDefinitionID = abilityDefinitionID;
                Rank = rank;
            }

            /// <summary>Gets the authored ability definition ID.</summary>
            internal string AbilityDefinitionID { get; }

            /// <summary>Gets the initial ability rank.</summary>
            internal int Rank { get; }
        }

        /// <summary>Initial weapon skill granted to a new character.</summary>
        internal sealed class InitialWeaponSkill
        {
            /// <summary>Creates an initial weapon skill grant.</summary>
            internal InitialWeaponSkill(int weaponType, int level, int experience)
            {
                if (!Enum.IsDefined(typeof(ItemWeaponType), weaponType) || weaponType < 0)
                    throw new ArgumentOutOfRangeException(nameof(weaponType), "Weapon type must be a defined nonnegative item weapon type.");
                if (level < 1)
                    throw new ArgumentOutOfRangeException(nameof(level), "Weapon skill level must be at least 1.");
                if (experience < 0)
                    throw new ArgumentOutOfRangeException(nameof(experience), "Weapon skill experience cannot be negative.");

                WeaponType = weaponType;
                Level = level;
                Experience = experience;
            }

            /// <summary>Gets the defined item weapon type value.</summary>
            internal int WeaponType { get; }

            /// <summary>Gets the initial weapon skill level.</summary>
            internal int Level { get; }

            /// <summary>Gets the initial weapon skill experience.</summary>
            internal int Experience { get; }
        }

        /// <summary>Initial map and position supplied for the new character.</summary>
        internal sealed class InitialWorldState
        {
            internal InitialWorldState(
                string mapID,
                Guid? instanceID,
                double positionX,
                double positionY,
                double positionZ,
                float facingX,
                float facingY,
                float facingZ,
                float facingW,
                string safeReturnLocationID)
            {
                MapID = mapID;
                InstanceID = instanceID;
                PositionX = positionX;
                PositionY = positionY;
                PositionZ = positionZ;
                FacingX = facingX;
                FacingY = facingY;
                FacingZ = facingZ;
                FacingW = facingW;
                SafeReturnLocationID = safeReturnLocationID;
            }

            internal string MapID { get; }
            internal Guid? InstanceID { get; }
            internal double PositionX { get; }
            internal double PositionY { get; }
            internal double PositionZ { get; }
            internal float FacingX { get; }
            internal float FacingY { get; }
            internal float FacingZ { get; }
            internal float FacingW { get; }
            internal string SafeReturnLocationID { get; }
        }

        /// <summary>Initial current value for one character resource.</summary>
        internal sealed class InitialResource
        {
            internal InitialResource(string resourceDefinitionID, float currentValue)
            {
                ResourceDefinitionID = resourceDefinitionID;
                CurrentValue = currentValue;
            }

            internal string ResourceDefinitionID { get; }
            internal float CurrentValue { get; }
        }

        /// <summary>Initial reputation value for one character faction.</summary>
        internal sealed class InitialReputation
        {
            internal InitialReputation(string factionDefinitionID, int reputation)
            {
                FactionDefinitionID = factionDefinitionID;
                Reputation = reputation;
            }

            internal string FactionDefinitionID { get; }
            internal int Reputation { get; }
        }

        /// <summary>Initial item state and durable character location.</summary>
        internal sealed class InitialItem
        {
            internal InitialItem(
                string itemDefinitionID,
                int stackSize,
                int? durability,
                string[] modificationIDs,
                MasterPersistenceProtocol.CharacterItemLocationKind locationKind,
                int slotIndex)
            {
                ItemDefinitionID = itemDefinitionID;
                StackSize = stackSize;
                Durability = durability;
                ModificationIDsStorage = modificationIDs == null
                    ? null
                    : (string[])modificationIDs.Clone();
                LocationKind = locationKind;
                SlotIndex = slotIndex;
            }

            internal string ItemDefinitionID { get; }
            internal int StackSize { get; }
            internal int? Durability { get; }
            internal string[] ModificationIDs => ModificationIDsStorage == null
                ? null
                : (string[])ModificationIDsStorage.Clone();
            internal MasterPersistenceProtocol.CharacterItemLocationKind LocationKind { get; }
            internal int SlotIndex { get; }

            private string[] ModificationIDsStorage { get; }
        }
    }
}
