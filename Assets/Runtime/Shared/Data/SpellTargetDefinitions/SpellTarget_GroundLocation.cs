using UnityEngine.Scripting.APIUpdating;
namespace Game.Shared.Data.SpellTargetDefinitions
{
    [System.Serializable]
    [MovedFrom(true, "Game.Shared.Data", "Game.Shared")]
    /// <summary>Targets a ground location and optionally actors around it.</summary>
    public class SpellTarget_GroundLocation : SpellTargetDefinition
    {
        /// <summary>Gets or sets the query radius.</summary>
        public float Radius = 10f;
        /// <summary>Gets or sets whether actors in the radius are returned.</summary>
        public bool GetActorsInRadius = false;

        /// <summary>Initializes a ground-location target definition.</summary>
        public SpellTarget_GroundLocation()
        {
            _requiresTarget = false;
        }

        /// <summary>Creates a copy of this target definition.</summary>
        public override SpellTargetDefinition Clone()
        {
            return new SpellTarget_GroundLocation
            {
                Radius = this.Radius,
                GetActorsInRadius = this.GetActorsInRadius,
            };
        }

    }
}
