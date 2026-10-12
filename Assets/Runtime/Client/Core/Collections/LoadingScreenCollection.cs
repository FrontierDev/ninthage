using Debug = Game.Shared.Utility.FormattedDebug;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Client.Collections
{
    /// <summary>
    /// ScriptableObject that stores a collection of splash art images for the loading screen.
    /// Create an instance of this in the project and assign it to the LoadingScreenController.
    /// </summary>
    [CreateAssetMenu(fileName = "LoadingScreenCollection", menuName = "NinthAge/Collections/Loading Screen Collection", order = 1)]
    public class LoadingScreenCollection : ScriptableObject
    {
        [FormerlySerializedAs("splashImages")]
        [SerializeField] private List<Sprite> _splashImages = new();

        /// <summary>
        /// Get a random splash art image from the collection.
        /// </summary>
        public Sprite GetRandom()
        {
            if (_splashImages == null || Count == 0)
            {
                Debug.Warning("No splash images configured.");
                return null;
            }

            return _splashImages[Random.Range(0, Count)];
        }

        /// <summary>
        /// Get a splash art image by index.
        /// </summary>
        public Sprite Get(int index)
        {
            if (_splashImages == null || Count == 0)
            {
                Debug.Warning("No splash images configured.");
                return null;
            }

            return _splashImages[Mathf.Clamp(index, 0, Count - 1)];
        }

        /// <summary>
        /// Get the total number of splash images in the collection.
        /// </summary>
        public int Count => _splashImages?.Count ?? 0;
    }
}
