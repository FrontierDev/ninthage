using Game.Shared.Runtime.Actors;
namespace Game.Shared.Runtime
{
    /// <summary>Defines pointer interactions supported by an actor.</summary>
    public interface IInteractable
    {
        /// <summary>Handles pointer entry from an actor.</summary>
        void OnPointerEnter(Actor interactor);
        /// <summary>Handles a pointer click from an actor.</summary>
        void OnPointerClick(Actor interactor);
    }
}
