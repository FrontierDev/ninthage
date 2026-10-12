using Game.Shared.Data;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_ReputationHeaderEntry</c> implementation.
    /// </summary>
    public sealed class UI_ReputationHeaderEntry : UI_ListEntry<FactionDefinition>
    {
        [FormerlySerializedAs("progressBar")]
        [SerializeField] private UI_ProgressBar _progressBar;
        [FormerlySerializedAs("factionIcon")]
        [SerializeField] private Image _factionIcon;

        private FactionDefinition _data;
        /// <summary>
        /// Gets the <c>Data</c> value.
        /// </summary>
        public FactionDefinition Data => _data;
        private float _progress;
        private int _index;

        /// <inheritdoc/>
        public override void Initialize(FactionDefinition data, int index)
        {
            this._data = data;
            this._index = index;

            _factionIcon.sprite = data.Icon;
            _progressBar.SetMaterial(_progressBar.graphics.material); // Ensure we have a unique material instance for this progress bar
        }

        /// <summary>
        /// Executes the client-side <c>UpdateEntry</c> operation.
        /// </summary>
        public void UpdateEntry(int rep)
        {
            // Get reputation progress. For factions we can go to war with, we need to offset the progress
            // by 50% so that '0' sits at 0.5.
            if (_data == null)
            {
                Debug.LogWarning("UI_ReputationHeaderEntry.UpdateEntry called before Initialize; data is null");
                return;
            }

            if (_progressBar == null)
            {
                Debug.LogWarning("UI_ReputationHeaderEntry.UpdateEntry: progressBar not assigned");
                return;
            }

            var denom = 2f * _data.MaxPoints;
            _progress = denom != 0f ? ((float)rep / denom) + (_data.CanWar ? 0.5f : 0f) : (_data.CanWar ? 0.5f : 0f);
            _progress = Mathf.Clamp01(_progress);
            _progressBar.SetProgress(_progress);

            // Set color based on whether we're in negative or positive territory, or if we've reached max rep
            if (_data.CanWar)
            {
                if (rep < 0)
                    _progressBar.SetColor(new Color(0.91f, 0.541f, 0.322f));
                else if (rep >= 0)
                    _progressBar.SetColor(new Color(0.22f, 0.725f, 0.525f));
            }
            else
            {
                _progressBar.SetColor(new Color(0.22f, 0.725f, 0.525f));
            }
        }

        /// <inheritdoc/>
        public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            UI_ReputationBodyPanel.Instance.SetFaction(_data, _progress);
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}
