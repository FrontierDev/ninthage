using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Shared.Data
{
    [Serializable]
    public struct ClassStatGrowth
    {
        public ActorStatDefinition stat;
        public float flatPerLevel;
    }

    [Serializable]
    public struct ClassSpell
    {
        public SpellDefinition spell;
        public int levelRequirement;
    }

    [Serializable]
    public struct ClassSpecialisation
    {
        public string name;
        public string description;
        public Sprite icon;
        public List<SpellDefinition> spells;
        public List<TalentDefinition> talents;
        public Color color;
    }

    /// <summary>
    /// Defines a player class (e.g. Warrior, Mage, Ranger).
    /// </summary>
    [CreateAssetMenu(fileName = "ClassDefinition_", menuName = "NinthAge/Definitions/Class Definition")]
    public class ClassDefinition : DataDefinition
    {
        [SerializeField][TextArea(3, 5)] private string description;

        [SerializeField] private Sprite icon;

        [SerializeField] private List<ClassStatGrowth> statGrowthPerLevel = new();

        [SerializeField] private List<ActorStatBaseEntry> baseStats = new();

        [SerializeField] private List<ClassSpell> classSpellList = new();

        [SerializeField] private List<ActorStatDefinition> focusStats = new();
        [SerializeField] private List<ActorStatDefinition> relevantStats = new();
        [SerializeField] private List<ItemWeaponType> weaponTypes = new();
        [SerializeField] private List<ClassSpecialisation> specialisations = new();

        [SerializeField] private ClassBehaviour classBehaviour;

        /// <summary>
        /// Detailed description of this class.
        /// </summary>
        public string Description => description;

        /// <summary>
        /// Icon sprite for UI display.
        /// </summary>
        public Sprite Icon => icon;

        /// <summary>
        /// Stat growth per level for this class.
        /// </summary>
        public List<ClassStatGrowth> StatGrowthPerLevel => statGrowthPerLevel;
        public List<ClassSpell> ClassSpellList => classSpellList;
        public IReadOnlyList<ActorStatBaseEntry> BaseStats => baseStats;
        public IReadOnlyList<ActorStatDefinition> FocusStats => focusStats;
        public IReadOnlyList<ActorStatDefinition> RelevantStats => relevantStats;
        public IReadOnlyList<ItemWeaponType> WeaponTypes => weaponTypes;
        public IReadOnlyList<ClassSpecialisation> Specialisations => specialisations;
        public ClassBehaviour ClassBehaviour => classBehaviour;

        protected override string AssetPrefix => "Class";

        protected override void OnDefinitionValidate()
        {
            for (int i = 0; i < classSpellList.Count; i++)
            {
                var classSpell = classSpellList[i];
                if (classSpell.spell == null)
                    continue;

                var assoc = classSpell.spell.AssociatedClasses;
                bool hasClass = assoc != null && assoc.Contains(this);
                if (!hasClass)
                {
                    classSpell.spell.AddAssociatedClass(this);
                }
            }

            for (int i = 0; i < specialisations.Count; i++)
            {
                var spec = specialisations[i];

                if (string.IsNullOrEmpty(spec.name))
                    Debug.LogWarning($"Class {DefinitionId} has a specialisation with an empty name.");

                if (spec.talents == null)
                    continue;

                for (int t = 0; t < spec.talents.Count; t++)
                {
                    var talent = spec.talents[t];
                    if (talent == null)
                        continue;

                    var assoc = talent.AssociatedSpecialisations;
                    bool hasSpec = assoc != null && assoc.Contains(spec);
                    if (!hasSpec)
                    {
                        talent.AddAssociatedSpecialisation(spec);
                    }
                }
            }
        }
    }
}
