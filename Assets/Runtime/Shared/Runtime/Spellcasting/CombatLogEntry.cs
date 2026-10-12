using Game.Shared.Data;
using Game.Shared.Networking;
using System.Collections.Generic;

namespace Game.Shared
{
    /// <summary>Identifies the kind of combat-history entry.</summary>
    public enum CombatHistoryEntryType
    {
        Damage,
        Heal,
        BuffApplied,
        DebuffApplied,
        BuffRemoved,
        DebuffRemoved
    }

    /// <summary>Identifies the result of a combat event.</summary>
    public enum CombatLogResultType
    {
        Hit,
        Miss,
        CriticalHit,
        CriticalHeal,
        Dodged,
        Parried,
        Blocked,
        Resisted,
        Absorbed,
        Reflected
    }

    [System.Serializable]
    /// <summary>Contains the data displayed for one combat-log entry.</summary>
    public struct CombatLogEntry
    {
        /// <summary>Gets or sets the source actor.</summary>
        public Actor Source;
        /// <summary>Gets or sets the target actor.</summary>
        public Actor Target;
        /// <summary>Gets or sets the amount involved.</summary>
        public int Amount;
        /// <summary>Gets or sets the entry type.</summary>
        public CombatHistoryEntryType EntryType;
        /// <summary>Gets or sets the result type.</summary>
        public CombatLogResultType ResultType;
        /// <summary>Gets or sets the spell hit type.</summary>
        public SpellHitType HitType;
        /// <summary>Gets or sets the damage schools involved.</summary>
        public List<DamageSchoolDefinition> DamageTypes;

        /// <summary>Initializes a combat-log entry.</summary>
        public CombatLogEntry(Actor source, Actor target, int amount, CombatHistoryEntryType entryType, CombatLogResultType resultType, List<DamageSchoolDefinition> damageTypes = null, SpellHitType hitType = SpellHitType.Auto)
        {
            Source = source;
            Target = target;
            EntryType = entryType;
            ResultType = resultType;
            HitType = hitType;
            Amount = amount;
            DamageTypes = damageTypes;
        }
    }
}
