using Debug = Game.Shared.Utility.FormattedDebug;
using Game.Shared.Data;
using Game.Shared.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterWeaponSkillEntry</c> implementation.
    /// </summary>
    public sealed class UI_CharacterWeaponSkillEntry : UI_ListEntry<ItemWeaponType>
    {
        [FormerlySerializedAs("weaponTypeText")]
        [SerializeField] private TextMeshProUGUI _weaponTypeText;
        [FormerlySerializedAs("levelText")]
        [SerializeField] private TextMeshProUGUI _levelText;
        [FormerlySerializedAs("weaponSkillBar")]
        [SerializeField] private UI_CharacterWeaponSkillBar _weaponSkillBar;

        private ItemWeaponType _weaponType;
        /// <summary>
        /// Gets the <c>WeaponType</c> value.
        /// </summary>
        public ItemWeaponType WeaponType => _weaponType;

        private int _index;

        /// <inheritdoc/>
        public override void Initialize(ItemWeaponType _weaponType, int _index)
        {
            this._index = _index;
            this._weaponType = _weaponType;

            _weaponTypeText.text = _weaponType.ToString().ToLower();
        }

        /// <summary>
        /// Executes the client-side <c>UpdateWeaponSkill</c> operation.
        /// </summary>
        public void UpdateWeaponSkill(int level, int experience)
        {
            _levelText.text = $"{level}";

            if (level < ExperienceCalculator.MIN_LEVEL
                || level > ExperienceCalculator.MAX_LEVEL)
            {
                Debug.Warning(
                    $"Weapon skill '{_weaponType}' has invalid level {level}; displaying zero progress.");
                _weaponSkillBar.SetProgress(0f);
                return;
            }

            _weaponSkillBar.SetProgress(
                ExperienceCalculator.GetLevelProgress01(level, experience));
        }

        /// <inheritdoc/>
        public override void OnClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            // No click behavior for stat entries in this implementation.
            base.OnClick(eventData); // Play SFX if assigned in the inspector.
        }
    }
}
