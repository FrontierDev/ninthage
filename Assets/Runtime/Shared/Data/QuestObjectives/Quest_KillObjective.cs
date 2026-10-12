using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Data
{
    [System.Serializable]
    /// <summary>Requires defeating a specified NPC.</summary>
    public sealed class Quest_KillObjective : QuestObjective
    {
        [SerializeField, FormerlySerializedAs("targetNpcID")] private string _targetNPCID;

        /// <summary>Gets the target NPC identifier.</summary>
        public string TargetNpcID => _targetNPCID;

        /// <summary>Initializes an empty kill objective.</summary>
        public Quest_KillObjective() { }

        /// <summary>Initializes a kill objective.</summary>
        public Quest_KillObjective(string description, int requiredAmount, string targetNpcID) : base(description, requiredAmount)
        {
            _targetNPCID = targetNpcID;
        }
    }
}
