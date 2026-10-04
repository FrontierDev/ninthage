using PurrNet;
using UnityEngine;

namespace Game.Shared
{
    public sealed class NPCActor : Actor, IInteractable
    {
        public override bool IsPlayer => false;
        public string NpcID = null;
        [SerializeField] private string DisplayName = "";
        [SerializeField, Range(1, 100)] public (int, int) levelRange = (1, 1);

        [SerializeField] public readonly SyncVar<int> level = new();

        private void Awake()
        {
            if (Game.Shared.Runtime.IsServer())
            {
                level.value = Random.Range(levelRange.Item1, levelRange.Item2 + 1);
            }
        }

        public override string GetName()
        {
            return string.IsNullOrEmpty(DisplayName) ? base.GetName() : DisplayName;
        }

        public override int GetLevel()
        {
            return level.value > 0 ? level.value : levelRange.Item1;
        }
    }
}