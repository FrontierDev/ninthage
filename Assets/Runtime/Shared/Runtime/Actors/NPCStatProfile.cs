using Game.Shared.Data;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Runtime.Actors
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
        [FormerlySerializedAs("baseStats")]
        private List<ActorStatBaseEntry> _baseStats = new();
        /// <summary>Gets the configured base stats.</summary>
        public List<ActorStatBaseEntry> BaseStats => _baseStats;

        [SerializeField, FormerlySerializedAs("mainHandDPS")] private float _mainHandDPS = 1;
        [SerializeField, FormerlySerializedAs("offHandDPS")] private float _offHandDPS = 0;
        [SerializeField, FormerlySerializedAs("mainHandSwingSpeed")] private float _mainHandSwingSpeed = 2.0f;
        [SerializeField, FormerlySerializedAs("offHandSwingSpeed")] private float _offHandSwingSpeed = 2.0f;

        /// <summary>Gets the weapon swing timer for an equipment slot.</summary>
        public float GetWeaponSwingTimer(ItemSlot slot)
        {
            return slot switch
            {
                ItemSlot.MainHand => _mainHandSwingSpeed,
                ItemSlot.OffHand => _offHandSwingSpeed,
                _ => 2f
            };
        }

        /// <summary>Gets the weapon damage for an equipment slot.</summary>
        public float GetWeaponDamage(ItemSlot slot, out float damage)
        {
            damage = slot switch
            {
                ItemSlot.MainHand => _mainHandDPS,
                ItemSlot.OffHand => _offHandDPS,
                _ => 0
            };

            return damage;
        }
    }
}
