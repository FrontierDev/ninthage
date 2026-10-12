using Game.Shared.Persistence;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Data
{
    /// <summary>Requires a player to have a specified class.</summary>
    [System.Serializable]
    public sealed class Condition_PlayerClass : ConditionDefinition
    {
        [SerializeField, FormerlySerializedAs("requiredClass")] private ClassDefinition _requiredClass;

        [FormerlySerializedAs("tooltipFormat")]
        private readonly string _tooltipFormat = "Requires class: {0}";

        /// <summary>Evaluates the player's class requirement.</summary>
        public override bool Evaluate(object context)
        {
            if (typeof(PlayerActor).IsAssignableFrom(context.GetType()))
            {
                CharacterData characterData = ((PlayerActor)context).CharacterData;
                return characterData.ClassID == _requiredClass.DefinitionId;
            }

            return false;
        }

        /// <summary>Gets the class requirement as tooltip text.</summary>
        public override string GetTooltipLine()
        {
            return string.Format(_tooltipFormat, _requiredClass.DisplayName);
        }
    }
}
