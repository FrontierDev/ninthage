using System;
using UnityEngine;

namespace Game.Client.VFX
{
    /// <summary>Notifies the owning client manager when a spawned VFX object is destroyed.</summary>
    public sealed class ClientVFXLifetimeNotifier : MonoBehaviour
    {
        /// <summary>Occurs once when the GameObject is destroyed.</summary>
        public event Action<GameObject> onDestroyed;

        private void OnDestroy()
        {
            var callback = onDestroyed;
            onDestroyed = null;
            callback?.Invoke(gameObject);
        }
    }
}
