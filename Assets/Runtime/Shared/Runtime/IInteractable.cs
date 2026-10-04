namespace Game.Shared
{
    public interface IInteractable
    {
        void OnPointerEnter(Actor interactor);
        void OnPointerClick(Actor interactor);
    }
}