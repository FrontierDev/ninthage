using Game.Server.Networking.Master;
using System;

namespace Game.Server.Persistence
{
    /// <summary>Immutable server-domain input for creating one durable character.</summary>
    internal sealed class CharacterCreationPersistenceCommand
    {
        private readonly InitialResource[] _resources;
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
            InitialReputation[] reputations,
            InitialItem[] items)
        {
            if (resources == null)
                throw new ArgumentNullException(nameof(resources));
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
            _reputations = (InitialReputation[])reputations.Clone();
            _items = (InitialItem[])items.Clone();
        }

        internal Guid AccountID { get; }
        internal string FirstName { get; }
        internal string LastName { get; }
        internal string RaceDefinitionID { get; }
        internal string ClassDefinitionID { get; }
        internal string FactionDefinitionID { get; }
        internal InitialWorldState WorldState { get; }
        internal InitialResource[] Resources => (InitialResource[])_resources.Clone();
        internal InitialReputation[] Reputations => (InitialReputation[])_reputations.Clone();
        internal InitialItem[] Items => (InitialItem[])_items.Clone();

        /// <summary>Initial map and position supplied for the new character.</summary>
        internal sealed class InitialWorldState
        {
            internal InitialWorldState(
                string mapID,
                Guid? instanceID,
                double positionX,
                double positionY,
                double positionZ,
                string safeReturnLocationID)
            {
                MapID = mapID;
                InstanceID = instanceID;
                PositionX = positionX;
                PositionY = positionY;
                PositionZ = positionZ;
                SafeReturnLocationID = safeReturnLocationID;
            }

            internal string MapID { get; }
            internal Guid? InstanceID { get; }
            internal double PositionX { get; }
            internal double PositionY { get; }
            internal double PositionZ { get; }
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
