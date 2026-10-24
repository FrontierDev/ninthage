using UnityEngine.Scripting.APIUpdating;
namespace Game.Shared.Data.SpellTargetDefinitions
{
    [System.Serializable]
    [MovedFrom(true, "Game.Shared.Data", "Game.Shared")]
    /// <summary>Targets actors within a cone in front of the caster.</summary>
    public class SpellTarget_Cone : SpellTargetDefinition
    {
        /// <summary>Gets or sets the cone range.</summary>
        public float Range = 10f;
        /// <summary>Gets or sets the cone angle.</summary>
        public float Angle = 30f;

        /// <summary>Initializes a cone target definition.</summary>
        public SpellTarget_Cone()
        {
            _requiresTarget = false;
        }

        /// <summary>Creates a copy of this target definition.</summary>
        public override SpellTargetDefinition Clone()
        {
            return new SpellTarget_Cone
            {
                Range = this.Range,
                Angle = this.Angle
            };
        }

    }
}
