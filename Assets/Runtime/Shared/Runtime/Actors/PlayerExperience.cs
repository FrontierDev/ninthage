using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Persistence;
using Game.Shared.Utility;
using PurrNet;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Shared
{
    /// <summary>Tracks a player's skill for one weapon type.</summary>
    public sealed class PlayerWeaponSkill
    {
        private ItemWeaponType weaponType;
        private int level;
        private int experience;

        /// <summary>Gets the weapon type.</summary>
        public ItemWeaponType WeaponType => weaponType;
        /// <summary>Gets the skill level.</summary>
        public int Level => level;
        /// <summary>Gets the skill experience.</summary>
        public int Experience => experience;

        /// <summary>Initializes a weapon skill.</summary>
        public PlayerWeaponSkill(ItemWeaponType weaponType, int level, int experience)
        {
            this.weaponType = weaponType;
            this.level = level;
            this.experience = experience;
        }
    }

    /// <summary>Tracks player and weapon experience.</summary>
    public sealed class PlayerExperience : NetworkBehaviour
    {
        private int experience;
        private int level;

        private List<PlayerWeaponSkill> weaponSkills = new();

        /// <summary>Gets the player's experience.</summary>
        public int Experience => experience;
        /// <summary>Gets the player's level.</summary>
        public int Level => level;
        /// <summary>Gets the player's weapon skills.</summary>
        public IReadOnlyList<PlayerWeaponSkill> WeaponSkills => weaponSkills;

        private void Awake()
        {
            CharacterEndpoint.onClientExperienceUpdated += SetExperience;
            CharacterEndpoint.onClientWeaponSkillUpdated += SetWeaponSkill;
        }

        /// <summary>Loads experience from persisted character data.</summary>
        public void LoadExperience(CharacterData data)
        {
            experience = data.Experience;
            level = data.Level;

            // Load the player's weapon skills.
            foreach (var weaponSkill in data.SavedWeaponSkills)
            {
                weaponSkills.Add(new PlayerWeaponSkill((ItemWeaponType)weaponSkill.WeaponType, weaponSkill.Level, weaponSkill.Experience));
            }
        }

        /// <summary>Applies a player experience update.</summary>
        public void SetExperience(int level, int experience, int delta)
        {
            this.experience = experience;
            this.level = level;
        }

        /// <summary>Applies a weapon skill update.</summary>
        public void SetWeaponSkill(ItemWeaponType weaponType, int level, int experience, int delta)
        {
            // Find existing skill
            var existingIndex = weaponSkills.FindIndex(ws => ws.WeaponType == weaponType);

            if (existingIndex >= 0)
            {
                // Replace existing skill
                weaponSkills[existingIndex] = new PlayerWeaponSkill(weaponType, level, experience);
            }
            else
            {
                // Add new skill
                weaponSkills.Add(new PlayerWeaponSkill(weaponType, level, experience));
            }
        }
    }
}
