namespace Game.Shared
{
    /// <summary>
    /// Implemented by VFX GameObjects that support a one-shot triggered effect.
    /// Allows server-authoritative systems to fire additional visuals (e.g. a pulse
    /// wave, shockwave ring, etc.) without a direct reference to Client assembly types.
    /// </summary>
    public interface IVFXTrigger
    {
        /// <summary>
        /// Fires the one-shot triggered effect (e.g. a pulse wave ring).
        /// Safe to call while other animations are running.
        /// </summary>
        void Trigger();
    }
}
