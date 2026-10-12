using UnityEngine;

namespace Game.Shared.Data
{
    [System.Serializable]
    /// <summary>Defines a measurable quest objective.</summary>
    public abstract class QuestObjective
    {
        [SerializeField] private string description = "Objective";
        [SerializeField] private int requiredAmount = 1;

        /// <summary>Gets the objective description.</summary>
        public string Description => description;
        /// <summary>Gets the amount required to complete the objective.</summary>
        public int RequiredAmount => requiredAmount;

        /// <summary>Initializes an empty objective.</summary>
        public QuestObjective() { }

        /// <summary>Initializes an objective with its description and required amount.</summary>
        public QuestObjective(string description, int requiredAmount)
        {
            this.description = description;
            this.requiredAmount = requiredAmount;
        }
    }
}
