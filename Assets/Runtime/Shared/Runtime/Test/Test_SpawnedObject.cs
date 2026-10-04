using UnityEngine;
using Debug = Game.Shared.FormattedDebug;
using System.Collections;
using PurrNet;

namespace Game.Shared.Test
{
    /// <summary>
    /// NPC spawning data for a single chunk.
    /// </summary>
    public class Test_SpawnedObject : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(PingCoroutine());
        }

        private IEnumerator PingCoroutine()
        {
            while (true && NetworkManager.main.isServer)
            {
                // Bounce up and down to visually confirm that the object is active and being updated on the server.
                float bounceHeight = 0.5f;
                float bounceSpeed = 1f;
                Vector3 pos = transform.position;
                pos.y = Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
                transform.position = pos;
                yield return new WaitForSeconds(1f / 60f);
            }
        }
    }
}