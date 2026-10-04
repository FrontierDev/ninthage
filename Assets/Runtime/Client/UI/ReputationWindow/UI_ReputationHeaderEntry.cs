using Game.Shared.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public sealed class UI_ReputationHeaderEntry : UI_ListEntry<FactionDefinition>
    {
        [SerializeField] private UI_ProgressBar progressBar;
        [SerializeField] private Image factionIcon;

        private FactionDefinition data;
        public FactionDefinition Data => data;
        private float progress;
        private int index;

        public override void Initialize(FactionDefinition data, int index)
        {
            this.data = data;
            this.index = index;

            factionIcon.sprite = data.Icon;
            progressBar.SetMaterial(progressBar.graphics.material); // Ensure we have a unique material instance for this progress bar
        }

        public void UpdateEntry(int rep)
        {
            // Get reputation progress. For factions we can go to war with, we need to offset the progress
            // by 50% so that '0' sits at 0.5.
            if (data == null)
            {
                Debug.LogWarning("UI_ReputationHeaderEntry.UpdateEntry called before Initialize; data is null");
                return;
            }

            if (progressBar == null)
            {
                Debug.LogWarning("UI_ReputationHeaderEntry.UpdateEntry: progressBar not assigned");
                return;
            }

            var denom = 2f * data.MaxPoints;
            progress = denom != 0f ? ((float)rep / denom) + (data.CanWar ? 0.5f : 0f) : (data.CanWar ? 0.5f : 0f);
            progress = Mathf.Clamp01(progress);
            progressBar.SetProgress(progress);

            // Set color based on whether we're in negative or positive territory, or if we've reached max rep
            if (data.CanWar)
            {
                if (rep < 0)
                    progressBar.SetColor(new Color(0.91f, 0.541f, 0.322f));
                else if (rep >= 0)
                    progressBar.SetColor(new Color(0.22f, 0.725f, 0.525f));
            }
            else
            {
                progressBar.SetColor(new Color(0.22f, 0.725f, 0.525f));
            }
        }

        public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            UI_ReputationBodyPanel.Instance.SetFaction(data, progress);
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}