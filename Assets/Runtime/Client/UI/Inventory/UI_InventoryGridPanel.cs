using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public sealed class UI_InventoryGridPanel : UI_Panel, IListPanel
    {
        private static UI_InventoryGridPanel _instance;
        public static UI_InventoryGridPanel Instance => _instance;

        [SerializeField] private GameObject container;
        [SerializeField] private UI_InventorySlotEntry slotEntryPrefab;
        GameObject IListPanel.contentContainer => container;
        GameObject IListPanel.entryPrefab => slotEntryPrefab.gameObject;
        public List<UI_ListEntry> currentEntries { get; set; } = new List<UI_ListEntry>();

        protected override void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;

            Game.Shared.Networking.CharacterService.onClientInventoryUpdated += Refresh;
            Game.Shared.Networking.ActorService.onPlayerActorAssigned += (actor) => Refresh();

            base.Awake();
        }

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

        public void PopulateList(bool forceClear = false)
        {
            if (ClientAccountManager.PlayerActor == null) return;

            var inventory = ClientAccountManager.PlayerActor.GetComponent<Game.Shared.PlayerInventory>();
            if (inventory == null) return;

            var items = inventory.Inventory;
            var slots = inventory.MaxSlots;

            if (forceClear)
                ClearEntries();

            for (int i = 0; i < slots; i++)
            {
                var item = items.ContainsKey(i) ? items[i] : null;
                var entryObj = Instantiate(slotEntryPrefab.gameObject, container.transform);
                var entry = entryObj.GetComponent<UI_InventorySlotEntry>();
                entry.Initialize(item, i);
                entry.Register(this);
                currentEntries.Add(entry);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponentInParent<RectTransform>());
            LayoutRebuilder.ForceRebuildLayoutImmediate(UI_InventoryWindow.Instance.GetComponent<RectTransform>());
        }
    }
}