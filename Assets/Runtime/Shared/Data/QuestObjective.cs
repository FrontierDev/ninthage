using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Data
{
    [System.Serializable]
    /// <summary>Defines a measurable quest objective.</summary>
    public abstract class QuestObjective
    {
        [SerializeField, FormerlySerializedAs("description")] private string _description = "Objective";
        [SerializeField, FormerlySerializedAs("requiredAmount")] private int _requiredAmount = 1;

        /// <summary>Gets the objective description.</summary>
        public string Description => _description;
        /// <summary>Gets the amount required to complete the objective.</summary>
        public int RequiredAmount => _requiredAmount;

        /// <summary>Initializes an empty objective.</summary>
        public QuestObjective() { }

        /// <summary>Initializes an objective with its description and required amount.</summary>
        public QuestObjective(string description, int requiredAmount)
        {
            _description = description;
            _requiredAmount = requiredAmount;
        }
    }
}
