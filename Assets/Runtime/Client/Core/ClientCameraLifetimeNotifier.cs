using System;
using UnityEngine;

namespace Game.Client
{
    internal sealed class ClientCameraLifetimeNotifier : MonoBehaviour
    {
        internal event Action<GameObject> onDestroyed;

        private void OnDestroy()
        {
            var callback = onDestroyed;
            onDestroyed = null;
            callback?.Invoke(gameObject);
        }
    }
}
