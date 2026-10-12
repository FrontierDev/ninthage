using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Persistence;
using PurrNet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField, FormerlySerializedAs("pvpFaction")] private FactionDefinition _pvpFaction;
        /// <summary>Gets the player's PvP faction.</summary>
        public FactionDefinition PVPFaction => _pvpFaction;

        [SerializeField, FormerlySerializedAs("reputations")] private Dictionary<string, int> _reputations = new();
        /// <summary>Gets reputation values by faction identifier.</summary>
        public IReadOnlyDictionary<string, int> Reputations => _reputations;

        void Awake()
        {
            CharacterEndpoint.onClientReputationUpdated += OnClientReputationUpdated;
        }

        /// <summary>Applies a client reputation update.</summary>
        public void OnClientReputationUpdated(string factionID, int reputation, int delta)
        {
            _reputations[factionID] = reputation;
        }

        /// <summary>
        /// Loads reputation data from the given character data.
        /// </summary>
        /// <param name="data"></param>
        public void LoadReputations(CharacterData data)
        {
            _reputations.Clear();
            foreach (var entry in data.SavedReputations)
            {
                _reputations[entry.FactionID] = entry.Reputation;
                Debug.Log($"Loaded reputation for faction {entry.FactionID} with value {entry.Reputation}");
            }

            _pvpFaction = data.PvPFactionID != null ? Game.Shared.Data.FactionDefinitionLibrary.Instance.GetDefinition(data.PvPFactionID) : null;
            CharacterEndpoint.onClientReputationUpdated?.Invoke(_pvpFaction?.DefinitionID, _reputations.TryGetValue(_pvpFaction?.DefinitionID ?? "", out int rep) ? rep : 0, 0);
        }

        /// <summary>Sets reputation for a faction.</summary>
        public void SetReputation(FactionDefinition def, int reputation)
        {
            _reputations[def.DefinitionID] = reputation;

            // NYI: Trigger any necessary updates based on reputation changes (e.g. unlocking content, updating NPC interactions, etc.)
        }

        /// <summary>Gets reputation for a faction if known.</summary>
        public bool TryGetReputation(string factionID, out int reputation)
        {
            return _reputations.TryGetValue(factionID, out reputation);
        }
    }
}
