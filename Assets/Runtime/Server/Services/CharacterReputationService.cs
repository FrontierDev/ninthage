using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using Game.Shared.Utility;

namespace Game.Server.Services
{
    /// <summary>Applies faction reputation gains to a server-side player character.</summary>
    public sealed class CharacterReputationService
    {
        private PlayerActor _player;
        private PlayerReputation _playerReputation;
        private FactionDefinition _faction;

        /// <summary>Initializes the service and applies the supplied reputation gain.</summary>
        /// <param name="playerActor">The player character receiving reputation.</param>
        /// <param name="faction">The faction whose reputation changes.</param>
        /// <param name="reputationGained">The amount of reputation to add.</param>
        public CharacterReputationService(PlayerActor playerActor, FactionDefinition faction, int reputationGained)
        {
            _player = playerActor;
            _playerReputation = playerActor.GetComponent<PlayerReputation>();
            _faction = faction;

            AddReputation(reputationGained);
        }

        /// <summary>Adds reputation for the configured faction.</summary>
        /// <param name="amount">The amount of reputation to add.</param>
        public void AddReputation(int amount)
        {
            if (_playerReputation.TryGetReputation(_faction.DefinitionID, out int currentReputation))
            {
                currentReputation += amount;
                _playerReputation.SetReputation(_faction, currentReputation);

                // Notify clients about the reputation update
                CharacterEndpoint.Client_ReputationUpdated(_player.Owner.Value, _faction.DefinitionID, currentReputation, amount);
            }
            else
            {
                _playerReputation.SetReputation(_faction, amount);

                // Notify clients about the reputation update
                CharacterEndpoint.Client_ReputationUpdated(_player.Owner.Value, _faction.DefinitionID, amount, amount);
            }
        }
    }
}
