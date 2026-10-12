using UnityEngine;

using Game.Shared.Runtime.Actors;
namespace Game.Shared.Data.ClassBehaviours
{
    /// <summary>Defines the lifecycle contract for a class behaviour.</summary>
    public interface IClassBehaviour
    {
        /// <summary>Gets the resource prefab associated with the behaviour.</summary>
        public GameObject ResourcePrefab { get; }
        /// <summary>Activates the behaviour for an actor.</summary>
        void OnActivate(PlayerActor actor);
        /// <summary>Deactivates the behaviour for an actor.</summary>
        void OnDeactivate(PlayerActor actor);
    }

    // ClassBehaviour.cs — abstract ScriptableObject, one subclass per behaviour
    /// <summary>Base ScriptableObject for class-specific passive behaviour.</summary>
    public abstract class ClassBehaviour : ScriptableObject, IClassBehaviour
    {
        /// <summary>Gets the resource prefab associated with the behaviour.</summary>
        public abstract GameObject ResourcePrefab { get; }
        /// <summary>Activates the behaviour for an actor.</summary>
        public abstract void OnActivate(PlayerActor actor);   // subscribe to events
        /// <summary>Deactivates the behaviour for an actor.</summary>
        public abstract void OnDeactivate(PlayerActor actor); // unsubscribe
    }
}
