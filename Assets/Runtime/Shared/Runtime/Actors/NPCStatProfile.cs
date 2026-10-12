using Game.Shared.Data;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared
{
    /// <summary>Defines a base stat value for an NPC.</summary>
    [System.Serializable]
    public class ActorStatBaseEntry
    {
        /// <summary>Gets or sets the stat definition.</summary>
        public ActorStatDefinition definition;
        /// <summary>Gets or sets the base stat value.</summary>
        public float baseValue;
    }

    /// <summary>Provides base stats and weapon values for an NPC.</summary>
    [RequireComponent(typeof(ActorStatContainer))]
    public sealed class NPCStatProfile : MonoBehaviour
    {
        [SerializeField]
        private List<ActorStatBaseEntry> baseStats = new();
        /// <summary>Gets the configured base stats.</summary>
        public List<ActorStatBaseEntry> BaseStats => baseStats;

        [SerializeField] private float mainHandDPS = 1;
        [SerializeField] private float offHandDPS = 0;
        [SerializeField] private float mainHandSwingSpeed = 2.0f;
        [SerializeField] private float offHandSwingSpeed = 2.0f;

        /// <summary>Gets the weapon swing timer for an equipment slot.</summary>
        public float GetWeaponSwingTimer(ItemSlot slot)
        {
            return slot switch
            {
                ItemSlot.MainHand => mainHandSwingSpeed,
                ItemSlot.OffHand => offHandSwingSpeed,
                _ => 2f
            };
        }

        /// <summary>Gets the weapon damage for an equipment slot.</summary>
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
