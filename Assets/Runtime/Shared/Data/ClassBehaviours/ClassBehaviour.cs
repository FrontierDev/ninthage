using UnityEngine;

namespace Game.Shared.Data
{
    public interface IClassBehaviour
    {
        public GameObject ResourcePrefab { get; }
        void OnActivate(PlayerActor actor);
        void OnDeactivate(PlayerActor actor);
    }

    // ClassBehaviour.cs — abstract ScriptableObject, one subclass per behaviour
    public abstract class ClassBehaviour : ScriptableObject, IClassBehaviour
    {
        public abstract GameObject ResourcePrefab { get; }
        public abstract void OnActivate(PlayerActor actor);   // subscribe to events
        public abstract void OnDeactivate(PlayerActor actor); // unsubscribe
    }
}