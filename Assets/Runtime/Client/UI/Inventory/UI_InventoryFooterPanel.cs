using Game.Shared.Runtime.Actors;
using TMPro;

using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_InventoryFooterPanel</c> implementation.
    /// </summary>
    public sealed class UI_InventoryFooterPanel : UI_Panel
    {
        [FormerlySerializedAs("capacityText")]
        [SerializeField] private TMP_Text _capacityText;
        [FormerlySerializedAs("currencyText")]
        [SerializeField] private TMP_Text _currencyText;

        protected override void Awake()
        {
            Game.Shared.Networking.CharacterEndpoint.onClientInventoryUpdated += OnClientInventoryUpdated;
            Game.Shared.Networking.CharacterEndpoint.onClientCurrencyUpdated += OnClientCurrencyUpdated;
            base.Awake();
        }

        private void OnClientInventoryUpdated()
        {
            var inventory = ClientAccountManager.PlayerActor.GetComponent<Game.Shared.Runtime.Actors.PlayerInventory>();
            SetCapacity(inventory.Inventory.Count, inventory.MaxSlots);
        }

        private void OnClientCurrencyUpdated()
        {
            // var inventory = ClientAccountManager.PlayerActor.GetComponent<Game.Shared.Runtime.Actors.PlayerInventory>();
            // SetCapacity(inventory.Inventory.Count, inventory.MaxSlots);
        }

        private void SetCapacity(int current, int max)
        {
            _capacityText.text = $"{current}/{max}";
        }

        private void SetCurrency(int current)
        {
            _currencyText.text = $"{current}";
        }
    }
}
