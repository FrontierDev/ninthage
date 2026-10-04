using UnityEngine;

namespace Game.Shared.Data
{
    [System.Serializable]
    public abstract class QuestObjective
    {
        [SerializeField] private string description = "Objective";
        [SerializeField] private int requiredAmount = 1;

        public string Description => description;
        public int RequiredAmount => requiredAmount;

        public QuestObjective() { }

        public QuestObjective(string description, int requiredAmount)
        {
            this.description = description;
            this.requiredAmount = requiredAmount;
        }
    }
}