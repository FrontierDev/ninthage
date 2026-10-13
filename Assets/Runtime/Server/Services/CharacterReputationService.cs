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
        private PlayerActor player;
        private PlayerReputation playerReputation;
        private FactionDefinition faction;

        /// <summary>Initializes the service and applies the supplied reputation gain.</summary>
        /// <param name="playerActor">The player character receiving reputation.</param>
        /// <param name="faction">The faction whose reputation changes.</param>
        /// <param name="reputationGained">The amount of reputation to add.</param>
        public CharacterReputationService(PlayerActor playerActor, FactionDefinition faction, int reputationGained)
        {
            this.player = playerActor;
            this.playerReputation = playerActor.GetComponent<PlayerReputation>();
            this.faction = faction;

            AddReputation(reputationGained);
        }

        /// <summary>Adds reputation for the configured faction.</summary>
        /// <param name="amount">The amount of reputation to add.</param>
        public void AddReputation(int amount)
        {
            if (playerReputation.TryGetReputation(faction.DefinitionID, out int currentReputation))
            {
                currentReputation += amount;
                playerReputation.SetReputation(faction, currentReputation);

                // Notify clients about the reputation update
                CharacterEndpoint.Client_ReputationUpdated(player.Owner.Value, faction.DefinitionID, currentReputation, amount);
            }
            else
            {
                playerReputation.SetReputation(faction, amount);

                // Notify clients about the reputation update
                CharacterEndpoint.Client_ReputationUpdated(player.Owner.Value, faction.DefinitionID, amount, amount);
            }
        }
    }
}
