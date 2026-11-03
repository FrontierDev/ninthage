using System;
using System.Collections.Generic;
using System.Linq;
using Game.Client;
using Game.Client.UI;
using TMPro;
using UnityEngine;

namespace Game.Client.UI.WorldSelection
{
    /// <summary>Owns the transient World catalogue, filters, sorting and selection presentation.</summary>
    public sealed class UI_WorldSelectionPanel : UI_Panel
    {
        private const int STATUS_MAX_LENGTH = 256;

        private static readonly string[] TYPE_FILTER_OPTIONS =
        {
            "All",
            "Casual",
            "PvP",
            "Roleplay",
            "Development"
        };

        private static readonly string[] AVAILABILITY_FILTER_OPTIONS =
        {
            "All",
            "Available",
            "Unavailable"
        };

        private enum SortColumn
        {
            World,
            Location,
            Type,
            PlayersOnline,
            Status
        }

        [SerializeField] private TMP_InputField _filterInput;
        [SerializeField] private TMP_Dropdown _typeFilter;
        [SerializeField] private TMP_Dropdown _availabilityFilter;
        [SerializeField] private UI_WorldListPanel _worldListPanel;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private UI_Button _confirmButton;
        [SerializeField] private TMP_Text _homeWorldText;

        private readonly List<ClientMasterSelectionProtocol.WorldSelectionWorld> _worlds = new();
        private Guid _homeWorldID;
        private long _accountRevision;
        private Guid? _selectedWorldID;
        private SortColumn? _sortColumn;
        private bool _sortAscending;

        protected override void Awake()
        {
            base.Awake();
            ConfigureFilters();

            _filterInput?.onValueChanged.AddListener(OnFilterChanged);
            _typeFilter?.onValueChanged.AddListener(OnTypeFilterChanged);
            _availabilityFilter?.onValueChanged.AddListener(OnAvailabilityFilterChanged);
        }

        private void OnDestroy()
        {
            _filterInput?.onValueChanged.RemoveListener(OnFilterChanged);
            _typeFilter?.onValueChanged.RemoveListener(OnTypeFilterChanged);
            _availabilityFilter?.onValueChanged.RemoveListener(OnAvailabilityFilterChanged);
        }

        /// <summary>Copies an accepted Master catalogue into the panel's transient snapshot.</summary>
        public void Initialize(ClientMasterSelectionProtocol.WorldSelectionCatalogueResponse catalogue)
        {
            if (catalogue == null)
                throw new ArgumentNullException(nameof(catalogue));
            if (catalogue.Worlds == null ||
                !catalogue.HomeWorldID.HasValue ||
                catalogue.HomeWorldID.Value == Guid.Empty ||
                !catalogue.AccountRevision.HasValue ||
                catalogue.AccountRevision.Value < 1)
            {
                throw new InvalidOperationException("World catalogue is not an accepted, validated response.");
            }

            ConfigureFilters();
            _worlds.Clear();
            _worlds.AddRange(catalogue.Worlds);
            _homeWorldID = catalogue.HomeWorldID.Value;
            _accountRevision = catalogue.AccountRevision.Value;
            _selectedWorldID = null;
            _sortColumn = null;
            _sortAscending = true;
            SetDefaultFilters();
            SetStatus(string.Empty);
            ClientConnectionManager.Instance.ClearSelectedWorld();
            SetConfirmEnabled(false);

            UpdateHomeWorldText();

            ApplyFiltersAndPopulate();
        }

        /// <summary>Sorts the visible rows by World name.</summary>
        public void SortByWorld() => SetSortColumn(SortColumn.World);

        /// <summary>Sorts the visible rows by Location.</summary>
        public void SortByLocation() => SetSortColumn(SortColumn.Location);

        /// <summary>Sorts the visible rows by World type.</summary>
        public void SortByType() => SetSortColumn(SortColumn.Type);

        /// <summary>Sorts the visible rows by online player count.</summary>
        public void SortByPlayersOnline() => SetSortColumn(SortColumn.PlayersOnline);

        /// <summary>Sorts the visible rows by access status.</summary>
        public void SortByStatus() => SetSortColumn(SortColumn.Status);

        /// <summary>Selects one available World without requesting allocation.</summary>
        public void SelectWorld(Guid worldID)
        {
            ClientMasterSelectionProtocol.WorldSelectionWorld world = _worlds.FirstOrDefault(
                candidate => candidate.WorldID == worldID);
            if (world == null ||
                world.AccessState != ClientMasterSelectionProtocol.WorldSelectionAccessState.Available)
            {
                return;
            }

            _selectedWorldID = worldID;
            ClientConnectionManager.Instance.SelectWorld(worldID);
            RefreshSelectionFocus();
            SetConfirmEnabled(true);
        }

