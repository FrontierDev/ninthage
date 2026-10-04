using System.Collections.Generic;
using Game.Shared.Data;
using UnityEngine;

namespace Game.Shared
{
    [System.Serializable]
    public class ActorStatBaseEntry
    {
        public ActorStatDefinition definition;
        public float baseValue;
    }

    [RequireComponent(typeof(ActorStatContainer))]
    public sealed class NPCStatProfile : MonoBehaviour
    {
        [SerializeField]
        private List<ActorStatBaseEntry> baseStats = new();
        public List<ActorStatBaseEntry> BaseStats => baseStats;

        [SerializeField] private float mainHandDPS = 1;
        [SerializeField] private float offHandDPS = 0;
        [SerializeField] private float mainHandSwingSpeed = 2.0f;
        [SerializeField] private float offHandSwingSpeed = 2.0f;

        public float GetWeaponSwingTimer(ItemSlot slot)
        {
            return slot switch
            {
                ItemSlot.MainHand => mainHandSwingSpeed,
                ItemSlot.OffHand => offHandSwingSpeed,
                _ => 2f
            };
        }

        public float GetWeaponDamage(ItemSlot slot, out float damage)
        {
            damage = slot switch
            {
                ItemSlot.MainHand => mainHandDPS,
                ItemSlot.OffHand => offHandDPS,
                _ => 0
            };

            return damage;
        }
    }
}