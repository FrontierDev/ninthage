namespace Game.Shared
{
    /// <summary>
    /// Implemented by VFX GameObjects that support an explicit open/reveal animation.
    /// Allows server-authoritative systems to restart or trigger the fade-in without
    /// a direct reference to Client assembly types.
    /// </summary>
    public interface IVFXFadeIn
    {
        /// <summary>
        /// Restarts the fade-in animation from the current state.
        /// Cancels any in-progress fade-out animation.
        /// </summary>
        void VFXFadeIn();
    }
}