        /// <summary>Requests Master approval to enter the explicitly selected World.</summary>
        public void ConfirmSelection()
        {
            if (!_selectedWorldID.HasValue)
            {
                SetConfirmEnabled(false);
                return;
            }

            SetConfirmEnabled(false);
            SetStatus("Connecting...");
            ClientConnectionManager.Instance.RequestWorldEntry(_selectedWorldID.Value);
        }

        /// <summary>Requests a Home World change using the current catalogue revision.</summary>
        public void RequestHomeWorldChange(Guid worldID)
        {
            ClientConnectionManager.Instance.RequestHomeWorldChange(worldID, _accountRevision);
        }

        /// <summary>Applies an accepted Home World response without changing row entitlements.</summary>
        internal void ApplyHomeWorldUpdate(Guid homeWorldID, long accountRevision)
        {
            if (homeWorldID == Guid.Empty || accountRevision < 1)
            {
                ShowStatus("Malformed or unavailable service.");
                return;
            }

            _homeWorldID = homeWorldID;
            _accountRevision = accountRevision;
            foreach (ClientMasterSelectionProtocol.WorldSelectionWorld world in _worlds)
                world.IsHomeWorld = world.WorldID == _homeWorldID;

            UpdateHomeWorldText();
            SetStatus(string.Empty);
            ApplyFiltersAndPopulate();
        }

        /// <summary>Shows a bounded World Selection status message.</summary>
        internal void ShowStatus(string message)
        {
            SetStatus(message);
        }

        /// <summary>Displays a World-entry failure and restores confirmation when selection remains valid.</summary>
        internal void SetWorldEntryFailure(string message)
        {
            SetStatus(message);
            bool hasSelectedAvailableWorld = _selectedWorldID.HasValue && _worlds.Any(
                world => world.WorldID == _selectedWorldID.Value &&
                    world.AccessState == ClientMasterSelectionProtocol.WorldSelectionAccessState.Available);
            SetConfirmEnabled(hasSelectedAvailableWorld);
        }

        /// <summary>Returns to the ordinary Main Menu login state and clears the local snapshot.</summary>
        public void ReturnToLogin()
        {
            HideImmediate();
            ClientConnectionManager.Instance.ClearPreWorldSelectionState();
            ClearSnapshot();
            UI_MainMenuWindow.Instance?.Show();
        }

        private void ConfigureFilters()
        {
            if (_typeFilter != null)
            {
                _typeFilter.ClearOptions();
                _typeFilter.AddOptions(new List<string>(TYPE_FILTER_OPTIONS));
            }

            if (_availabilityFilter != null)
            {
                _availabilityFilter.ClearOptions();
                _availabilityFilter.AddOptions(new List<string>(AVAILABILITY_FILTER_OPTIONS));
            }
        }

        private void SetDefaultFilters()
        {
            _filterInput?.SetTextWithoutNotify(string.Empty);
            _typeFilter?.SetValueWithoutNotify(0);
            _availabilityFilter?.SetValueWithoutNotify(0);
        }

        private void OnFilterChanged(string filter)
        {
            ApplyFiltersAndPopulate();
        }

        private void OnTypeFilterChanged(int value)
        {
            ApplyFiltersAndPopulate();
        }

        private void OnAvailabilityFilterChanged(int value)
        {
            ApplyFiltersAndPopulate();
        }

        private void SetSortColumn(SortColumn sortColumn)
        {
            if (_sortColumn == sortColumn)
                _sortAscending = !_sortAscending;
            else
            {
                _sortColumn = sortColumn;
                _sortAscending = true;
            }

            ApplyFiltersAndPopulate();
        }

        private void ApplyFiltersAndPopulate()
        {
            if (_worldListPanel == null)
                throw new InvalidOperationException("UI_WorldSelectionPanel requires a World List Panel reference.");

            string filter = _filterInput == null ? string.Empty : _filterInput.text.Trim();
            int typeFilter = _typeFilter == null ? 0 : _typeFilter.value;
            int availabilityFilter = _availabilityFilter == null ? 0 : _availabilityFilter.value;

            List<ClientMasterSelectionProtocol.WorldSelectionWorld> visibleWorlds = _worlds
                .Where(world => MatchesFilter(world, filter, typeFilter, availabilityFilter))
                .ToList();

            if (_selectedWorldID.HasValue && !visibleWorlds.Any(
                    world => world.WorldID == _selectedWorldID.Value))
            {
                _selectedWorldID = null;
                ClientConnectionManager.Instance.ClearSelectedWorld();
                SetConfirmEnabled(false);
            }

            if (_sortColumn.HasValue)
                visibleWorlds.Sort(CompareWorlds);

            _worldListPanel.PopulateRows(visibleWorlds);
            RefreshSelectionFocus();
        }

