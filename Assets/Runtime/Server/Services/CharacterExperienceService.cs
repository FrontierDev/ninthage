using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;

namespace Game.Server.Services
{
    /// <summary>Applies experience gains to a server-side player character.</summary>
    public sealed class CharacterExperienceService
    {
        private PlayerExperience playerExperience;
        private int level;
        private int experience;

        /// <summary>Initializes the service and applies the supplied experience gain.</summary>
        /// <param name="playerActor">The player character receiving experience.</param>
        /// <param name="experienceGained">The amount of experience to add.</param>
        public CharacterExperienceService(PlayerActor playerActor, int experienceGained)
        {
            this.playerExperience = playerActor.GetComponent<PlayerExperience>();
            this.experience = playerExperience.Experience;
            this.level = playerExperience.Level;

            AddExperience(experienceGained);

            // Update the player's experience and level on the PlayerExperience component
            playerExperience.SetExperience(level, experience, experienceGained);

            // Notify clients about the experience update
            CharacterEndpoint.Client_ExperienceUpdated(playerActor.owner.Value, level, experience, experienceGained);
        }

        /// <summary>Adds experience and advances the character level when required.</summary>
        /// <param name="amount">The amount of experience to add.</param>
        public void AddExperience(int amount)
        {
            if (level >= ExperienceCalculator.MAX_LEVEL)
                return;

            experience += amount;

            int required = ExperienceCalculator.GetExpForLevel(level);
            while (experience >= required && level < ExperienceCalculator.MAX_LEVEL)
            {
                experience -= required;
                level++;
                required = ExperienceCalculator.GetExpForLevel(level);
            }
        }
    }
}
