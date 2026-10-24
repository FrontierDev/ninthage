using UnityEngine.Scripting.APIUpdating;
namespace Game.Shared.Data.SpellTargetDefinitions
{
    [System.Serializable]
    [MovedFrom(true, "Game.Shared.Data", "Game.Shared")]
    /// <summary>Targets the actor selected by the spell context.</summary>
    public class SpellTarget_Single : SpellTargetDefinition
    {
        /// <summary>Initializes a single-target definition.</summary>
        public SpellTarget_Single()
        {
            _requiresTarget = true;
        }

        /// <summary>Creates a copy of this target definition.</summary>
        public override SpellTargetDefinition Clone()
        {
            return new SpellTarget_Single();
        }

    }
}
