using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;
using System.Linq;

namespace Game.Server.Services
{
    /// <summary>Applies weapon-skill experience to a server-side player character.</summary>
    public sealed class CharacterWeaponSkillService
    {
        private PlayerExperience _playerExperience;
        private ItemWeaponType _weaponType;
        private int _level;
        private int _experience;

        /// <summary>Initializes the service and applies the supplied weapon-skill experience.</summary>
        /// <param name="playerActor">The player character receiving experience.</param>
        /// <param name="weaponType">The weapon skill to update.</param>
        /// <param name="experienceGained">The amount of experience to add.</param>
        public CharacterWeaponSkillService(PlayerActor playerActor, ItemWeaponType weaponType, int experienceGained)
        {
            _playerExperience = playerActor.GetComponent<PlayerExperience>();
            _weaponType = weaponType;

            // Read the WEAPON SKILL's current level/exp, not the player's overall level/exp.
            var existing = _playerExperience.WeaponSkills.FirstOrDefault(ws => ws.WeaponType == weaponType);
            _level = existing?.Level ?? 1;
            _experience = existing?.Experience ?? 0;

            AddWeaponSkillExperience(experienceGained);

            // Update the player's experience and level on the PlayerExperience component
            _playerExperience.SetWeaponSkill(weaponType, _level, _experience, experienceGained);

            // Notify clients about the experience update
            CharacterEndpoint.Client_WeaponSkillUpdated(playerActor.owner.Value, (int)weaponType, _level, _experience, experienceGained);
        }

        /// <summary>Adds experience to the configured weapon skill.</summary>
        /// <param name="amount">The amount of experience to add.</param>
        public void AddWeaponSkillExperience(int amount)
        {
            if (_level >= ExperienceCalculator.MAX_LEVEL)
                return;

            _experience += amount;

            int required = ExperienceCalculator.GetExpForLevel(_level);
            while (_experience >= required && _level < _playerExperience.Level)
            {
                _experience -= required;
                _level++;
                required = ExperienceCalculator.GetExpForLevel(_level);
            }
        }
    }
}
