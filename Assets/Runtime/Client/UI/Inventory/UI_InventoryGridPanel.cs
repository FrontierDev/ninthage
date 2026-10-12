using Game.Shared.Runtime.Actors;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_InventoryGridPanel</c> implementation.
    /// </summary>
    public sealed class UI_InventoryGridPanel : UI_Panel, IListPanel
    {
        private static UI_InventoryGridPanel _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_InventoryGridPanel Instance => _instance;

        [FormerlySerializedAs("container")]
        [SerializeField] private GameObject _container;
        [FormerlySerializedAs("slotEntryPrefab")]
        [SerializeField] private UI_InventorySlotEntry _slotEntryPrefab;
        GameObject IListPanel.contentContainer => _container;
        GameObject IListPanel.entryPrefab => _slotEntryPrefab.gameObject;
        /// <summary>
        /// Gets the current list entries.
        /// </summary>
        public List<UI_ListEntry> currentEntries { get; set; } = new();

        protected override void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;

            Game.Shared.Networking.CharacterEndpoint.onClientInventoryUpdated += Refresh;
            Game.Shared.Networking.ActorEndpoint.onPlayerActorAssigned += OnPlayerActorAssigned;

            base.Awake();
        }

        private void OnDestroy()
        {
            Game.Shared.Networking.CharacterEndpoint.onClientInventoryUpdated -= Refresh;
            Game.Shared.Networking.ActorEndpoint.onPlayerActorAssigned -= OnPlayerActorAssigned;

            if (_instance == this)
                _instance = null;
        }

        private void OnPlayerActorAssigned(Game.Shared.Runtime.Actors.Actor actor)
        {
            Refresh();
        }

        /// <inheritdoc/>
        public override void Refresh()
        {
            base.Refresh();
            PopulateList(forceClear: true);
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
            if (ClientAccountManager.PlayerActor == null) return;

            var inventory = ClientAccountManager.PlayerActor.GetComponent<Game.Shared.Runtime.Actors.PlayerInventory>();
            if (inventory == null) return;

            var items = inventory.Inventory;
            var slots = inventory.MaxSlots;

            if (forceClear)
                ClearEntries();

            for (int i = 0; i < slots; i++)
            {
                var item = items.ContainsKey(i) ? items[i] : null;
                var entryObj = Instantiate(_slotEntryPrefab.gameObject, _container.transform);
                var entry = entryObj.GetComponent<UI_InventorySlotEntry>();
                entry.Initialize(item, i);
                entry.Register(this);
                currentEntries.Add(entry);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_container.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponentInParent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(UI_InventoryWindow.Instance.GetComponent<RectTransform>());
        }
    }
}
