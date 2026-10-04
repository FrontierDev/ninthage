using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Persistence;
using PurrNet;
using UnityEngine;
using Guid = System.Guid;

namespace Game.Shared
{
    [RequireComponent(typeof(NetworkIdentity))]
    [ToDo("Store references to the individual components for ease of access rather than relying on GetComponent calls.")]
    public class PlayerActor : Actor, IInteractable
    {
        public override bool IsPlayer => true;

        private CharacterData characterData;
        public CharacterData CharacterData => characterData;

        [SerializeField] public readonly SyncVar<int> level = new();
        [SerializeField] public readonly SyncVar<string> classID = new();
        [SerializeField] public readonly SyncVar<string> raceID = new();
        [SerializeField] public readonly SyncVar<string> characterName = new();
        [SerializeField] public readonly SyncVar<string> pvpFactionID = new();

        // This is done on the SERVER.
        public void SetCharacterData(CharacterData data)
        {
            characterData = data;

            // Push to SyncVars so observers get current values
            if (Game.Shared.Runtime.IsServer())
            {
                level.value = data.Level;
                classID.value = data.ClassID;
                raceID.value = data.RaceID;
                characterName.value = data.Name;
                pvpFactionID.value = data.PvPFactionID;

                GetComponent<ActorSpellcaster>().LoadKnownSpells(data.KnownSpellIDs);
                GetComponent<PlayerInventory>().LoadInventory(data);
                GetComponent<PlayerEquipment>().LoadEquipment(data);
                GetComponent<PlayerExperience>().LoadExperience(data);
                GetComponent<PlayerTalents>().LoadTalents(data);
                GetComponent<PlayerReputation>().LoadReputations(data);
                GetComponent<PlayerQuests>().LoadQuests(data);
                GetComponent<ActorEvents>().LoadClassEvents(data);
            }
        }

        public override float GetWeaponDamage(ItemSlot slot)
        {
            if (GetComponent<PlayerEquipment>().TryGetItem(slot, out var item))
            {
                var itemDef = item.BaseItem;
                if (itemDef != null)
                {
                    return (itemDef.DamagePerSecond * itemDef.SwingTimer);
                }
            }

            return 0;
        }

        public override float GetWeaponSwingTimer(ItemSlot slot)
        {
            if (GetComponent<PlayerEquipment>().TryGetItem(slot, out var item))
            {
                var itemDef = item.BaseItem;
                if (itemDef != null)
                {
                    return itemDef.SwingTimer;
                }
            }

            return 1.6f;
        }

        public override string GetName()
        {
            return characterName.value;
        }

        public override int GetLevel()
        {
            return level.value;
        }

        public override FactionDefinition GetCombatFaction()
        {
            return GetComponent<PlayerReputation>().PvPFaction;
        }
    }
}