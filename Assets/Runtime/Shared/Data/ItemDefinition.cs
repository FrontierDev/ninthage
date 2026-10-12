using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
    /// <summary>Defines a stat value granted by an item.</summary>
    public class ItemStat
    {
        [SerializeField, FormerlySerializedAs("stat")] private ActorStatDefinition _stat;
        [SerializeField, FormerlySerializedAs("value")] private float _value;

        /// <summary>Gets the stat affected by this entry.</summary>
        public ActorStatDefinition Stat => _stat;
        /// <summary>Gets the stat value granted by this entry.</summary>
        public float Value => _value;
    }

    /// <summary>
    /// Defines an item that can be equipped in a specific slot.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDefinition_", menuName = "NinthAge/Definitions/Item Definition")]
    public class ItemDefinition : DataDefinition
    {
        [SerializeField, FormerlySerializedAs("quality")] private ItemQuality _quality;
        [SerializeField, FormerlySerializedAs("icon")] private Sprite _icon;
        [SerializeField, FormerlySerializedAs("description")] private string _description;
        [SerializeField, FormerlySerializedAs("itemSetKey")] private string _itemSetKey;

        [SerializeField, FormerlySerializedAs("uniqueFlag")] private ItemUniqueFlag _uniqueFlag = ItemUniqueFlag.None;
        [SerializeField, FormerlySerializedAs("bindingFlag")] private ItemBindingFlag _bindingFlag = ItemBindingFlag.None;
        [SerializeField, FormerlySerializedAs("canStack")] private bool _canStack = true;
        [SerializeField, FormerlySerializedAs("maxStackSize")] private int _maxStackSize = 99;
        [SerializeField, FormerlySerializedAs("canTrade")] private bool _canTrade = true;
        [SerializeField, FormerlySerializedAs("canSell")] private bool _canSell = true;
        [SerializeField, FormerlySerializedAs("sellPrice")] private int _sellPrice = 0;
        [SerializeField, FormerlySerializedAs("canDisenchant")] private bool _canDisenchant = true;

        [SerializeField, FormerlySerializedAs("itemType")] private ItemType _itemType = ItemType.None;
        [SerializeField, FormerlySerializedAs("weaponType")] private ItemWeaponType _weaponType = ItemWeaponType.None;
        [SerializeField, FormerlySerializedAs("armorWeight")] private ItemArmorWeightClass _armorWeight = ItemArmorWeightClass.Cosmetic;

        [SerializeField, FormerlySerializedAs("isTwoHanded")] private bool _isTwoHanded = false; // For weapons, indicates if it requires two hands to equip
        [SerializeField, FormerlySerializedAs("damagePerSecond")] private float _damagePerSecond = 0; // For weapons, the average damage it deals (used for stat calculations)
        [SerializeField, FormerlySerializedAs("swingTimer")] private float _swingTimer = 1.0f; // For weapons, the time in seconds between attacks (used for stat calculations)
        [SerializeField, FormerlySerializedAs("damageSchool")] private DamageSchoolDefinition _damageSchool; // For weapons, the damage school it belongs to (used for stat calculations)

        [SerializeField, FormerlySerializedAs("validSlots")] private List<ItemSlot> _validSlots = new();
        [SerializeField, FormerlySerializedAs("stats")] private List<ItemStat> _stats = new();
        [SerializeReference, FormerlySerializedAs("conditions")] private List<ConditionDefinition> _conditions = new();

        [SerializeField, FormerlySerializedAs("onEquipAction")] private Action _onEquipAction;
        [SerializeField, FormerlySerializedAs("onUnequipAction")] private Action _onUnequipAction;

        /// <summary>Gets the item's quality.</summary>
        public ItemQuality Quality => _quality;
        /// <summary>Gets the item's quality using the legacy rarity name.</summary>
        public ItemQuality Rarity => _quality;
        /// <summary>Gets the item's icon.</summary>
        public Sprite Icon => _icon;
        /// <summary>Gets the item's description.</summary>
        public string Description => _description;
        /// <summary>Gets the item set key.</summary>
        public string ItemSetKey => _itemSetKey;

        /// <summary>Gets the item's category.</summary>
        public ItemType ItemType => _itemType;
        /// <summary>Gets the item's weapon type.</summary>
        public ItemWeaponType WeaponType => _weaponType;
        /// <summary>Gets the item's armor weight class.</summary>
        public ItemArmorWeightClass ArmorWeight => _armorWeight;

        /// <summary>Gets whether the item requires two hands.</summary>
        public bool IsTwoHanded => _isTwoHanded;
        /// <summary>Gets the weapon's damage per second.</summary>
        public float DamagePerSecond => _damagePerSecond;
        /// <summary>Gets the weapon's swing timer in seconds.</summary>
        public float SwingTimer => _swingTimer;
        /// <summary>Gets the weapon's damage school.</summary>
        public DamageSchoolDefinition DamageSchool => _damageSchool;

        /// <summary>Gets the item's uniqueness rules.</summary>
        public ItemUniqueFlag UniqueFlag => _uniqueFlag;
        /// <summary>Gets whether the item can stack.</summary>
        public bool CanStack => _canStack;
        /// <summary>Gets the maximum stack size.</summary>
        public int MaxStackSize => _maxStackSize;
        /// <summary>Gets whether the item can be traded.</summary>
        public bool CanTrade => _canTrade;
        /// <summary>Gets whether the item can be sold.</summary>
        public bool CanSell => _canSell;
        /// <summary>Gets the item's sell price.</summary>
        public int SellPrice => _sellPrice;
        /// <summary>Gets whether the item can be disenchanted.</summary>
        public bool CanDisenchant => _canDisenchant;

        /// <summary>Gets the valid equipment slots.</summary>
        public IReadOnlyList<ItemSlot> ValidSlots => _validSlots;
        /// <summary>Gets the item's stat entries.</summary>
        public IReadOnlyList<ItemStat> Stats => _stats;
        /// <summary>Gets the conditions required to use the item.</summary>
        public IReadOnlyList<ConditionDefinition> Conditions => _conditions;

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

            if (_icon == null)
                return false;

            if (_validSlots == null || _validSlots.Count == 0)
                return false;

            // Weapon validations
            if (_itemType == ItemType.Weapon)
            {
                if (_weaponType == ItemWeaponType.None)
                    return false;

                if (_damagePerSecond <= 0)
                    return false;

                if (_swingTimer <= 0)
                    return false;

                if (_damageSchool == null)
                    return false;

                // Weapons shouldn't be stackable
                if (_canStack)
                    return false;
            }

            // Armor validations
            if (_itemType == ItemType.Armor)
            {
                if (_armorWeight == ItemArmorWeightClass.Cosmetic)
                    return false;

                // Armor shouldn't be stackable
                if (_canStack)
                    return false;

                // Armor can't be two-handed
                if (_isTwoHanded)
                    return false;
            }

            // Consumable/Material validations
            if (_itemType == ItemType.Consumable || _itemType == ItemType.Material)
            {
                // These should be stackable
                if (!_canStack)
                    return false;

                // Non-weapons shouldn't have damage
                if (_damagePerSecond > 0)
                    return false;

                // Can't be two-handed
                if (_isTwoHanded)
                    return false;
            }

            return true;
        }
    }
}
