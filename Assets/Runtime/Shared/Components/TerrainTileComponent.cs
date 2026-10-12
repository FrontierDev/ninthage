using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Runtime.Shared
{
#if UNITY_EDITOR
    /// <summary>
    /// Component that identifies and displays the tile coordinates of a terrain chunk.
    /// Tracks erosion state and which profiles have been applied.
    /// Only present in edit mode for debugging and organization purposes.
    /// </summary>
    [ExecuteInEditMode]
    public class TerrainTileComponent : MonoBehaviour
    {
        [SerializeField]
        [FormerlySerializedAs("tileX")]
        private int _tileX = 0;

        [SerializeField]
        [FormerlySerializedAs("tileZ")]
        private int _tileZ = 0;

        [SerializeField]
        [FormerlySerializedAs("hasBeenEroded")]
        private bool _hasBeenEroded = false;

        [SerializeField]
        [FormerlySerializedAs("lastErosionProfileUsed")]
        private string _lastErosionProfileUsed = "";

        [Header("Zone & Texturing")]
        [SerializeField]
        [FormerlySerializedAs("zoneID")]
        private int _zoneID = 0;

        [SerializeField]
        private ZoneDefinition _zoneDefinition;

        public int TileX
        {
            get => _tileX;
            set => _tileX = value;
        }

        public int TileZ
        {
            get => _tileZ;
            set => _tileZ = value;
        }

        public bool HasBeenEroded
        {
            get => _hasBeenEroded;
            set => _hasBeenEroded = value;
        }

        public string LastErosionProfileUsed
        {
            get => _lastErosionProfileUsed;
            set => _lastErosionProfileUsed = value;
        }

        public int ZoneID
        {
            get => _zoneID;
            set => _zoneID = value;
        }

        public ZoneDefinition ZoneDefinition
        {
            get => _zoneDefinition;
            set => _zoneDefinition = value;
        }

        [Header("Map Marker")]
        [SerializeField, FormerlySerializedAs("_showPoiMarker")] private bool _showPOIMarker = false;
        [SerializeField] private string _poiLabel = "";
        [SerializeField] private int _poiTextSize = 14;
        [SerializeField] private Color _poiColor = new(1f, 0.75f, 0f, 1f);

        /// <summary>Gets or sets whether the point-of-interest marker is shown.</summary>
        public bool ShowPOIMarker { get => _showPOIMarker; set => _showPOIMarker = value; }
        /// <summary>Gets or sets the point-of-interest marker label.</summary>
        public string POILabel { get => _poiLabel; set => _poiLabel = value; }
        /// <summary>Gets or sets the point-of-interest marker text size.</summary>
        public int POITextSize { get => _poiTextSize; set => _poiTextSize = value; }
        /// <summary>Gets or sets the point-of-interest marker color.</summary>
        public Color POIColor { get => _poiColor; set => _poiColor = value; }

        private void OnEnable()
        {
            // Ensure this component is hidden in play mode
            if (Application.isPlaying)
            {
                enabled = false;
            }
        }
    }
#endif
}
