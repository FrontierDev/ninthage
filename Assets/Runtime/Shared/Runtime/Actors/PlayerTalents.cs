using System.Collections.Generic;
using System.Linq;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Persistence;
using PurrNet;
using UnityEngine;

namespace Game.Shared
{
    public sealed class PlayerTalents : NetworkBehaviour
    {
        [SerializeField] private Dictionary<TalentDefinition, int> talents = new();
        public IReadOnlyDictionary<TalentDefinition, int> Talents => talents;

        // Talent points
        private Dictionary<ClassSpecialisation, int> spentTalentPointsBySpecialisation = new();
        public int SpentTalentPoints => spentTalentPointsBySpecialisation.Values.Sum();
        public int GetUnspentTalentPoints() { return GetMaximumTalentPoints() - SpentTalentPoints; }

        private void Awake()
        {
            // Registration to events...
        }

        public int GetMaximumTalentPoints()
        {
            var level = GetComponent<Actor>().GetLevel();
            return Mathf.Clamp(level - 9, 0, int.MaxValue);
        }

        public int GetSpentTalentPoints(ClassSpecialisation spec)
        {
            return spentTalentPointsBySpecialisation.TryGetValue(spec, out var points) ? points : 0;
        }

        #region Talent Management
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
                CharacterService.onClientTalentsUpdated?.Invoke();
            else
                GetComponent<ActorSpellcaster>().RecalculateOverrides();
        }

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
                if (spentTalentPointsBySpecialisation.ContainsKey(spec))
                    spentTalentPointsBySpecialisation[spec] += rankDelta;
                else
                    spentTalentPointsBySpecialisation.Add(spec, rankDelta);
            }

            foreach (var mod in def.Modifiers)
            {
                GetComponent<ActorSpellcaster>().AddModifier($"talent_{def.DefinitionId}", mod, rank);
            }

            if (def.TalentBehaviour != null)
                def.TalentBehaviour.OnActivate(GetComponent<PlayerActor>());
        }

        public void RemoveTalent(string talentID)
        {
            var def = TalentDefinitionLibrary.Instance.GetDefinition(talentID);
            if (def == null) return;

            if (talents.Remove(def))
            {
                if (def.AssociatedSpecialisations.Count > 0)
                {
                    var spec = def.AssociatedSpecialisations[0];
                    if (spentTalentPointsBySpecialisation.ContainsKey(spec))
                        spentTalentPointsBySpecialisation[spec] -= talents[def];
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
        public void Server_RequestAddTalent(string talentID, int rank, RPCInfo rpcInfo = default)
        {
            // TODO: Validate first...
            AddTalent(talentID, rank);

            Client_UpdateTalents(rpcInfo.sender, new List<SavedTalentEntry> { new SavedTalentEntry { TalentID = talentID, Rank = rank } });
        }

        [ServerRpc]
        public void Server_RequestRemoveTalent(string talentID, int rank, RPCInfo rpcInfo = default)
        {
            // TODO: Validate first...
            if (rank == 0) RemoveTalent(talentID);
            else AddTalent(talentID, rank);

            Client_UpdateTalents(rpcInfo.sender, new List<SavedTalentEntry> { new SavedTalentEntry { TalentID = talentID, Rank = rank } });
        }

        [TargetRpc]
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

            CharacterService.onClientTalentsUpdated?.Invoke();
        }
        #endregion
    }
}