using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Data;
using PurrNet;
using System;
using System.Diagnostics;

using Game.Shared.Runtime.Actors;
namespace Game.Shared.Networking
{   /// <summary>
    /// This class is intended to manage character-related events and RPCs, 
    /// such as learning spells, unlocking achievements, or other character progression features.
    /// </summary>
    public static class CharacterEndpoint
    {
        // Client callbacks
        /// <summary>Raised when the client learns a spell.</summary>
        public static Action<string> onClientLearnedSpell;

        /// <summary>Raised when the client inventory changes.</summary>
        public static Action onClientInventoryUpdated;
        /// <summary>Raised when the client currency changes.</summary>
        public static Action onClientCurrencyUpdated;
        /// <summary>Raised when the client equipment changes.</summary>
        public static Action onClientEquipmentUpdated;
        /// <summary>Raised when the client talents change.</summary>
        public static Action onClientTalentsUpdated;


        /// <summary>Raised when the client experience changes.</summary>
        public static Action<int, int, int> onClientExperienceUpdated;
        /// <summary>Raised when a client weapon skill changes.</summary>
        public static Action<ItemWeaponType, int, int, int> onClientWeaponSkillUpdated;
        /// <summary>Raised when the client reputation changes.</summary>
        public static Action<string, int, int> onClientReputationUpdated;
        /// <summary>Raised when client quest progress changes.</summary>
        public static Action<string, int[], bool> onClientQuestUpdated;

        // Server callbacks
        /// <summary>Raised when a player requests to learn a spell.</summary>
        public static Action<PlayerID, string> onLearnSpellRequest;
        /// <summary>Raised when a client requests a stat update.</summary>
        public static Action<ActorStatContainer> onUpdateStatsRequest;

        /// <summary>Requests that the server learn a spell for the caller.</summary>
        [ServerRpc]
        public static void Server_RequestLearnSpell(string spellID, RPCInfo rpcInfo = default)
        {
            onLearnSpellRequest?.Invoke(rpcInfo.sender, spellID);
        }

        /// <summary>Notifies a client that it learned a spell.</summary>
        [TargetRpc]
        public static void Client_LearnSpell(PlayerID target, string spellID, RPCInfo rpcInfo = default)
        {
            onClientLearnedSpell?.Invoke(spellID);
        }

        /// <summary>Notifies a client of an experience update.</summary>
        [TargetRpc]
        public static void Client_ExperienceUpdated(PlayerID target, int level, int experience, int delta, RPCInfo rpcInfo = default)
        {
            onClientExperienceUpdated?.Invoke(level, experience, delta);
        }

        /// <summary>Notifies a client of a weapon-skill update.</summary>
        [TargetRpc]
        public static void Client_WeaponSkillUpdated(PlayerID target, int weaponType, int level, int experience, int delta, RPCInfo rpcInfo = default)
        {
            onClientWeaponSkillUpdated?.Invoke((ItemWeaponType)weaponType, level, experience, delta);
        }

        /// <summary>Notifies a client of a reputation update.</summary>
        [TargetRpc]
        public static void Client_ReputationUpdated(PlayerID target, string factionID, int reputation, int delta, RPCInfo rpcInfo = default)
        {
            onClientReputationUpdated?.Invoke(factionID, reputation, delta);
        }

        /// <summary>Notifies a client of a quest update.</summary>
        [TargetRpc]
        public static void Client_QuestUpdated(PlayerID target, string questID, int[] progress, bool isCompleted, RPCInfo rpcInfo = default)
        {
            onClientQuestUpdated?.Invoke(questID, progress, isCompleted);
        }
    }
}
