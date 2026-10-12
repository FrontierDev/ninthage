using System;
using System.Collections.Generic;
using System.Linq;
using Game.Shared.Runtime.Spellcasting;
using UnityEngine;
using UnityEngine.Serialization;

using Game.Shared.Runtime.Actors;
namespace Game.Shared.Data
{
    /// <summary>Defines behavior applied while a talent is active.</summary>
    public abstract class TalentBehaviour : ScriptableObject
    {
        /// <summary>Activates the behavior for an actor.</summary>
        public abstract void OnActivate(PlayerActor actor);
        /// <summary>Deactivates the behavior for an actor.</summary>
        public abstract void OnDeactivate(PlayerActor actor);
    }

    /// <summary>
    /// Defines a player race (e.g. Human, Elf, Dwarf).
    /// </summary>
    [CreateAssetMenu(fileName = "TalentDefinition_", menuName = "NinthAge/Definitions/Talent Definition")]
    public class TalentDefinition : DataDefinition
    {
        /// <summary>
        /// Detailed description of this race.
        /// </summary>
        [SerializeField, TextArea(3, 5), FormerlySerializedAs("description")] private string _description;
        public string Description => _description;

        /// <summary>
        /// Icon sprite for UI display.
        /// </summary>
        [SerializeField, FormerlySerializedAs("icon")] private Sprite _icon;
        public Sprite Icon => _icon;

        /// <summary>
        /// Maximum rank/level of this talent. Some talents can be leveled up multiple times for increased effect, while others are one-time unlocks.
        /// </summary>
        [SerializeField, FormerlySerializedAs("maxRank")] private int _maxRank = 1;
        public int MaxRank => _maxRank;

        [SerializeField, FormerlySerializedAs("minLevel")] private int _minLevel = 10;
        /// <summary>Gets the minimum player level required by this talent.</summary>
        public int MinLevel => _minLevel;

        /// <summary>
        /// List of prerequisite talent IDs that must be unlocked before this talent can be unlocked. 
        /// This allows for creating talent trees with dependencies between talents.
        /// </summary>
        [SerializeField, FormerlySerializedAs("prerequisiteTalentIDs")] private List<string> _prerequisiteTalentIDs = new();
        public IReadOnlyList<string> PrerequisiteTalentIDs => _prerequisiteTalentIDs;

        [SerializeField, FormerlySerializedAs("modifiers")] private List<ModifierEntry> _modifiers = new();
        /// <summary>Gets the spell modifiers provided by this talent.</summary>
        public IReadOnlyList<ISpellModifier> Modifiers => _modifiers.Select(e => e.Modifier).ToList();

        [SerializeField, FormerlySerializedAs("tags")] private List<string> _tags = new();
        /// <summary>Gets the tags assigned to this talent.</summary>
        public IReadOnlyList<string> Tags => _tags;

        [SerializeField, FormerlySerializedAs("associatedSpecialisations")] private List<ClassSpecialisation> _associatedSpecialisations = new();
        /// <summary>Gets the specialisations associated with this talent.</summary>
        public IReadOnlyList<ClassSpecialisation> AssociatedSpecialisations => _associatedSpecialisations;

        [SerializeField, FormerlySerializedAs("talentBehaviour")] private TalentBehaviour _talentBehaviour;
        /// <summary>Gets the behavior applied by this talent.</summary>
        public TalentBehaviour TalentBehaviour => _talentBehaviour;

        protected override string AssetPrefix => "Talent";

        /// <summary>Adds a specialisation association if it is not already present.</summary>
        public void AddAssociatedSpecialisation(ClassSpecialisation spec)
        {
            if (!_associatedSpecialisations.Contains(spec))
                _associatedSpecialisations.Add(spec);
        }

        protected override void OnDefinitionValidate() { }
    }
}
