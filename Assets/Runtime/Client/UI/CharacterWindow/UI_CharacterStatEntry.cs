using Game.Shared.Data;
using Game.Shared.Networking;
using Game.Client.Utility;
using TMPro;
using UnityEngine;
using Game.Shared;
using UnityEngine.UI;

namespace Game.Client.UI
{
    public sealed class UI_CharacterStatEntry : UI_ListEntry<ActorStatDefinition>
    {
        [SerializeField] private TextMeshProUGUI statNameText;
        [SerializeField] private TextMeshProUGUI currentValueText;
        [SerializeField] private Image statIcon;

        public ActorStatDefinition statDefinition;
        private int index;

        public void SetStat(StatSnapshotEntry statEntry)
        {
            statNameText.text = statEntry.StatId; // In a real implementation, you'd want to localize this.
            currentValueText.text = statEntry.EffectiveMax.ToString("0");
        }

        public void Refresh(ActorStatContainer statContainer)
        {
            var statEntry = statContainer.GetStat(statDefinition.DefinitionId);
            if (statEntry != null)
            {
                if (statDefinition.Category == ActorStatCategory.Resource)
                {
                    currentValueText.text = statEntry.EffectiveMaximum.ToString("0");
                }
                else
                {
                    currentValueText.text = statEntry.CurrentValue.ToString("0");
                }
            }
        }

        public override void Initialize(ActorStatDefinition _statDefinition, int _index)
        {
            // Metadata setup.
            index = _index;
            statDefinition = _statDefinition;

            // UI setup.
            statNameText.text = statDefinition.DisplayName;
            currentValueText.text = "0"; // Will be updated when stats are received.

            if (statIcon == null) return;

            if (statDefinition.Icon == null)
            {
                statIcon.gameObject.SetActive(false);
            }
            else
            {
                statIcon.gameObject.SetActive(true);
                statIcon.sprite = statDefinition.Icon;
            }
        }

        public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            // No click behavior for stat entries in this implementation.
        }

        public override void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);

            UI_TooltipWindow.Instance.Show(
                string.Empty,
                new UI_TooltipLine[]
                {
                    new UI_TooltipLine { text = new string[] { PlaceholderText.ResolveAll(statDefinition.Description) } },
                },
                string.Empty,
                null
            );
            UI_TooltipWindow.Instance.transform.position = eventData.position;
        }

        public override void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            UI_TooltipWindow.Instance.HideImmediate();
        }

        public void SetStatValue(float current)
        {
            currentValueText.text = $"{current:0}";
        }
    }
}