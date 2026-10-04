namespace Game.Client.UI
{
    public sealed class UI_CharacterWeaponSkillBar : UI_ProgressBar
    {
        private UI_CharacterWeaponSkillEntry weaponSkillEntry;

        private void Awake()
        {
            SetMaterial(graphics.material);
        }
    }
}