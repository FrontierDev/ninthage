using Game.Shared.Utility;

namespace Game.Shared.Networking
{
    /// <summary>
    /// Replicated presentation snapshot of a server-accepted actor position.
    /// </summary>
    public struct ActorPositionSnapshot
    {
        /// <summary>Gets whether this snapshot contains an accepted position.</summary>
        public bool IsValid;

        /// <summary>Gets the authoritative simulation tick that produced the snapshot.</summary>
        public long TickID;

        /// <summary>Gets the accepted absolute double-precision world position.</summary>
        public WorldPosition Position;
    }
}
