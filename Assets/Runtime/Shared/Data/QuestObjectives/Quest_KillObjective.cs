using UnityEngine;

namespace Game.Shared.Data
{
    [System.Serializable]
    public sealed class Quest_KillObjective : QuestObjective
    {
        [SerializeField] private string targetNpcID;

        public string TargetNpcID => targetNpcID;

        public Quest_KillObjective() { }

        public Quest_KillObjective(string description, int requiredAmount, string targetNpcID) : base(description, requiredAmount)
        {
            this.targetNpcID = targetNpcID;
        }
    }
}