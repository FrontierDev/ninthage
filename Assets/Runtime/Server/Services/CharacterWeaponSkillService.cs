using System.Linq;
using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Utility;

namespace Game.Server.Services
{
    public sealed class CharacterWeaponSkillService
    {
        private PlayerExperience playerExperience;
        private ItemWeaponType weaponType;
        private int level;
        private int experience;

        public CharacterWeaponSkillService(PlayerActor playerActor, ItemWeaponType weaponType, int experienceGained)
        {
            this.playerExperience = playerActor.GetComponent<PlayerExperience>();
            this.weaponType = weaponType;

            // Read the WEAPON SKILL's current level/exp, not the player's overall level/exp.
            var existing = playerExperience.WeaponSkills.FirstOrDefault(ws => ws.WeaponType == weaponType);
            this.level = existing?.Level ?? 1;
            this.experience = existing?.Experience ?? 0;

            AddWeaponSkillExperience(experienceGained);

            // Update the player's experience and level on the PlayerExperience component
            playerExperience.SetWeaponSkill(weaponType, level, experience, experienceGained);

            // Notify clients about the experience update
            CharacterService.Client_WeaponSkillUpdated(playerActor.owner.Value, (int)weaponType, level, experience, experienceGained);
        }

        public void AddWeaponSkillExperience(int amount)
        {
            if (level >= ExperienceCalculator.MaxLevel)
                return;

            experience += amount;

            int required = ExperienceCalculator.GetExpForLevel(level);
            while (experience >= required && level < playerExperience.Level)
            {
                experience -= required;
                level++;
                required = ExperienceCalculator.GetExpForLevel(level);
            }
        }
    }
}