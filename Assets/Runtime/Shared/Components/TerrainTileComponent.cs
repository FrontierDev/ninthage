using UnityEngine;

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
        private int tileX = 0;

        [SerializeField]
        private int tileZ = 0;

        [SerializeField]
        private bool hasBeenEroded = false;

        [SerializeField]
        private string lastErosionProfileUsed = "";

        [Header("Zone & Texturing")]
        [SerializeField]
        private int zoneID = 0;

        [SerializeField]
        private ZoneDefinition _zoneDefinition;

        public int TileX
        {
            get => tileX;
            set => tileX = value;
        }

        public int TileZ
        {
            get => tileZ;
            set => tileZ = value;
        }

        public bool HasBeenEroded
        {
            get => hasBeenEroded;
            set => hasBeenEroded = value;
        }

        public string LastErosionProfileUsed
        {
            get => lastErosionProfileUsed;
            set => lastErosionProfileUsed = value;
        }

        public int ZoneID
        {
            get => zoneID;
            set => zoneID = value;
        }

        public ZoneDefinition ZoneDefinition
        {
            get => _zoneDefinition;
            set => _zoneDefinition = value;
        }

        [Header("Map Marker")]
        [SerializeField] private bool _showPoiMarker = false;
        [SerializeField] private string _poiLabel = "";
        [SerializeField] private int _poiTextSize = 14;
        [SerializeField] private Color _poiColor = new Color(1f, 0.75f, 0f, 1f);

        public bool ShowPoiMarker { get => _showPoiMarker; set => _showPoiMarker = value; }
        public string PoiLabel { get => _poiLabel; set => _poiLabel = value; }
        public int PoiTextSize { get => _poiTextSize; set => _poiTextSize = value; }
        public Color PoiColor { get => _poiColor; set => _poiColor = value; }

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
