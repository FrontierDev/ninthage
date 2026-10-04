using System.Diagnostics;
using Game.Shared.Networking;
using Game.Shared.Utility;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Client.UI
{
    public sealed class UI_ExperienceBar : UI_ProgressBar
    {
        private void Awake()
        {
            SetMaterial(graphics.material);

            CharacterService.onClientExperienceUpdated += OnExperienceUpdated;
        }

        private void OnDestroy()
        {
            CharacterService.onClientExperienceUpdated -= OnExperienceUpdated;
        }

        private void OnExperienceUpdated(int level, int experience, int delta)
        {
            float progress = (float)experience / (float)ExperienceCalculator.GetExpForLevel(level);
            Debug.Log($"Experience updated: Level {level}, Experience {experience}, Progress {progress * 100}%");
            SetProgress(progress);
        }
    }
}