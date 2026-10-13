using Game.Shared;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;

namespace Game.Server.Services
{
    /// <summary>Applies experience gains to a server-side player character.</summary>
    public sealed class CharacterExperienceService
    {
        private PlayerExperience _playerExperience;
        private int _level;
        private int _experience;

        /// <summary>Initializes the service and applies the supplied experience gain.</summary>
        /// <param name="playerActor">The player character receiving experience.</param>
        /// <param name="experienceGained">The amount of experience to add.</param>
        public CharacterExperienceService(PlayerActor playerActor, int experienceGained)
        {
            this._playerExperience = playerActor.GetComponent<PlayerExperience>();
            this._experience = _playerExperience.Experience;
            this._level = _playerExperience.Level;

            AddExperience(experienceGained);

            // Update the player's experience and level on the PlayerExperience component
            _playerExperience.SetExperience(_level, _experience, experienceGained);

            // Notify clients about the experience update
            CharacterEndpoint.Client_ExperienceUpdated(playerActor.owner.Value, _level, _experience, experienceGained);
        }

        /// <summary>Adds experience and advances the character level when required.</summary>
        /// <param name="amount">The amount of experience to add.</param>
        public void AddExperience(int amount)
        {
            if (_level >= ExperienceCalculator.MAX_LEVEL)
                return;

            _experience += amount;

            int required = ExperienceCalculator.GetExpForLevel(_level);
            while (_experience >= required && _level < ExperienceCalculator.MAX_LEVEL)
            {
                _experience -= required;
                _level++;
                required = ExperienceCalculator.GetExpForLevel(_level);
            }
        }
    }
}
