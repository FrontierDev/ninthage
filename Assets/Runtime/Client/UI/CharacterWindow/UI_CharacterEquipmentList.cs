using Game.Shared;
using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Shared.Runtime.Actors;
using Game.Shared.Runtime.Items;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterEquipmentList</c> implementation.
    /// </summary>
    public sealed class UI_CharacterEquipmentList : UI_Panel, IListPanel
    {
        [SerializeField] private GameObject container;
        [SerializeField] private UI_CharacterEquipmentEntry slotEntryPrefab;
        [SerializeField] private List<ItemSlot> equipmentSlots;
        GameObject IListPanel.contentContainer => container;
        GameObject IListPanel.entryPrefab => slotEntryPrefab.gameObject;
        /// <summary>
        /// Gets the current list entries.
        /// </summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();

        protected override void Awake()
        {
            foreach (var slot in GetComponentsInChildren<UI_CharacterEquipmentEntry>())
            {
                currentEntries.Add(slot);
            }

            Game.Shared.Networking.ActorEndpoint.onPlayerActorAssigned += OnPlayerActorAssigned;
            CharacterEndpoint.onClientEquipmentUpdated += Refresh;

            base.Awake();
        }

        private void OnDestroy()
        {
            Game.Shared.Networking.ActorEndpoint.onPlayerActorAssigned -= OnPlayerActorAssigned;
            CharacterEndpoint.onClientEquipmentUpdated -= Refresh;
        }

        private void OnPlayerActorAssigned(Actor actor)
        {
            Refresh();
        }

        /// <inheritdoc/>
        public override void Refresh()
        {
            base.Refresh();
            PopulateList(false);
        }

        /// <summary>
        /// Executes the client-side <c>PopulateList</c> operation.
        /// </summary>
        public void PopulateList(bool forceClear = false)
        {
            if (forceClear)
            {
                foreach (var entry in currentEntries)
                {
                    Destroy(entry.gameObject);
                }
                currentEntries.Clear();
            }

            if (currentEntries.Count == 0)
            {
                foreach (var slot in equipmentSlots.OrderBy(x => (int)x))
                {
                    var entry = Instantiate(slotEntryPrefab, container.transform);
                    entry.Initialize(null, (int)slot);
                    entry.Register(this);
                    currentEntries.Add(entry);
                }

                PopulateList(false);
            }
            else
            {
                foreach (var _slot in currentEntries)
                {
                    var equipmentSlot = _slot as UI_CharacterEquipmentEntry;
                    var slot = equipmentSlot.slotId;
                    if (ClientAccountManager.PlayerActor.GetComponent<PlayerEquipment>().TryGetItem(slot, out ItemInstance item))
                    {
                        equipmentSlot.SetItem(item);
                    }
                    else
                    {
                        equipmentSlot.SetItem(null);
                    }
                }
            }
        }
    }
}
