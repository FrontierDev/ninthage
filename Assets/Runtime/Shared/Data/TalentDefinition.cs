using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Shared.Data
{
    public abstract class TalentBehaviour : ScriptableObject
    {
        public abstract void OnActivate(PlayerActor actor);
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
        [SerializeField][TextArea(3, 5)] private string description;
        public string Description => description;

        /// <summary>
        /// Icon sprite for UI display.
        /// </summary>
        [SerializeField] private Sprite icon;
        public Sprite Icon => icon;

        /// <summary>
        /// Maximum rank/level of this talent. Some talents can be leveled up multiple times for increased effect, while others are one-time unlocks.
        /// </summary>
        [SerializeField] private int maxRank = 1;
        public int MaxRank => maxRank;

        [SerializeField] private int minLevel = 10;
        public int MinLevel => minLevel;

        /// <summary>
        /// List of prerequisite talent IDs that must be unlocked before this talent can be unlocked. 
        /// This allows for creating talent trees with dependencies between talents.
        /// </summary>
        [SerializeField] private List<string> prerequisiteTalentIDs = new();
        public IReadOnlyList<string> PrerequisiteTalentIDs => prerequisiteTalentIDs;

        [SerializeField] private List<ModifierEntry> modifiers = new();
        public IReadOnlyList<ISpellModifier> Modifiers => modifiers.Select(e => e.Modifier).ToList();

        [SerializeField] private List<string> tags = new();
        public IReadOnlyList<string> Tags => tags;

        [SerializeField] private List<ClassSpecialisation> associatedSpecialisations = new();
        public IReadOnlyList<ClassSpecialisation> AssociatedSpecialisations => associatedSpecialisations;

        [SerializeField] private TalentBehaviour talentBehaviour;
        public TalentBehaviour TalentBehaviour => talentBehaviour;

        protected override string AssetPrefix => "Talent";

        public void AddAssociatedSpecialisation(ClassSpecialisation spec)
        {
            if (!associatedSpecialisations.Contains(spec))
                associatedSpecialisations.Add(spec);
        }

        protected override void OnDefinitionValidate() { }
    }
}
