using UnityEngine;
using System.Collections.Generic;
using Debug = Game.Shared.FormattedDebug;

namespace Game.Client.Collections
{
    /// <summary>
    /// ScriptableObject that stores a collection of splash art images for the loading screen.
    /// Create an instance of this in the project and assign it to the LoadingScreenController.
    /// </summary>
    [CreateAssetMenu(fileName = "LoadingScreenCollection", menuName = "NinthAge/Collections/Loading Screen Collection", order = 1)]
    public class LoadingScreenCollection : ScriptableObject
    {
        [SerializeField] private List<Sprite> splashImages = new List<Sprite>();

        /// <summary>
        /// Get a random splash art image from the collection.
        /// </summary>
        public Sprite GetRandom()
        {
            if (splashImages == null || Count == 0)
            {
                Debug.Warning("No splash images configured.");
                return null;
            }

            return splashImages[Random.Range(0, Count)];
        }

        /// <summary>
        /// Get a splash art image by index.
        /// </summary>
        public Sprite Get(int index)
        {
            if (splashImages == null || Count == 0)
            {
                Debug.Warning("No splash images configured.");
                return null;
            }

            return splashImages[Mathf.Clamp(index, 0, Count - 1)];
        }

        /// <summary>
        /// Get the total number of splash images in the collection.
        /// </summary>
        public int Count => splashImages?.Count ?? 0;
    }
}
