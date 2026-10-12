using Game.Shared.Data;
using Game.Shared.Utility;
using TMPro;
using UnityEngine;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterWeaponSkillEntry</c> implementation.
    /// </summary>
    public sealed class UI_CharacterWeaponSkillEntry : UI_ListEntry<ItemWeaponType>
    {
        [SerializeField] private TextMeshProUGUI weaponTypeText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private UI_CharacterWeaponSkillBar weaponSkillBar;

        private ItemWeaponType weaponType;
        /// <summary>
        /// Gets the <c>WeaponType</c> value.
        /// </summary>
        public ItemWeaponType WeaponType => weaponType;

        private int index;

        /// <inheritdoc/>
        public override void Initialize(ItemWeaponType _weaponType, int _index)
        {
            this.index = _index;
            this.weaponType = _weaponType;

            weaponTypeText.text = weaponType.ToString().ToLower();
        }

        /// <summary>
        /// Executes the client-side <c>UpdateWeaponSkill</c> operation.
        /// </summary>
        public void UpdateWeaponSkill(int level, int experience)
        {
            levelText.text = $"{level}";

            var progress = (float)experience / (float)ExperienceCalculator.GetExpForLevel(level);
            weaponSkillBar.SetProgress(progress);
        }

        /// <inheritdoc/>
        public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            // No click behavior for stat entries in this implementation.
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}