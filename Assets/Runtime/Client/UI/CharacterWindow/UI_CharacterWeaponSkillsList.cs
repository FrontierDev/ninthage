using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Runtime.Actors;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterWeaponSkillsList</c> implementation.
    /// </summary>
    public sealed class UI_CharacterWeaponSkillsList : UI_Panel, IListPanel
    {
        private static UI_CharacterWeaponSkillsList _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_CharacterWeaponSkillsList Instance => _instance;

        [FormerlySerializedAs("container")]
        [SerializeField] private GameObject _container;
        [FormerlySerializedAs("skillEntryPrefab")]
        [SerializeField] private GameObject _skillEntryPrefab;
        GameObject IListPanel.contentContainer => _container;
        GameObject IListPanel.entryPrefab => _skillEntryPrefab.gameObject;
        /// <summary>
        /// Gets the current list entries.
        /// </summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();

        [FormerlySerializedAs("trackedWeaponTypes")]
        [SerializeField] private List<ItemWeaponType> _trackedWeaponTypes = new();
        private PlayerExperience _playerExperience;

        protected override void Awake()
        {
            _instance = this;

            Game.Shared.Networking.ActorEndpoint.onPlayerActorAssigned += OnPlayerActorAssigned;
            Game.Shared.Networking.CharacterEndpoint.onClientWeaponSkillUpdated += OnWeaponSkillUpdated;

            base.Awake();
        }

        private void OnPlayerActorAssigned(Actor actor)
        {
            if (actor == null) return;

            // Get the player's class.
            var classId = ClientAccountManager.ActiveCharacter.ClassID;
            var classDef = ClassDefinitionLibrary.Instance.GetDefinition(classId);

            _trackedWeaponTypes = new List<ItemWeaponType>(classDef.WeaponTypes);
            _playerExperience = actor.GetComponent<PlayerExperience>();

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

        /// <inheritdoc/>
        public override void Refresh()
        {
            // Update the corresponding entry in the UI list if it's currently displayed.
            foreach (var entry in currentEntries)
            {
                if (entry is UI_CharacterWeaponSkillEntry skillEntry)
                {
                    var weaponSkill = _playerExperience.WeaponSkills.FirstOrDefault(ws => ws.WeaponType == skillEntry.WeaponType);
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

        /// <summary>
        /// Executes the client-side <c>PopulateList</c> operation.
        /// </summary>
        public void PopulateList(bool forceClear = false)
        {
            if (ClientAccountManager.PlayerActor == null || _playerExperience == null) return;

            if (forceClear)
            {
                ClearEntries();
            }

            int index = 0;
            foreach (var weaponType in _trackedWeaponTypes)
            {
                var entryObj = Instantiate(_skillEntryPrefab, _container.transform);
                var entry = entryObj.GetComponent<UI_CharacterWeaponSkillEntry>();
                entry.Initialize(weaponType, index);
                entry.Register(this);

                // Try live data first, fall back to saved character data
                var weaponSkill = _playerExperience.WeaponSkills.FirstOrDefault(ws => ws.WeaponType == weaponType);
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
