using System;
using System.Diagnostics;
using Game.Shared.Data;
using PurrNet;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Shared.Networking
{   /// <summary>
    /// This class is intended to manage character-related events and RPCs, 
    /// such as learning spells, unlocking achievements, or other character progression features.
    /// </summary>
    public static class CharacterService
    {
        // Client callbacks
        public static Action<string> onClientLearnedSpell;
        public static Action onClientKnownSpellsUpdated;

        public static Action onClientInventoryUpdated;
        public static Action onClientCurrencyUpdated;
        public static Action onClientEquipmentUpdated;
        public static Action onClientTalentsUpdated;


        public static Action<int, int, int> onClientExperienceUpdated;
        public static Action<ItemWeaponType, int, int, int> onClientWeaponSkillUpdated;
        public static Action<string, int, int> onClientReputationUpdated;
        public static Action<int> onClientLevelUpdated;
        public static Action<string, int[], bool> onClientQuestUpdated;

        // Server callbacks
        public static Action<PlayerID, string> onLearnSpellRequest;
        public static Action<ActorStatContainer> onUpdateStatsRequest;

        [ServerRpc]
        public static void Server_RequestLearnSpell(string spellID, RPCInfo rpcInfo = default)
        {
            onLearnSpellRequest?.Invoke(rpcInfo.sender, spellID);
        }

        [TargetRpc]
        public static void Client_LearnSpell(PlayerID target, string spellID, RPCInfo rpcInfo = default)
        {
            onClientLearnedSpell?.Invoke(spellID);
        }

        [TargetRpc]
        public static void Client_ExperienceUpdated(PlayerID target, int level, int experience, int delta, RPCInfo rpcInfo = default)
        {
            onClientExperienceUpdated?.Invoke(level, experience, delta);
        }

        [TargetRpc]
        public static void Client_WeaponSkillUpdated(PlayerID target, int weaponType, int level, int experience, int delta, RPCInfo rpcInfo = default)
        {
            onClientWeaponSkillUpdated?.Invoke((ItemWeaponType)weaponType, level, experience, delta);
        }

        [TargetRpc]
        public static void Client_ReputationUpdated(PlayerID target, string factionID, int reputation, int delta, RPCInfo rpcInfo = default)
        {
            onClientReputationUpdated?.Invoke(factionID, reputation, delta);
        }

        [TargetRpc]
        public static void Client_QuestUpdated(PlayerID target, string questID, int[] progress, bool isCompleted, RPCInfo rpcInfo = default)
        {
            onClientQuestUpdated?.Invoke(questID, progress, isCompleted);
        }
    }
}