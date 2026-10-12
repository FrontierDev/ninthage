using Game.Shared;
using Game.Shared.Runtime.Actors;

namespace Game.Client.UI
{
    /// <summary>
    /// Provides the client-side <c>UI_TargetWindow</c> implementation.
    /// </summary>
    public sealed class UI_TargetWindow : UI_Window
    {
        private static UI_TargetWindow _instance;
        /// <summary>
        /// Gets the shared client-side instance.
        /// </summary>
        public static UI_TargetWindow Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            _instance = this;

            Game.Shared.Networking.ActorEndpoint.onClientLateTargetedActorChanged += OnClientLateTargetedActorChanged;
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
