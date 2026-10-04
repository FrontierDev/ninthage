using Game.Shared.Persistence;
using UnityEngine;

namespace Game.Shared.Data
{
    [System.Serializable]
    public sealed class Condition_PlayerClass : ConditionDefinition
    {
        [SerializeField] private ClassDefinition requiredClass;

        private readonly string tooltipFormat = "Requires class: {0}";

        public override bool Evaluate(object context)
        {
            if (typeof(PlayerActor).IsAssignableFrom(context.GetType()))
            {
                CharacterData characterData = ((PlayerActor)context).CharacterData;
                return characterData.ClassID == requiredClass.DefinitionId;
            }

            return false;
        }

        public override string GetTooltipLine()
        {
            return string.Format(tooltipFormat, requiredClass.DisplayName);
        }
    }
}