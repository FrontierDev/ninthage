using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Persistence;
using PurrNet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Shared
{
    /// <summary>Describes the relation between factions.</summary>
    public enum FactionRelationState
    {
        Hostile,
        Neutral,
        Allied
    }

    /// <summary>Manages player faction reputation.</summary>
    public sealed class PlayerReputation : NetworkBehaviour
    {
        [SerializeField] private FactionDefinition pvpFaction;
        /// <summary>Gets the player's PvP faction.</summary>
        public FactionDefinition PvPFaction => pvpFaction;

        [SerializeField] private Dictionary<string, int> reputations = new();
        /// <summary>Gets reputation values by faction identifier.</summary>
        public IReadOnlyDictionary<string, int> Reputations => reputations;

        void Awake()
        {
            CharacterEndpoint.onClientReputationUpdated += OnClientReputationUpdated;
        }

        /// <summary>Applies a client reputation update.</summary>
        public void OnClientReputationUpdated(string factionID, int reputation, int delta)
        {
            reputations[factionID] = reputation;
        }

        /// <summary>
        /// Loads reputation data from the given character data.
        /// </summary>
        /// <param name="data"></param>
        public void LoadReputations(CharacterData data)
        {
            reputations.Clear();
            foreach (var entry in data.SavedReputations)
            {
                reputations[entry.FactionID] = entry.Reputation;
                Debug.Log($"Loaded reputation for faction {entry.FactionID} with value {entry.Reputation}");
            }

            pvpFaction = data.PvPFactionID != null ? Game.Shared.Data.FactionDefinitionLibrary.Instance.GetDefinition(data.PvPFactionID) : null;
            CharacterEndpoint.onClientReputationUpdated?.Invoke(pvpFaction?.DefinitionId, reputations.TryGetValue(pvpFaction?.DefinitionId ?? "", out int rep) ? rep : 0, 0);
        }

        /// <summary>Sets reputation for a faction.</summary>
        public void SetReputation(FactionDefinition def, int reputation)
        {
            reputations[def.DefinitionId] = reputation;

            // NYI: Trigger any necessary updates based on reputation changes (e.g. unlocking content, updating NPC interactions, etc.)
        }

        /// <summary>Gets reputation for a faction if known.</summary>
        public bool TryGetReputation(string factionID, out int reputation)
        {
            return reputations.TryGetValue(factionID, out reputation);
        }
    }
}
