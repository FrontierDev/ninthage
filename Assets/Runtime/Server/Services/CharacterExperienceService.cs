using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Utility;

namespace Game.Server.Services
{
    public sealed class CharacterExperienceService
    {
        private PlayerExperience playerExperience;
        private int level;
        private int experience;

        public CharacterExperienceService(PlayerActor playerActor, int experienceGained)
        {
            this.playerExperience = playerActor.GetComponent<PlayerExperience>();
            this.experience = playerExperience.Experience;
            this.level = playerExperience.Level;

            AddExperience(experienceGained);

            // Update the player's experience and level on the PlayerExperience component
            playerExperience.SetExperience(level, experience, experienceGained);

            // Notify clients about the experience update
            CharacterService.Client_ExperienceUpdated(playerActor.owner.Value, level, experience, experienceGained);
        }

        public void AddExperience(int amount)
        {
            if (level >= ExperienceCalculator.MaxLevel)
                return;

            experience += amount;

            int required = ExperienceCalculator.GetExpForLevel(level);
            while (experience >= required && level < ExperienceCalculator.MaxLevel)
            {
                experience -= required;
                level++;
                required = ExperienceCalculator.GetExpForLevel(level);
            }
        }
    }
}