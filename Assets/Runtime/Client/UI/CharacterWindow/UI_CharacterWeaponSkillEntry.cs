using Game.Shared.Data;
using Game.Shared.Utility;
using TMPro;
using UnityEngine;

namespace Game.Client.UI
{
    public sealed class UI_CharacterWeaponSkillEntry : UI_ListEntry<ItemWeaponType>
    {
        [SerializeField] private TextMeshProUGUI weaponTypeText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private UI_CharacterWeaponSkillBar weaponSkillBar;

        private ItemWeaponType weaponType;
        public ItemWeaponType WeaponType => weaponType;

        private int index;

        public override void Initialize(ItemWeaponType _weaponType, int _index)
        {
            this.index = _index;
            this.weaponType = _weaponType;

            weaponTypeText.text = weaponType.ToString().ToLower();
        }

        public void UpdateWeaponSkill(int level, int experience)
        {
            levelText.text = $"{level}";

            var progress = (float)experience / (float)ExperienceCalculator.GetExpForLevel(level);
            weaponSkillBar.SetProgress(progress);
        }

        public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            // No click behavior for stat entries in this implementation.
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}