        private static bool MatchesFilter(
            ClientMasterSelectionProtocol.WorldSelectionWorld world,
            string filter,
            int typeFilter,
            int availabilityFilter)
        {
            if (!string.IsNullOrEmpty(filter) &&
                (world.WorldName?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) ?? -1) < 0 &&
                (world.Location?.IndexOf(filter, StringComparison.OrdinalIgnoreCase) ?? -1) < 0)
            {
                return false;
            }

            if (typeFilter > 0 &&
                !string.Equals(
                    world.WorldType,
                    GetWorldTypeFilterValue(typeFilter),
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (availabilityFilter == 1 &&
                world.AccessState != ClientMasterSelectionProtocol.WorldSelectionAccessState.Available)
            {
                return false;
            }

            if (availabilityFilter == 2 &&
                world.AccessState == ClientMasterSelectionProtocol.WorldSelectionAccessState.Available)
            {
                return false;
            }

            return true;
        }

        private static string GetWorldTypeFilterValue(int typeFilter)
        {
            return typeFilter switch
            {
                1 => "casual",
                2 => "pvp",
                3 => "roleplay",
                4 => "development",
                _ => string.Empty
            };
        }

        private int CompareWorlds(
            ClientMasterSelectionProtocol.WorldSelectionWorld left,
            ClientMasterSelectionProtocol.WorldSelectionWorld right)
        {
            int comparison = _sortColumn.Value switch
            {
                SortColumn.World => StringComparer.OrdinalIgnoreCase.Compare(left.WorldName, right.WorldName),
                SortColumn.Location => StringComparer.OrdinalIgnoreCase.Compare(left.Location, right.Location),
                SortColumn.Type => StringComparer.OrdinalIgnoreCase.Compare(left.WorldType, right.WorldType),
                SortColumn.PlayersOnline => left.OnlinePlayerCount.CompareTo(right.OnlinePlayerCount),
                SortColumn.Status => GetStatusSortOrder(left.AccessState).CompareTo(
                    GetStatusSortOrder(right.AccessState)),
                _ => 0
            };

            if (!_sortAscending)
                comparison = -comparison;
            if (comparison != 0)
                return comparison;

            comparison = StringComparer.OrdinalIgnoreCase.Compare(left.WorldName, right.WorldName);
            return comparison != 0 ? comparison : left.WorldID.CompareTo(right.WorldID);
        }

        private static int GetStatusSortOrder(
            ClientMasterSelectionProtocol.WorldSelectionAccessState accessState)
        {
            return accessState switch
            {
                ClientMasterSelectionProtocol.WorldSelectionAccessState.Available => 0,
                ClientMasterSelectionProtocol.WorldSelectionAccessState.MembershipRequired => 1,
                ClientMasterSelectionProtocol.WorldSelectionAccessState.DevelopmentAccessRequired => 2,
                ClientMasterSelectionProtocol.WorldSelectionAccessState.AdditionalRequirementsNotEvaluated => 3,
                ClientMasterSelectionProtocol.WorldSelectionAccessState.Offline => 4,
                _ => int.MaxValue
            };
        }

        private void RefreshSelectionFocus()
        {
            if (_worldListPanel == null)
                return;

            foreach (UI_ListEntry listEntry in _worldListPanel.currentEntries)
            {
                if (listEntry is not UI_WorldListEntry worldEntry)
                    continue;

                if (_selectedWorldID.HasValue && worldEntry.WorldID == _selectedWorldID.Value)
                    worldEntry.Focus();
                else
                    worldEntry.Unfocus();
            }
        }

        private void ClearSnapshot()
        {
            _worlds.Clear();
            _homeWorldID = Guid.Empty;
            _accountRevision = 0;
            _selectedWorldID = null;
            _sortColumn = null;
            _sortAscending = true;
            SetDefaultFilters();
            SetStatus(string.Empty);
            if (_homeWorldText != null)
                _homeWorldText.text = string.Empty;
            SetConfirmEnabled(false);
            _worldListPanel?.ClearRows();
        }

        private void SetConfirmEnabled(bool enabled)
        {
            if (enabled)
                _confirmButton?.EnableButton();
            else
                _confirmButton?.DisableButton();
        }

        private void UpdateHomeWorldText()
        {
            if (_homeWorldText == null)
                return;

            ClientMasterSelectionProtocol.WorldSelectionWorld homeWorld = _worlds.FirstOrDefault(
                world => world.WorldID == _homeWorldID);
            _homeWorldText.text = homeWorld == null ? string.Empty : $"Home World: {homeWorld.WorldName}";
        }

        private void SetStatus(string message)
        {
            if (_statusText == null)
                return;

            string boundedMessage = string.IsNullOrWhiteSpace(message)
                ? string.Empty
                : message.Trim();
            if (boundedMessage.Length > STATUS_MAX_LENGTH)
                boundedMessage = boundedMessage.Substring(0, STATUS_MAX_LENGTH);

            _statusText.text = boundedMessage;
        }
    }
}
