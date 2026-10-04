using System.Collections.Generic;
using System.Linq;
using Game.Shared;
using Game.Shared.Data;
using UnityEngine;

namespace Game.Client.UI
{
    public sealed class UI_CharacterWeaponSkillsList : UI_Panel, IListPanel
    {
        private static UI_CharacterWeaponSkillsList _instance;
        public static UI_CharacterWeaponSkillsList Instance => _instance;

        [SerializeField] private GameObject container;
        [SerializeField] private GameObject skillEntryPrefab;
        GameObject IListPanel.contentContainer => container;
        GameObject IListPanel.entryPrefab => skillEntryPrefab.gameObject;
        public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();

        [SerializeField] private List<ItemWeaponType> trackedWeaponTypes = new();
        private PlayerExperience playerExperience;

        protected override void Awake()
        {
            _instance = this;

            Game.Shared.Networking.ActorService.onPlayerActorAssigned += OnPlayerActorAssigned;
            Game.Shared.Networking.CharacterService.onClientWeaponSkillUpdated += OnWeaponSkillUpdated;

            base.Awake();
        }

        private void OnPlayerActorAssigned(Actor actor)
        {
            if (actor == null) return;

            // Get the player's class.
            var classId = ClientAccountManager.ActiveCharacter.ClassID;
            var classDef = ClassDefinitionLibrary.Instance.GetDefinition(classId);

            trackedWeaponTypes = new List<ItemWeaponType>(classDef.WeaponTypes);
            playerExperience = actor.GetComponent<PlayerExperience>();

            PopulateList(true);
        }

        private void OnWeaponSkillUpdated(ItemWeaponType weaponType, int level, int experience, int delta)
        {
            // Update the corresponding entry in the UI list if it's currently displayed.
            foreach (var entry in currentEntries)
            {
                if (entry is UI_CharacterWeaponSkillEntry skillEntry && skillEntry.WeaponType == weaponType)
                {
                    skillEntry.UpdateWeaponSkill(level, experience);
                    break;
                }
            }
        }

        public override void Refresh()
        {
            // Update the corresponding entry in the UI list if it's currently displayed.
            foreach (var entry in currentEntries)
            {
                if (entry is UI_CharacterWeaponSkillEntry skillEntry)
                {
                    var weaponSkill = playerExperience.WeaponSkills.FirstOrDefault(ws => ws.WeaponType == skillEntry.WeaponType);
                    if (weaponSkill != null)
                    {
                        skillEntry.UpdateWeaponSkill(weaponSkill.Level, weaponSkill.Experience);
                    }
                }
            }
        }

        private void ClearEntries()
        {
            foreach (var entry in currentEntries)
            {
                Destroy(entry.gameObject);
            }
            currentEntries.Clear();
        }

        public void PopulateList(bool forceClear = false)
        {
            if (ClientAccountManager.PlayerActor == null || playerExperience == null) return;

            if (forceClear)
            {
                ClearEntries();
            }

            int index = 0;
            foreach (var weaponType in trackedWeaponTypes)
            {
                var entryObj = Instantiate(skillEntryPrefab, container.transform);
                var entry = entryObj.GetComponent<UI_CharacterWeaponSkillEntry>();
                entry.Initialize(weaponType, index);
                entry.Register(this);

                // Try live data first, fall back to saved character data
                var weaponSkill = playerExperience.WeaponSkills.FirstOrDefault(ws => ws.WeaponType == weaponType);
                if (weaponSkill != null)
                {
                    entry.UpdateWeaponSkill(weaponSkill.Level, weaponSkill.Experience);
                }
                else
                {
                    var saved = ClientAccountManager.ActiveCharacter?.SavedWeaponSkills
                        .FirstOrDefault(ws => ws.WeaponType == (int)weaponType);
                    if (saved != null)
                        entry.UpdateWeaponSkill(saved.Value.Level, saved.Value.Experience);
                }

                index++;
            }
        }
    }

}