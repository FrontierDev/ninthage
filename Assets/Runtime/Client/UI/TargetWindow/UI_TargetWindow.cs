using Game.Shared;

namespace Game.Client.UI
{
    public sealed class UI_TargetWindow : UI_Window
    {
        private static UI_TargetWindow _instance;
        public static UI_TargetWindow Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;

            Game.Shared.Networking.ActorService.onClientLateTargetedActorChanged += OnClientLateTargetedActorChanged;
        }

        private void OnClientLateTargetedActorChanged(Actor oldTarget, Actor newTarget)
        {
            if (newTarget != null)
            {
                ShowImmediate();
            }
            else
            {
                HideImmediate();
            }
        }
    }
}