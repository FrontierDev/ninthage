using System;
using System.Globalization;
using Game.Client;
using Game.Client.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Client.UI.WorldSelection
{
    /// <summary>Displays and handles one World Selection row.</summary>
    public sealed class UI_WorldListEntry : UI_ListEntry<ClientMasterSelectionProtocol.WorldSelectionWorld>
    {
        [SerializeField] private UnityEngine.UI.Button _favouriteButton;
        [SerializeField] private TMP_Text _favouriteText;
        [SerializeField] private TMP_Text _worldNameText;
        [SerializeField] private TMP_Text _locationText;
        [SerializeField] private TMP_Text _typeText;
        [SerializeField] private TMP_Text _playersOnlineText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private GameObject _offlineOverlay;
        [SerializeField] private GameObject _selectionHighlight;

        private ClientMasterSelectionProtocol.WorldSelectionWorld _data;
        private UI_WorldSelectionPanel _panel;

        /// <summary>Gets the World ID represented by this row.</summary>
        public Guid WorldID => _data == null ? Guid.Empty : _data.WorldID;

        /// <inheritdoc/>
        public override void Start()
        {
            base.Start();
            _favouriteButton?.onClick.AddListener(OnFavouriteButtonClicked);
        }

        private void OnDestroy()
        {
            _favouriteButton?.onClick.RemoveListener(OnFavouriteButtonClicked);
        }

        /// <inheritdoc/>
        public override void Initialize(
            ClientMasterSelectionProtocol.WorldSelectionWorld data,
            int index)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _panel = GetComponentInParent<UI_WorldSelectionPanel>();

            if (_worldNameText != null)
                _worldNameText.text = data.WorldName;
            if (_locationText != null)
                _locationText.text = data.Location;
            if (_typeText != null)
                _typeText.text = GetWorldTypeDisplay(data.WorldType);
            if (_playersOnlineText != null)
                _playersOnlineText.text = data.OnlinePlayerCount.ToString(CultureInfo.InvariantCulture);
            if (_statusText != null)
                _statusText.text = GetStatusDisplay(data.AccessState);

            bool isOffline =
                data.AccessState == ClientMasterSelectionProtocol.WorldSelectionAccessState.Offline;
            if (_offlineOverlay != null)
                _offlineOverlay.SetActive(isOffline);

            if (button != null)
            {
                button.interactable =
                    data.AccessState ==
                    ClientMasterSelectionProtocol.WorldSelectionAccessState.Available;
            }

            UpdateFavouriteGlyph();
        }

        /// <inheritdoc/>
        public override void OnClick(PointerEventData eventData = default)
        {
            if (_data != null &&
                _data.AccessState == ClientMasterSelectionProtocol.WorldSelectionAccessState.Available)
            {
                _panel?.SelectWorld(_data.WorldID);
            }

            base.OnClick(eventData);
        }

        /// <summary>Requests this row's World as the account Home World.</summary>
        public void OnFavouriteButtonClicked()
        {
            if (_data != null)
                _panel?.RequestHomeWorldChange(_data.WorldID);
        }

        /// <inheritdoc/>
        public override void Focus()
        {
            isFocused = true;
            if (_selectionHighlight != null)
                _selectionHighlight.SetActive(true);
        }

        /// <inheritdoc/>
        public override void Unfocus()
        {
            isFocused = false;
            if (_selectionHighlight != null)
                _selectionHighlight.SetActive(false);
        }

        private void UpdateFavouriteGlyph()
        {
            if (_favouriteText != null)
                _favouriteText.text = _data.IsHomeWorld ? "★" : "☆";
        }

        private static string GetWorldTypeDisplay(string worldType)
        {
            return worldType?.ToLowerInvariant() switch
            {
                "casual" => "Casual",
                "pvp" => "PvP",
                "roleplay" => "Roleplay",
                "development" => "Development",
                _ => string.IsNullOrWhiteSpace(worldType)
                    ? string.Empty
                    : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(worldType.ToLowerInvariant())
            };
        }

        private static string GetStatusDisplay(
            ClientMasterSelectionProtocol.WorldSelectionAccessState accessState)
        {
            return accessState switch
            {
                ClientMasterSelectionProtocol.WorldSelectionAccessState.Available => "Available",
                ClientMasterSelectionProtocol.WorldSelectionAccessState.Offline => "Offline",
                ClientMasterSelectionProtocol.WorldSelectionAccessState.MembershipRequired =>
                    "Membership required",
                ClientMasterSelectionProtocol.WorldSelectionAccessState.DevelopmentAccessRequired =>
                    "Development access required",
                ClientMasterSelectionProtocol.WorldSelectionAccessState.AdditionalRequirementsNotEvaluated =>
                    "Additional requirements",
                _ => "Additional requirements"
            };
        }
    }
}
