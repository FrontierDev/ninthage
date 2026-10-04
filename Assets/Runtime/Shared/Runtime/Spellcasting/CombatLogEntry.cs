using System.Collections.Generic;
using Game.Shared.Data;
using Game.Shared.Networking;

namespace Game.Shared
{
    public enum CombatHistoryEntryType
    {
        Damage,
        Heal,
        BuffApplied,
        DebuffApplied,
        BuffRemoved,
        DebuffRemoved
    }

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
    public struct CombatLogEntry
    {
        public Actor Source;
        public Actor Target;
        public int Amount;
        public CombatHistoryEntryType EntryType;
        public CombatLogResultType ResultType;
        public SpellHitType HitType;
        public List<DamageSchoolDefinition> DamageTypes;

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