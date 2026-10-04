using TMPro;
using UnityEngine;

namespace Game.Client.UI
{
    public sealed class UI_InventoryFooterPanel : UI_Panel
    {
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private TMP_Text currencyText;

        protected override void Awake()
        {
            Game.Shared.Networking.CharacterService.onClientInventoryUpdated += OnClientInventoryUpdated;
            Game.Shared.Networking.CharacterService.onClientCurrencyUpdated += OnClientCurrencyUpdated;
            base.Awake();
        }

        private void OnClientInventoryUpdated()
        {
            var inventory = ClientAccountManager.PlayerActor.GetComponent<Game.Shared.PlayerInventory>();
            SetCapacity(inventory.Inventory.Count, inventory.MaxSlots);
        }

        private void OnClientCurrencyUpdated()
        {
            // var inventory = ClientAccountManager.PlayerActor.GetComponent<Game.Shared.PlayerInventory>();
            // SetCapacity(inventory.Inventory.Count, inventory.MaxSlots);
        }

        private void SetCapacity(int current, int max)
        {
            capacityText.text = $"{current}/{max}";
        }

        private void SetCurrency(int current)
        {
            currencyText.text = $"{current}";
        }
    }
}