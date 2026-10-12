using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Persistence;
using PurrNet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Shared
{
    /// <summary>Manages a player's learned talents and specialisation points.</summary>
    public sealed class PlayerTalents : NetworkBehaviour
    {
        [SerializeField] private Dictionary<TalentDefinition, int> talents = new();
        /// <summary>Gets learned talents and their ranks.</summary>
        public IReadOnlyDictionary<TalentDefinition, int> Talents => talents;

        // Talent points
        private Dictionary<ClassSpecialisation, int> _spentTalentPointsBySpecialisation = new();
        /// <summary>Gets the number of spent talent points.</summary>
        public int SpentTalentPoints => _spentTalentPointsBySpecialisation.Values.Sum();
        /// <summary>Gets the number of unspent talent points.</summary>
        public int GetUnspentTalentPoints() { return GetMaximumTalentPoints() - SpentTalentPoints; }

        private void Awake()
        {
            // Registration to events...
        }

        /// <summary>Gets the maximum talent points available from the actor's level.</summary>
        public int GetMaximumTalentPoints()
        {
            var level = GetComponent<Actor>().GetLevel();
            return Mathf.Clamp(level - 9, 0, int.MaxValue);
        }

        /// <summary>Gets points spent in a specialisation.</summary>
        public int GetSpentTalentPoints(ClassSpecialisation spec)
        {
            return _spentTalentPointsBySpecialisation.TryGetValue(spec, out var points) ? points : 0;
        }

        #region Talent Management
        /// <summary>Loads talents from persisted character data.</summary>
        public void LoadTalents(CharacterData data)
        {
            talents.Clear();
            foreach (var entry in data.SavedTalents)
            {
                var def = TalentDefinitionLibrary.Instance.GetDefinition(entry.TalentID);
                if (def != null)
                {
                    talents.Add(def, entry.Rank);

                    foreach (var mod in def.Modifiers)
                        GetComponent<ActorSpellcaster>().AddModifier($"talent_{def.DefinitionId}", mod, entry.Rank);

                    if (def.TalentBehaviour != null)
                        def.TalentBehaviour.OnActivate(GetComponent<PlayerActor>());
                }
            }

            if (!Game.Shared.Runtime.IsServer())
                CharacterEndpoint.onClientTalentsUpdated?.Invoke();
            else
                GetComponent<ActorSpellcaster>().RecalculateOverrides();
        }

        /// <summary>Adds or updates a talent rank.</summary>
        public void AddTalent(string talentID, int rank)
        {
            var def = TalentDefinitionLibrary.Instance.GetDefinition(talentID);
            if (def == null) return;

            var rankDelta = rank - (talents.ContainsKey(def) ? talents[def] : 0);
            if (talents.ContainsKey(def))
                talents[def] = rank; // Update existing talent rank
            else
                talents.Add(def, rank); // Add new talent

            if (def.AssociatedSpecialisations.Count > 0)
            {
                var spec = def.AssociatedSpecialisations[0];
                if (_spentTalentPointsBySpecialisation.ContainsKey(spec))
                    _spentTalentPointsBySpecialisation[spec] += rankDelta;
                else
                    _spentTalentPointsBySpecialisation.Add(spec, rankDelta);
            }

            foreach (var mod in def.Modifiers)
            {
                GetComponent<ActorSpellcaster>().AddModifier($"talent_{def.DefinitionId}", mod, rank);
            }

            if (def.TalentBehaviour != null)
                def.TalentBehaviour.OnActivate(GetComponent<PlayerActor>());
        }

        /// <summary>Removes a learned talent.</summary>
        public void RemoveTalent(string talentID)
        {
            var def = TalentDefinitionLibrary.Instance.GetDefinition(talentID);
            if (def == null) return;

            if (talents.Remove(def))
            {
                if (def.AssociatedSpecialisations.Count > 0)
                {
                    var spec = def.AssociatedSpecialisations[0];
                    if (_spentTalentPointsBySpecialisation.ContainsKey(spec))
                        _spentTalentPointsBySpecialisation[spec] -= talents[def];
                }
                foreach (var mod in def.Modifiers)
                    GetComponent<ActorSpellcaster>().RemoveModifier($"talent_{def.DefinitionId}", mod);

                if (def.TalentBehaviour != null)
                    def.TalentBehaviour.OnDeactivate(GetComponent<PlayerActor>());
            }
        }
        #endregion

        #region Talent RPCs
        [ServerRpc]
        /// <summary>Requests that the server add a talent rank.</summary>
        public void Server_RequestAddTalent(string talentID, int rank, RPCInfo rpcInfo = default)
        {
            // TODO: Validate first...
            AddTalent(talentID, rank);

            Client_UpdateTalents(rpcInfo.sender, new List<SavedTalentEntry> { new SavedTalentEntry { TalentID = talentID, Rank = rank } });
        }

        [ServerRpc]
        /// <summary>Requests that the server remove or update a talent.</summary>
        public void Server_RequestRemoveTalent(string talentID, int rank, RPCInfo rpcInfo = default)
        {
            // TODO: Validate first...
            if (rank == 0) RemoveTalent(talentID);
            else AddTalent(talentID, rank);

            Client_UpdateTalents(rpcInfo.sender, new List<SavedTalentEntry> { new SavedTalentEntry { TalentID = talentID, Rank = rank } });
        }

        [TargetRpc]
        /// <summary>Applies a talent update on a client.</summary>
        public void Client_UpdateTalents(PlayerID target, List<SavedTalentEntry> updatedTalents, RPCInfo rpcInfo = default)
        {
            talents.Clear();
            foreach (var entry in updatedTalents)
            {
                var def = TalentDefinitionLibrary.Instance.GetDefinition(entry.TalentID);
                if (def != null)
                {
                    talents.Add(def, entry.Rank);

                    foreach (var mod in def.Modifiers)
                        GetComponent<ActorSpellcaster>().AddModifier($"talent_{def.DefinitionId}", mod, entry.Rank);
                }
            }

            CharacterEndpoint.onClientTalentsUpdated?.Invoke();
        }
        #endregion
    }
}
