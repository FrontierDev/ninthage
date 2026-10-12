using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Data
{
    /// <summary>Defines stat growth for a class level.</summary>
    [Serializable]
    public struct ClassStatGrowth
    {
        /// <summary>Gets or sets the stat affected by the growth.</summary>
        public ActorStatDefinition stat;
        /// <summary>Gets or sets the flat growth per level.</summary>
        public float flatPerLevel;
    }

    /// <summary>Defines a spell available to a class.</summary>
    [Serializable]
    public struct ClassSpell
    {
        /// <summary>Gets or sets the class spell.</summary>
        public SpellDefinition spell;
        /// <summary>Gets or sets the minimum level required to use the spell.</summary>
        public int levelRequirement;
    }

    /// <summary>Defines a class specialisation and its associated content.</summary>
    [Serializable]
    public struct ClassSpecialisation
    {
        /// <summary>Gets or sets the specialisation name.</summary>
        public string name;
        /// <summary>Gets or sets the specialisation description.</summary>
        public string description;
        /// <summary>Gets or sets the specialisation icon.</summary>
        public Sprite icon;
        /// <summary>Gets or sets the spells in this specialisation.</summary>
        public List<SpellDefinition> spells;
        /// <summary>Gets or sets the talents in this specialisation.</summary>
        public List<TalentDefinition> talents;
        /// <summary>Gets or sets the display color.</summary>
        public Color color;
    }

    /// <summary>
    /// Defines a player class (e.g. Warrior, Mage, Ranger).
    /// </summary>
    [CreateAssetMenu(fileName = "ClassDefinition_", menuName = "NinthAge/Definitions/Class Definition")]
    public class ClassDefinition : DataDefinition
    {
        [SerializeField, TextArea(3, 5), FormerlySerializedAs("description")] private string _description;

        [SerializeField, FormerlySerializedAs("icon")] private Sprite _icon;

        [SerializeField, FormerlySerializedAs("statGrowthPerLevel")] private List<ClassStatGrowth> _statGrowthPerLevel = new();

        [SerializeField, FormerlySerializedAs("baseStats")] private List<ActorStatBaseEntry> _baseStats = new();

        [SerializeField, FormerlySerializedAs("classSpellList")] private List<ClassSpell> _classSpellList = new();

        [SerializeField, FormerlySerializedAs("focusStats")] private List<ActorStatDefinition> _focusStats = new();
        [SerializeField, FormerlySerializedAs("relevantStats")] private List<ActorStatDefinition> _relevantStats = new();
        [SerializeField, FormerlySerializedAs("weaponTypes")] private List<ItemWeaponType> _weaponTypes = new();
        [SerializeField, FormerlySerializedAs("specialisations")] private List<ClassSpecialisation> _specialisations = new();

        [SerializeField, FormerlySerializedAs("classBehaviour")] private ClassBehaviour _classBehaviour;

        /// <summary>
        /// Detailed description of this class.
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// Icon sprite for UI display.
        /// </summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// Stat growth per level for this class.
        /// </summary>
        public List<ClassStatGrowth> StatGrowthPerLevel => _statGrowthPerLevel;
        /// <summary>Gets the spells available to the class.</summary>
        public List<ClassSpell> ClassSpellList => _classSpellList;
        /// <summary>Gets the class base stats.</summary>
        public IReadOnlyList<ActorStatBaseEntry> BaseStats => _baseStats;
        /// <summary>Gets the class focus stats.</summary>
        public IReadOnlyList<ActorStatDefinition> FocusStats => _focusStats;
        /// <summary>Gets the class relevant stats.</summary>
        public IReadOnlyList<ActorStatDefinition> RelevantStats => _relevantStats;
        /// <summary>Gets the weapon types supported by the class.</summary>
        public IReadOnlyList<ItemWeaponType> WeaponTypes => _weaponTypes;
        /// <summary>Gets the class specialisations.</summary>
        public IReadOnlyList<ClassSpecialisation> Specialisations => _specialisations;
        /// <summary>Gets the class behaviour.</summary>
        public ClassBehaviour ClassBehaviour => _classBehaviour;

        protected override string AssetPrefix => "Class";

        protected override void OnDefinitionValidate()
        {
            for (int i = 0; i < _classSpellList.Count; i++)
            {
                var classSpell = _classSpellList[i];
                if (classSpell.spell == null)
                    continue;

                var assoc = classSpell.spell.AssociatedClasses;
                bool hasClass = assoc != null && assoc.Contains(this);
                if (!hasClass)
                {
                    classSpell.spell.AddAssociatedClass(this);
                }
            }

            for (int i = 0; i < _specialisations.Count; i++)
            {
                var spec = _specialisations[i];

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
