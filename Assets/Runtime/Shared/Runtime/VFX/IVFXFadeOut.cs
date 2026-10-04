namespace Game.Shared
{
    /// <summary>
    /// Implemented by VFX GameObjects that manage their own close/destroy animation.
    /// Allows server-authoritative systems (e.g. <see cref="ActorSpellcaster"/>) to
    /// trigger a graceful shutdown without a direct reference to Client assembly types.
    /// </summary>
    public interface IVFXFadeOut
    {
        /// <summary>
        /// Triggers the close animation. The implementor is responsible for
        /// destroying the GameObject once the animation completes.
        /// </summary>
        void VFXFadeOut();
    }
}
