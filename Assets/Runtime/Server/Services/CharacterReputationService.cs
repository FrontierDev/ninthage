using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Utility;

namespace Game.Server.Services
{
    public sealed class CharacterReputationService
    {
        private PlayerActor player;
        private PlayerReputation playerReputation;
        private FactionDefinition faction;

        public CharacterReputationService(PlayerActor playerActor, FactionDefinition faction, int reputationGained)
        {
            this.player = playerActor;
            this.playerReputation = playerActor.GetComponent<PlayerReputation>();
            this.faction = faction;

            AddReputation(reputationGained);

            // Update the player's reputation on the PlayerReputation component
            playerReputation.SetReputation(faction, reputationGained);

            // Notify clients about the reputation update
            CharacterService.Client_ReputationUpdated(playerActor.owner.Value, faction.DefinitionId, reputationGained, reputationGained);
        }

        public void AddReputation(int amount)
        {
            if (playerReputation.TryGetReputation(faction.DefinitionId, out int currentReputation))
            {
                currentReputation += amount;
                playerReputation.SetReputation(faction, currentReputation);

                // Notify clients about the reputation update
                CharacterService.Client_ReputationUpdated(player.Owner.Value, faction.DefinitionId, currentReputation, amount);
            }
            else
            {
                playerReputation.SetReputation(faction, amount);

                // Notify clients about the reputation update
                CharacterService.Client_ReputationUpdated(player.Owner.Value, faction.DefinitionId, amount, amount);
            }
        }
    }
}