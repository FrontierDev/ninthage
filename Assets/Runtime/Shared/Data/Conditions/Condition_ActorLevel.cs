using Game.Shared.Persistence;
using Game.Shared.Runtime.Actors;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Game.Shared.Data.Conditions
{
    /// <summary>Requires an actor to meet a minimum level.</summary>
    [System.Serializable]
    [MovedFrom(true, "Game.Shared.Data", "Game.Shared")]
    public sealed class Condition_ActorLevel : ConditionDefinition
    {
        [SerializeField, FormerlySerializedAs("requiredLevel")] private int _requiredLevel;

        [FormerlySerializedAs("tooltipFormat")]
        private readonly string _tooltipFormat = "Requires level {0}";

        /// <summary>Evaluates the actor level requirement.</summary>
        public override bool Evaluate(object context)
        {
            if (typeof(PlayerActor).IsAssignableFrom(context.GetType()))
            {
                CharacterData characterData = ((PlayerActor)context).CharacterData;
                return ((PlayerActor)context).GetLevel() >= _requiredLevel;
            }
            else if (typeof(NPCActor).IsAssignableFrom(context.GetType()))
            {
                return ((NPCActor)context).GetLevel() >= _requiredLevel;
            }

            return false;
        }

        /// <summary>Gets the level requirement as tooltip text.</summary>
        public override string GetTooltipLine()
        {
            return string.Format(_tooltipFormat, _requiredLevel);
        }
    }
}
