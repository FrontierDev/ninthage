using Game.Shared.Data;
using PurrNet.Modules;
using PurrNet.Packing;
using System.Collections.Generic;

namespace Game.Shared.Networking
{
    /// <summary>Provides PurrNet serialization for combat log entries.</summary>
    public static class PackCombatLogEntry
    {
        [UsedByIL]
        /// <summary>Writes a combat log entry to a bit packer.</summary>
        public static void Write(this BitPacker packer, CombatLogEntry value)
        {
            Packer<Actor>.Write(packer, value.Source);
            Packer<Actor>.Write(packer, value.Target);
            packer.Write((int)value.EntryType);
            packer.Write(value.Amount);
            int damageTypesLength = 0;
            if (value.DamageTypes != null)
                foreach (var dt in value.DamageTypes)
                    if (dt != null) damageTypesLength++;
            packer.Write(damageTypesLength);
            if (value.DamageTypes != null)
                foreach (var dt in value.DamageTypes)
                {
                    if (dt == null) continue;
                    packer.Write(dt.DefinitionID);
                }
            packer.Write((int)value.ResultType);
            packer.Write((int)value.HitType);
        }

        [UsedByIL]
        /// <summary>Reads a combat log entry from a bit packer.</summary>
        public static void Read(this BitPacker packer, ref CombatLogEntry value)
        {
            Actor source = default;
            Actor target = default;
            int entryType = default;
            int amount = default;
            Packer<Actor>.Read(packer, ref source);
            Packer<Actor>.Read(packer, ref target);
            packer.Read(ref entryType);
            packer.Read(ref amount);

            int damageTypesLength = default;
            packer.Read(ref damageTypesLength);

            List<DamageSchoolDefinition> damageTypes = null;
            if (damageTypesLength > 0)
            {
                damageTypes = new(damageTypesLength);
                var library = DamageSchoolDefinitionLibrary.Instance;
                for (int i = 0; i < damageTypesLength; i++)
                {
                    string definitionId = default;
                    packer.Read(ref definitionId);
                    damageTypes.Add(library.GetDefinition(definitionId));
                }
            }

            int resultType = default;
            int hitTypeInt = default;
            packer.Read(ref resultType);
            packer.Read(ref hitTypeInt);

            value = new CombatLogEntry
            {
                Source = source,
                Target = target,
                EntryType = (CombatHistoryEntryType)entryType,
                Amount = amount,
                DamageTypes = damageTypes,
                ResultType = (CombatLogResultType)resultType,
                HitType = (SpellHitType)hitTypeInt
            };
        }
    }
}
