using System.Collections.Generic;
using System.Linq;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Persistence;
using PurrNet;
using UnityEngine;

namespace Game.Shared
{
    public enum FactionRelationState
    {
        Hostile,
        Neutral,
        Allied
    }

    public sealed class PlayerReputation : NetworkBehaviour
    {
        [SerializeField] private FactionDefinition pvpFaction;
        public FactionDefinition PvPFaction => pvpFaction;

        [SerializeField] private Dictionary<string, int> reputations = new();
        public IReadOnlyDictionary<string, int> Reputations => reputations;

        void Awake()
        {
            CharacterService.onClientReputationUpdated += OnClientReputationUpdated;
        }

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
            CharacterService.onClientReputationUpdated?.Invoke(pvpFaction?.DefinitionId, reputations.TryGetValue(pvpFaction?.DefinitionId ?? "", out int rep) ? rep : 0, 0);
        }

        public void SetReputation(FactionDefinition def, int reputation)
        {
            reputations[def.DefinitionId] = reputation;

            // NYI: Trigger any necessary updates based on reputation changes (e.g. unlocking content, updating NPC interactions, etc.)
        }

        public bool TryGetReputation(string factionID, out int reputation)
        {
            return reputations.TryGetValue(factionID, out reputation);
        }
    }
}