using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Persistence;
using PurrNet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Shared.Runtime.Actors
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

            _pvpFaction = data.PVPFactionID != null ? Game.Shared.Data.FactionDefinitionLibrary.Instance.GetDefinition(data.PVPFactionID) : null;
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

        /// <summary>Publishes the complete live reputation state to its current owner.</summary>
        public void PublishOwnerBootstrap(PlayerID playerID)
        {
            if (!owner.HasValue || owner.Value != playerID)
                throw new System.InvalidOperationException("Reputation bootstrap requires the current owner.");

            List<SavedReputationEntry> reputations = new(_reputations.Count);
            foreach (KeyValuePair<string, int> reputation in _reputations)
            {
                if (string.IsNullOrWhiteSpace(reputation.Key))
                    throw new System.InvalidOperationException("Live reputation state contained a blank faction ID.");

                reputations.Add(new SavedReputationEntry
                {
                    FactionID = reputation.Key,
                    Reputation = reputation.Value
                });
            }

            Client_ReplaceReputation(playerID, _pvpFaction?.DefinitionID, reputations);
        }

        [TargetRpc]
        public void Client_ReplaceReputation(
            PlayerID target,
            string pvpFactionID,
            List<SavedReputationEntry> reputations,
            RPCInfo rpcInfo = default)
        {
            _reputations.Clear();
            foreach (SavedReputationEntry reputation in reputations)
                _reputations[reputation.FactionID] = reputation.Reputation;

            _pvpFaction = string.IsNullOrWhiteSpace(pvpFactionID)
                ? null
                : Game.Shared.Data.FactionDefinitionLibrary.Instance.GetDefinition(pvpFactionID);
            CharacterEndpoint.onClientReputationUpdated?.Invoke(
                _pvpFaction?.DefinitionID,
                _pvpFaction != null && _reputations.TryGetValue(_pvpFaction.DefinitionID, out int value)
                    ? value
                    : 0,
                0);
        }
    }
}
