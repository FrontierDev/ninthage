using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Game.Shared.Data.QuestObjectives
{
    [System.Serializable]
    [MovedFrom(true, "Game.Shared.Data", "Game.Shared")]
    /// <summary>Requires defeating a specified NPC.</summary>
    public sealed class Quest_KillObjective : QuestObjective
    {
        [SerializeField, FormerlySerializedAs("targetNpcID")] private string _targetNPCID;

        /// <summary>Gets the target NPC identifier.</summary>
        public string TargetNPCID => _targetNPCID;

        /// <summary>Initializes an empty kill objective.</summary>
        public Quest_KillObjective() { }

        /// <summary>Initializes a kill objective.</summary>
        public Quest_KillObjective(string description, int requiredAmount, string targetNPCID) : base(description, requiredAmount)
        {
            _targetNPCID = targetNPCID;
        }
    }
}
