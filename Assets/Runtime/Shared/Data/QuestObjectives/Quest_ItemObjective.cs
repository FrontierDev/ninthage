using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Game.Shared.Data.QuestObjectives
{
    [System.Serializable]
    [MovedFrom(true, "Game.Shared.Data", "Game.Shared")]
    /// <summary>Requires collecting a specified item.</summary>
    public sealed class Quest_ItemObjective : QuestObjective
    {
        [SerializeField, FormerlySerializedAs("targetItemID")] private string _targetItemID;

        /// <summary>Gets the target item identifier.</summary>
        public string TargetItemID => _targetItemID;

        /// <summary>Initializes an empty item objective.</summary>
        public Quest_ItemObjective() { }

        /// <summary>Initializes an item objective.</summary>
        public Quest_ItemObjective(string description, int requiredAmount, string targetItemID) : base(description, requiredAmount)
        {
            _targetItemID = targetItemID;
        }
    }
}
