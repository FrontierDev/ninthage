using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Persistence;
using Game.Shared.Utility;
using PurrNet;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared.Runtime.Actors
{
    /// <summary>Tracks a player's skill for one weapon type.</summary>
    public sealed class PlayerWeaponSkill
    {
        private ItemWeaponType _weaponType;
        private int _level;
        private int _experience;

        /// <summary>Gets the weapon type.</summary>
        public ItemWeaponType WeaponType => _weaponType;
        /// <summary>Gets the skill level.</summary>
        public int Level => _level;
        /// <summary>Gets the skill experience.</summary>
        public int Experience => _experience;

        /// <summary>Initializes a weapon skill.</summary>
        public PlayerWeaponSkill(ItemWeaponType weaponType, int level, int experience)
        {
            _weaponType = weaponType;
            _level = level;
            _experience = experience;
        }
    }

    /// <summary>Tracks player and weapon experience.</summary>
    public sealed class PlayerExperience : NetworkBehaviour
    {
        private int _experience;
        private int _level;

        private List<PlayerWeaponSkill> _weaponSkills = new();

        /// <summary>Gets the player's experience.</summary>
        public int Experience => _experience;
        /// <summary>Gets the player's level.</summary>
        public int Level => _level;
        /// <summary>Gets the player's weapon skills.</summary>
        public IReadOnlyList<PlayerWeaponSkill> WeaponSkills => _weaponSkills;

        private void Awake()
        {
            CharacterEndpoint.onClientExperienceUpdated += SetExperience;
            CharacterEndpoint.onClientWeaponSkillUpdated += SetWeaponSkill;
        }

        /// <summary>Loads experience from persisted character data.</summary>
        public void LoadExperience(CharacterData data)
        {
            _experience = data.Experience;
            _level = data.Level;

            // Load the player's weapon skills.
            foreach (var weaponSkill in data.SavedWeaponSkills)
            {
                _weaponSkills.Add(new PlayerWeaponSkill((ItemWeaponType)weaponSkill.WeaponType, weaponSkill.Level, weaponSkill.Experience));
            }
        }

        /// <summary>Applies a player experience update.</summary>
        public void SetExperience(int level, int experience, int delta)
        {
            _experience = experience;
            _level = level;
        }

        /// <summary>Applies a weapon skill update.</summary>
        public void SetWeaponSkill(ItemWeaponType weaponType, int level, int experience, int delta)
        {
            // Find existing skill
            var existingIndex = _weaponSkills.FindIndex(ws => ws.WeaponType == weaponType);

            if (existingIndex >= 0)
            {
                // Replace existing skill
                _weaponSkills[existingIndex] = new PlayerWeaponSkill(weaponType, level, experience);
            }
            else
            {
                // Add new skill
                _weaponSkills.Add(new PlayerWeaponSkill(weaponType, level, experience));
            }
        }
    }
}
