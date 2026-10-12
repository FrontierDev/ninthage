namespace Game.Shared
{
    /// <summary>Defines an object that can be targeted by an actor.</summary>
    public interface ITargettable
    {
        /// <summary>Handles being targeted by an actor.</summary>
        void OnTargeted(Actor targetingActor);
    }
}
