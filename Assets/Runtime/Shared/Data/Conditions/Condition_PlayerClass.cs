using Game.Shared.Persistence;
using UnityEngine;

namespace Game.Shared.Data
{
    /// <summary>Requires a player to have a specified class.</summary>
    [System.Serializable]
    public sealed class Condition_PlayerClass : ConditionDefinition
    {
        [SerializeField] private ClassDefinition requiredClass;

        private readonly string tooltipFormat = "Requires class: {0}";

        /// <summary>Evaluates the player's class requirement.</summary>
        public override bool Evaluate(object context)
        {
            if (typeof(PlayerActor).IsAssignableFrom(context.GetType()))
            {
                CharacterData characterData = ((PlayerActor)context).CharacterData;
                return characterData.ClassID == requiredClass.DefinitionId;
            }

            return false;
        }

        /// <summary>Gets the class requirement as tooltip text.</summary>
        public override string GetTooltipLine()
        {
            return string.Format(tooltipFormat, requiredClass.DisplayName);
        }
    }
}
