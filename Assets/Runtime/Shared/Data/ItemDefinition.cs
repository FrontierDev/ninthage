using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>
    /// Defines the equipment slot(s) that an item can be equipped in.
    /// Items can be equipped in multiple slots (e.g., ring 1, ring 2).
    /// </summary>
    public enum ItemSlot
    {
        None = 0,
        Head = 1,
        Shoulders = 2,
        Chest = 3,
        Tabard = 4,
        Hands = 5,
        Legs = 6,
        Feet = 7,
        Finger = 8,
        Trinket = 9,
        Cloak = 10,
        MainHand = 11,
        OffHand = 12,
    }

    /// <summary>
    /// Defines the type/category of an item, which can affect how it's used and what stats it has.
    /// </summary>
    public enum ItemType
    {
        None = 0,
        Weapon = 1,
        Armor = 2,
        Consumable = 3,
        Material = 4
    }

    /// <summary>
    /// Defines the weapon type for Weapon (ItemType == 1) items, which can affect what stats 
    /// they have and what skills can use them.
    /// </summary>
    public enum ItemWeaponType
    {
        None = 0,
        Sword = 1,
        Axe = 2,
        Mace = 3,
        Dagger = 4,
        Staff = 5,
        Bow = 6,
        Crossbow = 7,
        Gun = 8
    }

    /// <summary>
    /// Weight class for Armor (ItemType == 2) items.
    /// </summary>
    public enum ItemArmorWeightClass
    {
        Cosmetic = 0,
        Light = 1,
        Medium = 2,
        Heavy = 3
    }

    /// <summary>
    /// Flags that define the uniqueness of an item.
    /// </summary>
    public enum ItemUniqueFlag
    {
        None = 0,
        UniqueEquipped = 1,  // Only one instance can be equipped at a time
        UniqueOwned = 2      // Only one instance can be owned at a time (including inventory, bank, etc.)
    }

    /// <summary>
    /// Flags that define the binding behavior of an item (e.g., when it becomes soulbound).
    /// </summary>
    public enum ItemBindingFlag
    {
        None = 0,
        BindOnPickup = 1,    // Item becomes soulbound when picked up
        BindOnEquip = 2,      // Item becomes soulbound when equipped
        BindOnUse = 3,       // Item becomes soulbound when used (e.g., right-click in inventory)
        QuestItem = 4
    }

    /// <summary>
    /// Defines the quality/rarity of an item, which can affect its color in the UI and other mechanics.
    /// </summary>
    /// </summary>
    public enum ItemQuality
    {
        Poor,
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [System.Serializable]
    public class ItemStat
    {
        [SerializeField] private ActorStatDefinition stat;
        [SerializeField] private float value;

        public ActorStatDefinition Stat => stat;
        public float Value => value;
    }

    /// <summary>
    /// Defines an item that can be equipped in a specific slot.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDefinition_", menuName = "NinthAge/Definitions/Item Definition")]
    public class ItemDefinition : DataDefinition
    {
        [SerializeField] private ItemQuality quality;
        [SerializeField] private Sprite icon;
        [SerializeField] private string description;
        [SerializeField] private string itemSetKey;

        [SerializeField] private ItemUniqueFlag uniqueFlag = ItemUniqueFlag.None;
        [SerializeField] private ItemBindingFlag bindingFlag = ItemBindingFlag.None;
        [SerializeField] private bool canStack = true;
        [SerializeField] private int maxStackSize = 99;
        [SerializeField] private bool canTrade = true;
        [SerializeField] private bool canSell = true;
        [SerializeField] private int sellPrice = 0;
        [SerializeField] private bool canDisenchant = true;

        [SerializeField] private ItemType itemType = ItemType.None;
        [SerializeField] private ItemWeaponType weaponType = ItemWeaponType.None;
        [SerializeField] private ItemArmorWeightClass armorWeight = ItemArmorWeightClass.Cosmetic;

        [SerializeField] private bool isTwoHanded = false; // For weapons, indicates if it requires two hands to equip
        [SerializeField] private float damagePerSecond = 0; // For weapons, the average damage it deals (used for stat calculations) 
        [SerializeField] private float swingTimer = 1.0f; // For weapons, the time in seconds between attacks (used for stat calculations)
        [SerializeField] private DamageSchoolDefinition damageSchool; // For weapons, the damage school it belongs to (used for stat calculations)

        [SerializeField] private List<ItemSlot> validSlots = new List<ItemSlot>();
        [SerializeField] private List<ItemStat> stats = new List<ItemStat>();
        [SerializeReference] private List<ConditionDefinition> conditions = new List<ConditionDefinition>();

        [SerializeField] private Action onEquipAction;
        [SerializeField] private Action onUnequipAction;

        public ItemQuality Quality => quality;
        public ItemQuality Rarity => quality;
        public Sprite Icon => icon;
        public string Description => description;
        public string ItemSetKey => itemSetKey;

        public ItemType ItemType => itemType;
        public ItemWeaponType WeaponType => weaponType;
        public ItemArmorWeightClass ArmorWeight => armorWeight;

        public bool IsTwoHanded => isTwoHanded;
        public float DamagePerSecond => damagePerSecond;
        public float SwingTimer => swingTimer;
        public DamageSchoolDefinition DamageSchool => damageSchool;

        public ItemUniqueFlag UniqueFlag => uniqueFlag;
        public bool CanStack => canStack;
        public int MaxStackSize => maxStackSize;
        public bool CanTrade => canTrade;
        public bool CanSell => canSell;
        public int SellPrice => sellPrice;
        public bool CanDisenchant => canDisenchant;

        public IReadOnlyList<ItemSlot> ValidSlots => validSlots;
        public IReadOnlyList<ItemStat> Stats => stats;
        public IReadOnlyList<ConditionDefinition> Conditions => conditions;

        protected override string AssetPrefix => "Item";

        /// <summary>
        /// Validates an item for common configuration errors.
        /// Returns true if valid, false if there are validation issues.
        /// </summary>
        public bool Validate()
        {
            // Basic validations
            if (string.IsNullOrEmpty(DisplayName))
                return false;

            if (icon == null)
                return false;

            if (validSlots == null || validSlots.Count == 0)
                return false;

            // Weapon validations
            if (itemType == ItemType.Weapon)
            {
                if (weaponType == ItemWeaponType.None)
                    return false;

                if (damagePerSecond <= 0)
                    return false;

                if (swingTimer <= 0)
                    return false;

                if (damageSchool == null)
                    return false;

                // Weapons shouldn't be stackable
                if (canStack)
                    return false;
            }

            // Armor validations
            if (itemType == ItemType.Armor)
            {
                if (armorWeight == ItemArmorWeightClass.Cosmetic)
                    return false;

                // Armor shouldn't be stackable
                if (canStack)
                    return false;

                // Armor can't be two-handed
                if (isTwoHanded)
                    return false;
            }

            // Consumable/Material validations
            if (itemType == ItemType.Consumable || itemType == ItemType.Material)
            {
                // These should be stackable
                if (!canStack)
                    return false;

                // Non-weapons shouldn't have damage
                if (damagePerSecond > 0)
                    return false;

                // Can't be two-handed
                if (isTwoHanded)
                    return false;
            }

            return true;
        }
    }
}