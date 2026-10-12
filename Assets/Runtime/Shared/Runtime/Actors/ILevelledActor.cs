namespace Game.Shared.Runtime.Actors
{
    /// <summary>Provides level information for an actor.</summary>
    public interface ILevelledActor
    {
        /// <summary>Gets the actor's level.</summary>
        public int GetLevel();
    }
}
