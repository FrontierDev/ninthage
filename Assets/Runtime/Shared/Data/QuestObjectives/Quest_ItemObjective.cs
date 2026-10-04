using UnityEngine;

namespace Game.Shared.Data
{
    [System.Serializable]
    public sealed class Quest_ItemObjective : QuestObjective
    {
        [SerializeField] private string targetItemID;

        public string TargetItemID => targetItemID;

        public Quest_ItemObjective() { }

        public Quest_ItemObjective(string description, int requiredAmount, string targetItemID) : base(description, requiredAmount)
        {
            this.targetItemID = targetItemID;
        }
    }
}