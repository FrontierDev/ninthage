namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_CharacterWeaponSkillBar</c> implementation.
    /// </summary>
    public sealed class UI_CharacterWeaponSkillBar : UI_ProgressBar
    {
        private UI_CharacterWeaponSkillEntry _weaponSkillEntry;

        private void Awake()
        {
            SetMaterial(graphics.material);
        }
    }
}
