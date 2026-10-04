using System.Collections.Generic;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Persistence;
using Game.Shared.Utility;
using PurrNet;
using UnityEngine;

namespace Game.Shared
{
    public sealed class PlayerWeaponSkill
    {
        private ItemWeaponType weaponType;
        private int level;
        private int experience;

        public ItemWeaponType WeaponType => weaponType;
        public int Level => level;
        public int Experience => experience;

        public PlayerWeaponSkill(ItemWeaponType weaponType, int level, int experience)
        {
            this.weaponType = weaponType;
            this.level = level;
            this.experience = experience;
        }
    }

    public sealed class PlayerExperience : NetworkBehaviour
    {
        private int experience;
        private int level;

        private List<PlayerWeaponSkill> weaponSkills = new List<PlayerWeaponSkill>();

        public int Experience => experience;
        public int Level => level;
        public IReadOnlyList<PlayerWeaponSkill> WeaponSkills => weaponSkills;

        private void Awake()
        {
            CharacterService.onClientExperienceUpdated += SetExperience;
            CharacterService.onClientWeaponSkillUpdated += SetWeaponSkill;
        }

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

        public void SetExperience(int level, int experience, int delta)
        {
            this.experience = experience;
            this.level = level;
        }

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