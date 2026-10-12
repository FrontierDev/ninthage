using PurrNet;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared
{
    /// <summary>Represents a non-player actor.</summary>
    public sealed class NPCActor : Actor, IInteractable
    {
        /// <summary>Gets whether this actor is a player.</summary>
        public override bool IsPlayer => false;
        /// <summary>Gets or sets the NPC definition identifier.</summary>
        [FormerlySerializedAs("NpcID")]
        public string NPCID = null;
        [SerializeField, FormerlySerializedAs("DisplayName")] private string _displayName = "";
        [SerializeField, Range(1, 100)] public (int, int) levelRange = (1, 1);

        [SerializeField] public readonly SyncVar<int> level = new();

        private void Awake()
        {
            if (Game.Shared.Runtime.IsServer())
            {
                level.value = Random.Range(levelRange.Item1, levelRange.Item2 + 1);
            }
        }

        /// <summary>Gets the NPC's display name.</summary>
        public override string GetName()
        {
            return string.IsNullOrEmpty(_displayName) ? base.GetName() : _displayName;
        }

        /// <summary>Gets the NPC's current level.</summary>
        public override int GetLevel()
        {
            return level.value > 0 ? level.value : levelRange.Item1;
        }
    }
}
